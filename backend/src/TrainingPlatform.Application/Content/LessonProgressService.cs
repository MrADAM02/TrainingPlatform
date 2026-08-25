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
        var enrollment = await dbContext.Enrollments
            .SingleOrDefaultAsync(e => e.CourseId == course.Id && e.UserId == currentUser.UserId, cancellationToken);

        if (enrollment is null)
        {
            return;
        }

        var alreadyRecorded = await dbContext.Progresses
            .AnyAsync(p => p.EnrollmentId == enrollment.Id && p.DocumentId == documentId, cancellationToken);

        if (!alreadyRecorded)
        {
            dbContext.Progresses.Add(Progress.Create(enrollment.Id, documentId));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await CourseCompletionService.CompleteAndIssueCertificateIfEligibleAsync(
            course, enrollment, currentUser.FullName, dbContext, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
