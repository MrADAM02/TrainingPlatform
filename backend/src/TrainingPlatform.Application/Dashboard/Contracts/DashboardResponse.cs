using TrainingPlatform.Domain.Enrollments;

namespace TrainingPlatform.Application.Dashboard.Contracts;

public sealed record DashboardCourseItem(
    Guid CourseId,
    string CourseTitle,
    EnrollmentStatus Status,
    int TotalDocuments,
    int CompletedDocuments);

public sealed record RecentDocumentItem(
    Guid DocumentId,
    string DocumentTitle,
    Guid CourseId,
    string CourseTitle,
    DateTime UploadedAtUtc);

public sealed record DashboardResponse(
    IReadOnlyList<DashboardCourseItem> Courses,
    IReadOnlyList<RecentDocumentItem> RecentlyAdded);
