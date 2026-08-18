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

namespace TrainingPlatform.Application.Enrollments.RemoveEnrollment;

public sealed record RemoveEnrollmentCommand(Guid EnrollmentId) : ICommand;

public sealed class RemoveEnrollmentCommandValidator : AbstractValidator<RemoveEnrollmentCommand>
{
    public RemoveEnrollmentCommandValidator()
    {
        RuleFor(c => c.EnrollmentId).NotEmpty();
    }
}

public sealed class RemoveEnrollmentCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<RemoveEnrollmentCommand>
{
    public async Task<Result> Handle(RemoveEnrollmentCommand command, CancellationToken cancellationToken)
    {
        var enrollment = await dbContext.Enrollments
            .SingleOrDefaultAsync(e => e.Id == command.EnrollmentId, cancellationToken);
        if (enrollment is null)
        {
            return Result.Failure(EnrollmentErrors.NotFound(command.EnrollmentId));
        }

        var course = await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == enrollment.CourseId, cancellationToken);
        if (course is null || !CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure(ContentErrors.NotCourseOwner);
        }

        dbContext.Enrollments.Remove(enrollment);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(
            currentUser.UserId, ActivityActions.UserUnenrolled, "Course", enrollment.CourseId.ToString(), cancellationToken);

        return Result.Success();
    }
}
