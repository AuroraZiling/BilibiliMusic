namespace BiliBiliMusic.Core.Models;

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