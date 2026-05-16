namespace ZimMarket.Infrastructure.Persistence;

/// <summary>
/// Applies repository root <c>.env</c> so EF migrations and design-time tools see the same variables as Docker Compose.
/// Does not overwrite keys already set in the environment (shell and process wins over file).
/// </summary>
public static class RepositoryDotEnv
{
    public static void TryApply()
    {
        string? solutionRoot = FindDirectoryContainingFile("ZimMarket.sln");
        if (solutionRoot is null)
            return;

        string path = Path.Combine(solutionRoot, ".env");
        if (!File.Exists(path))
            return;

        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            int eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            string key = line[..eq].Trim();
            if (key.Length == 0)
                continue;

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                continue;

            string value = line[(eq + 1)..].Trim();
            if (value.Length >= 2
                && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string? FindDirectoryContainingFile(string fileName)
    {
        string? directory = Directory.GetCurrentDirectory();
        for (int depth = 0; depth < 12 && directory is not null; depth++)
        {
            if (File.Exists(Path.Combine(directory, fileName)))
                return directory;

            directory = Directory.GetParent(directory)?.FullName;
        }

        return null;
    }
}
