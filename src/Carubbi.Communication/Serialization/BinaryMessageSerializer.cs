using System.Collections;
using System.Reflection;

namespace Carubbi.Communication.Serialization;

internal sealed class BinaryMessageSerializer<T> : IMessageSerializer<T>
    where T : class
{
    public byte[] Serialize(T message)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream))
        {
            WriteValue(writer, message, typeof(T));
        }

        return stream.ToArray();
    }

    public T Deserialize(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);
        return (T?)ReadValue(reader, typeof(T))
               ?? throw new InvalidOperationException("The binary payload could not be deserialized.");
    }

    private static void WriteValue(BinaryWriter writer, object? value, Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            type = underlying;
        }

        if (type == typeof(string))
        {
            writer.Write((string)value!);
        }
        else if (type == typeof(char))
        {
            writer.Write((char?)value ?? default);
        }
        else if (type == typeof(int))
        {
            writer.Write((int?)value ?? default);
        }
        else if (type == typeof(long))
        {
            writer.Write((long?)value ?? default);
        }
        else if (type == typeof(short))
        {
            writer.Write((short?)value ?? default);
        }
        else if (type == typeof(byte))
        {
            writer.Write((byte?)value ?? default);
        }
        else if (type == typeof(sbyte))
        {
            writer.Write((sbyte?)value ?? default);
        }
        else if (type == typeof(ushort))
        {
            writer.Write((ushort?)value ?? default);
        }
        else if (type == typeof(uint))
        {
            writer.Write((uint?)value ?? default);
        }
        else if (type == typeof(ulong))
        {
            writer.Write((ulong?)value ?? default);
        }
        else if (type == typeof(bool))
        {
            writer.Write((bool?)value ?? default);
        }
        else if (type == typeof(float))
        {
            writer.Write((float?)value ?? default);
        }
        else if (type == typeof(double))
        {
            writer.Write((double?)value ?? default);
        }
        else if (type == typeof(decimal))
        {
            writer.Write((decimal?)value ?? default);
        }
        else if (type == typeof(DateTime))
        {
            var dateTime = (DateTime?)value ?? default;
            writer.Write(dateTime.Ticks);
            writer.Write((int)dateTime.Kind);
        }
        else if (type == typeof(TimeSpan))
        {
            writer.Write(((TimeSpan?)value ?? default).Ticks);
        }
        else if (type == typeof(Guid))
        {
            writer.Write(((Guid?)value ?? Guid.Empty).ToString("D"));
        }
        else if (type.IsEnum)
        {
            writer.Write(value is null ? 0 : Convert.ToInt32(value));
        }
        else if (type == typeof(byte[]))
        {
            var bytes = (byte[]?)value;
            if (bytes is null)
            {
                writer.Write(-1);
            }
            else
            {
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }
        else if (type.IsArray)
        {
            WriteArray(writer, (Array?)value, type.GetElementType()!);
        }
        else if (IsList(type))
        {
            WriteList(writer, (IEnumerable?)value, GetListElementType(type));
        }
        else
        {
            WriteObject(writer, value, type);
        }
    }

    private static object? ReadValue(BinaryReader reader, Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            type = underlying;
        }

        if (type == typeof(string))
        {
            return reader.ReadString();
        }

        if (type == typeof(char))
        {
            return reader.ReadChar();
        }

        if (type == typeof(int))
        {
            return reader.ReadInt32();
        }

        if (type == typeof(long))
        {
            return reader.ReadInt64();
        }

        if (type == typeof(short))
        {
            return reader.ReadInt16();
        }

        if (type == typeof(byte))
        {
            return reader.ReadByte();
        }

        if (type == typeof(sbyte))
        {
            return reader.ReadSByte();
        }

        if (type == typeof(ushort))
        {
            return reader.ReadUInt16();
        }

        if (type == typeof(uint))
        {
            return reader.ReadUInt32();
        }

        if (type == typeof(ulong))
        {
            return reader.ReadUInt64();
        }

        if (type == typeof(bool))
        {
            return reader.ReadBoolean();
        }

        if (type == typeof(float))
        {
            return reader.ReadSingle();
        }

        if (type == typeof(double))
        {
            return reader.ReadDouble();
        }

        if (type == typeof(decimal))
        {
            return reader.ReadDecimal();
        }

        if (type == typeof(DateTime))
        {
            long ticks = reader.ReadInt64();
            var kind = (DateTimeKind)reader.ReadInt32();
            return new DateTime(ticks, kind);
        }

        if (type == typeof(TimeSpan))
        {
            return new TimeSpan(reader.ReadInt64());
        }

        if (type == typeof(Guid))
        {
            return Guid.Parse(reader.ReadString());
        }

        if (type.IsEnum)
        {
            return Enum.ToObject(type, reader.ReadInt32());
        }

        if (type == typeof(byte[]))
        {
            int length = reader.ReadInt32();
            return length < 0 ? null : reader.ReadBytes(length);
        }

        if (type.IsArray)
        {
            return ReadArray(reader, type.GetElementType()!);
        }

        if (IsList(type))
        {
            return ReadList(reader, GetListElementType(type));
        }

        return ReadObject(reader, type);
    }

    private static void WriteArray(BinaryWriter writer, Array? array, Type elementType)
    {
        if (array is null)
        {
            writer.Write(-1);
            return;
        }

        writer.Write(array.Length);
        for (int i = 0; i < array.Length; i++)
        {
            WriteValue(writer, array.GetValue(i), elementType);
        }
    }

    private static object? ReadArray(BinaryReader reader, Type elementType)
    {
        int length = reader.ReadInt32();
        if (length < 0)
        {
            return null;
        }

        var array = Array.CreateInstance(elementType, length);
        for (int i = 0; i < length; i++)
        {
            array.SetValue(ReadValue(reader, elementType), i);
        }

        return array;
    }

    private static void WriteList(BinaryWriter writer, IEnumerable? items, Type elementType)
    {
        if (items is null)
        {
            writer.Write(-1);
            return;
        }

        var list = (ICollection)items!;
        writer.Write(list.Count);
        foreach (var item in list)
        {
            WriteValue(writer, item, elementType);
        }
    }

    private static object? ReadList(BinaryReader reader, Type elementType)
    {
        int length = reader.ReadInt32();
        if (length < 0)
        {
            return null;
        }

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType)!;
        for (int i = 0; i < length; i++)
        {
            list.Add(ReadValue(reader, elementType));
        }

        return list;
    }

    private static void WriteObject(BinaryWriter writer, object? value, Type type)
    {
        if (value is null)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        foreach (var property in GetSerializableProperties(type))
        {
            WriteValue(writer, property.GetValue(value), property.PropertyType);
        }
    }

    private static object? ReadObject(BinaryReader reader, Type type)
    {
        if (!reader.ReadBoolean())
        {
            return null;
        }

        var instance = Activator.CreateInstance(type)!;
        foreach (var property in GetSerializableProperties(type))
        {
            property.SetValue(instance, ReadValue(reader, property.PropertyType));
        }

        return instance;
    }

    private static IEnumerable<PropertyInfo> GetSerializableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead
                && p.CanWrite
                && p.GetIndexParameters().Length == 0);
    }

    private static bool IsList(Type type)
    {
        return type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(List<>);
    }

    private static Type GetListElementType(Type type)
    {
        return type.GetGenericArguments()[0];
    }
}
