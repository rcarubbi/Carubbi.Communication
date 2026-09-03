using Carubbi.Communication.Serialization;
using ProtoBuf;

namespace Carubbi.Communication.Tests.Serialization;

[ProtoContract]
public sealed class TestMessage : IEquatable<TestMessage>
{
    [ProtoMember(1)]
    public string Name { get; set; } = string.Empty;

    [ProtoMember(2)]
    public int Count { get; set; }

    [ProtoMember(3)]
    public decimal Amount { get; set; }

    [ProtoMember(4)]
    public bool Enabled { get; set; }

    [ProtoMember(5)]
    public DateTime When { get; set; }

    [ProtoMember(6)]
    public Guid Id { get; set; }

    [ProtoMember(7)]
    public List<string> Tags { get; set; } = [];

    public bool Equals(TestMessage? other)
        => other is not null
            && Name == other.Name
            && Count == other.Count
            && Amount == other.Amount
            && Enabled == other.Enabled
            && When == other.When
            && Id == other.Id
            && Tags.SequenceEqual(other.Tags);

    public override bool Equals(object? obj) => Equals(obj as TestMessage);

    public override int GetHashCode() => HashCode.Combine(Name, Count, Amount, Enabled, When, Id);
}

public class MessageSerializerTests
{
    private IMessageSerializer<TestMessage> _sut = null!;

    private void CreateSut(MessageFormat format)
        => _sut = MessageSerializerFactory.Create<TestMessage>(format);

    private static TestMessage CreateOriginal()
        => new()
        {
            Name = "Carubbi",
            Count = 42,
            Amount = 1234.56m,
            Enabled = true,
            When = new DateTime(2024, 5, 1, 10, 30, 0, DateTimeKind.Utc),
            Id = Guid.NewGuid(),
            Tags = ["alpha", "beta", "gamma"]
        };

    [Test]
    [Arguments(MessageFormat.Xml)]
    [Arguments(MessageFormat.Json)]
    [Arguments(MessageFormat.Binary)]
    [Arguments(MessageFormat.Protobuf)]
    public async Task RoundTrip_When_GivenFullMessage_Then_ReturnsEqualMessage(MessageFormat format)
    {
        CreateSut(format);
        var original = CreateOriginal();

        var payload = _sut.Serialize(original);
        var result = _sut.Deserialize(payload);

        await Assert.That(payload).IsNotEmpty();
        await Assert.That(result).IsEqualTo(original);
    }

    [Test]
    [Arguments(MessageFormat.Xml)]
    [Arguments(MessageFormat.Json)]
    [Arguments(MessageFormat.Binary)]
    [Arguments(MessageFormat.Protobuf)]
    public async Task Serialize_When_GivenMessageWithNoCollection_Then_ReturnsRoundTrippableData(MessageFormat format)
    {
        CreateSut(format);
        var original = new TestMessage { Name = "Minimal", Count = 1, Tags = [] };

        var result = _sut.Deserialize(_sut.Serialize(original));

        await Assert.That(result).IsEqualTo(original);
    }
}
