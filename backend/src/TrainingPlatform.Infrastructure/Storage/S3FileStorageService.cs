using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using TrainingPlatform.Application.Abstractions.Storage;

namespace TrainingPlatform.Infrastructure.Storage;

public sealed class S3FileStorageService(IAmazonS3 s3Client, IOptions<StorageOptions> options) : IFileStorageService
{
    private readonly StorageOptions _options = options.Value;
    private static readonly TimeSpan UploadUrlLifetime = TimeSpan.FromMinutes(15);
    // Long enough to cover a full video-watching session (including pauses/seeks, which each
    // re-validate the signature against Expires) without being unbounded — presigned URLs are
    // unauthenticated bearer capabilities once issued.
    private static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromHours(1);

    // GetPreSignedUrlRequest.Protocol controls the generated URL's scheme independently of the
    // client's UseHttp setting — without this it's always signed as https://, which breaks
    // against a plain-HTTP local MinIO.
    private Protocol UrlProtocol =>
        new Uri(_options.ServiceUrl).Scheme == Uri.UriSchemeHttp ? Protocol.HTTP : Protocol.HTTPS;

    public async Task<string> GetUploadUrlAsync(string storageKey, string contentType, CancellationToken cancellationToken)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            Protocol = UrlProtocol,
            Expires = DateTime.UtcNow.Add(UploadUrlLifetime),
            ContentType = contentType,
        };

        return await s3Client.GetPreSignedURLAsync(request);
    }

    public async Task<string> GetDownloadUrlAsync(string storageKey, CancellationToken cancellationToken)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Protocol = UrlProtocol,
            Expires = DateTime.UtcNow.Add(DownloadUrlLifetime),
        };

        return await s3Client.GetPreSignedURLAsync(request);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        await s3Client.DeleteObjectAsync(_options.BucketName, storageKey, cancellationToken);
    }
}
