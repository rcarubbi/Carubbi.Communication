namespace Carubbi.Communication.NamedPipe;

public sealed class Unsubscriber<TResponseMessage> : IDisposable
    where TResponseMessage : class
{
    private readonly List<IObserver<TResponseMessage>> _subscribers;
    private readonly IObserver<TResponseMessage> _subscriber;

    internal Unsubscriber(List<IObserver<TResponseMessage>> subscribers, IObserver<TResponseMessage> subscriber)
    {
        _subscribers = subscribers;
        _subscriber = subscriber;
    }

    public void Dispose()
    {
        if (_subscribers.Contains(_subscriber))
        {
            _subscribers.Remove(_subscriber);
        }
    }
}
