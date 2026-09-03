using Carubbi.Communication.NamedPipe;
using NSubstitute;

namespace Carubbi.Communication.Tests.NamedPipe;

public class UnsubscriberTests
{
    private readonly List<IObserver<string>> _subscribers = [];
    private IObserver<string> _subscriber = null!;
    private Unsubscriber<string> _sut = null!;

    private void CreateSut(IObserver<string> subscriber)
    {
        _subscriber = subscriber;
        _subscribers.Add(subscriber);
        _sut = new Unsubscriber<string>(_subscribers, subscriber);
    }

    [Test]
    public async Task Dispose_When_SubscriberPresent_Then_RemovesItFromList()
    {
        var subscriber = Substitute.For<IObserver<string>>();
        CreateSut(subscriber);

        _sut.Dispose();

        await Assert.That(_subscribers).DoesNotContain(subscriber);
    }

    [Test]
    public async Task Dispose_When_SubscriberNotInList_Then_DoesNotThrow()
    {
        var subscriber = Substitute.For<IObserver<string>>();
        CreateSut(subscriber);
        _subscribers.Remove(_subscriber);

        _sut.Dispose();

        await Assert.That(_subscribers.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Dispose_When_CalledTwice_Then_DoesNotThrow()
    {
        var subscriber = Substitute.For<IObserver<string>>();
        CreateSut(subscriber);

        _sut.Dispose();
        _sut.Dispose();

        await Assert.That(_subscribers).DoesNotContain(subscriber);
    }
}
