using FluentValidation;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Users;

namespace TrainingPlatform.Application.Users.SetUserActiveStatus;

public sealed record SetUserActiveStatusCommand(Guid UserId, bool IsActive) : ICommand;

public sealed class SetUserActiveStatusCommandValidator : AbstractValidator<SetUserActiveStatusCommand>
{
    public SetUserActiveStatusCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
    }
}

public sealed class SetUserActiveStatusCommandHandler(
    IIdentityService identityService,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<SetUserActiveStatusCommand>
{
    public async Task<Result> Handle(SetUserActiveStatusCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == currentUser.UserId)
        {
            return Result.Failure(UserErrors.CannotActOnSelf);
        }

        var result = await identityService.SetUserActiveStatusAsync(command.UserId, command.IsActive, cancellationToken);

        if (result.IsSuccess)
        {
            var action = command.IsActive ? ActivityActions.UserActivated : ActivityActions.UserDeactivated;
            await activityLog.LogAsync(currentUser.UserId, action, "User", command.UserId.ToString(), cancellationToken);
        }

        return result;
    }
}
