namespace TrainingPlatform.Domain.Quizzes;

/// <summary>
/// QuestionId/SelectedChoiceId are deliberately FK-less snapshots of what was actually selected
/// at attempt time — editing or deleting a question/choice later must never corrupt or cascade
/// into a trainee's already-recorded answer.
/// </summary>
public sealed class QuizAttemptAnswer
{
    public Guid Id { get; private set; }

    public Guid QuizAttemptId { get; private set; }

    public Guid QuestionId { get; private set; }

    public Guid SelectedChoiceId { get; private set; }

    private QuizAttemptAnswer()
    {
    }

    public static QuizAttemptAnswer Create(Guid quizAttemptId, Guid questionId, Guid selectedChoiceId)
    {
        return new QuizAttemptAnswer
        {
            Id = Guid.NewGuid(),
            QuizAttemptId = quizAttemptId,
            QuestionId = questionId,
            SelectedChoiceId = selectedChoiceId,
        };
    }
}
