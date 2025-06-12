namespace BiliBiliMusic.Models;

public enum ResolveStatus
{
    Validating,
    WaitForResolving,
    Resolving,
    ResolveFailed,
    Downloading,
    Downloaded,
    DownloadFailed
}