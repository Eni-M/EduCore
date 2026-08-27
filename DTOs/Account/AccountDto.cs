namespace EduCore.Dtos.Account;

// The sign-in behind a person, for the screens that manage it.
//
// A Student, Teacher and Admin each own a User row, and until now nothing but
// the login response ever said so. That left the deactivate and reset-password
// endpoints unreachable from any list: they take a user id, and no list carried
// one -- so the API could deactivate an account and no screen could ask it to.
//
// Why one record shared by three lists rather than three near-identical ones:
// these two fields mean exactly the same thing in every list that carries them,
// and the moment they are copied is the moment one of the copies stops matching
// the endpoint it feeds.
//
// Why it hangs off the existing list DTOs as a nullable member, rather than
// being a second "…WithAccountDto" record per entity: the rows are the same
// rows, the columns are the same columns, and only this block is privileged.
// Null means "not yours to see" -- one shape for the client to hold, and the
// question "may I manage this person" answered by whether the block is there.
// A separate record earns its keep when the audience wants a genuinely
// different screen, not when it wants the same screen minus a field.
//
// DeactivatedAt is the timestamp rather than a bool because "since when" is what
// an administrator looking at a disabled account actually wants to know, and the
// bool is one comparison away.
public record AccountDto(Guid UserId, DateTime? DeactivatedAt);
