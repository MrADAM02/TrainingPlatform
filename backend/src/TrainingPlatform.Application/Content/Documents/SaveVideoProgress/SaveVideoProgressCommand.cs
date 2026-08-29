using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Documents.SaveVideoProgress;

/// <summary>Records where a trainee left off in a video (2026-08-29), so
/// <c>DocumentVideoPlayer.vue</c> can resume from that position next time. Called frequently
/// (throttled client-side) during playback — deliberately does not log to the activity log or
/// touch course-completion, unlike the one-time "consumed this document" recording in
/// <see cref="LessonProgressService"/>; this is just a position bookmark.</summary>
public sealed record SaveVideoProgressCommand(Guid DocumentId, int PositionSeconds) : ICommand;

public sealed class SaveVideoProgressCommandValidator : AbstractValidator<SaveVideoProgressCommand>
{
    public SaveVideoProgressCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.PositionSeconds).GreaterThanOrEqualTo(0);
    }
}

public sealed class SaveVideoProgressCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext currentUser) : ICommandHandler<SaveVideoProgressCommand>
{
    public async Task<Result> Handle(SaveVideoProgressCommand command, CancellationToken cancellationToken)
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

        var enrollment = await LessonProgressService.FindEnrollmentAsync(course, dbContext, currentUser, cancellationToken);
        if (enrollment is null)
        {
            // Admin/Trainer previewing via ownership, not an enrolled trainee — nothing to
            // bookmark a position against. Not an error, just nothing to do.
            return Result.Success();
        }

        var progress = await LessonProgressService.GetOrCreateProgressAsync(
            enrollment.Id, document.Id, dbContext, cancellationToken);
        progress.UpdatePlaybackPosition(command.PositionSeconds);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
