using FluentValidation;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Auth.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthTokensResponse>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler(IIdentityService identityService)
    : ICommandHandler<RefreshTokenCommand, AuthTokensResponse>
{
    public Task<Result<AuthTokensResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken) =>
        identityService.RefreshTokenAsync(command.RefreshToken, cancellationToken);
}
