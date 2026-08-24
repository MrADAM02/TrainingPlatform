using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content.Contracts;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Content.Bookmarks.GetMyBookmarks;

public sealed record GetMyBookmarksQuery : IQuery<IReadOnlyList<CourseSummary>>;

public sealed class GetMyBookmarksQueryHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : IQueryHandler<GetMyBookmarksQuery, IReadOnlyList<CourseSummary>>
{
    public async Task<Result<IReadOnlyList<CourseSummary>>> Handle(
        GetMyBookmarksQuery query, CancellationToken cancellationToken)
    {
        var bookmarks = await dbContext.CourseBookmarks.AsNoTracking()
            .Where(b => b.UserId == currentUser.UserId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var courseIds = bookmarks.Select(b => b.CourseId).ToList();
        var courses = await dbContext.Courses.AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var enrolledCourseIds = (await dbContext.Enrollments.AsNoTracking()
                .Where(e => e.UserId == currentUser.UserId && courseIds.Contains(e.CourseId))
                .Select(e => e.CourseId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        IReadOnlyList<CourseSummary> items = bookmarks
            .Where(b => courses.ContainsKey(b.CourseId))
            .Select(b =>
            {
                var course = courses[b.CourseId];
                return new CourseSummary(
                    course.Id, course.Title, course.Description, course.TrainerId, course.IsPublished,
                    course.CreatedAtUtc, enrolledCourseIds.Contains(course.Id), IsBookmarked: true);
            })
            .ToList();

        return Result.Success(items);
    }
}
