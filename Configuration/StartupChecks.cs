namespace EduCore.Configuration;

public static class StartupChecks
{
    public static IEnumerable<string> TokenAndScheduleSettings(IConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config["Jwt:Issuer"]))
            yield return "Jwt:Issuer must be set: tokens are issued and validated against it.";
        if (string.IsNullOrWhiteSpace(config["Jwt:Audience"]))
            yield return "Jwt:Audience must be set: tokens are issued and validated against it.";

        var skew = config.GetValue("Jwt:ClockSkewMinutes", 1);
        if (skew is < 0 or > 10)
            yield return $"Jwt:ClockSkewMinutes ({skew}) must be between 0 and 10; it extends every token's life.";

        var expiry = config.GetValue("Jwt:ExpiryMinutes", 60);
        if (expiry is < 5 or > 1440)
            yield return $"Jwt:ExpiryMinutes ({expiry}) must be between 5 and 1440.";

        var runHour = config.GetValue("ScheduledEmail:RunHourUtc", 17);
        if (runHour is < 0 or > 23)
            yield return $"ScheduledEmail:RunHourUtc ({runHour}) must be an hour of the day, 0 to 23.";
    }
}
