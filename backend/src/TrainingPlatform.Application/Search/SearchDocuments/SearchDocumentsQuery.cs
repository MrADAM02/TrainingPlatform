using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Search.Contracts;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Users;

namespace TrainingPlatform.Application.Search.SearchDocuments;

/// <summary>REQ-SRCH-01/02/03: keyword + course/content-type/trainer filters. Open-catalog
/// browsing (see <see cref="Content.CourseAccess"/>): results are pre-scoped to courses the
/// caller can see (owned/managed, or published) — not to enrollment, which instead determines
/// whether each result can actually be downloaded.</summary>
public sealed record SearchDocumentsQuery(
    string? Keyword = null,
    Guid? CourseId = null,
    DocumentType? ContentType = null,
    Guid? TrainerId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedList<DocumentSearchResult>>;

public sealed class SearchDocumentsQueryHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : IQueryHandler<SearchDocumentsQuery, PaginatedList<DocumentSearchResult>>
{
    public async Task<Result<PaginatedList<DocumentSearchResult>>> Handle(
        SearchDocumentsQuery query, CancellationToken cancellationToken)
    {
        var results =
            from doc in dbContext.Documents.AsNoTracking()
            join module in dbContext.Modules.AsNoTracking() on doc.ModuleId equals module.Id
            join course in dbContext.Courses.AsNoTracking() on module.CourseId equals course.Id
            select new { doc, module, course };

        if (!currentUser.Roles.Contains(Roles.Administrator))
        {
            results = results.Where(x => x.course.TrainerId == currentUser.UserId || x.course.IsPublished);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.ToLower();
            results = results.Where(x => x.doc.Title.ToLower().Contains(keyword));
        }

        if (query.CourseId.HasValue)
        {
            results = results.Where(x => x.course.Id == query.CourseId.Value);
        }

        if (query.ContentType.HasValue)
        {
            results = results.Where(x => x.doc.FileType == query.ContentType.Value);
        }

        if (query.TrainerId.HasValue)
        {
            results = results.Where(x => x.course.TrainerId == query.TrainerId.Value);
        }

        results = results.OrderByDescending(x => x.doc.UploadedAtUtc);

        var totalCount = await results.CountAsync(cancellationToken);

        var pagedResults = await results
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new
            {
                x.doc.Id,
                x.doc.Title,
                x.doc.FileType,
                CourseId = x.course.Id,
                CourseTitle = x.course.Title,
                CourseTrainerId = x.course.TrainerId,
                ModuleId = x.module.Id,
                ModuleTitle = x.module.Title,
                x.doc.UploadedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var pageCourseIds = pagedResults.Select(r => r.CourseId).Distinct().ToList();
        var enrolledCourseIds = await dbContext.Enrollments
            .Where(e => e.UserId == currentUser.UserId && pageCourseIds.Contains(e.CourseId))
            .Select(e => e.CourseId)
            .ToListAsync(cancellationToken);

        var isAdmin = currentUser.Roles.Contains(Roles.Administrator);

        var items = pagedResults
            .Select(r =>
            {
                var canDownload = isAdmin || r.CourseTrainerId == currentUser.UserId || enrolledCourseIds.Contains(r.CourseId);
                return new DocumentSearchResult(
                    r.Id, r.Title, r.FileType, r.CourseId, r.CourseTitle, r.ModuleId, r.ModuleTitle, r.UploadedAtUtc, canDownload);
            })
            .ToList();

        return new PaginatedList<DocumentSearchResult>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }
}
