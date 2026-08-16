namespace TrainingPlatform.Domain.Users;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Trainer = "Trainer";
    public const string Trainee = "Trainee";

    public static readonly string[] All = [Administrator, Trainer, Trainee];
}
