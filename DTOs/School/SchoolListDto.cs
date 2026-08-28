using EduCore.Enums;

namespace EduCore.Dtos.School;

// DeactivatedAt is here but deliberately NOT on SchoolEditDto: switching a school off is its
// own action with its own endpoint, exactly as deactivating a user account is. Carrying it on
// the edit DTO would mean every plain rename could silently flip it, and a field a form did
// not render could be sent by anything that could reach the endpoint.
public record SchoolListDto(
    Guid Id,
    string Name,
    SchoolType Type,
    string? City,
    string? Zone,
    DateTime? DeactivatedAt);
