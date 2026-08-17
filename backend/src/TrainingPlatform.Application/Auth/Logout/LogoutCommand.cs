using FluentValidation;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Auth.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty();
    }
}

public sealed class LogoutCommandHandler(IIdentityService identityService)
    : ICommandHandler<LogoutCommand>
{
    public Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken) =>
        identityService.LogoutAsync(command.RefreshToken, cancellationToken);
}
