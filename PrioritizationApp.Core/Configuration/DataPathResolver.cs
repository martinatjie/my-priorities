namespace PrioritizationApp.Configuration;

public static class DataPathResolver
{
    public static string Resolve(string? configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? GetDefaultPath()
            : ExpandPath(configuredPath);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        return path;
    }

    public static string GetDefaultPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PrioritizationApp",
            "prioritization-data.json");

    private static string ExpandPath(string path)
    {
        path = Environment.ExpandEnvironmentVariables(path);

        if (path.StartsWith("~/", StringComparison.Ordinal) ||
            path.StartsWith("~\\", StringComparison.Ordinal))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(home, path[2..]);
        }

        return Path.GetFullPath(path);
    }
}
