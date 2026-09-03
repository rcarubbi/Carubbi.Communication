namespace Carubbi.Communication.Serialization;

public static class MessageSerializerFactory
{
    public static IMessageSerializer<T> Create<T>(MessageFormat format)
        where T : class
        => format switch
        {
            MessageFormat.Xml => new XmlMessageSerializer<T>(),
            MessageFormat.Json => new JsonMessageSerializer<T>(),
            MessageFormat.Binary => new BinaryMessageSerializer<T>(),
            MessageFormat.Protobuf => new ProtobufMessageSerializer<T>(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
}
