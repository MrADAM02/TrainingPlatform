using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using TrainingPlatform.Api.Endpoints.Activity;
using TrainingPlatform.Api.Endpoints.Auth;
using TrainingPlatform.Api.Endpoints.Certificates;
using TrainingPlatform.Api.Endpoints.Courses;
using TrainingPlatform.Api.Endpoints.Dashboard;
using TrainingPlatform.Api.Endpoints.Documents;
using TrainingPlatform.Api.Endpoints.Enrollments;
using TrainingPlatform.Api.Endpoints.Modules;
using TrainingPlatform.Api.Endpoints.Quizzes;
using TrainingPlatform.Api.Endpoints.Reports;
using TrainingPlatform.Api.Endpoints.Search;
using TrainingPlatform.Api.Endpoints.Users;
using TrainingPlatform.Application;
using TrainingPlatform.Infrastructure;
using TrainingPlatform.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("auth-strict", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("token-refresh", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Training Platform API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer")] = [],
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapActivityEndpoints();
app.MapCourseEndpoints();
app.MapModuleEndpoints();
app.MapDocumentEndpoints();
app.MapEnrollmentEndpoints();
app.MapDashboardEndpoints();
app.MapSearchEndpoints();
app.MapCertificateEndpoints();
app.MapQuizEndpoints();
app.MapReportEndpoints();

app.Run();
