using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Quizzes;

namespace TrainingPlatform.Application.Quizzes.CreateQuiz;

/// <summary>
/// Creates a minimal quiz shell — mirrors CreateModuleCommand's shape exactly. Questions are
/// added afterward via UpdateQuizCommand from the builder page, not in one atomic call: a quiz
/// with zero questions is a valid (if not yet useful) state, matching how course/module creation
/// already works in this codebase (create shell, then navigate to a full editor).
/// </summary>
public sealed record CreateQuizCommand(
    Guid ModuleId, string Title, int PassingScorePercent, bool IsRequiredForCompletion) : ICommand<Guid>;

public sealed class CreateQuizCommandValidator : AbstractValidator<CreateQuizCommand>
{
    public CreateQuizCommandValidator()
    {
        RuleFor(c => c.ModuleId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.PassingScorePercent).InclusiveBetween(0, 100);
    }
}

public sealed class CreateQuizCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<CreateQuizCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateQuizCommand command, CancellationToken cancellationToken)
    {
        var module = await dbContext.Modules.SingleOrDefaultAsync(m => m.Id == command.ModuleId, cancellationToken);
        if (module is null)
        {
            return Result.Failure<Guid>(ContentErrors.ModuleNotFound(command.ModuleId));
        }

        var course = await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);
        if (course is null || !CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure<Guid>(ContentErrors.NotCourseOwner);
        }

        var quiz = Quiz.Create(command.ModuleId, command.Title, command.PassingScorePercent, command.IsRequiredForCompletion);
        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(currentUser.UserId, ActivityActions.QuizCreated, "Quiz", quiz.Id.ToString(), cancellationToken);

        return quiz.Id;
    }
}
