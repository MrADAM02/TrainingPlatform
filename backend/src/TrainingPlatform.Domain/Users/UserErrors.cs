using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Users;

public static class UserErrors
{
    public static readonly Error EmailAlreadyInUse = Error.Conflict(
        "Users.EmailAlreadyInUse",
        "An account with this email already exists.");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Users.InvalidCredentials",
        "The email or password is incorrect.");

    public static readonly Error AccountDeactivated = Error.Forbidden(
        "Users.AccountDeactivated",
        "This account has been deactivated. Contact an administrator.");

    public static readonly Error InvalidOrExpiredRefreshToken = Error.Unauthorized(
        "Users.InvalidOrExpiredRefreshToken",
        "The refresh token is invalid or has expired. Please log in again.");

    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"User '{userId}' was not found.");

    public static readonly Error CannotActOnSelf = Error.Validation(
        "Users.CannotActOnSelf",
        "You cannot perform this action on your own account.");
}
