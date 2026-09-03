using Carubbi.Communication.Serialization;

namespace Carubbi.Communication.Tests.Serialization;

public class MessageSerializerFactoryTests
{
    [Test]
    [Arguments(MessageFormat.Xml)]
    [Arguments(MessageFormat.Json)]
    [Arguments(MessageFormat.Binary)]
    [Arguments(MessageFormat.Protobuf)]
    public async Task Create_When_GivenSupportedFormat_Then_ReturnsSerializer(MessageFormat format)
    {
        var serializer = MessageSerializerFactory.Create<TestMessage>(format);

        await Assert.That(serializer).IsNotNull();
    }

    [Test]
    public async Task Create_When_GivenUndefinedFormat_Then_ThrowsArgumentOutOfRange()
    {
        var undefined = (MessageFormat)999;

        await Assert.That(() => MessageSerializerFactory.Create<TestMessage>(undefined))
            .Throws<ArgumentOutOfRangeException>();
    }
}
