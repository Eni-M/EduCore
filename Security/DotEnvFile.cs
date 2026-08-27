namespace EduCore.Security;

// Reads a .env file into environment variables, before configuration is built.
//
// The secrets this app needs -- the JWT signing key, the database password, the
// first super administrator's credentials -- used to sit in appsettings.json,
// which is committed. Anyone who could read the repository could mint a
// SuperAdmin token for any school, because the signing key is the whole of the
// app's security: every authorization decision is made from claims, and claims
// are only worth anything because that key is secret.
//
// So .env holds them instead, and .gitignore keeps it out of the repository.
// .env.example is the committed copy, with the names and no values.
//
// The file format is deliberately the simplest thing that works: one
// NAME=value per line, blank lines and lines starting with # ignored, no quotes
// and no substitution. Names use the double underscore that .NET already reads
// as a section separator, so Jwt__Key here becomes Jwt:Key in configuration and
// nothing else in the app has to know where the value came from.
public static class DotEnvFile
{
    public static void Load(string path)
    {
        if (!File.Exists(path)) return;

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();

            if (trimmed == "" || trimmed.StartsWith("#")) continue;

            // Split on the first = only, so a value may contain more of them --
            // a connection string does.
            var parts = trimmed.Split('=', 2);
            if (parts.Length != 2) continue;

            var name = parts[0].Trim();
            var value = parts[1].Trim();

            // Set only what is not already set. A real environment variable wins,
            // which is how a deployment supplies its secrets the normal way and
            // never ships a .env at all -- and how a stale file on a server
            // cannot quietly override it.
            if (Environment.GetEnvironmentVariable(name) == null)
                Environment.SetEnvironmentVariable(name, value);
        }
    }
}
