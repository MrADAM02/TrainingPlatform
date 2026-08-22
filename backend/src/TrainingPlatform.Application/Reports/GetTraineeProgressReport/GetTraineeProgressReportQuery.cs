using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content;
using TrainingPlatform.Application.Reports.Contracts;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Reports.GetTraineeProgressReport;

/// <summary>Per-trainee drill-down for a single course (REQ-REP-01): document completion,
/// required-quiz pass count, and certificate status — the same authorization boundary as
/// <see cref="Enrollments.GetCourseEnrollments.GetCourseEnrollmentsQuery"/>.</summary>
public sealed record GetTraineeProgressReportQuery(Guid CourseId) : IQuery<IReadOnlyList<TraineeProgressReportItem>>;

public sealed class GetTraineeProgressReportQueryHandler(
    IApplicationDbContext dbContext,
    IIdentityService identityService,
    IUserContext currentUser) : IQueryHandler<GetTraineeProgressReportQuery, IReadOnlyList<TraineeProgressReportItem>>
{
    public async Task<Result<IReadOnlyList<TraineeProgressReportItem>>> Handle(
        GetTraineeProgressReportQuery query, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == query.CourseId, cancellationToken);
        if (course is null)
        {
            return Result.Failure<IReadOnlyList<TraineeProgressReportItem>>(ContentErrors.CourseNotFound(query.CourseId));
        }

        if (!CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure<IReadOnlyList<TraineeProgressReportItem>>(ContentErrors.NotCourseOwner);
        }

        var enrollments = await dbContext.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == query.CourseId)
            .OrderByDescending(e => e.EnrolledAtUtc)
            .ToListAsync(cancellationToken);

        var usersResult = await identityService.GetUsersByIdsAsync(
            enrollments.Select(e => e.UserId).ToList(), cancellationToken);
        if (usersResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<TraineeProgressReportItem>>(usersResult.Error);
        }

        var usersById = usersResult.Value.ToDictionary(u => u.Id);

        var moduleIds = await dbContext.Modules.AsNoTracking()
            .Where(m => m.CourseId == query.CourseId)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var totalDocuments = await dbContext.Documents.AsNoTracking()
            .CountAsync(d => moduleIds.Contains(d.ModuleId), cancellationToken);

        var enrollmentIds = enrollments.Select(e => e.Id).ToList();
        var progressCountsByEnrollment = await dbContext.Progresses.AsNoTracking()
            .Where(p => enrollmentIds.Contains(p.EnrollmentId))
            .GroupBy(p => p.EnrollmentId)
            .Select(g => new { EnrollmentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var requiredQuizIds = await dbContext.Quizzes.AsNoTracking()
            .Where(q => moduleIds.Contains(q.ModuleId) && q.IsRequiredForCompletion)
            .Select(q => q.Id)
            .ToListAsync(cancellationToken);
        var requiredQuizzesTotal = requiredQuizIds.Count;

        var userIds = enrollments.Select(e => e.UserId).Distinct().ToList();

        var passedRequiredQuizzesByUser = requiredQuizzesTotal == 0
            ? new Dictionary<Guid, int>()
            : await dbContext.QuizAttempts.AsNoTracking()
                .Where(a => requiredQuizIds.Contains(a.QuizId) && userIds.Contains(a.UserId) && a.Passed)
                .Select(a => new { a.UserId, a.QuizId })
                .Distinct()
                .GroupBy(a => a.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.UserId, g => g.Count, cancellationToken);

        var certifiedUserIds = (await dbContext.Certificates.AsNoTracking()
                .Where(c => c.CourseId == query.CourseId && userIds.Contains(c.UserId))
                .Select(c => c.UserId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        IReadOnlyList<TraineeProgressReportItem> items = enrollments
            .Where(e => usersById.ContainsKey(e.UserId))
            .Select(e =>
            {
                var completedDocuments = progressCountsByEnrollment.FirstOrDefault(p => p.EnrollmentId == e.Id)?.Count ?? 0;
                var requiredQuizzesPassed = passedRequiredQuizzesByUser.GetValueOrDefault(e.UserId);

                return new TraineeProgressReportItem(
                    e.UserId,
                    usersById[e.UserId].Email,
                    usersById[e.UserId].FullName,
                    e.Status,
                    e.EnrolledAtUtc,
                    completedDocuments,
                    totalDocuments,
                    requiredQuizzesPassed,
                    requiredQuizzesTotal,
                    certifiedUserIds.Contains(e.UserId));
            })
            .ToList();

        return Result.Success(items);
    }
}
