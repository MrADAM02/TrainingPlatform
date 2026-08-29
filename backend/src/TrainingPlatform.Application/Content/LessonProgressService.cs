using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Enrollments;

namespace TrainingPlatform.Application.Content;

/// <summary>Shared by both <c>GetDocumentDownloadUrlQuery</c> (file-backed documents) and
/// <c>MarkLessonViewedCommand</c> (fileless Text lessons, 2026-08-25) — the two triggers for
/// "this trainee consumed this piece of content" differ, but what happens afterward (record
/// progress, check for course completion) is identical either way.</summary>
internal static class LessonProgressService
{
    /// <summary>REQ-ENR-02/03: consuming a document records progress for the caller's own
    /// enrollment (no-op for Admin/Trainer previewing via ownership, since they have none), and
    /// once every document in the course has been consumed the enrollment is auto-completed and
    /// a certificate is issued (REQ-CERT-01).</summary>
    public static async Task RecordProgressIfEnrolledAsync(
        Course course,
        Guid documentId,
        IApplicationDbContext dbContext,
        IUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var enrollment = await FindEnrollmentAsync(course, dbContext, currentUser, cancellationToken);

        if (enrollment is null)
        {
            return;
        }

        await GetOrCreateProgressAsync(enrollment.Id, documentId, dbContext, cancellationToken);

        await CourseCompletionService.CompleteAndIssueCertificateIfEligibleAsync(
            course, enrollment, currentUser.FullName, dbContext, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The caller's own enrollment for this course, or null if they're not enrolled
    /// (an Admin/Trainer previewing via ownership has none, and there's nothing to record
    /// progress against for them) — the boundary every progress-touching operation shares.</summary>
    public static Task<Enrollment?> FindEnrollmentAsync(
        Course course, IApplicationDbContext dbContext, IUserContext currentUser, CancellationToken cancellationToken)
    {
        return dbContext.Enrollments
            .SingleOrDefaultAsync(e => e.CourseId == course.Id && e.UserId == currentUser.UserId, cancellationToken);
    }

    /// <summary>Used by both the one-time "consumed this document" recording above and
    /// <c>SaveVideoProgressCommand</c> (2026-08-29), which needs the same row to update a
    /// playback position on — a video's Progress row already exists by the time playback starts
    /// (created on the first download-url fetch), but this stays defensive rather than assuming
    /// that ordering always holds.</summary>
    public static async Task<Progress> GetOrCreateProgressAsync(
        Guid enrollmentId, Guid documentId, IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var progress = await dbContext.Progresses
            .SingleOrDefaultAsync(p => p.EnrollmentId == enrollmentId && p.DocumentId == documentId, cancellationToken);

        if (progress is null)
        {
            progress = Progress.Create(enrollmentId, documentId);
            dbContext.Progresses.Add(progress);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return progress;
    }
}
