using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Reports.Contracts;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Enrollments;

namespace TrainingPlatform.Application.Reports.GetOrgSummaryReport;

/// <summary>Administrator-only, org-wide reporting counts (REQ-REP-01). "Active trainees" is
/// derived from real engagement signals already in the schema (document progress + quiz
/// attempts) rather than login activity, since that's the more meaningful signal for a training
/// platform and avoids depending on the Activity Log abstraction from this read-only query.</summary>
public sealed record GetOrgSummaryReportQuery : IQuery<OrgSummaryReport>;

public sealed class GetOrgSummaryReportQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetOrgSummaryReportQuery, OrgSummaryReport>
{
    private static readonly TimeSpan ActiveWindow = TimeSpan.FromDays(30);

    public async Task<Result<OrgSummaryReport>> Handle(GetOrgSummaryReportQuery query, CancellationToken cancellationToken)
    {
        var totalCourses = await dbContext.Courses.CountAsync(cancellationToken);
        var publishedCourses = await dbContext.Courses.CountAsync(c => c.IsPublished, cancellationToken);
        var totalEnrollments = await dbContext.Enrollments.CountAsync(cancellationToken);
        var completedEnrollments = await dbContext.Enrollments
            .CountAsync(e => e.Status == EnrollmentStatus.Completed, cancellationToken);
        var totalCertificates = await dbContext.Certificates.CountAsync(cancellationToken);

        var activeCutoff = DateTime.UtcNow - ActiveWindow;

        var activeFromProgress = await dbContext.Progresses.AsNoTracking()
            .Where(p => p.CompletedAtUtc >= activeCutoff)
            .Join(dbContext.Enrollments.AsNoTracking(), p => p.EnrollmentId, e => e.Id, (p, e) => e.UserId)
            .ToListAsync(cancellationToken);

        var activeFromQuizzes = await dbContext.QuizAttempts.AsNoTracking()
            .Where(a => a.AttemptedAtUtc >= activeCutoff)
            .Select(a => a.UserId)
            .ToListAsync(cancellationToken);

        var activeTrainees = activeFromProgress.Concat(activeFromQuizzes).Distinct().Count();

        return new OrgSummaryReport(
            totalCourses, publishedCourses, totalEnrollments, completedEnrollments, totalCertificates, activeTrainees);
    }
}
