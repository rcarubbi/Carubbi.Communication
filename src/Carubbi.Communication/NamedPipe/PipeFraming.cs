using System.IO;

namespace Carubbi.Communication.NamedPipe;

internal static class PipeFraming
{
    private const int LengthPrefixSize = sizeof(int);

    public static void WriteFrame(Stream stream, byte[] payload)
    {
        var lengthPrefix = BitConverter.GetBytes(payload.Length);
        stream.Write(lengthPrefix, 0, LengthPrefixSize);
        stream.Write(payload, 0, payload.Length);
        stream.Flush();
    }

    public static byte[] ReadFrame(Stream stream)
    {
        var lengthPrefix = new byte[LengthPrefixSize];
        stream.ReadExactly(lengthPrefix, 0, LengthPrefixSize);

        int length = BitConverter.ToInt32(lengthPrefix, 0);
        if (length < 0)
        {
            throw new IOException("Invalid frame length received from the pipe stream.");
        }

        var payload = new byte[length];
        stream.ReadExactly(payload, 0, length);
        return payload;
    }
}
