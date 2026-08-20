using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Certificates;

public static class CertificateErrors
{
    public static Error NotFound(Guid certificateId) => Error.NotFound(
        "Certificates.NotFound",
        $"Certificate '{certificateId}' was not found.");
}
