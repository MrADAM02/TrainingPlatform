using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Courses.UpdateCourse;

public sealed record UpdateCourseCommand(Guid CourseId, string Title, string Description) : ICommand;

public sealed class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(4000);
    }
}

public sealed class UpdateCourseCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<UpdateCourseCommand>
{
    public async Task<Result> Handle(UpdateCourseCommand command, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken);
        if (course is null)
        {
            return Result.Failure(ContentErrors.CourseNotFound(command.CourseId));
        }

        if (!CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure(ContentErrors.NotCourseOwner);
        }

        course.UpdateDetails(command.Title, command.Description);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(currentUser.UserId, ActivityActions.CourseUpdated, "Course", course.Id.ToString(), cancellationToken);

        return Result.Success();
    }
}
