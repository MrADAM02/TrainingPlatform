namespace TrainingPlatform.Domain.Quizzes;

/// <summary>
/// Single member for now (REQ-QUIZ-01 ships single-choice questions only) — reserved as its own
/// column/enum rather than hard-coded so multiple-choice can be added later without a breaking
/// migration, mirroring how <c>Domain.Content.DocumentType</c> is declared.
/// </summary>
public enum QuestionType
{
    SingleChoice = 0,
}
