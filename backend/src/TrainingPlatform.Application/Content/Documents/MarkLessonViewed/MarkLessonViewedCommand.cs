using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Documents.MarkLessonViewed;

/// <summary>The Text-lesson equivalent of <c>GetDocumentDownloadUrlQuery</c> (2026-08-25): records
/// progress/completion for a fileless lesson, since there is no download to trigger it via. Same
/// authorization boundary (<see cref="CourseAccess.CanDownloadAsync"/>) as every other lesson
/// type — reading a text lesson still requires an active enrollment (or ownership), exactly like
/// downloading a document does.</summary>
public sealed record MarkLessonViewedCommand(Guid DocumentId) : ICommand;

public sealed class MarkLessonViewedCommandValidator : AbstractValidator<MarkLessonViewedCommand>
{
    public MarkLessonViewedCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
    }
}

public sealed class MarkLessonViewedCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<MarkLessonViewedCommand>
{
    public async Task<Result> Handle(MarkLessonViewedCommand command, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents.AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == command.DocumentId, cancellationToken);
        if (document is null)
        {
            return Result.Failure(ContentErrors.DocumentNotFound(command.DocumentId));
        }

        var module = await dbContext.Modules.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == document.ModuleId, cancellationToken);
        var course = module is null
            ? null
            : await dbContext.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);

        if (course is null || !await CourseAccess.CanDownloadAsync(course, currentUser, dbContext, cancellationToken))
        {
            return Result.Failure(ContentErrors.CourseNotAccessible);
        }

        await LessonProgressService.RecordProgressIfEnrolledAsync(course, document.Id, dbContext, currentUser, cancellationToken);

        await activityLog.LogAsync(
            currentUser.UserId, ActivityActions.LessonViewed, "Document", document.Id.ToString(), cancellationToken);

        return Result.Success();
    }
}
