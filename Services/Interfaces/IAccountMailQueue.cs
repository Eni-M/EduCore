using EduCore.Services.Email;

namespace EduCore.Services.Interfaces;

public interface IAccountMailQueue
{
    // Never throws and never blocks: the caller is inside a save, and a bad mail server must
    // not fail an account creation.
    void Enqueue(AccountMailRequest request);

    IAsyncEnumerable<AccountMailRequest> ReadAllAsync(CancellationToken ct);
}
