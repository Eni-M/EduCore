namespace EduCore.Security;

public static class DotEnvFile
{
    public static void Load(string path)
    {
        if (!File.Exists(path)) return;

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();

            if (trimmed == "" || trimmed.StartsWith("#")) continue;

            var parts = trimmed.Split('=', 2);
            if (parts.Length != 2) continue;

            var name = parts[0].Trim();
            var value = parts[1].Trim();

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
                Environment.SetEnvironmentVariable(name, value);
        }
    }
}
