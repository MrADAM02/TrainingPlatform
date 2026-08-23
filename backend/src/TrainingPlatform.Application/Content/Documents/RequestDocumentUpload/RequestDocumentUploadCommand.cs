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

namespace TrainingPlatform.Application.Content.Documents.RequestDocumentUpload;

/// <summary>
/// Returns a pre-signed PUT URL the client uploads directly to; the API process never buffers
/// the file bytes (REQ-CONT-03/04). The Document row is created immediately with the
/// client-declared metadata — there is no separate "confirm upload" step in this MVP, so a
/// document whose PUT never completes leaves an orphaned row pointing at a missing object. That's
/// an accepted simplification for now, not a silently ignored gap.
/// </summary>
public sealed record RequestDocumentUploadCommand(
    Guid ModuleId,
    string Title,
    string FileName,
    string ContentType,
    long SizeBytes) : ICommand<UploadTicket>;

public sealed class RequestDocumentUploadCommandValidator : AbstractValidator<RequestDocumentUploadCommand>
{
    public RequestDocumentUploadCommandValidator()
    {
        RuleFor(c => c.ModuleId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.FileName).NotEmpty().MaximumLength(255);
        RuleFor(c => c.ContentType).NotEmpty();
        RuleFor(c => c.SizeBytes).GreaterThan(0);
    }
}

public sealed class RequestDocumentUploadCommandHandler(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorage,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<RequestDocumentUploadCommand, UploadTicket>
{
    private static readonly TimeSpan UploadUrlLifetime = TimeSpan.FromMinutes(15);

    public async Task<Result<UploadTicket>> Handle(RequestDocumentUploadCommand command, CancellationToken cancellationToken)
    {
        var module = await dbContext.Modules.SingleOrDefaultAsync(m => m.Id == command.ModuleId, cancellationToken);
        if (module is null)
        {
            return Result.Failure<UploadTicket>(ContentErrors.ModuleNotFound(command.ModuleId));
        }

        var course = await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);
        if (course is null || !CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure<UploadTicket>(ContentErrors.NotCourseOwner);
        }

        var fileType = Document.InferFileType(command.ContentType);
        var documentId = Guid.NewGuid();
        var storageKey = $"courses/{course.Id}/modules/{module.Id}/{documentId}/{SanitizeFileName(command.FileName)}";

        var document = Document.Create(
            module.Id, command.Title, fileType, command.ContentType, storageKey, command.SizeBytes, currentUser.UserId);
        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        var uploadUrl = await fileStorage.GetUploadUrlAsync(storageKey, command.ContentType, cancellationToken);

        await activityLog.LogAsync(
            currentUser.UserId, ActivityActions.DocumentUploadRequested, "Document", document.Id.ToString(), cancellationToken);

        return new UploadTicket(document.Id, uploadUrl, DateTime.UtcNow.Add(UploadUrlLifetime));
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
