using ProtoBuf;

namespace Carubbi.Communication.Serialization;

internal sealed class ProtobufMessageSerializer<T> : IMessageSerializer<T>
    where T : class
{
    public byte[] Serialize(T message)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, message);
        return stream.ToArray();
    }

    public T Deserialize(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return Serializer.Deserialize<T>(stream);
    }
}
