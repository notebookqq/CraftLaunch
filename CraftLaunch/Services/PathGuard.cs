using System.IO;

namespace CraftLaunch.Services;

public static class PathGuard
{
    public static string SafeJoin(string baseDirectory, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new IOException("路径为空，已拒绝");

        var baseFull = Path.GetFullPath(baseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(baseDirectory, relativePath));

        if (!full.StartsWith(baseFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(full, baseFull, StringComparison.OrdinalIgnoreCase))
            throw new IOException($"检测到路径穿越，已拒绝: {relativePath}");

        return full;
    }

    public static string SafeJoinRelative(string baseDirectory, string relativePath)
    {
        var full = SafeJoin(baseDirectory, relativePath);
        var baseFull = Path.GetFullPath(baseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return full.Substring(baseFull.Length + 1);
    }

    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "file.bin";

        fileName = Path.GetFileName(fileName.Replace('\\', '/'));

        foreach (var c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');

        fileName = fileName.Trim().TrimEnd('.');

        var upper = fileName.ToUpperInvariant();
        foreach (var reserved in new[] { "CON", "PRN", "AUX", "NUL" })
        {
            if (upper == reserved || upper.StartsWith(reserved + "."))
                fileName = "_" + fileName;
        }

        if (fileName.Length == 0)
            fileName = "file.bin";
        return fileName;
    }

    public static bool IsSha1Hex(string? value)
    {
        if (value is null || value.Length != 40)
            return false;
        foreach (var c in value)
        {
            if (!Uri.IsHexDigit(c))
                return false;
        }
        return true;
    }
}
