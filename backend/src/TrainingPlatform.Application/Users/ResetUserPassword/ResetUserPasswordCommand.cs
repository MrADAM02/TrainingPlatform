using FluentValidation;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Users.ResetUserPassword;

/// <summary>Admin-forced password reset (REQ-AUTH-05).</summary>
public sealed record ResetUserPasswordCommand(Guid UserId) : ICommand<string>;

public sealed class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
    }
}

public sealed class ResetUserPasswordCommandHandler(
    IIdentityService identityService,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<ResetUserPasswordCommand, string>
{
    public async Task<Result<string>> Handle(ResetUserPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await identityService.AdminResetPasswordAsync(command.UserId, cancellationToken);

        if (result.IsSuccess)
        {
            await activityLog.LogAsync(
                currentUser.UserId, ActivityActions.UserPasswordReset, "User", command.UserId.ToString(), cancellationToken);
        }

        return result;
    }
}
