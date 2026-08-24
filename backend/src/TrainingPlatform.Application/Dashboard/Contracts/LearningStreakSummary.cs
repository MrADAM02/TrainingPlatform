namespace TrainingPlatform.Application.Dashboard.Contracts;

/// <summary><paramref name="MinutesThisMonth"/> is a lower bound, not exact watch-time: it only
/// sums <c>Document.DurationMinutes</c> for videos actually consumed this month, and that field
/// is trainer-entered and optional — a video without a set duration simply doesn't contribute.
/// Deliberately not fabricated from file size or any other proxy.</summary>
public sealed record LearningStreakSummary(
    int CurrentStreakDays,
    int MinutesThisMonth,
    int ItemsCompletedThisMonth);
