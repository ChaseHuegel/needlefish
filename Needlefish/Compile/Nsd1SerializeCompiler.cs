using Needlefish.Schema;
using System;
using System.Text;

namespace Needlefish.Compile;

internal class Nsd1SerializeCompiler : INsdTypeCompiler
{
    private const string SerializeTemplate =
@"public byte[] Serialize()
{
    byte[] buffer = new byte[GetSize()];
    SerializeInto(buffer, 0);
    return buffer;
}";

    private const string SerializeIntoTemplate =
@"public unsafe int SerializeInto(byte[] buffer, int start)
{
    unchecked
    {
        if (buffer.Length == 0)
        {
            return 0;
        }

        fixed (byte* b = &buffer[start])
        {
            byte* offset = b;

            $serialize:fields

            return (int)(offset - b);
        }
    }
}";

    private const string FieldTemplate =
@"$serialize:header:field

$serialize:value";

    private const string OptionalFieldTemplate =
@"if ($field:accessor:base != null)
{
    $serialize:header:field

    *((byte*)offset) = (byte)1;
    offset += 1;

    $serialize:value
}";

    private const string ArrayFieldTemplate =
@"$serialize:header:field

$serialize:header:length

for (int i = 0; i < $field:accessor:base?.Length; i++)
{
    $serialize:value
}";

    private const string OptionalArrayFieldTemplate =
@"if ($field:accessor:base != null)
{
    $serialize:header:field

    *((byte*)offset) = (byte)1;
    offset += 1;

    $serialize:header:length

    for (int i = 0; i < $field:accessor:base?.Length; i++)
    {
        $serialize:value
    }
}";

    private const string StringTemplate =
@"$serialize:header:field

if ($field:accessor:base != null)
{
    $serialize:header:length

    for (int i = 0; i < $field:accessor:base.Length; i++)
    {
        *((char*)offset) = BitConverter.IsLittleEndian ? $field:accessor:context[i] : (char)BinaryPrimitives.ReverseEndianness($field:accessor:context[i]);
        offset += 2;
    }
}
else
{
    *((ushort*)offset) = (ushort)0;
    offset += 2;
}";

    private const string OptionalStringTemplate =
@"if ($field:accessor:base != null)
{
    $serialize:header:field

    *((byte*)offset) = (byte)1;
    offset += 1;

    $serialize:header:length

    for (int i = 0; i < $field:accessor:context?.Length; i++)
    {
        *((char*)offset) = BitConverter.IsLittleEndian ? $field:accessor:context[i] : (char)BinaryPrimitives.ReverseEndianness($field:accessor:context[i]);
        offset += 2;
    }
}";

    private const string StringArrayTemplate =
@"$serialize:header:field

$serialize:header:length

for (int i = 0; i < $field:accessor:base?.Length; i++)
{
    string item = $field:accessor:base[i];

    *((ushort*)offset) = BitConverter.IsLittleEndian ? (ushort)(item?.Length ?? 0) : BinaryPrimitives.ReverseEndianness((ushort)(item?.Length ?? 0));
    offset += 2;

    if (item != null)
    {
        for (int n = 0; n < item.Length; n++)
        {
            *((char*)offset) = BitConverter.IsLittleEndian ? item[n] : (char)BinaryPrimitives.ReverseEndianness(item[n]);
            offset += 2;
        }
    }
}";

    private const string OptionalStringArrayTemplate =
@"if ($field:accessor:base != null)
{
    $serialize:header:field

    *((byte*)offset) = (byte)1;
    offset += 1;

    $serialize:header:length

    for (int i = 0; i < $field:accessor:base?.Length; i++)
    {
        string item = $field:accessor:base[i];

        *((ushort*)offset) = BitConverter.IsLittleEndian ? (ushort)(item?.Length ?? 0) : BinaryPrimitives.ReverseEndianness((ushort)(item?.Length ?? 0));
        offset += 2;

        if (item != null)
        {
            for (int n = 0; n < item.Length; n++)
            {
                *((char*)offset) = BitConverter.IsLittleEndian ? item[n] : (char)BinaryPrimitives.ReverseEndianness(item[n]);
                offset += 2;
            }
        }
    }
}";

    private const string FieldHeaderTemplate =
@"*((ushort*)offset) = BitConverter.IsLittleEndian ? $field:name_ID : BinaryPrimitives.ReverseEndianness($field:name_ID);
    offset += 2;";

    private const string LengthHeaderTemplate =
@"*((ushort*)offset) = BitConverter.IsLittleEndian ? (ushort)($field:accessor:base?.Length ?? 0) : BinaryPrimitives.ReverseEndianness((ushort)($field:accessor:base?.Length ?? 0));
    offset += 2;";

    private const string DefaultValueTemplate =
@"*(($field:type*)offset) = BitConverter.IsLittleEndian ? $field:accessor:context : BinaryPrimitives.ReverseEndianness($field:accessor:context);
    offset += $field:size;";

    private const string FloatValueTemplate =
@"float g__$field:name_Copy = $field:accessor:context;
*((float*)offset) = BitConverter.IsLittleEndian ? $field:accessor:context : BinaryPrimitives.ReverseEndianness(*(uint*)&g__$field:name_Copy);
    offset += 4;";

    private const string DoubleValueTemplate =
@"double g__$field:name_Copy = $field:accessor:context;
*((double*)offset) = BitConverter.IsLittleEndian ? $field:accessor:context : BinaryPrimitives.ReverseEndianness(*(ulong*)&g__$field:name_Copy);
    offset += 8;";

    private const string BoolValueTemplate =
@"*((bool*)offset) = $field:accessor:context;
    offset += $field:size;";

    private const string ObjectValueTemplate =
@"ushort g__$field:name_ObjectLength = (ushort)$field:accessor:context.GetSize();
    * ((ushort*)offset) = BitConverter.IsLittleEndian ? g__$field:name_ObjectLength : BinaryPrimitives.ReverseEndianness(g__$field:name_ObjectLength);
    offset += 2;

    $field:accessor:context.SerializeInto(buffer, (int)(offset - b));
    offset += g__$field:name_ObjectLength;";

    private const string EnumValueTemplate =
@"*((int*)offset) = BitConverter.IsLittleEndian ? (int)$field:accessor:context : BinaryPrimitives.ReverseEndianness((int)$field:accessor:context);
    offset += 4;";

    public bool CanCompile(TypeDefinition typeDefinition)
    {
        return typeDefinition.Keyword == Nsd1MessageCompiler.Keyword;
    }

    public StringBuilder Compile(TypeDefinition typeDefinition)
    {
        StringBuilder fieldsBuilder = new();
        foreach (FieldDefinition field in typeDefinition.FieldDefinitions)
        {
            StringBuilder fieldBuilder = CompileFieldSerializeInto(field);
            fieldsBuilder.Append(fieldBuilder);
            fieldsBuilder.AppendLine();
        }

        fieldsBuilder.Replace("\n", "\n" + Nsd1Compiler.Indent + Nsd1Compiler.Indent + Nsd1Compiler.Indent);

        string serializeInto = SerializeIntoTemplate.Replace("$serialize:fields", fieldsBuilder.ToString());

        StringBuilder builder = new();
        builder.AppendLine(SerializeTemplate);
        builder.AppendLine();
        builder.AppendLine(serializeInto);
        return builder;
    }

    private StringBuilder CompileFieldSerializeInto(FieldDefinition field)
    {
        string template = string.Empty;
        if (field.TypeName != "string")
        {
            if (!field.IsOptional && !field.IsArray)
            {
                template = FieldTemplate;
            }
            else if (!field.IsOptional && field.IsArray)
            {
                template = ArrayFieldTemplate;
            }
            else if (field.IsOptional && !field.IsArray)
            {
                template = OptionalFieldTemplate;
            }
            else if (field.IsOptional && field.IsArray)
            {
                template = OptionalArrayFieldTemplate;
            }
        }
        else
        {
            if (!field.IsOptional && !field.IsArray)
            {
                template = StringTemplate;
            }
            else if (!field.IsOptional && field.IsArray)
            {
                template = StringArrayTemplate;
            }
            else if (field.IsOptional && !field.IsArray)
            {
                template = OptionalStringTemplate;
            }
            else if (field.IsOptional && field.IsArray)
            {
                template = OptionalStringArrayTemplate;
            }
        }

        StringBuilder builder = new();

        builder.AppendLine("#region Serialize $field:name");
        builder.AppendLine("$field:qualified_type $field:accessor:base = $field:name;");
        builder.AppendLine(template);
        builder.AppendLine("#endregion");

        builder.Replace("$serialize:header:field", FieldHeaderTemplate);
        builder.Replace("$serialize:header:length", LengthHeaderTemplate);
        builder.Replace("$serialize:value", GetFieldSerializeValueTemplate(field));

        builder.Replace("$field:name", field.Name);
        builder.Replace("$field:qualified_type", field.GetFullyQualifiedType());
        builder.Replace("$field:accessor:base", GetBaseAccessor(field));
        builder.Replace("$field:accessor:context", GetContextAccessor(field));
        builder.Replace("$field:type", field.TypeName);
        builder.Replace("$field:size", SizeOfPrimitive(field).ToString());

        return builder;
    }

    private string GetBaseAccessor(FieldDefinition field)
    {
        return $"g__{field.Name}";
    }

    private string GetContextAccessor(FieldDefinition field)
    {
        var baseAccessor = GetBaseAccessor(field);
        string accessor = field.IsArray ? $"{baseAccessor}[i]" : baseAccessor;
        if (field.IsOptional && !field.IsArray && field.TypeName != "string")
        {
            accessor += ".Value";
        }

        return accessor;
    }

    private string GetFieldSerializeValueTemplate(FieldDefinition field)
    {
        switch (field.Type)
        {
            case FieldType.Object:
                return ObjectValueTemplate;
            case FieldType.Enum:
                return EnumValueTemplate;
            case FieldType.Primitive:
                switch (field.TypeName)
                {
                    case "bool":
                        return BoolValueTemplate;
                    case "float":
                        return FloatValueTemplate;
                    case "double":
                        return DoubleValueTemplate;
                    default:
                        return DefaultValueTemplate;
                }
            default:
                throw new NotSupportedException($"{field.TypeName} serialization is not supported.");
        }
    }

    private int SizeOfPrimitive(FieldDefinition field)
    {
        return field.TypeName switch
        {
            "bool" or "byte" or "sbyte" => 1,
            "short" or "ushort" or "char" => 2,
            "int" or "uint" => 4,
            "long" or "ulong" => 8,
            _ => -1
        };
    }
}