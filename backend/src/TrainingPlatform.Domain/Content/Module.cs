namespace TrainingPlatform.Domain.Content;

public sealed class Module
{
    public Guid Id { get; private set; }

    public Guid CourseId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public int Order { get; private set; }

    private Module()
    {
    }

    public static Module Create(Guid courseId, string title, int order)
    {
        return new Module
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = title,
            Order = order,
        };
    }

    public void UpdateDetails(string title, int order)
    {
        Title = title;
        Order = order;
    }
}
