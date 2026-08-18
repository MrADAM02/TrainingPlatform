using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Dashboard.Contracts;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Dashboard.GetMyDashboard;

/// <summary>REQ-DASH-01/02/03: the caller's enrolled courses with completion progress, plus
/// materials added to those courses in the last two weeks.</summary>
public sealed record GetMyDashboardQuery : IQuery<DashboardResponse>;

public sealed class GetMyDashboardQueryHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : IQueryHandler<GetMyDashboardQuery, DashboardResponse>
{
    private static readonly TimeSpan RecentWindow = TimeSpan.FromDays(14);
    private const int RecentLimit = 10;

    public async Task<Result<DashboardResponse>> Handle(GetMyDashboardQuery query, CancellationToken cancellationToken)
    {
        var enrollments = await dbContext.Enrollments.AsNoTracking()
            .Where(e => e.UserId == currentUser.UserId)
            .ToListAsync(cancellationToken);

        var courseIds = enrollments.Select(e => e.CourseId).ToList();

        var courses = await dbContext.Courses.AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var moduleIdsByCourse = await dbContext.Modules.AsNoTracking()
            .Where(m => courseIds.Contains(m.CourseId))
            .Select(m => new { m.Id, m.CourseId })
            .ToListAsync(cancellationToken);

        var allModuleIds = moduleIdsByCourse.Select(m => m.Id).ToList();

        var documentCountsByModule = await dbContext.Documents.AsNoTracking()
            .Where(d => allModuleIds.Contains(d.ModuleId))
            .GroupBy(d => d.ModuleId)
            .Select(g => new { ModuleId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var enrollmentIds = enrollments.Select(e => e.Id).ToList();

        var progressCountsByEnrollment = await dbContext.Progresses.AsNoTracking()
            .Where(p => enrollmentIds.Contains(p.EnrollmentId))
            .GroupBy(p => p.EnrollmentId)
            .Select(g => new { EnrollmentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var courseItems = enrollments
            .Where(e => courses.ContainsKey(e.CourseId))
            .Select(e =>
            {
                var moduleIds = moduleIdsByCourse.Where(m => m.CourseId == e.CourseId).Select(m => m.Id).ToList();
                var totalDocuments = documentCountsByModule.Where(d => moduleIds.Contains(d.ModuleId)).Sum(d => d.Count);
                var completedDocuments = progressCountsByEnrollment.FirstOrDefault(p => p.EnrollmentId == e.Id)?.Count ?? 0;

                return new DashboardCourseItem(e.CourseId, courses[e.CourseId].Title, e.Status, totalDocuments, completedDocuments);
            })
            .ToList();

        var recentCutoff = DateTime.UtcNow - RecentWindow;
        var recentDocuments = await dbContext.Documents.AsNoTracking()
            .Where(d => allModuleIds.Contains(d.ModuleId) && d.UploadedAtUtc >= recentCutoff)
            .OrderByDescending(d => d.UploadedAtUtc)
            .Take(RecentLimit)
            .ToListAsync(cancellationToken);

        var recentItems = recentDocuments
            .Select(d =>
            {
                var courseId = moduleIdsByCourse.First(m => m.Id == d.ModuleId).CourseId;
                return new RecentDocumentItem(d.Id, d.Title, courseId, courses[courseId].Title, d.UploadedAtUtc);
            })
            .ToList();

        return new DashboardResponse(courseItems, recentItems);
    }
}
