namespace TrainingPlatform.Application.Content.Contracts;

public sealed record UploadTicket(Guid DocumentId, string UploadUrl, DateTime ExpiresAtUtc);
