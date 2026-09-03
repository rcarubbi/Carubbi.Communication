using System.Text.Json;

namespace Carubbi.Communication.Serialization;

internal sealed class JsonMessageSerializer<T> : IMessageSerializer<T>
    where T : class
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General);

    public byte[] Serialize(T message) => JsonSerializer.SerializeToUtf8Bytes(message, Options);

    public T Deserialize(byte[] data)
        => JsonSerializer.Deserialize<T>(data, Options)
           ?? throw new InvalidOperationException("The JSON payload could not be deserialized.");
}
