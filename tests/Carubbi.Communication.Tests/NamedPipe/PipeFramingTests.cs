using System.IO;
using Carubbi.Communication.NamedPipe;

namespace Carubbi.Communication.Tests.NamedPipe;

public class PipeFramingTests
{
    private MemoryStream _stream = null!;

    private void CreateStream() => _stream = new MemoryStream();

    private void ResetStream()
    {
        _stream.Position = 0;
    }

    [Test]
    public async Task WriteReadFrame_When_GivenPayload_Then_ReturnsSameBytes()
    {
        CreateStream();
        var payload = new byte[] { 1, 2, 3, 4, 255, 0, 42 };

        PipeFraming.WriteFrame(_stream, payload);
        ResetStream();

        var result = PipeFraming.ReadFrame(_stream);

        await Assert.That(result).IsEquivalentTo(payload);
    }

    [Test]
    public async Task WriteReadFrame_When_MultipleFrames_Then_ReturnsInOrder()
    {
        CreateStream();
        var first = new byte[] { 10, 20 };
        var second = System.Text.Encoding.UTF8.GetBytes("hello");

        PipeFraming.WriteFrame(_stream, first);
        PipeFraming.WriteFrame(_stream, second);
        ResetStream();

        var firstRead = PipeFraming.ReadFrame(_stream);
        var secondRead = PipeFraming.ReadFrame(_stream);

        await Assert.That(firstRead).IsEquivalentTo(first);
        await Assert.That(secondRead).IsEquivalentTo(second);
    }

    [Test]
    public async Task WriteReadFrame_When_EmptyPayload_Then_ReturnsEmptyArray()
    {
        CreateStream();
        var payload = Array.Empty<byte>();

        PipeFraming.WriteFrame(_stream, payload);
        ResetStream();

        var result = PipeFraming.ReadFrame(_stream);

        await Assert.That(result.Length).IsEqualTo(0);
    }

    [Test]
    public async Task ReadFrame_When_StreamHasNoData_Then_Throws()
    {
        CreateStream();

        await Assert.That(() => PipeFraming.ReadFrame(_stream)).Throws<EndOfStreamException>();
    }

    [Test]
    public async Task ReadFrame_When_NegativeLengthPrefix_Then_ThrowsIOException()
    {
        CreateStream();
        var length = BitConverter.GetBytes(-1);
        await _stream.WriteAsync(length, 0, length.Length);
        ResetStream();

        await Assert.That(() => PipeFraming.ReadFrame(_stream)).Throws<IOException>();
    }
}
