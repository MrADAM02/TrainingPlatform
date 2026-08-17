using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Content;

public static class ContentErrors
{
    public static Error CourseNotFound(Guid courseId) => Error.NotFound(
        "Content.CourseNotFound", $"Course '{courseId}' was not found.");

    public static Error ModuleNotFound(Guid moduleId) => Error.NotFound(
        "Content.ModuleNotFound", $"Module '{moduleId}' was not found.");

    public static Error DocumentNotFound(Guid documentId) => Error.NotFound(
        "Content.DocumentNotFound", $"Document '{documentId}' was not found.");

    public static readonly Error NotCourseOwner = Error.Forbidden(
        "Content.NotCourseOwner", "You can only manage courses you own.");

    public static readonly Error CourseNotAccessible = Error.Forbidden(
        "Content.CourseNotAccessible", "This course is not published.");
}
