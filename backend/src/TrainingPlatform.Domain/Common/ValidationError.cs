namespace TrainingPlatform.Domain.Common;

public sealed record ValidationError(Error[] Errors)
    : Error("Validation.General", "One or more validation errors occurred.", ErrorType.Validation);
