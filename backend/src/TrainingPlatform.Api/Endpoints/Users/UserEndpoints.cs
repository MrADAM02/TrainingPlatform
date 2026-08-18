using System.Text;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Users.BulkImportUsers;
using TrainingPlatform.Application.Users.CreateUser;
using TrainingPlatform.Application.Users.DeleteUser;
using TrainingPlatform.Application.Users.GetUserById;
using TrainingPlatform.Application.Users.GetUsers;
using TrainingPlatform.Application.Users.ResetUserPassword;
using TrainingPlatform.Application.Users.SearchTrainees;
using TrainingPlatform.Application.Users.SetUserActiveStatus;
using TrainingPlatform.Application.Users.UpdateUser;

namespace TrainingPlatform.Api.Endpoints.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users").WithTags("Users").RequireAuthorization("RequireAdministrator");

        group.MapGet("/", async (ISender sender, CancellationToken ct, int page = 1, int pageSize = 20) =>
        {
            var query = new GetUsersQuery(page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);
            var result = await sender.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUserByIdQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        group.MapPost("/", async (CreateUserCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/users/{result.Value}", new { id = result.Value })
                : CustomResults.Problem(result);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateUserRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateUserCommand(id, request.FullName), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetUserActiveStatusCommand(id, true), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetUserActiveStatusCommand(id, false), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        group.MapPost("/{id:guid}/reset-password", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ResetUserPasswordCommand(id), ct);
            return result.IsSuccess
                ? Results.Ok(new { temporaryPassword = result.Value })
                : CustomResults.Problem(result);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteUserCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        group.MapPost("/bulk-import", async (IFormFile file, ISender sender, CancellationToken ct) =>
        {
            IReadOnlyList<BulkImportUserRow> rows;
            try
            {
                await using var stream = file.OpenReadStream();
                rows = await ParseCsvAsync(stream, ct);
            }
            catch (InvalidDataException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            var result = await sender.Send(new BulkImportUsersCommand(rows), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        }).DisableAntiforgery();

        // Deliberately outside the Administrator-only group above: Trainers need this to find
        // trainees to enroll in their own courses, but must not see the full user directory.
        app.MapGet("/api/v1/users/trainees", async (ISender sender, CancellationToken ct, string? keyword = null, int limit = 20) =>
        {
            var result = await sender.Send(new SearchTraineesQuery(keyword, limit), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        })
        .WithTags("Users")
        .RequireAuthorization("RequireTrainerOrAdministrator");

        return app;
    }

    private static async Task<IReadOnlyList<BulkImportUserRow>> ParseCsvAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var headerLine = await reader.ReadLineAsync(ct);
        if (headerLine is null)
        {
            throw new InvalidDataException("The CSV file is empty.");
        }

        var headers = SplitCsvLine(headerLine).Select(h => h.Trim().ToLowerInvariant()).ToList();
        var emailIndex = headers.IndexOf("email");
        var fullNameIndex = headers.IndexOf("fullname");
        var roleIndex = headers.IndexOf("role");

        if (emailIndex < 0 || fullNameIndex < 0 || roleIndex < 0)
        {
            throw new InvalidDataException("The CSV header must contain 'email', 'fullName', and 'role' columns.");
        }

        var rows = new List<BulkImportUserRow>();
        var rowNumber = 1; // header is row 1, so the first data row is row 2

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitCsvLine(line);
            var email = fields.ElementAtOrDefault(emailIndex)?.Trim() ?? string.Empty;
            var fullName = fields.ElementAtOrDefault(fullNameIndex)?.Trim() ?? string.Empty;
            var role = fields.ElementAtOrDefault(roleIndex)?.Trim() ?? string.Empty;

            rows.Add(new BulkImportUserRow(rowNumber, email, fullName, role));
        }

        return rows;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}

public sealed record UpdateUserRequest(string FullName);
