using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Abstractions.Storage;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Documents.GetDocumentDownloadUrl;

/// <summary>Authorization is checked before a signed URL is ever issued (REQ-CONT-05). Not valid
/// for <see cref="DocumentType.Text"/> lessons — those have no file; see
/// <c>MarkLessonViewedCommand</c> for the equivalent trigger on that path.</summary>
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

        if (document.StorageKey is null)
        {
            return Result.Failure<string>(ContentErrors.DocumentHasNoFile);
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

        await LessonProgressService.RecordProgressIfEnrolledAsync(course, document.Id, dbContext, currentUser, cancellationToken);

        var url = await fileStorage.GetDownloadUrlAsync(document.StorageKey, cancellationToken);

        await activityLog.LogAsync(
            currentUser.UserId, ActivityActions.DocumentDownloaded, "Document", document.Id.ToString(), cancellationToken);

        return url;
    }
}
