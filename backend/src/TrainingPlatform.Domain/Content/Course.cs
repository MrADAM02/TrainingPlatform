namespace TrainingPlatform.Domain.Content;

public sealed class Course
{
    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    /// <summary>The owning trainer's user id. No FK navigation — <c>ApplicationUser</c> lives in
    /// Infrastructure (Identity), which Domain must not depend on.</summary>
    public Guid TrainerId { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private Course()
    {
    }

    public static Course Create(string title, string description, Guid trainerId)
    {
        return new Course
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            TrainerId = trainerId,
            IsPublished = false,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void UpdateDetails(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public void Publish() => IsPublished = true;

    public void Unpublish() => IsPublished = false;
}
