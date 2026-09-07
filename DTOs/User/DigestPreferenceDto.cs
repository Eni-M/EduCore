namespace EduCore.Dtos.User;

/// <summary>
/// True to stop receiving scheduled digest emails -- the daily schedule, the Sunday week-ahead,
/// and the guardian weekly summary. Transactional mail (password reset, the notice that a
/// password changed) is not covered and cannot be switched off: it is security correspondence,
/// not a newsletter.
/// </summary>
public record DigestPreferenceDto(bool OptOut);
