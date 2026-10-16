using System.Reflection;

namespace ViceSharp.Chips.Cpu;

public partial class Mos6502
{
    private static readonly FieldInfo[] ExecutionFields = typeof(Mos6502)
        .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(static field => field.FieldType.IsValueType)
        .OrderBy(static field => field.MetadataToken)
        .ToArray();

    /// <summary>
    /// FR-SNP-003. Copies every value-type CPU field, including the in-flight
    /// instruction. The public PC setter restarts that instruction. Reference
    /// fields (bus, hooks) stay on the destination machine.
    /// </summary>
    /// <returns>The image <see cref="RestoreExecutionImage"/> writes back.</returns>
    internal byte[] CaptureExecutionImage()
    {
        using var buffer = new MemoryStream();
        using var writer = new BinaryWriter(buffer);
        writer.Write(ExecutionFields.Length);
        for (var i = 0; i < ExecutionFields.Length; i++)
            WriteField(writer, ExecutionFields[i].GetValue(this), ExecutionFields[i]);

        return buffer.ToArray();
    }

    /// <summary>
    /// FR-SNP-003. Restores an image from <see cref="CaptureExecutionImage"/>.
    /// Does not call the PC setter and does not clear in-flight state.
    /// </summary>
    /// <param name="image">Bytes previously captured from this CPU type.</param>
    internal void RestoreExecutionImage(ReadOnlySpan<byte> image)
    {
        using var buffer = new MemoryStream(image.ToArray());
        using var reader = new BinaryReader(buffer);
        var count = reader.ReadInt32();
        if (count != ExecutionFields.Length)
            throw new InvalidOperationException("CPU execution image does not match this build.");

        for (var i = 0; i < ExecutionFields.Length; i++)
            ExecutionFields[i].SetValue(this, ReadField(reader, ExecutionFields[i]));
    }

    private static void WriteField(BinaryWriter writer, object? value, FieldInfo field)
    {
        var type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
                writer.Write((bool)value!);
                return;
            case TypeCode.Byte:
                writer.Write((byte)value!);
                return;
            case TypeCode.SByte:
                writer.Write((sbyte)value!);
                return;
            case TypeCode.Int16:
                writer.Write((short)value!);
                return;
            case TypeCode.UInt16:
                writer.Write((ushort)value!);
                return;
            case TypeCode.Int32:
                writer.Write(Convert.ToInt32(value));
                return;
            case TypeCode.UInt32:
                writer.Write(Convert.ToUInt32(value));
                return;
            case TypeCode.Int64:
                writer.Write(Convert.ToInt64(value));
                return;
            case TypeCode.UInt64:
                writer.Write(Convert.ToUInt64(value));
                return;
            default:
                throw new InvalidOperationException($"CPU field {field.Name} has unsupported type {field.FieldType}.");
        }
    }

    private static object ReadField(BinaryReader reader, FieldInfo field)
    {
        var type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        var enumType = type.IsEnum ? type : null;
        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);

        object raw = Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => reader.ReadBoolean(),
            TypeCode.Byte => reader.ReadByte(),
            TypeCode.SByte => reader.ReadSByte(),
            TypeCode.Int16 => reader.ReadInt16(),
            TypeCode.UInt16 => reader.ReadUInt16(),
            TypeCode.Int32 => reader.ReadInt32(),
            TypeCode.UInt32 => reader.ReadUInt32(),
            TypeCode.Int64 => reader.ReadInt64(),
            TypeCode.UInt64 => reader.ReadUInt64(),
            _ => throw new InvalidOperationException($"CPU field {field.Name} has unsupported type {field.FieldType}.")
        };

        if (enumType is not null)
            return Enum.ToObject(enumType, raw);

        return raw;
    }
}
