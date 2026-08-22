using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content;
using TrainingPlatform.Application.Quizzes.Contracts;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Quizzes;

namespace TrainingPlatform.Application.Quizzes.GetQuizForManagement;

/// <summary>Trainer/Admin builder view — includes IsCorrect per choice, unlike the trainee-facing
/// GetQuizForAttemptQuery.</summary>
public sealed record GetQuizForManagementQuery(Guid QuizId) : IQuery<QuizManagementDetails>;

public sealed class GetQuizForManagementQueryHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : IQueryHandler<GetQuizForManagementQuery, QuizManagementDetails>
{
    public async Task<Result<QuizManagementDetails>> Handle(
        GetQuizForManagementQuery query, CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes.AsNoTracking()
            .SingleOrDefaultAsync(q => q.Id == query.QuizId, cancellationToken);
        if (quiz is null)
        {
            return Result.Failure<QuizManagementDetails>(QuizErrors.QuizNotFound(query.QuizId));
        }

        var module = await dbContext.Modules.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == quiz.ModuleId, cancellationToken);
        var course = module is null
            ? null
            : await dbContext.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);

        if (course is null || !CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure<QuizManagementDetails>(ContentErrors.NotCourseOwner);
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

        var questionDetails = questions
            .Select(q => new QuestionManagementDetails(
                q.Id,
                q.Text,
                q.Order,
                choices
                    .Where(c => c.QuestionId == q.Id)
                    .Select(c => new ChoiceManagementDetails(c.Id, c.Text, c.IsCorrect, c.Order))
                    .ToList()))
            .ToList();

        return new QuizManagementDetails(
            quiz.Id, quiz.ModuleId, quiz.Title, quiz.PassingScorePercent, quiz.IsRequiredForCompletion, questionDetails);
    }
}
