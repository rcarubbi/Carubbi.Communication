using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Carubbi.Communication.Serialization;

internal sealed class XmlMessageSerializer<T> : IMessageSerializer<T>
    where T : class
{
    private static readonly XmlSerializer Xml = new(typeof(T));

    public byte[] Serialize(T message)
    {
        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };
        using (var writer = XmlWriter.Create(stream, settings))
        {
            Xml.Serialize(writer, message);
        }

        return stream.ToArray();
    }

    public T Deserialize(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = XmlReader.Create(stream);
        return (T?)Xml.Deserialize(reader) ?? throw new InvalidOperationException("The XML payload could not be deserialized.");
    }
}
