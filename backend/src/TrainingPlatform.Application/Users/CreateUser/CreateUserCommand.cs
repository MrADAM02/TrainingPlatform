using FluentValidation;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Users;

namespace TrainingPlatform.Application.Users.CreateUser;

/// <summary>
/// Provisions a new user account. Must only be reachable behind an Administrator-only
/// authorization policy at the endpoint (REQ-ADM-01) — this platform has no self-registration.
/// </summary>
public sealed record CreateUserCommand(
    string Email,
    string FullName,
    string Password,
    string Role) : ICommand<Guid>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8);
        RuleFor(c => c.Role).NotEmpty().Must(role => Roles.All.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}.");
    }
}

public sealed class CreateUserCommandHandler(
    IIdentityService identityService,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await identityService.CreateUserAsync(
            command.Email, command.FullName, command.Password, command.Role, cancellationToken);

        if (result.IsSuccess)
        {
            await activityLog.LogAsync(
                currentUser.UserId, ActivityActions.UserCreated, "User", result.Value.ToString(), cancellationToken);
        }

        return result;
    }
}
