using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content;
using TrainingPlatform.Application.Quizzes.Contracts;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Quizzes;

namespace TrainingPlatform.Application.Quizzes.SubmitQuizAttempt;

public sealed record SubmitQuizAttemptCommand(Guid QuizId, IReadOnlyList<AnswerInput> Answers) : ICommand<QuizAttemptResult>;

public sealed class SubmitQuizAttemptCommandValidator : AbstractValidator<SubmitQuizAttemptCommand>
{
    public SubmitQuizAttemptCommandValidator()
    {
        RuleFor(c => c.QuizId).NotEmpty();
        RuleFor(c => c.Answers).NotEmpty();
    }
}

public sealed class SubmitQuizAttemptCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<SubmitQuizAttemptCommand, QuizAttemptResult>
{
    public async Task<Result<QuizAttemptResult>> Handle(SubmitQuizAttemptCommand command, CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes.SingleOrDefaultAsync(q => q.Id == command.QuizId, cancellationToken);
        if (quiz is null)
        {
            return Result.Failure<QuizAttemptResult>(QuizErrors.QuizNotFound(command.QuizId));
        }

        var module = await dbContext.Modules.SingleOrDefaultAsync(m => m.Id == quiz.ModuleId, cancellationToken);
        var course = module is null
            ? null
            : await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);

        if (course is null || !await CourseAccess.CanDownloadAsync(course, currentUser, dbContext, cancellationToken))
        {
            return Result.Failure<QuizAttemptResult>(ContentErrors.CourseNotAccessible);
        }

        var questions = await dbContext.Questions
            .Where(q => q.QuizId == quiz.Id)
            .ToListAsync(cancellationToken);

        if (questions.Count == 0)
        {
            return Result.Failure<QuizAttemptResult>(QuizErrors.InvalidAnswers);
        }

        var questionIds = questions.Select(q => q.Id).ToHashSet();

        // Every question must be answered exactly once, and each selected choice must actually
        // belong to the question it's an answer for — this DB-dependent check can't live in the
        // validator, which has no database access.
        var answeredQuestionIds = command.Answers.Select(a => a.QuestionId).ToList();
        if (answeredQuestionIds.Count != questionIds.Count
            || answeredQuestionIds.Distinct().Count() != questionIds.Count
            || !questionIds.SetEquals(answeredQuestionIds))
        {
            return Result.Failure<QuizAttemptResult>(QuizErrors.InvalidAnswers);
        }

        var choices = await dbContext.QuestionChoices
            .Where(c => questionIds.Contains(c.QuestionId))
            .ToListAsync(cancellationToken);

        var choicesByQuestion = choices.ToLookup(c => c.QuestionId);

        var correctCount = 0;
        foreach (var answer in command.Answers)
        {
            var selectedChoice = choicesByQuestion[answer.QuestionId]
                .SingleOrDefault(c => c.Id == answer.SelectedChoiceId);

            if (selectedChoice is null)
            {
                return Result.Failure<QuizAttemptResult>(QuizErrors.InvalidAnswers);
            }

            if (selectedChoice.IsCorrect)
            {
                correctCount++;
            }
        }

        var scorePercent = (int)Math.Round(correctCount * 100.0 / questions.Count);

        var attempt = QuizAttempt.Create(quiz.Id, currentUser.UserId, scorePercent, quiz.PassingScorePercent);
        dbContext.QuizAttempts.Add(attempt);

        foreach (var answer in command.Answers)
        {
            dbContext.QuizAttemptAnswers.Add(
                QuizAttemptAnswer.Create(attempt.Id, answer.QuestionId, answer.SelectedChoiceId));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var courseCompleted = false;
        Guid? certificateId = null;

        var enrollment = await dbContext.Enrollments
            .SingleOrDefaultAsync(e => e.CourseId == course.Id && e.UserId == currentUser.UserId, cancellationToken);

        if (enrollment is not null)
        {
            var outcome = await CourseCompletionService.CompleteAndIssueCertificateIfEligibleAsync(
                course, enrollment, currentUser.FullName, dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            courseCompleted = outcome.Completed;
            certificateId = outcome.CertificateId;
        }

        await activityLog.LogAsync(
            currentUser.UserId, ActivityActions.QuizAttemptSubmitted, "Quiz", quiz.Id.ToString(), cancellationToken);

        return new QuizAttemptResult(scorePercent, attempt.Passed, courseCompleted, certificateId is not null, certificateId);
    }
}
