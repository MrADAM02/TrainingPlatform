using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Enrollments;

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

    DbSet<Enrollment> Enrollments { get; }

    DbSet<Progress> Progresses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
