using FluentValidation;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Users;

namespace TrainingPlatform.Application.Users.BulkImportUsers;

public sealed record BulkImportUserRow(int RowNumber, string Email, string FullName, string Role);

public sealed record BulkImportUserSuccess(string Email, string FullName, string Role, string TemporaryPassword);

public sealed record BulkImportUserFailure(int RowNumber, string Email, string Reason);

public sealed record BulkImportUsersResult(
    IReadOnlyList<BulkImportUserSuccess> Created,
    IReadOnlyList<BulkImportUserFailure> Failed);

/// <summary>
/// REQ-ADM-02: bulk-provision accounts from a CSV roster instead of one-by-one creation.
/// Rows are processed independently — one invalid or duplicate row does not abort the batch —
/// since a ~500-row roster realistically always has a handful of bad rows and the admin needs
/// the rest to still go through. Each created account gets a generated temporary password
/// (mirrors <see cref="Authentication.IIdentityService.AdminResetPasswordAsync"/> — there is no
/// email delivery yet, so passwords are returned to the caller to relay out of band).
/// </summary>
public sealed record BulkImportUsersCommand(IReadOnlyList<BulkImportUserRow> Rows) : ICommand<BulkImportUsersResult>;

public sealed class BulkImportUsersCommandValidator : AbstractValidator<BulkImportUsersCommand>
{
    public BulkImportUsersCommandValidator()
    {
        RuleFor(c => c.Rows).NotEmpty().WithMessage("The CSV file contains no data rows.");
        RuleFor(c => c.Rows.Count).LessThanOrEqualTo(2000)
            .WithMessage("A single import is limited to 2000 rows.");
    }
}

public sealed class BulkImportUsersCommandHandler(
    IIdentityService identityService,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<BulkImportUsersCommand, BulkImportUsersResult>
{
    public async Task<Result<BulkImportUsersResult>> Handle(
        BulkImportUsersCommand command, CancellationToken cancellationToken)
    {
        var created = new List<BulkImportUserSuccess>();
        var failed = new List<BulkImportUserFailure>();

        foreach (var row in command.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.Email) || string.IsNullOrWhiteSpace(row.FullName))
            {
                failed.Add(new BulkImportUserFailure(row.RowNumber, row.Email, "Email and full name are required."));
                continue;
            }

            if (!Roles.All.Contains(row.Role))
            {
                failed.Add(new BulkImportUserFailure(
                    row.RowNumber, row.Email, $"Role must be one of: {string.Join(", ", Roles.All)}."));
                continue;
            }

            var result = await identityService.CreateUserWithTemporaryPasswordAsync(
                row.Email, row.FullName, row.Role, cancellationToken);

            if (result.IsFailure)
            {
                failed.Add(new BulkImportUserFailure(row.RowNumber, row.Email, result.Error.Description));
                continue;
            }

            created.Add(new BulkImportUserSuccess(row.Email, row.FullName, row.Role, result.Value.TemporaryPassword));

            await activityLog.LogAsync(
                currentUser.UserId, ActivityActions.UserCreated, "User", result.Value.UserId.ToString(), cancellationToken);
        }

        return new BulkImportUsersResult(created, failed);
    }
}
