namespace TrainingPlatform.Application.Reports.Contracts;

public sealed record CourseCompletionReportItem(
    Guid CourseId,
    string CourseTitle,
    bool IsPublished,
    int EnrolledCount,
    int CompletedCount,
    double CompletionPercent,
    double? AvgCompletionDays);
