using FluentValidation;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Users;

namespace TrainingPlatform.Application.Content.Courses.CreateCourse;

/// <summary>A Trainer may only create a course under their own TrainerId; an Administrator may
/// assign it to any trainer.</summary>
public sealed record CreateCourseCommand(string Title, string Description, Guid TrainerId) : ICommand<Guid>;

public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(4000);
        RuleFor(c => c.TrainerId).NotEmpty();
    }
}

public sealed class CreateCourseCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<CreateCourseCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCourseCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.Roles.Contains(Roles.Administrator) && command.TrainerId != currentUser.UserId)
        {
            return Result.Failure<Guid>(ContentErrors.NotCourseOwner);
        }

        var course = Course.Create(command.Title, command.Description, command.TrainerId);
        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(currentUser.UserId, ActivityActions.CourseCreated, "Course", course.Id.ToString(), cancellationToken);

        return course.Id;
    }
}
