namespace TrainingPlatform.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public required string BucketName { get; set; }

    public required string ServiceUrl { get; set; }

    public required string AccessKey { get; set; }

    public required string SecretKey { get; set; }

    public string Region { get; set; } = "us-east-1";

    /// <summary>MinIO and Hetzner Object Storage both use path-style addressing
    /// (host/bucket/key) rather than AWS's default virtual-hosted style (bucket.host/key).</summary>
    public bool ForcePathStyle { get; set; } = true;
}
