using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Abstractions.Storage;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Documents.GetDocumentVersionDownloadUrl;

/// <summary>Download URL for a specific archived version (REQ-CONT-06) — Trainer/Admin only, same
/// boundary as <see cref="GetDocumentVersions.GetDocumentVersionsQuery"/>. Unlike the live
/// document's own download-url endpoint, this never records trainee progress: browsing history is
/// a management action, not course consumption.</summary>
public sealed record GetDocumentVersionDownloadUrlQuery(Guid DocumentVersionId) : IQuery<string>;

public sealed class GetDocumentVersionDownloadUrlQueryHandler(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorage,
    IActivityLogService activityLog,
    IUserContext currentUser) : IQueryHandler<GetDocumentVersionDownloadUrlQuery, string>
{
    public async Task<Result<string>> Handle(GetDocumentVersionDownloadUrlQuery query, CancellationToken cancellationToken)
    {
        var version = await dbContext.DocumentVersions.AsNoTracking()
            .SingleOrDefaultAsync(v => v.Id == query.DocumentVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure<string>(ContentErrors.DocumentNotFound(query.DocumentVersionId));
        }

        var document = await dbContext.Documents.AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == version.DocumentId, cancellationToken);
        var module = document is null
            ? null
            : await dbContext.Modules.AsNoTracking().SingleOrDefaultAsync(m => m.Id == document.ModuleId, cancellationToken);
        var course = module is null
            ? null
            : await dbContext.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);

        if (course is null || !CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure<string>(ContentErrors.NotCourseOwner);
        }

        var url = await fileStorage.GetDownloadUrlAsync(version.StorageKey, cancellationToken);

        await activityLog.LogAsync(
            currentUser.UserId, ActivityActions.DocumentDownloaded, "DocumentVersion", version.Id.ToString(), cancellationToken);

        return url;
    }
}
