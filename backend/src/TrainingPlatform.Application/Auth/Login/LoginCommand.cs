using FluentValidation;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthTokensResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
        RuleFor(c => c.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(IIdentityService identityService)
    : ICommandHandler<LoginCommand, AuthTokensResponse>
{
    public Task<Result<AuthTokensResponse>> Handle(LoginCommand command, CancellationToken cancellationToken) =>
        identityService.LoginAsync(command.Email, command.Password, cancellationToken);
}
