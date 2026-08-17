using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Abstractions.Storage;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Courses.DeleteCourse;

public sealed record DeleteCourseCommand(Guid CourseId) : ICommand;

public sealed class DeleteCourseCommandValidator : AbstractValidator<DeleteCourseCommand>
{
    public DeleteCourseCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
    }
}

public sealed class DeleteCourseCommandHandler(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorage,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<DeleteCourseCommand>
{
    public async Task<Result> Handle(DeleteCourseCommand command, CancellationToken cancellationToken)
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

        var moduleIds = await dbContext.Modules
            .Where(m => m.CourseId == course.Id)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var storageKeys = await dbContext.Documents
            .Where(d => moduleIds.Contains(d.ModuleId))
            .Select(d => d.StorageKey)
            .ToListAsync(cancellationToken);

        foreach (var storageKey in storageKeys)
        {
            await fileStorage.DeleteAsync(storageKey, cancellationToken);
        }

        // EF cascade-delete (Document -> Module -> Course) removes the child rows.
        dbContext.Courses.Remove(course);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(currentUser.UserId, ActivityActions.CourseDeleted, "Course", course.Id.ToString(), cancellationToken);

        return Result.Success();
    }
}
