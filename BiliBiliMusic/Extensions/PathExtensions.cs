using System.IO;
using System.Linq;

namespace BiliBiliMusic.Extensions;

public static class PathExtensions
{
    public static string? GetValidFileName(this string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var invalidChars = Path.GetInvalidFileNameChars();
        path = invalidChars.Aggregate(path, (current, c) => current.Replace(c, '_'));

        const int maxLength = 255;
        return path.Length is > maxLength or 0 ? null : path;
    }
}