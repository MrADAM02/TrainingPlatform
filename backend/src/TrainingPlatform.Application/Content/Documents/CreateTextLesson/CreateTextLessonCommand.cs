using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Documents.CreateTextLesson;

/// <summary>Creates a fileless <see cref="DocumentType.Text"/> lesson (2026-08-25 lesson-type
/// redesign) — mirrors <see cref="RequestDocumentUpload.RequestDocumentUploadCommand"/>'s
/// authorization pattern, but touches no storage and returns no upload ticket, since there is
/// nothing to upload.</summary>
public sealed record CreateTextLessonCommand(
    Guid ModuleId,
    string Title,
    string BodyText,
    string? Quote) : ICommand<Guid>;

public sealed class CreateTextLessonCommandValidator : AbstractValidator<CreateTextLessonCommand>
{
    public CreateTextLessonCommandValidator()
    {
        RuleFor(c => c.ModuleId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.BodyText).NotEmpty();
    }
}

public sealed class CreateTextLessonCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<CreateTextLessonCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTextLessonCommand command, CancellationToken cancellationToken)
    {
        var module = await dbContext.Modules.SingleOrDefaultAsync(m => m.Id == command.ModuleId, cancellationToken);
        if (module is null)
        {
            return Result.Failure<Guid>(ContentErrors.ModuleNotFound(command.ModuleId));
        }

        var course = await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);
        if (course is null || !CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure<Guid>(ContentErrors.NotCourseOwner);
        }

        var document = Document.CreateTextLesson(module.Id, command.Title, command.BodyText, command.Quote, currentUser.UserId);
        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(
            currentUser.UserId, ActivityActions.TextLessonCreated, "Document", document.Id.ToString(), cancellationToken);

        return document.Id;
    }
}
