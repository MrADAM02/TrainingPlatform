using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Courses.PublishCourse;

public sealed record PublishCourseCommand(Guid CourseId, bool IsPublished) : ICommand;

public sealed class PublishCourseCommandValidator : AbstractValidator<PublishCourseCommand>
{
    public PublishCourseCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
    }
}

public sealed class PublishCourseCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<PublishCourseCommand>
{
    public async Task<Result> Handle(PublishCourseCommand command, CancellationToken cancellationToken)
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

        if (command.IsPublished)
        {
            course.Publish();
        }
        else
        {
            course.Unpublish();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var action = command.IsPublished ? ActivityActions.CoursePublished : ActivityActions.CourseUnpublished;
        await activityLog.LogAsync(currentUser.UserId, action, "Course", course.Id.ToString(), cancellationToken);

        return Result.Success();
    }
}
