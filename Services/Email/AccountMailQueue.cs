using System.Threading.Channels;

namespace EduCore.Services.Email;

public interface IAccountMailQueue
{
    /// <summary>
    /// Queues an account email. Never throws and never blocks: the caller is inside a database
    /// save, and a mail server having a bad afternoon must not fail somebody's account creation.
    /// </summary>
    void Enqueue(AccountMailRequest request);

    IAsyncEnumerable<AccountMailRequest> ReadAllAsync(CancellationToken ct);
}

/// <summary>
/// A hand-off between the write that causes an email and the background service that sends it.
///
/// The alternative -- sending inline from the save -- puts an SMTP round trip, typically the best
/// part of a second, into the request that created the account, and makes a mail outage look to
/// the user like a failure to create the student.
///
/// Bounded on purpose. An unbounded channel turns a stuck sender into a memory leak that ends as
/// a crash; this drops the oldest queued mail instead and says so in the log. Losing a welcome
/// email is a far better outcome than losing the process.
///
/// The queue is in memory, so anything still waiting is lost on restart. That is an accepted
/// trade for these messages -- none of them is the only copy of anything, and the reset flow
/// covers the case where a welcome mail never arrives. Anything that must survive a restart
/// needs a database-backed outbox instead, which this is deliberately not.
/// </summary>
public class AccountMailQueue(ILogger<AccountMailQueue> logger) : IAccountMailQueue
{
    private readonly Channel<AccountMailRequest> _channel =
        Channel.CreateBounded<AccountMailRequest>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public void Enqueue(AccountMailRequest request)
    {
        if (!_channel.Writer.TryWrite(request))
            logger.LogWarning(
                "Account mail queue rejected a {Kind} message; it was not sent.", request.Kind);
    }

    public IAsyncEnumerable<AccountMailRequest> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
