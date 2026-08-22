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

namespace TrainingPlatform.Application.Quizzes.DeleteQuiz;

public sealed record DeleteQuizCommand(Guid QuizId) : ICommand;

public sealed class DeleteQuizCommandValidator : AbstractValidator<DeleteQuizCommand>
{
    public DeleteQuizCommandValidator()
    {
        RuleFor(c => c.QuizId).NotEmpty();
    }
}

public sealed class DeleteQuizCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<DeleteQuizCommand>
{
    public async Task<Result> Handle(DeleteQuizCommand command, CancellationToken cancellationToken)
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

        dbContext.Quizzes.Remove(quiz);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(currentUser.UserId, ActivityActions.QuizDeleted, "Quiz", quiz.Id.ToString(), cancellationToken);

        return Result.Success();
    }
}
