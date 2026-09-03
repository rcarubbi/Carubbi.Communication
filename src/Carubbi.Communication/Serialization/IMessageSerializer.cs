namespace Carubbi.Communication.Serialization;

public interface IMessageSerializer<T>
    where T : class
{
    byte[] Serialize(T message);

    T Deserialize(byte[] data);
}
