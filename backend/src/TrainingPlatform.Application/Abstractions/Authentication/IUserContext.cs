namespace TrainingPlatform.Application.Abstractions.Authentication;

public interface IUserContext
{
    Guid UserId { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Roles { get; }
}
