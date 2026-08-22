using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Domain.Certificates;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Enrollments;

namespace TrainingPlatform.Application.Content;

/// <summary>Whether the completion check just completed the course, and the certificate id if
/// one was issued (or already existed) as a result — <see langword="null"/> if the course wasn't
/// completed by this call.</summary>
internal sealed record CourseCompletionOutcome(bool Completed, Guid? CertificateId);

/// <summary>
/// Single place that decides "has this trainee finished this course?" and, if so, marks the
/// enrollment complete and issues a certificate (REQ-CERT-01). Shared by every trigger that can
/// complete a course — document downloads (<c>GetDocumentDownloadUrlQuery</c>) and quiz attempts
/// (<c>SubmitQuizAttemptCommand</c>) — so completion logic never drifts between the two call sites.
/// </summary>
internal static class CourseCompletionService
{
    /// <summary>A course is complete for a user when every document has been downloaded AND
    /// every quiz flagged IsRequiredForCompletion on that course has at least one passing
    /// attempt by that user (any historical pass counts, per the unlimited-retakes design —
    /// not just the latest attempt).</summary>
    public static async Task<bool> IsCourseCompleteAsync(
        Course course, Guid userId, IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var enrollment = await dbContext.Enrollments
            .SingleOrDefaultAsync(e => e.CourseId == course.Id && e.UserId == userId, cancellationToken);

        if (enrollment is null)
        {
            return false;
        }

        var moduleIds = await dbContext.Modules
            .Where(m => m.CourseId == course.Id)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var totalDocuments = await dbContext.Documents
            .CountAsync(d => moduleIds.Contains(d.ModuleId), cancellationToken);

        var completedDocuments = await dbContext.Progresses
            .CountAsync(p => p.EnrollmentId == enrollment.Id, cancellationToken);

        if (totalDocuments == 0 || completedDocuments < totalDocuments)
        {
            return false;
        }

        var requiredQuizIds = await dbContext.Quizzes
            .Where(q => moduleIds.Contains(q.ModuleId) && q.IsRequiredForCompletion)
            .Select(q => q.Id)
            .ToListAsync(cancellationToken);

        foreach (var quizId in requiredQuizIds)
        {
            var passed = await dbContext.QuizAttempts
                .AnyAsync(a => a.QuizId == quizId && a.UserId == userId && a.Passed, cancellationToken);

            if (!passed)
            {
                return false;
            }
        }

        return true;
    }

    public static async Task<CourseCompletionOutcome> CompleteAndIssueCertificateIfEligibleAsync(
        Course course,
        Enrollment enrollment,
        string recipientFullName,
        IApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (enrollment.Status != EnrollmentStatus.Active)
        {
            return new CourseCompletionOutcome(false, null);
        }

        if (!await IsCourseCompleteAsync(course, enrollment.UserId, dbContext, cancellationToken))
        {
            return new CourseCompletionOutcome(false, null);
        }

        enrollment.MarkCompleted();
        var certificateId = await IssueCertificateIfNotAlreadyIssuedAsync(
            course, enrollment.UserId, recipientFullName, dbContext, cancellationToken);

        return new CourseCompletionOutcome(true, certificateId);
    }

    private static async Task<Guid> IssueCertificateIfNotAlreadyIssuedAsync(
        Course course,
        Guid userId,
        string recipientFullName,
        IApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Certificates
            .SingleOrDefaultAsync(c => c.UserId == userId && c.CourseId == course.Id, cancellationToken);

        if (existing is not null)
        {
            return existing.Id;
        }

        var certificate = Certificate.Create(userId, course.Id, course.Title, recipientFullName);
        dbContext.Certificates.Add(certificate);
        return certificate.Id;
    }
}
