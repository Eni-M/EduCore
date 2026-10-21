using System.Collections.Concurrent;
using System.Threading.Channels;
using EduCore.Enums;

namespace EduCore.Services;

// Wakes the open bell streams (GET /api/notifications/stream) when a role's unread count in a
// school may have changed. A wake carries no data: each stream recounts from the database, so a
// coalesced, extra or late wake is harmless -- the database stays the only source of truth.
// ponytail: in-process only. With more than one API replica, a write on one replica does not wake
// streams held by another until they reconnect; move to Postgres LISTEN/NOTIFY if replicas are added.
public class NotificationSignals
{
    private readonly ConcurrentDictionary<Channel<bool>, (UserRole Role, IReadOnlyCollection<Guid> SchoolIds)> _streams = new();

    public Channel<bool> Subscribe(UserRole role, IReadOnlyCollection<Guid> schoolIds)
    {
        // Capacity one, extra writes dropped: ten changes before the stream wakes are one recount.
        var channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });
        _streams[channel] = (role, schoolIds);
        return channel;
    }

    public void Unsubscribe(Channel<bool> channel) => _streams.TryRemove(channel, out _);

    public void Changed(Guid schoolId, UserRole role)
    {
        foreach (var (channel, scope) in _streams)
            if (scope.Role == role && scope.SchoolIds.Contains(schoolId))
                channel.Writer.TryWrite(true);
    }
}
