using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Certificates;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Enrollments;
using TrainingPlatform.Domain.Quizzes;
using TrainingPlatform.Infrastructure.Identity;

namespace TrainingPlatform.Infrastructure.Database;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Module> Modules => Set<Module>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    public DbSet<CourseBookmark> CourseBookmarks => Set<CourseBookmark>();

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public DbSet<Progress> Progresses => Set<Progress>();

    public DbSet<Certificate> Certificates => Set<Certificate>();

    public DbSet<Quiz> Quizzes => Set<Quiz>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<QuestionChoice> QuestionChoices => Set<QuestionChoice>();

    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ASP.NET Core Identity sets explicit table names (AspNetUsers, etc.) via Fluent API,
        // which the snake_case naming convention does not override — rename them here so every
        // table in this Postgres database follows the same convention.
        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        builder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.HasIndex(t => t.UserId);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("activity_logs");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(100);
            entity.Property(a => a.EntityType).HasMaxLength(100);
            entity.Property(a => a.EntityId).HasMaxLength(100);
            entity.Property(a => a.IpAddress).HasMaxLength(45);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.TimestampUtc);

            // Deliberately no FK to users: an audit trail must survive deletion of the
            // account it references, so this is a plain, unconstrained UserId column.
        });

        builder.Entity<Course>(entity =>
        {
            entity.ToTable("courses");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Description).IsRequired().HasMaxLength(4000);
            entity.HasIndex(c => c.TrainerId);

            // Restrict rather than cascade: deleting a trainer who still owns courses should
            // fail loudly and force an explicit reassignment, not silently orphan the courses.
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(c => c.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Module>(entity =>
        {
            entity.ToTable("modules");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Title).IsRequired().HasMaxLength(200);
            entity.HasIndex(m => m.CourseId);
            entity.HasOne<Course>()
                .WithMany()
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Title).IsRequired().HasMaxLength(200);
            entity.Property(d => d.ContentType).IsRequired().HasMaxLength(200);
            entity.Property(d => d.StorageKey).IsRequired().HasMaxLength(1000);
            entity.HasIndex(d => d.ModuleId);
            entity.HasOne<Module>()
                .WithMany()
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            // UploadedByUserId deliberately has no FK — same reasoning as Course.TrainerId.
        });

        builder.Entity<DocumentVersion>(entity =>
        {
            entity.ToTable("document_versions");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.ContentType).IsRequired().HasMaxLength(200);
            entity.Property(v => v.StorageKey).IsRequired().HasMaxLength(1000);
            entity.HasIndex(v => v.DocumentId);
            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // UploadedByUserId deliberately has no FK, same reasoning as Document's own field.
        });

        builder.Entity<CourseBookmark>(entity =>
        {
            entity.ToTable("course_bookmarks");
            entity.HasKey(b => b.Id);
            entity.HasIndex(b => new { b.UserId, b.CourseId }).IsUnique();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Course>()
                .WithMany()
                .HasForeignKey(b => b.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("enrollments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Course>()
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Progress>(entity =>
        {
            entity.ToTable("document_progress");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => new { p.EnrollmentId, p.DocumentId }).IsUnique();
            entity.HasOne<Enrollment>()
                .WithMany()
                .HasForeignKey(p => p.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(p => p.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Certificate>(entity =>
        {
            entity.ToTable("certificates");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.CourseTitle).IsRequired().HasMaxLength(200);
            entity.Property(c => c.RecipientFullName).IsRequired().HasMaxLength(200);
            entity.Property(c => c.CertificateNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(c => c.CertificateNumber).IsUnique();
            entity.HasIndex(c => new { c.UserId, c.CourseId }).IsUnique();

            // Deliberately no FK to users/courses, same reasoning as ActivityLog: a certificate
            // is a permanent achievement record and must survive deletion of either.
        });

        builder.Entity<Quiz>(entity =>
        {
            entity.ToTable("quizzes");
            entity.HasKey(q => q.Id);
            entity.Property(q => q.Title).IsRequired().HasMaxLength(200);
            entity.HasIndex(q => q.ModuleId);
            entity.HasOne<Module>()
                .WithMany()
                .HasForeignKey(q => q.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Question>(entity =>
        {
            entity.ToTable("questions");
            entity.HasKey(q => q.Id);
            entity.Property(q => q.Text).IsRequired().HasMaxLength(1000);
            entity.HasIndex(q => q.QuizId);
            entity.HasOne<Quiz>()
                .WithMany()
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuestionChoice>(entity =>
        {
            entity.ToTable("question_choices");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Text).IsRequired().HasMaxLength(500);
            entity.HasIndex(c => c.QuestionId);
            entity.HasOne<Question>()
                .WithMany()
                .HasForeignKey(c => c.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuizAttempt>(entity =>
        {
            entity.ToTable("quiz_attempts");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => new { a.QuizId, a.UserId });
            entity.HasOne<Quiz>()
                .WithMany()
                .HasForeignKey(a => a.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deliberately no FK to users, same reasoning as ActivityLog/Certificate: attempt
            // history is a permanent record and must survive account deletion.
        });

        builder.Entity<QuizAttemptAnswer>(entity =>
        {
            entity.ToTable("quiz_attempt_answers");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.QuizAttemptId);
            entity.HasOne<QuizAttempt>()
                .WithMany()
                .HasForeignKey(a => a.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // QuestionId/SelectedChoiceId deliberately have no FK: they snapshot what was
            // actually selected at attempt time, so editing or deleting a question/choice later
            // never corrupts or cascades into a trainee's already-recorded answer.
        });
    }
}
