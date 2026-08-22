using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Quizzes.Contracts;
using TrainingPlatform.Application.Quizzes.CreateQuiz;
using TrainingPlatform.Application.Quizzes.DeleteQuiz;
using TrainingPlatform.Application.Quizzes.GetQuizForAttempt;
using TrainingPlatform.Application.Quizzes.GetQuizForManagement;
using TrainingPlatform.Application.Quizzes.SubmitQuizAttempt;
using TrainingPlatform.Application.Quizzes.UpdateQuiz;

namespace TrainingPlatform.Api.Endpoints.Quizzes;

public static class QuizEndpoints
{
    public static IEndpointRouteBuilder MapQuizEndpoints(this IEndpointRouteBuilder app)
    {
        var moduleQuizzes = app.MapGroup("/api/v1/modules/{moduleId:guid}/quizzes")
            .WithTags("Quizzes")
            .RequireAuthorization("RequireTrainerOrAdministrator");

        moduleQuizzes.MapPost("/", async (Guid moduleId, CreateQuizRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateQuizCommand(moduleId, request.Title, request.PassingScorePercent, request.IsRequiredForCompletion);
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/quizzes/{result.Value}", new { id = result.Value })
                : CustomResults.Problem(result);
        });

        var manageQuizzes = app.MapGroup("/api/v1/quizzes")
            .WithTags("Quizzes")
            .RequireAuthorization("RequireTrainerOrAdministrator");

        manageQuizzes.MapGet("/{id:guid}/manage", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetQuizForManagementQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        manageQuizzes.MapPut("/{id:guid}", async (Guid id, UpdateQuizRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateQuizCommand(
                id, request.Title, request.PassingScorePercent, request.IsRequiredForCompletion, request.Questions);
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        manageQuizzes.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteQuizCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        var attempts = app.MapGroup("/api/v1/quizzes")
            .WithTags("Quizzes")
            .RequireAuthorization();

        attempts.MapGet("/{id:guid}/attempt", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetQuizForAttemptQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        attempts.MapPost("/{id:guid}/attempts", async (Guid id, SubmitQuizAttemptRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitQuizAttemptCommand(id, request.Answers), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        return app;
    }
}

public sealed record CreateQuizRequest(string Title, int PassingScorePercent, bool IsRequiredForCompletion);

public sealed record UpdateQuizRequest(
    string Title,
    int PassingScorePercent,
    bool IsRequiredForCompletion,
    IReadOnlyList<QuestionInput> Questions);

public sealed record SubmitQuizAttemptRequest(IReadOnlyList<AnswerInput> Answers);
