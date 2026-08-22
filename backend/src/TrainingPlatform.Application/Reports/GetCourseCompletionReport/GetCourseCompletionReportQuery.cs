using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Reports.Contracts;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Enrollments;
using TrainingPlatform.Domain.Users;

namespace TrainingPlatform.Application.Reports.GetCourseCompletionReport;

/// <summary>Per-course completion stats (REQ-REP-01). Admin sees every course; a Trainer sees
/// only their own — same scoping rule as <c>GetCoursesQuery(ManagedOnly: true)</c>, but always
/// on here since a reporting view has no "browse" mode.</summary>
public sealed record GetCourseCompletionReportQuery : IQuery<IReadOnlyList<CourseCompletionReportItem>>;

public sealed class GetCourseCompletionReportQueryHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : IQueryHandler<GetCourseCompletionReportQuery, IReadOnlyList<CourseCompletionReportItem>>
{
    public async Task<Result<IReadOnlyList<CourseCompletionReportItem>>> Handle(
        GetCourseCompletionReportQuery query, CancellationToken cancellationToken)
    {
        var coursesQuery = dbContext.Courses.AsNoTracking().AsQueryable();
        if (!currentUser.Roles.Contains(Roles.Administrator))
        {
            coursesQuery = coursesQuery.Where(c => c.TrainerId == currentUser.UserId);
        }

        var courses = await coursesQuery.OrderBy(c => c.Title).ToListAsync(cancellationToken);
        var courseIds = courses.Select(c => c.Id).ToList();

        var enrollments = await dbContext.Enrollments.AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId))
            .ToListAsync(cancellationToken);

        var enrollmentIds = enrollments.Select(e => e.Id).ToList();
        var lastProgressByEnrollment = await dbContext.Progresses.AsNoTracking()
            .Where(p => enrollmentIds.Contains(p.EnrollmentId))
            .GroupBy(p => p.EnrollmentId)
            .Select(g => new { EnrollmentId = g.Key, LastCompletedAtUtc = g.Max(p => p.CompletedAtUtc) })
            .ToListAsync(cancellationToken);

        var items = courses
            .Select(c =>
            {
                var courseEnrollments = enrollments.Where(e => e.CourseId == c.Id).ToList();
                var enrolledCount = courseEnrollments.Count;
                var completedEnrollments = courseEnrollments.Where(e => e.Status == EnrollmentStatus.Completed).ToList();
                var completedCount = completedEnrollments.Count;
                var completionPercent = enrolledCount == 0 ? 0 : Math.Round(100.0 * completedCount / enrolledCount, 1);

                var completionDays = new List<double>();
                foreach (var enrollment in completedEnrollments)
                {
                    var lastProgress = lastProgressByEnrollment.FirstOrDefault(p => p.EnrollmentId == enrollment.Id);
                    if (lastProgress is not null)
                    {
                        completionDays.Add((lastProgress.LastCompletedAtUtc - enrollment.EnrolledAtUtc).TotalDays);
                    }
                }

                var avgCompletionDays = completionDays.Count > 0 ? Math.Round(completionDays.Average(), 1) : (double?)null;

                return new CourseCompletionReportItem(
                    c.Id, c.Title, c.IsPublished, enrolledCount, completedCount, completionPercent, avgCompletionDays);
            })
            .ToList();

        return Result.Success<IReadOnlyList<CourseCompletionReportItem>>(items);
    }
}
