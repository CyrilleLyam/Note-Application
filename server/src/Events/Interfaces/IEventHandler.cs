namespace server.src.Events.Interfaces;

public interface IEventHandler<in TEvent> where TEvent : notnull
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
