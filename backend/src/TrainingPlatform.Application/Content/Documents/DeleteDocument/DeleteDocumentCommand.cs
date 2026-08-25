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

namespace TrainingPlatform.Application.Content.Documents.DeleteDocument;

public sealed record DeleteDocumentCommand(Guid DocumentId) : ICommand;

public sealed class DeleteDocumentCommandValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
    }
}

public sealed class DeleteDocumentCommandHandler(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorage,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<DeleteDocumentCommand>
{
    public async Task<Result> Handle(DeleteDocumentCommand command, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents.SingleOrDefaultAsync(d => d.Id == command.DocumentId, cancellationToken);
        if (document is null)
        {
            return Result.Failure(ContentErrors.DocumentNotFound(command.DocumentId));
        }

        var module = await dbContext.Modules.SingleOrDefaultAsync(m => m.Id == document.ModuleId, cancellationToken);
        var course = module is null
            ? null
            : await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);

        if (course is null || !CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure(ContentErrors.NotCourseOwner);
        }

        var archivedVersions = await dbContext.DocumentVersions.AsNoTracking()
            .Where(v => v.DocumentId == document.Id)
            .Select(v => v.StorageKey)
            .ToListAsync(cancellationToken);

        foreach (var storageKey in archivedVersions)
        {
            await fileStorage.DeleteAsync(storageKey, cancellationToken);
        }

        if (document.StorageKey is not null)
        {
            await fileStorage.DeleteAsync(document.StorageKey, cancellationToken);
        }

        dbContext.Documents.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(
            currentUser.UserId, ActivityActions.DocumentDeleted, "Document", document.Id.ToString(), cancellationToken);

        return Result.Success();
    }
}
