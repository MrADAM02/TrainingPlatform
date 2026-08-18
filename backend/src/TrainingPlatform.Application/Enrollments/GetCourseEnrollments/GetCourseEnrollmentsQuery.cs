using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content;
using TrainingPlatform.Application.Enrollments.Contracts;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Enrollments.GetCourseEnrollments;

public sealed record GetCourseEnrollmentsQuery(Guid CourseId) : IQuery<IReadOnlyList<EnrollmentSummary>>;

public sealed class GetCourseEnrollmentsQueryHandler(
    IApplicationDbContext dbContext,
    IIdentityService identityService,
    IUserContext currentUser) : IQueryHandler<GetCourseEnrollmentsQuery, IReadOnlyList<EnrollmentSummary>>
{
    public async Task<Result<IReadOnlyList<EnrollmentSummary>>> Handle(
        GetCourseEnrollmentsQuery query, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == query.CourseId, cancellationToken);
        if (course is null)
        {
            return Result.Failure<IReadOnlyList<EnrollmentSummary>>(ContentErrors.CourseNotFound(query.CourseId));
        }

        if (!CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure<IReadOnlyList<EnrollmentSummary>>(ContentErrors.NotCourseOwner);
        }

        var enrollments = await dbContext.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == query.CourseId)
            .OrderByDescending(e => e.EnrolledAtUtc)
            .ToListAsync(cancellationToken);

        var usersResult = await identityService.GetUsersByIdsAsync(
            enrollments.Select(e => e.UserId).ToList(), cancellationToken);

        if (usersResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<EnrollmentSummary>>(usersResult.Error);
        }

        var usersById = usersResult.Value.ToDictionary(u => u.Id);

        IReadOnlyList<EnrollmentSummary> items = enrollments
            .Where(e => usersById.ContainsKey(e.UserId))
            .Select(e => new EnrollmentSummary(
                e.Id,
                e.UserId,
                usersById[e.UserId].Email,
                usersById[e.UserId].FullName,
                e.CourseId,
                e.Status,
                e.EnrolledAtUtc))
            .ToList();

        return Result.Success(items);
    }
}
