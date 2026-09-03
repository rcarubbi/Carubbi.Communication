using Carubbi.Communication.NamedPipe;
using NSubstitute;

namespace Carubbi.Communication.Tests.NamedPipe;

public class ClientTests
{
    private Client<string, string> _sut = null!;

    private void CreateSut() => _sut = new Client<string, string>("ClientTests");

    [Test]
    public async Task Subscribe_When_ValidObserver_Then_ReturnsSubscription()
    {
        CreateSut();
        var subscriber = Substitute.For<IObserver<string>>();

        var subscription = _sut.Subscribe(subscriber);

        await Assert.That(subscription).IsNotNull();
    }

    [Test]
    public async Task SendRequest_When_NotConnected_Then_ThrowsInvalidOperationException()
    {
        CreateSut();

        await Assert.That(() => _sut.SendRequest(["message"])).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Dispose_When_NeverConnected_Then_DoesNotThrow()
    {
        CreateSut();

        _sut.Dispose();

        var subscription = _sut.Subscribe(Substitute.For<IObserver<string>>());
        await Assert.That(subscription).IsNotNull();
    }

    [Test]
    public async Task Dispose_When_NeverConnectedAndCalledTwice_Then_DoesNotThrow()
    {
        CreateSut();

        _sut.Dispose();
        _sut.Dispose();

        var subscription = _sut.Subscribe(Substitute.For<IObserver<string>>());
        await Assert.That(subscription).IsNotNull();
    }
}
