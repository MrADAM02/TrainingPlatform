using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content.Contracts;
using TrainingPlatform.Application.Quizzes.Contracts;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Courses.GetCourseById;

public sealed record GetCourseByIdQuery(Guid CourseId) : IQuery<CourseDetails>;

public sealed class GetCourseByIdQueryHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : IQueryHandler<GetCourseByIdQuery, CourseDetails>
{
    public async Task<Result<CourseDetails>> Handle(GetCourseByIdQuery query, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == query.CourseId, cancellationToken);

        if (course is null)
        {
            return Result.Failure<CourseDetails>(ContentErrors.CourseNotFound(query.CourseId));
        }

        if (!CourseAccess.CanView(course, currentUser))
        {
            return Result.Failure<CourseDetails>(ContentErrors.CourseNotAccessible);
        }

        var enrollment = await dbContext.Enrollments
            .SingleOrDefaultAsync(e => e.CourseId == course.Id && e.UserId == currentUser.UserId, cancellationToken);
        var isEnrolled = enrollment is not null;
        var canDownload = CourseAccess.CanManage(course, currentUser) || isEnrolled;

        var progressByDocumentId = enrollment is null
            ? []
            : await dbContext.Progresses.AsNoTracking()
                .Where(p => p.EnrollmentId == enrollment.Id)
                .ToDictionaryAsync(p => p.DocumentId, p => p.LastPositionSeconds, cancellationToken);

        var modules = await dbContext.Modules.AsNoTracking()
            .Where(m => m.CourseId == course.Id)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);

        var moduleIds = modules.Select(m => m.Id).ToList();

        var documents = await dbContext.Documents.AsNoTracking()
            .Where(d => moduleIds.Contains(d.ModuleId))
            .OrderBy(d => d.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        var quizzes = await dbContext.Quizzes.AsNoTracking()
            .Where(q => moduleIds.Contains(q.ModuleId))
            .ToListAsync(cancellationToken);

        var moduleDetails = modules
            .Select(m => new ModuleDetails(
                m.Id,
                m.CourseId,
                m.Title,
                m.Order,
                documents
                    .Where(d => d.ModuleId == m.Id)
                    .Select(d => new DocumentSummary(
                        d.Id, d.ModuleId, d.Title, d.FileType, d.ContentType, d.SizeBytes, d.Version, d.UploadedAtUtc,
                        d.TranscriptText, d.SummaryText, d.KeyTakeaway, d.DurationMinutes, d.PageCount, d.Quote,
                        progressByDocumentId.ContainsKey(d.Id),
                        progressByDocumentId.GetValueOrDefault(d.Id)))
                    .ToList(),
                quizzes
                    .Where(q => q.ModuleId == m.Id)
                    .Select(q => new QuizSummary(q.Id, q.ModuleId, q.Title, q.PassingScorePercent, q.IsRequiredForCompletion))
                    .ToList()))
            .ToList();

        var isBookmarked = await dbContext.CourseBookmarks
            .AnyAsync(b => b.UserId == currentUser.UserId && b.CourseId == course.Id, cancellationToken);

        return new CourseDetails(
            course.Id, course.Title, course.Description, course.TrainerId, course.IsPublished, course.CreatedAtUtc,
            isEnrolled, canDownload, isBookmarked, moduleDetails);
    }
}
