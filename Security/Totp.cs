using System.Security.Cryptography;

namespace EduCore.Security;

// Authenticator-app codes, RFC 6238: HMAC-SHA1 over the count of 30-second steps since the epoch,
// six digits -- the defaults every authenticator app uses. NIST SP 800-63B-4 no longer accepts an
// emailed code as an authenticator; an app code is the second factor that needs no new hardware.
public static class Totp
{
    private const int StepSeconds = 30;
    private const int Digits = 6;

    // One step either side, so a phone clock 30 seconds out still works.
    private const int Window = 1;

    public static byte[] NewSecret() => RandomNumberGenerator.GetBytes(20);

    // The step the code matched, or null. The caller stores it and refuses any step at or before
    // the last one used, so a code seen over someone's shoulder cannot be replayed.
    public static long? Verify(byte[] secret, string code, DateTimeOffset now, long? lastUsedStep)
    {
        if (code.Length != Digits || !code.All(char.IsAsciiDigit))
            return null;

        var current = now.ToUnixTimeSeconds() / StepSeconds;

        for (var step = current - Window; step <= current + Window; step++)
        {
            if (step <= lastUsedStep)
                continue;

            if (CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.ASCII.GetBytes(Code(secret, step)),
                    System.Text.Encoding.ASCII.GetBytes(code)))
                return step;
        }

        return null;
    }

    public static string Code(byte[] secret, long step)
    {
        Span<byte> counter = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counter, step);

        Span<byte> hash = stackalloc byte[20];
        HMACSHA1.HashData(secret, counter, hash);

        // Dynamic truncation, RFC 4226 section 5.3.
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];

        return (binary % 1_000_000).ToString("D6");
    }

    // The link an authenticator app reads (from a QR code, or tapped on the phone itself).
    public static string Uri(string base32Secret, string account) =>
        $"otpauth://totp/EduCore:{System.Uri.EscapeDataString(account)}" +
        $"?secret={base32Secret}&issuer=EduCore&algorithm=SHA1&digits={Digits}&period={StepSeconds}";

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string ToBase32(byte[] bytes)
    {
        var output = new System.Text.StringBuilder((bytes.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;

        foreach (var b in bytes)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                output.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
            output.Append(Alphabet[(buffer << (5 - bits)) & 31]);

        return output.ToString();
    }

    public static byte[] FromBase32(string text)
    {
        var output = new List<byte>(text.Length * 5 / 8);
        int buffer = 0, bits = 0;

        foreach (var c in text.TrimEnd('=').ToUpperInvariant())
        {
            var value = Alphabet.IndexOf(c);
            if (value < 0)
                throw new FormatException("Not a base32 secret.");

            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }

        return output.ToArray();
    }
}
