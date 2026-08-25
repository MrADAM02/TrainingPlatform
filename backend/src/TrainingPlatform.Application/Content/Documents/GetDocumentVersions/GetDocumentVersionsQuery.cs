using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content.Contracts;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Documents.GetDocumentVersions;

/// <summary>Version history (REQ-CONT-06) — Trainer/Admin only, per the access decision to keep
/// this a content-management view rather than trainee-facing. The current version is built from
/// the live <see cref="Domain.Content.Document"/> row, not the <see
/// cref="Domain.Content.DocumentVersion"/> table, since that table only ever holds superseded
/// versions.</summary>
public sealed record GetDocumentVersionsQuery(Guid DocumentId) : IQuery<IReadOnlyList<DocumentVersionItem>>;

public sealed class GetDocumentVersionsQueryHandler(
    IApplicationDbContext dbContext,
    IIdentityService identityService,
    IUserContext currentUser) : IQueryHandler<GetDocumentVersionsQuery, IReadOnlyList<DocumentVersionItem>>
{
    public async Task<Result<IReadOnlyList<DocumentVersionItem>>> Handle(
        GetDocumentVersionsQuery query, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents.AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == query.DocumentId, cancellationToken);
        if (document is null)
        {
            return Result.Failure<IReadOnlyList<DocumentVersionItem>>(ContentErrors.DocumentNotFound(query.DocumentId));
        }

        var module = await dbContext.Modules.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == document.ModuleId, cancellationToken);
        var course = module is null
            ? null
            : await dbContext.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);

        if (course is null || !CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure<IReadOnlyList<DocumentVersionItem>>(ContentErrors.NotCourseOwner);
        }

        if (document.SizeBytes is null)
        {
            return Result.Failure<IReadOnlyList<DocumentVersionItem>>(ContentErrors.DocumentHasNoFile);
        }

        var archivedVersions = await dbContext.DocumentVersions.AsNoTracking()
            .Where(v => v.DocumentId == document.Id)
            .OrderByDescending(v => v.Version)
            .ToListAsync(cancellationToken);

        var uploaderIds = archivedVersions
            .Select(v => v.UploadedByUserId)
            .Append(document.UploadedByUserId)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var uploadersById = uploaderIds.Count == 0
            ? []
            : (await identityService.GetUsersByIdsAsync(uploaderIds, cancellationToken))
            .Value.ToDictionary(u => u.Id);

        UserSummary? ResolveUploader(Guid? userId) =>
            userId.HasValue && uploadersById.TryGetValue(userId.Value, out var uploader) ? uploader : null;

        var currentUploader = ResolveUploader(document.UploadedByUserId);

        var items = new List<DocumentVersionItem>
        {
            new(null, document.Version, true, document.SizeBytes.Value, document.UploadedAtUtc, currentUploader?.Email, currentUploader?.FullName),
        };

        items.AddRange(archivedVersions.Select(v =>
        {
            var uploader = ResolveUploader(v.UploadedByUserId);
            return new DocumentVersionItem(v.Id, v.Version, false, v.SizeBytes, v.UploadedAtUtc, uploader?.Email, uploader?.FullName);
        }));

        return Result.Success<IReadOnlyList<DocumentVersionItem>>(items);
    }
}
