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

    private const string UnpackArrayTemplate =
@"public unsafe int Unpack(byte[] buffer, int start, int length)
{
    if (buffer.Length == 0 || length == 0)
    {
        return 0;
    }

    if (start < 0 || length < 0 || length > buffer.Length - start)
    {
        throw new InvalidDataException(""Malformed input: start and length exceed the buffer."");
    }

    return UnpackSpan(buffer.AsSpan(start, length));
}";

    private const string UnpackSpanTemplate =
@"public unsafe int Unpack(Span<byte> buffer)
{
    return UnpackSpan(buffer);
}";

    private const string UnpackSpanOverloadTemplate =
@"public unsafe int Unpack(Span<byte> buffer, int start, int length)
{
    if (buffer.Length == 0 || length == 0)
    {
        return 0;
    }

    if (start < 0 || length < 0 || length > buffer.Length - start)
    {
        throw new InvalidDataException(""Malformed input: start and length exceed the buffer."");
    }

    return UnpackSpan(buffer.Slice(start, length));
}";

    private const string UnpackReadOnlySpanTemplate =
@"public unsafe int Unpack(ReadOnlySpan<byte> buffer)
{
    return UnpackSpan(buffer);
}";

    private const string UnpackReadOnlySpanOverloadTemplate =
@"public unsafe int Unpack(ReadOnlySpan<byte> buffer, int start, int length)
{
    if (buffer.Length == 0 || length == 0)
    {
        return 0;
    }

    if (start < 0 || length < 0 || length > buffer.Length - start)
    {
        throw new InvalidDataException(""Malformed input: start and length exceed the buffer."");
    }

    return UnpackSpan(buffer.Slice(start, length));
}";

    private const string UnpackSpanCoreTemplate =
@"private unsafe int UnpackSpan(ReadOnlySpan<byte> buffer)
{
    if (buffer.Length == 0)
    {
        return 0;
    }

    fixed (byte* g__origin = &buffer[0])
    {
        return UnpackCore(g__origin, g__origin + buffer.Length);
    }
}";

    private const string UnpackCoreTemplate =
@"internal unsafe int UnpackCore(byte* offset, byte* end)
{
    unchecked
    {
        byte* g__start = offset;
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

        return (int)(offset - g__start);
    }
}";

    private const string FieldReadTemplate = "bool g__$field:name_Read = false;";

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

        string unpackCore = ReplaceIndented(
            ReplaceIndented(UnpackCoreTemplate, "$deserialize:reads", readBuilder.ToString()),
            "$deserialize:cases", casesBuilder.ToString());

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
        builder.AppendLine(UnpackArrayTemplate);
        builder.AppendLine();
        builder.AppendLine(UnpackSpanTemplate);
        builder.AppendLine();
        builder.AppendLine(UnpackSpanOverloadTemplate);
        builder.AppendLine();
        builder.AppendLine(UnpackReadOnlySpanTemplate);
        builder.AppendLine();
        builder.AppendLine(UnpackReadOnlySpanOverloadTemplate);
        builder.AppendLine();
        builder.AppendLine(UnpackSpanCoreTemplate);
        builder.AppendLine();
        builder.AppendLine(unpackCore);
        builder.Replace("$type", typeDefinition.Name);
        builder.Replace("$fields:count", typeDefinition.FieldDefinitions.Length.ToString());
        return builder;
    }

    private StringBuilder CompileFieldCase(FieldDefinition field)
    {
        string body = CompileFieldBody(field);
        string text = CaseTemplate
            .Replace("$deserialize:field", IndentContinuation(body, 1))
            .Replace("$field:name", field.Name);

        return new StringBuilder(text);
    }

    private string CompileFieldBody(FieldDefinition field)
    {
        if (field.TypeName == "string")
        {
            if (field.IsArray)
            {
                return field.IsOptional
                    ? CompileWrappedBody(field, CompileStringArrayBody(field))
                    : CompileStringArrayBody(field);
            }

            return field.IsOptional
                ? CompileWrappedBody(field, CompileStringReadBody(field))
                : CompileStringReadBody(field);
        }

        if (field.Type == FieldType.Object)
        {
            if (field.IsArray)
            {
                return field.IsOptional
                    ? CompileWrappedBody(field, CompileObjectArrayBody(field))
                    : CompileObjectArrayBody(field);
            }

            return field.IsOptional
                ? CompileWrappedBody(field, CompileOptionalObjectBody(field))
                : CompileObjectBody(field);
        }

        if (field.IsArray)
        {
            return field.IsOptional
                ? CompileWrappedBody(field, CompileArrayBody(field))
                : CompileArrayBody(field);
        }

        string value = SubstituteScalar(field, CompileScalarValue(field));

        if (field.IsOptional)
        {
            return OptionalFieldTemplate
                .Replace("$deserialize:value", IndentContinuation(value, 1))
                .Replace("$field:name", field.Name);
        }

        return value;
    }

    private static string SubstituteScalar(FieldDefinition field, string template)
    {
        return template
            .Replace("$field:name", field.Name)
            .Replace("$field:accessor", field.Name)
            .Replace("$field:type", field.GetTypeReference())
            .Replace("$field:size", SizeOfPrimitive(field).ToString());
    }

    /// <summary>
    /// Wraps an optional container body (array or nullable object) in the has-value flag.
    /// </summary>
    private string CompileWrappedBody(FieldDefinition field, string body)
    {
        StringBuilder builder = new();
        builder.AppendLine("if (offset + 1 > end)");
        builder.AppendLine("{");
        builder.AppendLine($"    throw new InvalidDataException(\"Malformed input: field '{field.Name}' is truncated.\");");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("bool g__" + field.Name + "_HasValue = *((byte*)offset) == 0 ? false : true;");
        builder.AppendLine("offset += 1;");
        builder.AppendLine();
        builder.AppendLine($"if (g__{field.Name}_HasValue)");
        builder.AppendLine("{");
        builder.AppendLine(IndentAll(body, 1));
        builder.AppendLine("}");
        builder.AppendLine("else");
        builder.AppendLine("{");
        builder.AppendLine($"    {field.Name} = null;");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    private string CompileScalarValue(FieldDefinition field)
    {
        switch (field.Type)
        {
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

    private string CompileStringReadBody(FieldDefinition field)
    {
        StringBuilder builder = new();
        builder.AppendLine(LengthHeaderTemplate.Replace("$field:name", field.Name));
        builder.AppendLine();
        builder.AppendLine($"if (offset + 2 * g__{field.Name}_Length > end)");
        builder.AppendLine("{");
        builder.AppendLine($"    throw new InvalidDataException(\"Malformed input: field '{field.Name}' is truncated.\");");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine($"if (g__{field.Name}_Length == 0)");
        builder.AppendLine("{");
        builder.AppendLine($"    {field.Name} = string.Empty;");
        builder.AppendLine("}");
        builder.AppendLine("else");
        builder.AppendLine("{");
        builder.AppendLine($"    if (BitConverter.IsLittleEndian)");
        builder.AppendLine("    {");
        builder.AppendLine($"        {field.Name} = new string((char*)offset, 0, g__{field.Name}_Length);");
        builder.AppendLine("    }");
        builder.AppendLine("    else");
        builder.AppendLine("    {");
        builder.AppendLine($"        {field.Name} = string.Create(g__{field.Name}_Length, (IntPtr)offset, static (chars, ptr) =>");
        builder.AppendLine("        {");
        builder.AppendLine("            char* source = (char*)ptr;");
        builder.AppendLine("            for (int i = 0; i < chars.Length; i++)");
        builder.AppendLine("            {");
        builder.AppendLine("                chars[i] = (char)BinaryPrimitives.ReverseEndianness((ushort)source[i]);");
        builder.AppendLine("            }");
        builder.AppendLine("        });");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine($"    offset += 2 * g__{field.Name}_Length;");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    private string CompileArrayBody(FieldDefinition field)
    {
        StringBuilder builder = new();
        builder.AppendLine(LengthHeaderTemplate.Replace("$field:name", field.Name));
        builder.AppendLine();
        builder.AppendLine(GetArrayBoundsCheck(field));
        builder.AppendLine();
        builder.AppendLine($"if (g__{field.Name}_Length == 0)");
        builder.AppendLine("{");
        builder.AppendLine($"    {field.Name} = Array.Empty<{field.GetTypeReference()}>();");
        builder.AppendLine("}");
        builder.AppendLine("else");
        builder.AppendLine("{");
        builder.AppendLine($"    {field.Name} = new {field.GetTypeReference()}[g__{field.Name}_Length];");
        builder.AppendLine();
        builder.AppendLine(IndentAll(CompileArrayRead(field, field.Name), 1));
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    private string CompileArrayRead(FieldDefinition field, string accessor)
    {
        string elementType = field.GetTypeReference();
        int elementSize = GetElementSize(field);

        StringBuilder builder = new();

        builder.AppendLine($"if (BitConverter.IsLittleEndian && g__{field.Name}_Length >= {GetMemcpyThreshold(elementSize)})");
        builder.AppendLine("{");
        builder.AppendLine($"    fixed ({elementType}* g__{field.Name}_Ptr = {accessor})");
        builder.AppendLine("    {");
        builder.AppendLine($"        long g__{field.Name}_Bytes = (long)g__{field.Name}_Length * {elementSize};");
        builder.AppendLine($"        Buffer.MemoryCopy(offset, g__{field.Name}_Ptr, g__{field.Name}_Bytes, g__{field.Name}_Bytes);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine($"    offset += g__{field.Name}_Length * {elementSize};");
        builder.AppendLine("}");
        builder.AppendLine("else");
        builder.AppendLine("{");
        builder.AppendLine($"    for (int i = 0; i < g__{field.Name}_Length; i++)");
        builder.AppendLine("    {");
        builder.AppendLine(IndentAll(CompileArrayElementRead(field), 2));
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString().TrimEnd();
    }

    private string CompileArrayElementRead(FieldDefinition field)
    {
        string accessor = $"{field.Name}[i]";

        switch (field.Type)
        {
            case FieldType.Enum:
                return
                    $"{accessor} = ({field.GetTypeReference()})(BitConverter.IsLittleEndian ? *((int*)offset) : BinaryPrimitives.ReverseEndianness(*((int*)offset)));\n" +
                    "offset += 4;";

            case FieldType.Primitive:
                switch (field.TypeName)
                {
                    case "bool":
                        return
                            $"{accessor} = *((byte*)offset) == 0 ? false : true;\n" +
                            "offset += 1;";

                    case "byte":
                        return
                            $"{accessor} = *((byte*)offset);\n" +
                            "offset += 1;";

                    case "float":
                        return
                            $"uint g__{field.Name}_Raw = BitConverter.IsLittleEndian ? *((uint*)offset) : BinaryPrimitives.ReverseEndianness(*((uint*)offset));\n" +
                            $"{accessor} = *(float*)&g__{field.Name}_Raw;\n" +
                            "offset += 4;";

                    case "double":
                        return
                            $"ulong g__{field.Name}_Raw = BitConverter.IsLittleEndian ? *((ulong*)offset) : BinaryPrimitives.ReverseEndianness(*((ulong*)offset));\n" +
                            $"{accessor} = *(double*)&g__{field.Name}_Raw;\n" +
                            "offset += 8;";

                    default:
                        return
                            $"{accessor} = BitConverter.IsLittleEndian ? *(({field.TypeName}*)offset) : BinaryPrimitives.ReverseEndianness(*(({field.TypeName}*)offset));\n" +
                            $"offset += {SizeOfPrimitive(field)};";
                }

            default:
                throw new NotSupportedException($"{field.TypeName} deserialization is not supported.");
        }
    }

    private string CompileStringArrayBody(FieldDefinition field)
    {
        StringBuilder builder = new();
        builder.AppendLine(LengthHeaderTemplate.Replace("$field:name", field.Name));
        builder.AppendLine();
        builder.AppendLine($"if (g__{field.Name}_Length == 0)");
        builder.AppendLine("{");
        builder.AppendLine($"    {field.Name} = Array.Empty<string>();");
        builder.AppendLine("}");
        builder.AppendLine("else");
        builder.AppendLine("{");
        builder.AppendLine($"    {field.Name} = new string[g__{field.Name}_Length];");
        builder.AppendLine();
        builder.AppendLine($"    for (int i = 0; i < g__{field.Name}_Length; i++)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (offset + 2 > end)");
        builder.AppendLine("        {");
        builder.AppendLine($"            throw new InvalidDataException(\"Malformed input: field '{field.Name}' is truncated.\");");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine($"        ushort g__{field.Name}_ItemLength = BitConverter.IsLittleEndian ? *((ushort*)offset) : BinaryPrimitives.ReverseEndianness(*((ushort*)offset));");
        builder.AppendLine("        offset += 2;");
        builder.AppendLine();
        builder.AppendLine($"        if (offset + 2 * g__{field.Name}_ItemLength > end)");
        builder.AppendLine("        {");
        builder.AppendLine($"            throw new InvalidDataException(\"Malformed input: field '{field.Name}' is truncated.\");");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine($"        if (g__{field.Name}_ItemLength == 0)");
        builder.AppendLine("        {");
        builder.AppendLine($"            {field.Name}[i] = string.Empty;");
        builder.AppendLine("        }");
        builder.AppendLine("        else");
        builder.AppendLine("        {");
        builder.AppendLine("            if (BitConverter.IsLittleEndian)");
        builder.AppendLine("            {");
        builder.AppendLine($"                {field.Name}[i] = new string((char*)offset, 0, g__{field.Name}_ItemLength);");
        builder.AppendLine("            }");
        builder.AppendLine("            else");
        builder.AppendLine("            {");
        builder.AppendLine($"                {field.Name}[i] = string.Create(g__{field.Name}_ItemLength, (IntPtr)offset, static (chars, ptr) =>");
        builder.AppendLine("                {");
        builder.AppendLine("                    char* source = (char*)ptr;");
        builder.AppendLine("                    for (int n = 0; n < chars.Length; n++)");
        builder.AppendLine("                    {");
        builder.AppendLine("                        chars[n] = (char)BinaryPrimitives.ReverseEndianness((ushort)source[n]);");
        builder.AppendLine("                    }");
        builder.AppendLine("                });");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine($"        offset += 2 * g__{field.Name}_ItemLength;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    private string CompileObjectBody(FieldDefinition field)
    {
        StringBuilder builder = new();
        builder.AppendLine(CompileObjectLengthHeader(field));
        builder.AppendLine();
        builder.AppendLine($"{field.Name}.UnpackCore(offset, offset + g__{field.Name}_ObjectLength);");
        builder.AppendLine($"offset += g__{field.Name}_ObjectLength;");
        return builder.ToString().TrimEnd();
    }

    private string CompileOptionalObjectBody(FieldDefinition field)
    {
        StringBuilder builder = new();
        builder.AppendLine(CompileObjectLengthHeader(field));
        builder.AppendLine();
        builder.AppendLine($"{field.GetTypeReference()} g__{field.Name}_Value = {field.Name}.GetValueOrDefault();");
        builder.AppendLine($"g__{field.Name}_Value.UnpackCore(offset, offset + g__{field.Name}_ObjectLength);");
        builder.AppendLine($"{field.Name} = g__{field.Name}_Value;");
        builder.AppendLine($"offset += g__{field.Name}_ObjectLength;");
        return builder.ToString().TrimEnd();
    }

    private string CompileObjectArrayBody(FieldDefinition field)
    {
        StringBuilder builder = new();
        builder.AppendLine(LengthHeaderTemplate.Replace("$field:name", field.Name));
        builder.AppendLine();
        builder.AppendLine($"if (g__{field.Name}_Length == 0)");
        builder.AppendLine("{");
        builder.AppendLine($"    {field.Name} = Array.Empty<{field.GetTypeReference()}>();");
        builder.AppendLine("}");
        builder.AppendLine("else");
        builder.AppendLine("{");
        builder.AppendLine($"    {field.Name} = new {field.GetTypeReference()}[g__{field.Name}_Length];");
        builder.AppendLine();
        builder.AppendLine($"    for (int i = 0; i < g__{field.Name}_Length; i++)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (offset + 2 > end)");
        builder.AppendLine("        {");
        builder.AppendLine($"            throw new InvalidDataException(\"Malformed input: field '{field.Name}' is truncated.\");");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine($"        ushort g__{field.Name}_ObjectLength = BitConverter.IsLittleEndian ? *((ushort*)offset) : BinaryPrimitives.ReverseEndianness(*((ushort*)offset));");
        builder.AppendLine("        offset += 2;");
        builder.AppendLine();
        builder.AppendLine($"        if (offset + g__{field.Name}_ObjectLength > end)");
        builder.AppendLine("        {");
        builder.AppendLine($"            throw new InvalidDataException(\"Malformed input: field '{field.Name}' is truncated.\");");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine($"        {field.Name}[i].UnpackCore(offset, offset + g__{field.Name}_ObjectLength);");
        builder.AppendLine($"        offset += g__{field.Name}_ObjectLength;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    private string CompileObjectLengthHeader(FieldDefinition field)
    {
        StringBuilder builder = new();
        builder.AppendLine("if (offset + 2 > end)");
        builder.AppendLine("{");
        builder.AppendLine($"    throw new InvalidDataException(\"Malformed input: field '{field.Name}' is truncated.\");");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine($"ushort g__{field.Name}_ObjectLength = BitConverter.IsLittleEndian ? *((ushort*)offset) : BinaryPrimitives.ReverseEndianness(*((ushort*)offset));");
        builder.AppendLine("offset += 2;");
        builder.AppendLine();
        builder.AppendLine($"if (offset + g__{field.Name}_ObjectLength > end)");
        builder.AppendLine("{");
        builder.AppendLine($"    throw new InvalidDataException(\"Malformed input: field '{field.Name}' is truncated.\");");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    private string GetArrayBoundsCheck(FieldDefinition field)
    {
        int elementSize = GetElementSize(field);
        return
            $"if (offset + g__{field.Name}_Length * {elementSize} > end)\n" +
            "{\n" +
            $"    throw new InvalidDataException(\"Malformed input: field '{field.Name}' is truncated.\");\n" +
            "}";
    }

    private static int GetElementSize(FieldDefinition field)
    {
        if (field.Type == FieldType.Enum)
        {
            return 4;
        }

        return field.TypeName switch
        {
            "bool" or "byte" or "sbyte" => 1,
            "short" or "ushort" or "char" => 2,
            "int" or "uint" => 4,
            "long" or "ulong" => 8,
            "float" => 4,
            "double" => 8,
            _ => SizeOfPrimitive(field),
        };
    }

    /// <summary>
    /// Copies of at least this many bytes use Buffer.MemoryCopy; smaller values use an
    /// inline loop because the call overhead dominates tiny copies.
    /// </summary>
    private static int GetMemcpyThreshold(int elementSize)
    {
        return Math.Max(1, 64 / elementSize);
    }

    private static int SizeOfPrimitive(FieldDefinition field)
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

    private static string IndentContinuation(string text, int levels)
    {
        string prefix = new(' ', levels * Nsd1Compiler.INDENT.Length);
        return text.Replace("\n", "\n" + prefix);
    }

    private static string IndentAll(string text, int levels)
    {
        string prefix = new(' ', levels * Nsd1Compiler.INDENT.Length);
        return prefix + text.Replace("\n", "\n" + prefix);
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
}
