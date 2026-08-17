namespace TrainingPlatform.Application.Abstractions.Storage;

public interface IFileStorageService
{
    /// <summary>A short-lived, pre-signed PUT URL — the client uploads directly to object
    /// storage, so large files never pass through the API process (REQ-CONT-03/04).</summary>
    Task<string> GetUploadUrlAsync(string storageKey, string contentType, CancellationToken cancellationToken);

    /// <summary>A short-lived, pre-signed GET URL, only issued after the caller's authorization
    /// has already been checked (REQ-CONT-05).</summary>
    Task<string> GetDownloadUrlAsync(string storageKey, CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
