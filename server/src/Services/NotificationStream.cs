using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using server.src.Dtos;
using server.src.Services.Interfaces;

namespace server.src.Services;

public class NotificationStream : INotificationStream
{
    private const int SubscriberBufferSize = 100;

    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, Channel<NotificationResponseDto>>> _subscribers = new();

    public async IAsyncEnumerable<NotificationResponseDto?> Subscribe(int userId, TimeSpan heartbeatInterval, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<NotificationResponseDto>(new BoundedChannelOptions(SubscriberBufferSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
        var subscriptionId = Guid.NewGuid();
        var userSubscribers = _subscribers.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Channel<NotificationResponseDto>>());
        userSubscribers[subscriptionId] = channel;

        try
        {
            Task<bool>? waitForItems = null;
            while (true)
            {
                waitForItems ??= channel.Reader.WaitToReadAsync(cancellationToken).AsTask();

                using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var heartbeat = Task.Delay(heartbeatInterval, heartbeatCts.Token);
                var finished = await Task.WhenAny(waitForItems, heartbeat);
                heartbeatCts.Cancel();

                if (finished == heartbeat)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return null;
                    continue;
                }

                if (!await waitForItems)
                {
                    yield break;
                }

                waitForItems = null;
                while (channel.Reader.TryRead(out var notification))
                {
                    yield return notification;
                }
            }
        }
        finally
        {
            userSubscribers.TryRemove(subscriptionId, out _);
            if (userSubscribers.IsEmpty)
            {
                _subscribers.TryRemove(new KeyValuePair<int, ConcurrentDictionary<Guid, Channel<NotificationResponseDto>>>(userId, userSubscribers));
            }
        }
    }

    public void Publish(int userId, NotificationResponseDto notification)
    {
        if (!_subscribers.TryGetValue(userId, out var userSubscribers))
        {
            return;
        }

        foreach (var channel in userSubscribers.Values)
        {
            channel.Writer.TryWrite(notification);
        }
    }
}
