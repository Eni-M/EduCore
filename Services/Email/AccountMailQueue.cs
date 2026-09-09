using System.Threading.Channels;
using EduCore.Services.Interfaces;

namespace EduCore.Services.Email;

// Hands account mail from the write that causes it to the background sender, keeping the SMTP
// round trip out of the request. Bounded and drop-oldest, so a stuck sender cannot grow into a
// crash; in memory, so anything queued is lost on restart. Mail that must survive one needs a
// database-backed outbox, which this deliberately is not.
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
