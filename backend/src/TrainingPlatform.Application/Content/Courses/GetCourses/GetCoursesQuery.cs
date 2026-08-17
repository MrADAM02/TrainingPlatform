using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content.Contracts;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Users;

namespace TrainingPlatform.Application.Content.Courses.GetCourses;

public sealed record GetCoursesQuery(int Page = 1, int PageSize = 20) : IQuery<PaginatedList<CourseSummary>>;

/// <summary>Interim visibility (see <see cref="CourseAccess"/>): Admin sees every course,
/// Trainer sees their own regardless of publish state, everyone else sees published only.</summary>
public sealed class GetCoursesQueryHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : IQueryHandler<GetCoursesQuery, PaginatedList<CourseSummary>>
{
    public async Task<Result<PaginatedList<CourseSummary>>> Handle(GetCoursesQuery query, CancellationToken cancellationToken)
    {
        var courses = dbContext.Courses.AsNoTracking().AsQueryable();

        if (!currentUser.Roles.Contains(Roles.Administrator))
        {
            courses = currentUser.Roles.Contains(Roles.Trainer)
                ? courses.Where(c => c.IsPublished || c.TrainerId == currentUser.UserId)
                : courses.Where(c => c.IsPublished);
        }

        courses = courses.OrderByDescending(c => c.CreatedAtUtc);

        var totalCount = await courses.CountAsync(cancellationToken);

        var items = await courses
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CourseSummary(c.Id, c.Title, c.Description, c.TrainerId, c.IsPublished, c.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PaginatedList<CourseSummary>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }
}
