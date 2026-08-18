using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Abstractions.Storage;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Enrollments;

namespace TrainingPlatform.Application.Content.Documents.GetDocumentDownloadUrl;

/// <summary>Authorization is checked before a signed URL is ever issued (REQ-CONT-05).</summary>
public sealed record GetDocumentDownloadUrlQuery(Guid DocumentId) : IQuery<string>;

public sealed class GetDocumentDownloadUrlQueryHandler(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorage,
    IActivityLogService activityLog,
    IUserContext currentUser) : IQueryHandler<GetDocumentDownloadUrlQuery, string>
{
    public async Task<Result<string>> Handle(GetDocumentDownloadUrlQuery query, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents.AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == query.DocumentId, cancellationToken);
        if (document is null)
        {
            return Result.Failure<string>(ContentErrors.DocumentNotFound(query.DocumentId));
        }

        var module = await dbContext.Modules.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == document.ModuleId, cancellationToken);
        var course = module is null
            ? null
            : await dbContext.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);

        if (course is null || !await CourseAccess.CanDownloadAsync(course, currentUser, dbContext, cancellationToken))
        {
            return Result.Failure<string>(ContentErrors.CourseNotAccessible);
        }

        await RecordProgressIfEnrolledAsync(course.Id, document.Id, cancellationToken);

        var url = await fileStorage.GetDownloadUrlAsync(document.StorageKey, cancellationToken);

        await activityLog.LogAsync(
            currentUser.UserId, ActivityActions.DocumentDownloaded, "Document", document.Id.ToString(), cancellationToken);

        return url;
    }

    /// <summary>REQ-ENR-02/03: consuming a document records progress for the caller's own
    /// enrollment (no-op for Admin/Trainer previewing via ownership, since they have none), and
    /// once every document in the course has been consumed the enrollment is auto-completed.</summary>
    private async Task RecordProgressIfEnrolledAsync(Guid courseId, Guid documentId, CancellationToken cancellationToken)
    {
        var enrollment = await dbContext.Enrollments
            .SingleOrDefaultAsync(e => e.CourseId == courseId && e.UserId == currentUser.UserId, cancellationToken);

        if (enrollment is null)
        {
            return;
        }

        var alreadyRecorded = await dbContext.Progresses
            .AnyAsync(p => p.EnrollmentId == enrollment.Id && p.DocumentId == documentId, cancellationToken);

        if (!alreadyRecorded)
        {
            dbContext.Progresses.Add(Progress.Create(enrollment.Id, documentId));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (enrollment.Status == EnrollmentStatus.Active)
        {
            var moduleIds = await dbContext.Modules
                .Where(m => m.CourseId == courseId)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            var totalDocuments = await dbContext.Documents
                .CountAsync(d => moduleIds.Contains(d.ModuleId), cancellationToken);

            var completedDocuments = await dbContext.Progresses
                .CountAsync(p => p.EnrollmentId == enrollment.Id, cancellationToken);

            if (totalDocuments > 0 && completedDocuments >= totalDocuments)
            {
                enrollment.MarkCompleted();
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
