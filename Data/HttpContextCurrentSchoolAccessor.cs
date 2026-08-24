using Microsoft.AspNetCore.Http;

namespace EduCore.Data;

// Reads the acting user's school from a "school_id" claim, mirroring how
// AuditSaveChangesInterceptor reads the user id from ClaimTypes.NameIdentifier.
// No login flow issues this claim yet -- until it does, CurrentSchoolId is null and every
// school-scoped query correctly returns nothing rather than everything.
public class HttpContextCurrentSchoolAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentSchoolAccessor
{
    public Guid? CurrentSchoolId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst("school_id")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }
}
