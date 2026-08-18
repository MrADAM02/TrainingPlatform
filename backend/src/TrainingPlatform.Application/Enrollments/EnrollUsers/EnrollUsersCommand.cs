using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Enrollments;

namespace TrainingPlatform.Application.Enrollments.EnrollUsers;

/// <summary>Enrolls one or more users into a course (REQ-ENR-01). Already-enrolled users in the
/// batch are silently skipped rather than failing the whole request.</summary>
public sealed record EnrollUsersCommand(Guid CourseId, IReadOnlyList<Guid> UserIds) : ICommand<int>;

public sealed class EnrollUsersCommandValidator : AbstractValidator<EnrollUsersCommand>
{
    public EnrollUsersCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
        RuleFor(c => c.UserIds).NotEmpty();
    }
}

public sealed class EnrollUsersCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<EnrollUsersCommand, int>
{
    public async Task<Result<int>> Handle(EnrollUsersCommand command, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken);
        if (course is null)
        {
            return Result.Failure<int>(ContentErrors.CourseNotFound(command.CourseId));
        }

        if (!CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure<int>(ContentErrors.NotCourseOwner);
        }

        var alreadyEnrolledUserIds = await dbContext.Enrollments
            .Where(e => e.CourseId == command.CourseId && command.UserIds.Contains(e.UserId))
            .Select(e => e.UserId)
            .ToListAsync(cancellationToken);

        var toEnroll = command.UserIds.Except(alreadyEnrolledUserIds).Distinct().ToList();

        foreach (var userId in toEnroll)
        {
            dbContext.Enrollments.Add(Enrollment.Create(userId, command.CourseId));
        }

        if (toEnroll.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            await activityLog.LogAsync(
                currentUser.UserId, ActivityActions.UsersEnrolled, "Course", command.CourseId.ToString(), cancellationToken);
        }

        return toEnroll.Count;
    }
}
