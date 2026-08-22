using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content;
using TrainingPlatform.Application.Quizzes.Contracts;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Quizzes;

namespace TrainingPlatform.Application.Quizzes.GetQuizForAttempt;

/// <summary>Trainee attempt-taking view. Authorization mirrors CourseAccess.CanDownloadAsync's
/// reasoning: owned/managed, or actively enrolled — the same rule that gates document
/// downloads gates taking a quiz.</summary>
public sealed record GetQuizForAttemptQuery(Guid QuizId) : IQuery<QuizAttemptView>;

public sealed class GetQuizForAttemptQueryHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : IQueryHandler<GetQuizForAttemptQuery, QuizAttemptView>
{
    public async Task<Result<QuizAttemptView>> Handle(GetQuizForAttemptQuery query, CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes.AsNoTracking()
            .SingleOrDefaultAsync(q => q.Id == query.QuizId, cancellationToken);
        if (quiz is null)
        {
            return Result.Failure<QuizAttemptView>(QuizErrors.QuizNotFound(query.QuizId));
        }

        var module = await dbContext.Modules.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == quiz.ModuleId, cancellationToken);
        var course = module is null
            ? null
            : await dbContext.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);

        if (course is null || !await CourseAccess.CanDownloadAsync(course, currentUser, dbContext, cancellationToken))
        {
            return Result.Failure<QuizAttemptView>(ContentErrors.CourseNotAccessible);
        }

        var questions = await dbContext.Questions.AsNoTracking()
            .Where(q => q.QuizId == quiz.Id)
            .OrderBy(q => q.Order)
            .ToListAsync(cancellationToken);

        var questionIds = questions.Select(q => q.Id).ToList();

        var choices = await dbContext.QuestionChoices.AsNoTracking()
            .Where(c => questionIds.Contains(c.QuestionId))
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken);

        var questionViews = questions
            .Select(q => new QuestionAttemptView(
                q.Id,
                q.Text,
                q.Order,
                choices
                    .Where(c => c.QuestionId == q.Id)
                    .Select(c => new ChoiceAttemptView(c.Id, c.Text, c.Order))
                    .ToList()))
            .ToList();

        var attempts = await dbContext.QuizAttempts.AsNoTracking()
            .Where(a => a.QuizId == quiz.Id && a.UserId == currentUser.UserId)
            .ToListAsync(cancellationToken);

        var hasPassed = attempts.Any(a => a.Passed);
        var bestScore = attempts.Count > 0 ? attempts.Max(a => a.ScorePercent) : (int?)null;

        return new QuizAttemptView(
            quiz.Id, quiz.ModuleId, quiz.Title, quiz.PassingScorePercent, questionViews, hasPassed, bestScore);
    }
}
