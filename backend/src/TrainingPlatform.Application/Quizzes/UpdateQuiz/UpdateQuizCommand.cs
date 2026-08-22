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

namespace TrainingPlatform.Application.Quizzes.UpdateQuiz;

/// <summary>
/// Saves quiz details and replaces the full question/choice set (delete-and-recreate children)
/// rather than diffing — the simplest correct approach given there's no precedent anywhere in
/// this codebase for partial nested-collection updates.
/// </summary>
public sealed record UpdateQuizCommand(
    Guid QuizId,
    string Title,
    int PassingScorePercent,
    bool IsRequiredForCompletion,
    IReadOnlyList<QuestionInput> Questions) : ICommand;

public sealed class ChoiceInputValidator : AbstractValidator<ChoiceInput>
{
    public ChoiceInputValidator()
    {
        RuleFor(c => c.Text).NotEmpty().MaximumLength(500);
    }
}

public sealed class QuestionInputValidator : AbstractValidator<QuestionInput>
{
    public QuestionInputValidator()
    {
        RuleFor(q => q.Text).NotEmpty().MaximumLength(1000);
        RuleFor(q => q.Choices).Must(c => c.Count >= 2)
            .WithMessage("Each question needs at least 2 choices.");
        RuleFor(q => q.Choices).Must(c => c.Count(x => x.IsCorrect) == 1)
            .WithMessage("Each question must have exactly one correct choice.");
        RuleForEach(q => q.Choices).SetValidator(new ChoiceInputValidator());
    }
}

public sealed class UpdateQuizCommandValidator : AbstractValidator<UpdateQuizCommand>
{
    public UpdateQuizCommandValidator()
    {
        RuleFor(c => c.QuizId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.PassingScorePercent).InclusiveBetween(0, 100);
        RuleForEach(c => c.Questions).SetValidator(new QuestionInputValidator());
    }
}

public sealed class UpdateQuizCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<UpdateQuizCommand>
{
    public async Task<Result> Handle(UpdateQuizCommand command, CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes.SingleOrDefaultAsync(q => q.Id == command.QuizId, cancellationToken);
        if (quiz is null)
        {
            return Result.Failure(QuizErrors.QuizNotFound(command.QuizId));
        }

        var module = await dbContext.Modules.SingleOrDefaultAsync(m => m.Id == quiz.ModuleId, cancellationToken);
        var course = module is null
            ? null
            : await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);

        if (course is null || !CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure(ContentErrors.NotCourseOwner);
        }

        quiz.UpdateDetails(command.Title, command.PassingScorePercent, command.IsRequiredForCompletion);

        var existingQuestions = await dbContext.Questions
            .Where(q => q.QuizId == quiz.Id)
            .ToListAsync(cancellationToken);

        var existingQuestionIds = existingQuestions.Select(q => q.Id).ToList();

        var existingChoices = await dbContext.QuestionChoices
            .Where(c => existingQuestionIds.Contains(c.QuestionId))
            .ToListAsync(cancellationToken);

        dbContext.QuestionChoices.RemoveRange(existingChoices);
        dbContext.Questions.RemoveRange(existingQuestions);

        foreach (var questionInput in command.Questions)
        {
            var question = Question.Create(quiz.Id, questionInput.Text, questionInput.Order);
            dbContext.Questions.Add(question);

            foreach (var choiceInput in questionInput.Choices)
            {
                dbContext.QuestionChoices.Add(
                    QuestionChoice.Create(question.Id, choiceInput.Text, choiceInput.IsCorrect, choiceInput.Order));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(currentUser.UserId, ActivityActions.QuizUpdated, "Quiz", quiz.Id.ToString(), cancellationToken);

        return Result.Success();
    }
}
