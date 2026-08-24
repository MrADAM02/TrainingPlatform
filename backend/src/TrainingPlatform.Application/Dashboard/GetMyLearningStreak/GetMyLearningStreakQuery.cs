using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Dashboard.Contracts;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Dashboard.GetMyLearningStreak;

/// <summary>REQ-DASH (2026-08-24 redesign): a "learning streak" widget derived purely from
/// activity that already exists (<see cref="Enrollments.Progress.CompletedAtUtc"/>, <see
/// cref="Quizzes.QuizAttempt.AttemptedAtUtc"/>) — no new tracking table, no fabricated
/// watch-time.</summary>
public sealed record GetMyLearningStreakQuery : IQuery<LearningStreakSummary>;

public sealed class GetMyLearningStreakQueryHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : IQueryHandler<GetMyLearningStreakQuery, LearningStreakSummary>
{
    public async Task<Result<LearningStreakSummary>> Handle(
        GetMyLearningStreakQuery query, CancellationToken cancellationToken)
    {
        var enrollmentIds = await dbContext.Enrollments.AsNoTracking()
            .Where(e => e.UserId == currentUser.UserId)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var progressDates = await dbContext.Progresses.AsNoTracking()
            .Where(p => enrollmentIds.Contains(p.EnrollmentId))
            .Select(p => p.CompletedAtUtc)
            .ToListAsync(cancellationToken);

        var quizDates = await dbContext.QuizAttempts.AsNoTracking()
            .Where(a => a.UserId == currentUser.UserId)
            .Select(a => a.AttemptedAtUtc)
            .ToListAsync(cancellationToken);

        var activeDates = progressDates.Concat(quizDates).Select(d => d.Date).ToHashSet();
        var currentStreakDays = ComputeStreakDays(activeDates);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var documentIdsCompletedThisMonth = await dbContext.Progresses.AsNoTracking()
            .Where(p => enrollmentIds.Contains(p.EnrollmentId) && p.CompletedAtUtc >= monthStart)
            .Select(p => p.DocumentId)
            .ToListAsync(cancellationToken);

        var minutesThisMonth = await dbContext.Documents.AsNoTracking()
            .Where(d => documentIdsCompletedThisMonth.Contains(d.Id) && d.DurationMinutes != null)
            .SumAsync(d => d.DurationMinutes!.Value, cancellationToken);

        var quizzesPassedThisMonth = await dbContext.QuizAttempts.AsNoTracking()
            .Where(a => a.UserId == currentUser.UserId && a.Passed && a.AttemptedAtUtc >= monthStart)
            .Select(a => a.QuizId)
            .Distinct()
            .CountAsync(cancellationToken);

        var itemsCompletedThisMonth = documentIdsCompletedThisMonth.Count + quizzesPassedThisMonth;

        return new LearningStreakSummary(currentStreakDays, minutesThisMonth, itemsCompletedThisMonth);
    }

    private static int ComputeStreakDays(HashSet<DateTime> activeDates)
    {
        if (activeDates.Count == 0)
        {
            return 0;
        }

        var today = DateTime.UtcNow.Date;
        var cursor = activeDates.Contains(today) ? today : today.AddDays(-1);
        if (!activeDates.Contains(cursor))
        {
            return 0;
        }

        var streak = 0;
        while (activeDates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }
}
