using FluentValidation;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Users;

namespace TrainingPlatform.Application.Users.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId) : ICommand;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
    }
}

public sealed class DeleteUserCommandHandler(
    IIdentityService identityService,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<DeleteUserCommand>
{
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == currentUser.UserId)
        {
            return Result.Failure(UserErrors.CannotActOnSelf);
        }

        var result = await identityService.DeleteUserAsync(command.UserId, cancellationToken);

        if (result.IsSuccess)
        {
            await activityLog.LogAsync(
                currentUser.UserId, ActivityActions.UserDeleted, "User", command.UserId.ToString(), cancellationToken);
        }

        return result;
    }
}
