using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Domain.Certificates;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Enrollments;
using TrainingPlatform.Domain.Quizzes;

namespace TrainingPlatform.Application.Abstractions.Data;

/// <summary>
/// Narrow view of the EF Core context exposed to the Application layer — only for entities
/// that have no dependency on ASP.NET Core Identity, which lives in Infrastructure and
/// Application must not reference.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Course> Courses { get; }

    DbSet<Module> Modules { get; }

    DbSet<Document> Documents { get; }

    DbSet<DocumentVersion> DocumentVersions { get; }

    DbSet<Enrollment> Enrollments { get; }

    DbSet<Progress> Progresses { get; }

    DbSet<Certificate> Certificates { get; }

    DbSet<Quiz> Quizzes { get; }

    DbSet<Question> Questions { get; }

    DbSet<QuestionChoice> QuestionChoices { get; }

    DbSet<QuizAttempt> QuizAttempts { get; }

    DbSet<QuizAttemptAnswer> QuizAttemptAnswers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
