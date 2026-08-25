using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Abstractions.Storage;
using TrainingPlatform.Application.Content.Contracts;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Documents.ReplaceDocumentFile;

/// <summary>Replaces a document's file with a new one (REQ-CONT-06), archiving the current state
/// as a <see cref="DocumentVersion"/> first. Same "no confirm-upload step" simplification as
/// <see cref="RequestDocumentUpload.RequestDocumentUploadCommand"/> — the archive and the version
/// bump happen immediately, before the client's PUT to the presigned URL actually completes.</summary>
public sealed record ReplaceDocumentFileCommand(
    Guid DocumentId,
    string FileName,
    string ContentType,
    long SizeBytes) : ICommand<UploadTicket>;

public sealed class ReplaceDocumentFileCommandValidator : AbstractValidator<ReplaceDocumentFileCommand>
{
    public ReplaceDocumentFileCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.FileName).NotEmpty().MaximumLength(255);
        RuleFor(c => c.ContentType).NotEmpty();
        RuleFor(c => c.SizeBytes).GreaterThan(0);
    }
}

public sealed class ReplaceDocumentFileCommandHandler(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorage,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<ReplaceDocumentFileCommand, UploadTicket>
{
    private static readonly TimeSpan UploadUrlLifetime = TimeSpan.FromMinutes(15);

    public async Task<Result<UploadTicket>> Handle(ReplaceDocumentFileCommand command, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents.SingleOrDefaultAsync(d => d.Id == command.DocumentId, cancellationToken);
        if (document is null)
        {
            return Result.Failure<UploadTicket>(ContentErrors.DocumentNotFound(command.DocumentId));
        }

        var module = await dbContext.Modules.SingleOrDefaultAsync(m => m.Id == document.ModuleId, cancellationToken);
        var course = module is null
            ? null
            : await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);

        if (course is null || module is null || !CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure<UploadTicket>(ContentErrors.NotCourseOwner);
        }

        if (document.StorageKey is null || document.ContentType is null || document.SizeBytes is null)
        {
            return Result.Failure<UploadTicket>(ContentErrors.DocumentHasNoFile);
        }

        var archivedVersion = DocumentVersion.Create(
            document.Id, document.Version, document.FileType, document.ContentType, document.StorageKey,
            document.SizeBytes.Value, document.UploadedByUserId, document.UploadedAtUtc);
        dbContext.DocumentVersions.Add(archivedVersion);

        var fileType = Document.InferFileType(command.ContentType);
        var newVersion = document.Version + 1;
        var storageKey =
            $"courses/{course.Id}/modules/{module.Id}/{document.Id}/v{newVersion}/{SanitizeFileName(command.FileName)}";

        document.ReplaceFile(fileType, command.ContentType, storageKey, command.SizeBytes, currentUser.UserId);
        await dbContext.SaveChangesAsync(cancellationToken);

        var uploadUrl = await fileStorage.GetUploadUrlAsync(storageKey, command.ContentType, cancellationToken);

        await activityLog.LogAsync(
            currentUser.UserId, ActivityActions.DocumentReplaced, "Document", document.Id.ToString(), cancellationToken);

        return new UploadTicket(document.Id, uploadUrl, DateTime.UtcNow.Add(UploadUrlLifetime));
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
