using Needlefish.Schema;
using System;
using System.Text;

namespace Needlefish.Compile;

internal class Nsd1DeserializeCompiler : INsdTypeCompiler
{
    private const string DeserializeTemplate =
@"public static $type Deserialize(byte[] buffer, int start, int length)
{
    $type value = new $type();
    value.Unpack(buffer, start, length);
    return value;
}";
    
    
    private const string DeserializeSpanTemplate =
@"public static $type Deserialize(Span<byte> buffer)
{
    $type value = new $type();
    value.Unpack(buffer);
    return value;
}";
    
    private const string DeserializeSpanOverloadTemplate =
@"public static $type Deserialize(Span<byte> buffer, int start, int length)
{
    $type value = new $type();
    value.Unpack(buffer, start, length);
    return value;
}";
    
    
    private const string DeserializeReadOnlySpanTemplate =
@"public static $type Deserialize(ReadOnlySpan<byte> buffer)
{
    $type value = new $type();
    value.Unpack(buffer);
    return value;
}";
    
    private const string DeserializeReadOnlySpanOverloadTemplate =
@"public static $type Deserialize(ReadOnlySpan<byte> buffer, int start, int length)
{
    $type value = new $type();
    value.Unpack(buffer, start, length);
    return value;
}";

    private const string UnpackTemplate =
@"public unsafe int Unpack(byte[] buffer, int start, int length)
{
    unchecked
    {
        if (buffer.Length == 0 || length == 0)
        {
            return 0;
        }

        if (length < 0 || length > buffer.Length - start)
        {
            throw new InvalidDataException(""Malformed input: start and length exceed the buffer."");
        }

        fixed (byte* b = &buffer[start])
        {
            byte* end = b + length;
            byte* offset = b;

            int readsCompleted = 0;
            $deserialize:reads

            while (readsCompleted < $fields:count && offset + 2 <= end)
            {
                ushort id = BitConverter.IsLittleEndian ? *((ushort*)offset) : BinaryPrimitives.ReverseEndianness(*((ushort*)offset));
                offset += 2;

                switch (id)
                {
                    $deserialize:cases
                }
            }

            return (int)(offset - b);
        }
    }
}";
    
    private const string UnpackSpanTemplate =
@"public unsafe int Unpack(Span<byte> buffer)
{
    return Unpack(buffer, 0, buffer.Length);
}";
    
    
    private const string UnpackReadOnlySpanTemplate =
@"public unsafe int Unpack(ReadOnlySpan<byte> buffer)
{
    return Unpack(buffer, 0, buffer.Length);
}";
    
    private const string UnpackSpanOverloadTemplate =
@"public unsafe int Unpack(Span<byte> buffer, int start, int length)
{
    unchecked
    {
        if (buffer.Length == 0 || length == 0)
        {
            return 0;
        }

        if (length < 0 || length > buffer.Length - start)
        {
            throw new InvalidDataException(""Malformed input: start and length exceed the buffer."");
        }

        fixed (byte* b = &buffer[start])
        {
            byte* end = b + length;
            byte* offset = b;

            int readsCompleted = 0;
            $deserialize:reads

            while (readsCompleted < $fields:count && offset + 2 <= end)
            {
                ushort id = BitConverter.IsLittleEndian ? *((ushort*)offset) : BinaryPrimitives.ReverseEndianness(*((ushort*)offset));
                offset += 2;

                switch (id)
                {
                    $deserialize:cases
                }
            }

            return (int)(offset - b);
        }
    }
}";
    
    
    private const string UnpackReadOnlySpanOverloadTemplate =
@"public unsafe int Unpack(ReadOnlySpan<byte> buffer, int start, int length)
{
    unchecked
    {
        if (buffer.Length == 0 || length == 0)
        {
            return 0;
        }

        if (length < 0 || length > buffer.Length - start)
        {
            throw new InvalidDataException(""Malformed input: start and length exceed the buffer."");
        }

        fixed (byte* b = &buffer[start])
        {
            byte* end = b + length;
            byte* offset = b;

            int readsCompleted = 0;
            $deserialize:reads

            while (readsCompleted < $fields:count && offset + 2 <= end)
            {
                ushort id = BitConverter.IsLittleEndian ? *((ushort*)offset) : BinaryPrimitives.ReverseEndianness(*((ushort*)offset));
                offset += 2;

                switch (id)
                {
                    $deserialize:cases
                }
            }

            return (int)(offset - b);
        }
    }
}";

    private const string FieldReadTemplate =
@"bool g__$field:name_Read = false;";

    private const string CaseTemplate =
@"#region Deserialize $field:name
case $field:name_ID:
    if (g__$field:name_Read)
    {
        break;
    }

    $deserialize:field

    g__$field:name_Read = true;
    readsCompleted++;
    break;
#endregion";

    private const string FieldTemplate =
@"$deserialize:value";

    private const string OptionalFieldTemplate =
@"if (offset + 1 > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

bool g__$field:name_HasValue = *((byte*)offset) == 0 ? false : true;
offset += 1;

if (g__$field:name_HasValue)
{
    $deserialize:value
}
else
{
    $field:name = null;
}";

    private const string ArrayFieldTemplate =
@"$deserialize:header:length
$deserialize:check:array

if (g__$field:name_Length == 0)
{
    $field:name = Array.Empty<$field:type>();
}
else
{
    $field:name = new $field:type[g__$field:name_Length];

    for (int i = 0; i < g__$field:name_Length; i++)
    {
        $deserialize:value
    }
}";

    private const string OptionalArrayFieldTemplate =
@"if (offset + 1 > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

bool g__$field:name_HasValue = *((byte*)offset) == 0 ? false : true;
offset += 1;

if (g__$field:name_HasValue)
{
    $deserialize:header:length
    $deserialize:check:array

    if (g__$field:name_Length == 0)
    {
        $field:name = Array.Empty<$field:type>();
    }
    else
    {
        $field:name = new $field:type[g__$field:name_Length];

        for (int i = 0; i < g__$field:name_Length; i++)
        {
            $deserialize:value
        }
    }
}
else
{
    $field:name = null;
}";

    private const string StringTemplate =
@"$deserialize:header:length

if (offset + 2 * g__$field:name_Length > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

if (g__$field:name_Length == 0)
{
    $field:accessor = string.Empty;
}
else
{
    char* chars = (char*)offset;
    if (!BitConverter.IsLittleEndian)
    {
        for (int n = 0; n < g__$field:name_Length; n++)
        {
            *((ushort*)chars) = BinaryPrimitives.ReverseEndianness(*((ushort*)chars));
            chars += 2;
        }
    }

    $field:accessor = new string(chars, 0, g__$field:name_Length);
    offset += 2 * g__$field:name_Length;
}";

    private const string OptionalStringTemplate =
@"if (offset + 1 > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

bool g__$field:name_HasValue = *((byte*)offset) == 0 ? false : true;
offset += 1;

if (g__$field:name_HasValue)
{
    $deserialize:header:length

    if (offset + 2 * g__$field:name_Length > end)
    {
        throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
    }

    if (g__$field:name_Length == 0)
    {
        $field:accessor = string.Empty;
    }
    else
    {
        char* chars = (char*)offset;
        if (!BitConverter.IsLittleEndian)
        {
            for (int n = 0; n < g__$field:name_Length; n++)
            {
                *((ushort*)chars) = BinaryPrimitives.ReverseEndianness(*((ushort*)chars));
                chars += 2;
            }
        }

        $field:accessor = new string(chars, 0, g__$field:name_Length);
        offset += 2 * g__$field:name_Length;
    }
}
else
{
    $field:name = null;
}";

    private const string StringArrayTemplate =
@"$deserialize:header:length

if (g__$field:name_Length == 0)
{
    $field:name = Array.Empty<$field:type>();
}
else
{
    $field:name = new $field:type[g__$field:name_Length];

    for (int i = 0; i < g__$field:name_Length; i++)
    {
        if (offset + 2 > end)
        {
            throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
        }

        ushort g__$field:name_i_length = BitConverter.IsLittleEndian ? *((ushort*)offset) : BinaryPrimitives.ReverseEndianness(*((ushort*)offset));
        offset += 2;

        if (offset + 2 * g__$field:name_i_length > end)
        {
            throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
        }

        char* chars = (char*)offset;
        if (!BitConverter.IsLittleEndian)
        {
            for (int n = 0; n < g__$field:name_i_length; n++)
            {
                *((ushort*)chars) = BinaryPrimitives.ReverseEndianness(*((ushort*)chars));
                chars += 2;
            }
        }

        $field:name[i] = new string(chars, 0, g__$field:name_i_length);
        offset += 2 * g__$field:name_i_length;
    }
}";

    private const string OptionalStringArrayTemplate =
@"if (offset + 1 > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

bool g__$field:name_HasValue = *((byte*)offset) == 0 ? false : true;
offset += 1;

if (g__$field:name_HasValue)
{
    $deserialize:header:length

    if (g__$field:name_Length == 0)
    {
        $field:name = Array.Empty<$field:type>();
    }
    else
    {
        $field:name = new $field:type[g__$field:name_Length];

        for (int i = 0; i < g__$field:name_Length; i++)
        {
            if (offset + 2 > end)
            {
                throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
            }

            ushort g__$field:name_i_length = BitConverter.IsLittleEndian ? *((ushort*)offset) : BinaryPrimitives.ReverseEndianness(*((ushort*)offset));
            offset += 2;

            if (offset + 2 * g__$field:name_i_length > end)
            {
                throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
            }

            char* chars = (char*)offset;
            if (!BitConverter.IsLittleEndian)
            {
                for (int n = 0; n < g__$field:name_i_length; n++)
                {
                    *((ushort*)chars) = BinaryPrimitives.ReverseEndianness(*((ushort*)chars));
                    chars += 2;
                }
            }

            $field:name[i] = new string(chars, 0, g__$field:name_i_length);
            offset += 2 * g__$field:name_i_length;
        }
    }
}";

    private const string LengthHeaderTemplate =
@"if (offset + 2 > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

ushort g__$field:name_Length = BitConverter.IsLittleEndian ? *((ushort*)offset) : BinaryPrimitives.ReverseEndianness(*((ushort*)offset));
offset += 2;";

    private const string DefaultValueTemplate =
@"if (offset + $field:size > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

$field:accessor = BitConverter.IsLittleEndian ? *(($field:type*)offset) : BinaryPrimitives.ReverseEndianness(*(($field:type*)offset));
offset += $field:size;";

    private const string FloatValueTemplate =
@"if (offset + 4 > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

uint g__$field:name_Raw = BitConverter.IsLittleEndian ? *((uint*)offset) : BinaryPrimitives.ReverseEndianness(*((uint*)offset));
$field:accessor = *(float*)&g__$field:name_Raw;
offset += 4;";

    private const string DoubleValueTemplate =
@"if (offset + 8 > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

ulong g__$field:name_Raw = BitConverter.IsLittleEndian ? *((ulong*)offset) : BinaryPrimitives.ReverseEndianness(*((ulong*)offset));
$field:accessor = *(double*)&g__$field:name_Raw;
offset += 8;";

    private const string BoolValueTemplate =
@"if (offset + $field:size > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

$field:accessor = *((byte*)offset) == 0 ? false : true;
offset += $field:size;";

    private const string ObjectValueTemplate =
@"if (offset + 2 > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

ushort g__$field:name_ObjectLength = BitConverter.IsLittleEndian ? *((ushort*)offset) : BinaryPrimitives.ReverseEndianness(*((ushort*)offset));
offset += 2;

if (offset + g__$field:name_ObjectLength > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

int g__$field:name_Start = start + (int)(offset - b);
$field:accessor.Unpack(buffer, g__$field:name_Start, g__$field:name_ObjectLength);
offset += g__$field:name_ObjectLength;";

    private const string OptionalObjectValueTemplate =
@"if (offset + 2 > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

ushort g__$field:name_ObjectLength = BitConverter.IsLittleEndian ? *((ushort*)offset) : BinaryPrimitives.ReverseEndianness(*((ushort*)offset));
offset += 2;

if (offset + g__$field:name_ObjectLength > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

int g__$field:name_Start = start + (int)(offset - b);
if ($field:accessor == null)
{
    $field:accessor = $field:type.Deserialize(buffer, g__$field:name_Start, g__$field:name_ObjectLength);
}
else
{
    $field:accessor.Value.Unpack(buffer, g__$field:name_Start, g__$field:name_ObjectLength);
}
offset += g__$field:name_ObjectLength;";

    private const string EnumValueTemplate =
@"if (offset + 4 > end)
{
    throw new InvalidDataException(""Malformed input: field '$field:name' is truncated."");
}

$field:accessor = ($field:type)(BitConverter.IsLittleEndian ? *((int*)offset) : BinaryPrimitives.ReverseEndianness(*((int*)offset)));
offset += 4;";

    public bool CanCompile(TypeDefinition typeDefinition)
    {
        return typeDefinition.Keyword == Nsd1MessageCompiler.KEYWORD;
    }

    public StringBuilder Compile(TypeDefinition typeDefinition)
    {
        StringBuilder readBuilder = new();
        StringBuilder casesBuilder = new();
        foreach (FieldDefinition field in typeDefinition.FieldDefinitions)
        {
            readBuilder.AppendLine(FieldReadTemplate.Replace("$field:name", field.Name));

            StringBuilder fieldBuilder = CompileFieldCase(field);
            casesBuilder.Append(fieldBuilder);
            casesBuilder.AppendLine();
        }

        readBuilder.Replace("\n", "\n" + Nsd1Compiler.INDENT + Nsd1Compiler.INDENT + Nsd1Compiler.INDENT);

        casesBuilder.Replace("\n", "\n" + Nsd1Compiler.INDENT + Nsd1Compiler.INDENT + Nsd1Compiler.INDENT + Nsd1Compiler.INDENT + Nsd1Compiler.INDENT);

        string unpack = UnpackTemplate
            .Replace("$deserialize:cases", casesBuilder.ToString())
            .Replace("$deserialize:reads", readBuilder.ToString());

        string unpackSpanOverload = UnpackSpanOverloadTemplate
            .Replace("$deserialize:cases", casesBuilder.ToString())
            .Replace("$deserialize:reads", readBuilder.ToString());
        
        string unpackReadOnlySpanOverload = UnpackReadOnlySpanOverloadTemplate
            .Replace("$deserialize:cases", casesBuilder.ToString())
            .Replace("$deserialize:reads", readBuilder.ToString());
        
        StringBuilder builder = new();
        builder.AppendLine(DeserializeTemplate);
        builder.AppendLine();
        builder.AppendLine(DeserializeSpanTemplate);
        builder.AppendLine();
        builder.AppendLine(DeserializeSpanOverloadTemplate);
        builder.AppendLine();
        builder.AppendLine(DeserializeReadOnlySpanTemplate);
        builder.AppendLine();
        builder.AppendLine(DeserializeReadOnlySpanOverloadTemplate);
        builder.AppendLine();
        builder.AppendLine(unpack);
        builder.AppendLine();
        builder.AppendLine(UnpackSpanTemplate);
        builder.AppendLine();
        builder.AppendLine(unpackSpanOverload);
        builder.AppendLine();
        builder.AppendLine(UnpackReadOnlySpanTemplate);
        builder.AppendLine();
        builder.AppendLine(unpackReadOnlySpanOverload);
        builder.Replace("$type", typeDefinition.Name);
        builder.Replace("$fields:count", typeDefinition.FieldDefinitions.Length.ToString());
        return builder;
    }

    private StringBuilder CompileFieldCase(FieldDefinition field)
    {
        string text = CaseTemplate.Replace("$deserialize:field", CompileFieldDeserialize(field).ToString());

        text = ReplaceIndented(text, "$deserialize:header:length", LengthHeaderTemplate);
        text = ReplaceIndented(text, "$deserialize:value", GetFieldDeserializeValueTemplate(field));
        text = ReplaceIndented(text, "$deserialize:check:array", GetArrayBoundsCheck(field));

        text = text.Replace("$field:name", field.Name);
        text = text.Replace("$field:accessor", GetFieldAccessor(field));
        text = text.Replace("$field:type", field.TypeName);
        text = text.Replace("$field:size", SizeOfPrimitive(field).ToString());

        return new StringBuilder(text);
    }

    private static string ReplaceIndented(string text, string placeholder, string template)
    {
        int index = 0;
        while ((index = text.IndexOf(placeholder, index, StringComparison.Ordinal)) >= 0)
        {
            int lineStart = text.LastIndexOf('\n', index) + 1;
            int column = index - lineStart;
            string indented = template.Replace("\n", "\n" + new string(' ', column));
            text = text.Remove(index, placeholder.Length).Insert(index, indented);
            index += indented.Length;
        }

        return text;
    }

    private StringBuilder CompileFieldDeserialize(FieldDefinition field)
    {
        StringBuilder builder = new();

        if (field.TypeName != "string")
        {
            if (!field.IsOptional && !field.IsArray)
            {
                builder.AppendLine(FieldTemplate);
            }
            else if (!field.IsOptional && field.IsArray)
            {
                builder.AppendLine(ArrayFieldTemplate);
            }
            else if (field.IsOptional && !field.IsArray)
            {
                builder.AppendLine(OptionalFieldTemplate);
            }
            else if (field.IsOptional && field.IsArray)
            {
                builder.AppendLine(OptionalArrayFieldTemplate);
            }
        }
        else
        {
            if (!field.IsOptional && !field.IsArray)
            {
                builder.AppendLine(StringTemplate);
            }
            else if (!field.IsOptional && field.IsArray)
            {
                builder.AppendLine(StringArrayTemplate);
            }
            else if (field.IsOptional && !field.IsArray)
            {
                builder.AppendLine(OptionalStringTemplate);
            }
            else if (field.IsOptional && field.IsArray)
            {
                builder.AppendLine(OptionalStringArrayTemplate);
            }
        }

        builder.Replace("\n", "\n" + Nsd1Compiler.INDENT);
        return builder;
    }

    private string GetFieldAccessor(FieldDefinition field)
    {
        return field.IsArray ? $"{field.Name}[i]" : field.Name;
    }

    private string GetFieldDeserializeValueTemplate(FieldDefinition field)
    {
        switch (field.Type)
        {
            case FieldType.Object:
                return field.IsOptional && !field.IsArray ? OptionalObjectValueTemplate : ObjectValueTemplate;
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
                throw new NotSupportedException($"{field.TypeName} deserialization is not supported.");
        }
    }

    private string GetArrayBoundsCheck(FieldDefinition field)
    {
        if (field.Type == FieldType.Object)
        {
            return string.Empty;
        }

        int elementSize = field.Type switch
        {
            FieldType.Enum => 4,
            FieldType.Primitive => field.TypeName switch
            {
                "float" => 4,
                "double" => 8,
                _ => SizeOfPrimitive(field),
            },
            _ => SizeOfPrimitive(field),
        };
        return "if (offset + g__$field:name_Length * " + elementSize + " > end)" + "\n" +
               "{" + "\n" +
               "    throw new InvalidDataException(\"Malformed input: field '$field:name' is truncated.\");" + "\n" +
               "}" + "\n";
    }

    private int SizeOfPrimitive(FieldDefinition field)
    {
        return field.TypeName switch
        {
            "bool" or "byte" or "sbyte" => 1,
            "short" or "ushort" or "char" => 2,
            "int" or "uint" => 4,
            "long" or "ulong" => 8,
            _ => -1,
        };
    }
}