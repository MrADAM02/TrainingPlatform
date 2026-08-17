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

namespace TrainingPlatform.Application.Content.Modules.DeleteModule;

public sealed record DeleteModuleCommand(Guid ModuleId) : ICommand;

public sealed class DeleteModuleCommandValidator : AbstractValidator<DeleteModuleCommand>
{
    public DeleteModuleCommandValidator()
    {
        RuleFor(c => c.ModuleId).NotEmpty();
    }
}

public sealed class DeleteModuleCommandHandler(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorage,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<DeleteModuleCommand>
{
    public async Task<Result> Handle(DeleteModuleCommand command, CancellationToken cancellationToken)
    {
        var module = await dbContext.Modules.SingleOrDefaultAsync(m => m.Id == command.ModuleId, cancellationToken);
        if (module is null)
        {
            return Result.Failure(ContentErrors.ModuleNotFound(command.ModuleId));
        }

        var course = await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);
        if (course is null || !CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure(ContentErrors.NotCourseOwner);
        }

        var storageKeys = await dbContext.Documents
            .Where(d => d.ModuleId == module.Id)
            .Select(d => d.StorageKey)
            .ToListAsync(cancellationToken);

        foreach (var storageKey in storageKeys)
        {
            await fileStorage.DeleteAsync(storageKey, cancellationToken);
        }

        dbContext.Modules.Remove(module);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(currentUser.UserId, ActivityActions.ModuleDeleted, "Module", module.Id.ToString(), cancellationToken);

        return Result.Success();
    }
}
