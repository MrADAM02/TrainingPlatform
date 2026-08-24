using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Documents.UpdateDocumentLessonDetails;

/// <summary>Edits a document's metadata (title + lesson-detail fields) without touching its
/// file — the first command that does so; every other document edit today is either a brand new
/// upload or a file replace (REQ-CONT-06). Meaningful mainly for video documents, but not
/// type-restricted here — a non-video document simply has no reason to be given these fields via
/// the frontend, and nothing breaks if it is.</summary>
public sealed record UpdateDocumentLessonDetailsCommand(
    Guid DocumentId,
    string Title,
    string? TranscriptText,
    string? SummaryText,
    string? KeyTakeaway,
    int? DurationMinutes) : ICommand;

public sealed class UpdateDocumentLessonDetailsCommandValidator : AbstractValidator<UpdateDocumentLessonDetailsCommand>
{
    public UpdateDocumentLessonDetailsCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.DurationMinutes).GreaterThan(0).When(c => c.DurationMinutes.HasValue);
    }
}

public sealed class UpdateDocumentLessonDetailsCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<UpdateDocumentLessonDetailsCommand>
{
    public async Task<Result> Handle(UpdateDocumentLessonDetailsCommand command, CancellationToken cancellationToken)
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

        document.UpdateLessonDetails(
            command.Title, command.TranscriptText, command.SummaryText, command.KeyTakeaway, command.DurationMinutes);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(
            currentUser.UserId, ActivityActions.DocumentUpdated, "Document", document.Id.ToString(), cancellationToken);

        return Result.Success();
    }
}
