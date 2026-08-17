using System.Text;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Http;
using TrainingPlatform.Application.Abstractions.Storage;
using TrainingPlatform.Domain.Users;
using TrainingPlatform.Infrastructure.Activity;
using TrainingPlatform.Infrastructure.Authentication;
using TrainingPlatform.Infrastructure.Database;
using TrainingPlatform.Infrastructure.Http;
using TrainingPlatform.Infrastructure.Identity;
using TrainingPlatform.Infrastructure.Storage;

namespace TrainingPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddIdentityAndAuth(services, configuration);
        AddStorage(services, configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserContext, CurrentUserContext>();
        services.AddScoped<IClientContext, ClientContext>();
        services.AddScoped<IActivityLogService, ActivityLogService>();

        return services;
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
    }

    private static void AddIdentityAndAuth(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Without this, JwtSecurityTokenHandler silently remaps short inbound claim
                // names ("sub", "role", "email") to long legacy XML/SOAP claim URIs before
                // RoleClaimType/NameClaimType below ever see them, breaking role checks against
                // the "role" claim actually issued by TokenService.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = "role",
                    NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdministrator", policy => policy.RequireRole(Roles.Administrator))
            .AddPolicy("RequireTrainerOrAdministrator", policy => policy.RequireRole(Roles.Trainer, Roles.Administrator));
    }

    private static void AddStorage(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        var storageOptions = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
            ?? throw new InvalidOperationException("Storage configuration section is missing.");

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = storageOptions.ServiceUrl,
                ForcePathStyle = storageOptions.ForcePathStyle,
                AuthenticationRegion = storageOptions.Region,
                // AmazonS3Config otherwise always signs URLs as https:// regardless of the
                // ServiceURL scheme, which breaks against a plain-HTTP local MinIO.
                UseHttp = new Uri(storageOptions.ServiceUrl).Scheme == Uri.UriSchemeHttp,
            };

            return new AmazonS3Client(storageOptions.AccessKey, storageOptions.SecretKey, config);
        });

        services.AddScoped<IFileStorageService, S3FileStorageService>();
    }
}
