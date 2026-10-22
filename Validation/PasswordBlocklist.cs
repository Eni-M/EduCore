namespace EduCore.Validation;

// NIST SP 800-63B-4 asks for new passwords to be checked against values known to be commonly
// used, expected or compromised. At 15 characters and up, what people actually pick is mostly
// keyboard walks, counting, a word typed twice, or one character held down -- so this catches
// those shapes plus a short list of the long passwords that top breach corpora.
// ponytail: static list and shape checks only; check the Pwned Passwords k-anonymity API if a
// fuller breach check is ever wanted.
public static class PasswordBlocklist
{
    private static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
    {
        "passwordpassword", "password12345678", "password123456789", "iloveyouiloveyou",
        "1q2w3e4r5t6y7u8i", "1qaz2wsx3edc4rfv", "zaq12wsxcde34rfv", "qazwsxedcrfvtgb",
        "qazwsxedcrfvtgbyhn", "welcome123456789", "administrator123", "changemechangeme",
        "letmeinletmein12", "trustno1trustno1", "football12345678", "princessprincess",
        "educoreeducore12", "educore123456789", "educorepassword", "mypasswordis1234",
        "thisismypassword", "correcthorsebatterystaple", "monkeymonkeymonkey",
    };

    private static readonly string[] Runs =
    [
        "abcdefghijklmnopqrstuvwxyz",
        "01234567890123456789",
        "qwertyuiopasdfghjklzxcvbnm",
        "1qaz2wsx3edc4rfv5tgb6yhn7ujm8ik9ol0p",
        "qazwsxedcrfvtgbyhnujmikolp",
    ];

    public static bool IsBlocked(string password)
    {
        var value = password.ToLowerInvariant();

        if (Common.Contains(value) || value.Distinct().Count() < 5)
            return true;

        // Counting or walking the keyboard, either direction.
        foreach (var run in Runs)
            if (run.Contains(value) || Reverse(run).Contains(value))
                return true;

        // The same short unit typed over and over: "abcabcabcabcabc", "hello1hello1hello1".
        for (var unit = 1; unit <= value.Length / 2; unit++)
            if (value.Length % unit == 0 && string.Concat(Enumerable.Repeat(value[..unit], value.Length / unit)) == value)
                return true;

        return false;
    }

    private static string Reverse(string s) => new(s.Reverse().ToArray());
}
