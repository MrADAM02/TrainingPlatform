using FluentValidation;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Users.UpdateUser;

public sealed record UpdateUserCommand(Guid UserId, string FullName) : ICommand;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateUserCommandHandler(
    IIdentityService identityService,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await identityService.UpdateUserAsync(command.UserId, command.FullName, cancellationToken);

        if (result.IsSuccess)
        {
            await activityLog.LogAsync(
                currentUser.UserId, ActivityActions.UserUpdated, "User", command.UserId.ToString(), cancellationToken);
        }

        return result;
    }
}
