using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content.Contracts;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Users;

namespace TrainingPlatform.Application.Content.Courses.GetCourses;

/// <summary><paramref name="ManagedOnly"/> distinguishes the management view (Trainer's "My
/// Courses", owned courses only) from the open-catalog browse view (owned/managed + every
/// published course) — without it, a Trainer's management list would include other trainers'
/// published courses with Edit/Delete actions that shouldn't apply to them.</summary>
public sealed record GetCoursesQuery(int Page = 1, int PageSize = 20, bool ManagedOnly = false)
    : IQuery<PaginatedList<CourseSummary>>;

public sealed class GetCoursesQueryHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : IQueryHandler<GetCoursesQuery, PaginatedList<CourseSummary>>
{
    public async Task<Result<PaginatedList<CourseSummary>>> Handle(GetCoursesQuery query, CancellationToken cancellationToken)
    {
        var courses = dbContext.Courses.AsNoTracking().AsQueryable();
        var isAdmin = currentUser.Roles.Contains(Roles.Administrator);

        if (!isAdmin)
        {
            courses = query.ManagedOnly
                ? courses.Where(c => c.TrainerId == currentUser.UserId)
                : courses.Where(c => c.TrainerId == currentUser.UserId || c.IsPublished);
        }

        courses = courses.OrderByDescending(c => c.CreatedAtUtc);

        var totalCount = await courses.CountAsync(cancellationToken);

        var pagedCourses = await courses
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var courseIds = pagedCourses.Select(c => c.Id).ToList();
        var enrolledCourseIds = await dbContext.Enrollments
            .Where(e => e.UserId == currentUser.UserId && courseIds.Contains(e.CourseId))
            .Select(e => e.CourseId)
            .ToListAsync(cancellationToken);

        var items = pagedCourses
            .Select(c => new CourseSummary(
                c.Id, c.Title, c.Description, c.TrainerId, c.IsPublished, c.CreatedAtUtc, enrolledCourseIds.Contains(c.Id)))
            .ToList();

        return new PaginatedList<CourseSummary>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }
}
