using Needlefish.Schema;
using System;
using System.Text;

namespace Needlefish.Compile;

internal class Nsd1SerializeCompiler : INsdTypeCompiler
{
    private const string SerializeTemplate =
@"public readonly byte[] Serialize()
{
    byte[] buffer = new byte[GetSize()];
    SerializeInto(buffer, 0);
    return buffer;
}";

    private const string SerializeIntoArrayTemplate =
@"public readonly unsafe int SerializeInto(byte[] buffer, int start)
{
    return SerializeInto(buffer.AsSpan(), start);
}";

    private const string SerializeIntoSpanTemplate =
@"public readonly int SerializeInto(Span<byte> buffer)
{
    return SerializeInto(buffer, 0);
}";

    private const string SerializeIntoSpanOverloadTemplate =
@"public readonly unsafe int SerializeInto(Span<byte> buffer, int start)
{
    unchecked
    {
        if (buffer.Length == 0)
        {
            return 0;
        }

        fixed (byte* g__offset = &buffer[start])
        {
            return SerializeCore(g__offset);
        }
    }
}";

    private const string SerializeCoreTemplate =
@"internal readonly unsafe int SerializeCore(byte* offset)
{
    unchecked
    {
        byte* g__serializeStart = offset;

        $serialize:fields

        return (int)(offset - g__serializeStart);
    }
}";

    private const string HasValueTemplate =
@"*((byte*)offset) = (byte)1;
offset += 1;";

    /// <summary>
    /// Copies of at least this many bytes use Buffer.MemoryCopy; smaller values use an
    /// inline loop because the call overhead dominates tiny copies.
    /// </summary>
    private const int MemcpyThresholdBytes = 64;

    private const int MemcpyThresholdChars = MemcpyThresholdBytes / 2;

    public bool CanCompile(TypeDefinition typeDefinition)
    {
        return typeDefinition.Keyword == Nsd1MessageCompiler.KEYWORD;
    }

    public StringBuilder Compile(TypeDefinition typeDefinition)
    {
        StringBuilder fieldsBuilder = new();
        foreach (FieldDefinition field in typeDefinition.FieldDefinitions)
        {
            fieldsBuilder.Append(CompileFieldSerialize(field));
            fieldsBuilder.AppendLine();
        }

        fieldsBuilder.Replace("\n", "\n" + Nsd1Compiler.INDENT + Nsd1Compiler.INDENT);

        string serializeCore = SerializeCoreTemplate.Replace("$serialize:fields", fieldsBuilder.ToString());

        StringBuilder builder = new();
        builder.AppendLine(SerializeTemplate);
        builder.AppendLine();
        builder.AppendLine(SerializeIntoArrayTemplate);
        builder.AppendLine();
        builder.AppendLine(SerializeIntoSpanTemplate);
        builder.AppendLine();
        builder.AppendLine(SerializeIntoSpanOverloadTemplate);
        builder.AppendLine();
        builder.AppendLine(serializeCore);
        return builder;
    }

    private StringBuilder CompileFieldSerialize(FieldDefinition field)
    {
        StringBuilder builder = new();
        builder.AppendLine($"#region Serialize {field.Name}");
        builder.AppendLine($"{field.GetFullyQualifiedType()} g__{field.Name} = {field.Name};");
        builder.AppendLine();

        if (field.TypeName == "string")
        {
            if (field.IsArray)
            {
                AppendField(builder, field, () => CompileStringArrayBody(field, nullable: field.IsOptional ? false : true));
            }
            else
            {
                AppendField(builder, field, () => CompileStringBody(field, nullable: field.IsOptional ? false : true));
            }
        }
        else if (field.Type == FieldType.Object)
        {
            if (field.IsArray)
            {
                AppendField(builder, field, () => CompileObjectArrayBody(field, nullable: field.IsOptional ? false : true));
            }
            else if (field.IsOptional)
            {
                builder.AppendLine(CompileOptionalWrapper(field, CompileObjectBody(field, $"g__{field.Name}.Value")));
            }
            else
            {
                builder.AppendLine(CompileHeader(field));
                builder.AppendLine();
                builder.AppendLine(CompileObjectBody(field, $"g__{field.Name}"));
            }
        }
        else
        {
            if (field.IsArray)
            {
                AppendField(builder, field, () => CompileArrayBody(field, nullable: field.IsOptional ? false : true));
            }
            else if (field.IsOptional)
            {
                builder.AppendLine(CompileOptionalWrapper(field, CompileScalarStore(field, $"g__{field.Name}.Value")));
            }
            else
            {
                builder.AppendLine(CompileHeader(field));
                builder.AppendLine();
                builder.AppendLine(CompileScalarStore(field, $"g__{field.Name}"));
            }
        }

        builder.AppendLine("#endregion");
        return builder;
    }

    /// <summary>
    /// Appends a field value body, wrapping it in an optional check when the field is optional.
    /// </summary>
    private static void AppendField(StringBuilder builder, FieldDefinition field, Func<string> compileBody)
    {
        string body = compileBody();

        if (field.IsOptional)
        {
            builder.AppendLine(CompileOptionalWrapper(field, body));
        }
        else
        {
            builder.AppendLine(CompileHeader(field));
            builder.AppendLine();
            builder.AppendLine(body);
        }
    }

    private static string CompileHeader(FieldDefinition field)
    {
        return
            $"*((ushort*)offset) = BitConverter.IsLittleEndian ? {field.Name}_ID : BinaryPrimitives.ReverseEndianness({field.Name}_ID);\n" +
            "offset += 2;";
    }

    private static string CompileOptionalWrapper(FieldDefinition field, string body)
    {
        StringBuilder builder = new();
        builder.AppendLine($"if (g__{field.Name} != null)");
        builder.AppendLine("{");
        builder.AppendLine(Indent(CompileHeader(field)));
        builder.AppendLine(Indent(HasValueTemplate));
        builder.AppendLine();
        builder.AppendLine(Indent(body));
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    private static string CompileLengthLocal(FieldDefinition field, bool nullable)
    {
        string lengthExpr = nullable ? $"g__{field.Name}?.Length ?? 0" : $"g__{field.Name}.Length";

        StringBuilder builder = new();
        builder.AppendLine($"int g__{field.Name}_Length = {lengthExpr};");
        builder.AppendLine($"if (g__{field.Name}_Length > ushort.MaxValue)");
        builder.AppendLine("{");
        builder.AppendLine($"    throw new InvalidOperationException(\"Field '{field.Name}' exceeds the maximum encodable length of 65535.\");");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    private static string CompileLengthHeader(FieldDefinition field)
    {
        return
            $"*((ushort*)offset) = BitConverter.IsLittleEndian ? (ushort)g__{field.Name}_Length : BinaryPrimitives.ReverseEndianness((ushort)g__{field.Name}_Length);\n" +
            "offset += 2;";
    }

    private static string CompileStringBody(FieldDefinition field, bool nullable)
    {
        string name = field.Name;

        StringBuilder builder = new();
        builder.AppendLine(CompileLengthLocal(field, nullable));
        builder.AppendLine();
        builder.AppendLine(CompileLengthHeader(field));
        builder.AppendLine();
        builder.AppendLine($"if (g__{name}_Length != 0)");
        builder.AppendLine("{");
        builder.AppendLine($"    if (BitConverter.IsLittleEndian && g__{name}_Length >= {MemcpyThresholdChars})");
        builder.AppendLine("    {");
        builder.AppendLine($"        fixed (char* g__{name}_Chars = g__{name})");
        builder.AppendLine("        {");
        builder.AppendLine($"            long g__{name}_Bytes = (long)g__{name}_Length * 2;");
        builder.AppendLine($"            Buffer.MemoryCopy(g__{name}_Chars, offset, g__{name}_Bytes, g__{name}_Bytes);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine($"        offset += g__{name}_Length * 2;");
        builder.AppendLine("    }");
        builder.AppendLine("    else");
        builder.AppendLine("    {");
        builder.AppendLine($"        for (int i = 0; i < g__{name}_Length; i++)");
        builder.AppendLine("        {");
        builder.AppendLine($"            *((char*)offset) = BitConverter.IsLittleEndian ? g__{name}[i] : (char)BinaryPrimitives.ReverseEndianness(g__{name}[i]);");
        builder.AppendLine("            offset += 2;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    private static string CompileStringArrayBody(FieldDefinition field, bool nullable)
    {
        string name = field.Name;

        StringBuilder builder = new();
        builder.AppendLine(CompileLengthLocal(field, nullable));
        builder.AppendLine();
        builder.AppendLine(CompileLengthHeader(field));
        builder.AppendLine();
        builder.AppendLine($"if (g__{name}_Length != 0)");
        builder.AppendLine("{");
        builder.AppendLine($"    for (int i = 0; i < g__{name}_Length; i++)");
        builder.AppendLine("    {");
        builder.AppendLine($"        string g__{name}_Item = g__{name}[i];");
        builder.AppendLine($"        int g__{name}_ItemLength = g__{name}_Item?.Length ?? 0;");
        builder.AppendLine($"        if (g__{name}_ItemLength > ushort.MaxValue)");
        builder.AppendLine("        {");
        builder.AppendLine($"            throw new InvalidOperationException(\"Field '{name}' exceeds the maximum encodable length of 65535.\");");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine($"        *((ushort*)offset) = BitConverter.IsLittleEndian ? (ushort)g__{name}_ItemLength : BinaryPrimitives.ReverseEndianness((ushort)g__{name}_ItemLength);");
        builder.AppendLine("        offset += 2;");
        builder.AppendLine();
        builder.AppendLine($"        if (g__{name}_ItemLength != 0)");
        builder.AppendLine("        {");
        builder.AppendLine($"            if (BitConverter.IsLittleEndian && g__{name}_ItemLength >= {MemcpyThresholdChars})");
        builder.AppendLine("            {");
        builder.AppendLine($"                fixed (char* g__{name}_ItemChars = g__{name}_Item)");
        builder.AppendLine("                {");
        builder.AppendLine($"                    long g__{name}_ItemBytes = (long)g__{name}_ItemLength * 2;");
        builder.AppendLine($"                    Buffer.MemoryCopy(g__{name}_ItemChars, offset, g__{name}_ItemBytes, g__{name}_ItemBytes);");
        builder.AppendLine("                }");
        builder.AppendLine();
        builder.AppendLine($"                offset += g__{name}_ItemLength * 2;");
        builder.AppendLine("            }");
        builder.AppendLine("            else");
        builder.AppendLine("            {");
        builder.AppendLine($"                for (int n = 0; n < g__{name}_ItemLength; n++)");
        builder.AppendLine("                {");
        builder.AppendLine($"                    *((char*)offset) = BitConverter.IsLittleEndian ? g__{name}_Item[n] : (char)BinaryPrimitives.ReverseEndianness(g__{name}_Item[n]);");
        builder.AppendLine("                    offset += 2;");
        builder.AppendLine("                }");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    private static string CompileArrayBody(FieldDefinition field, bool nullable)
    {
        string name = field.Name;
        string elementType = field.GetTypeReference();
        int elementSize = GetElementSize(field);

        StringBuilder builder = new();
        builder.AppendLine(CompileLengthLocal(field, nullable));
        builder.AppendLine();
        builder.AppendLine(CompileLengthHeader(field));
        builder.AppendLine();
        builder.AppendLine($"if (g__{name}_Length != 0)");
        builder.AppendLine("{");
        builder.AppendLine($"    if (BitConverter.IsLittleEndian && g__{name}_Length >= {GetMemcpyThreshold(elementSize)})");
        builder.AppendLine("    {");
        builder.AppendLine($"        fixed ({elementType}* g__{name}_Ptr = g__{name})");
        builder.AppendLine("        {");
        builder.AppendLine($"            long g__{name}_Bytes = (long)g__{name}_Length * {elementSize};");
        builder.AppendLine($"            Buffer.MemoryCopy(g__{name}_Ptr, offset, g__{name}_Bytes, g__{name}_Bytes);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine($"        offset += g__{name}_Length * {elementSize};");
        builder.AppendLine("    }");
        builder.AppendLine("    else");
        builder.AppendLine("    {");
        builder.AppendLine($"        for (int i = 0; i < g__{name}_Length; i++)");
        builder.AppendLine("        {");
        builder.AppendLine(Indent(CompileScalarStore(field, $"g__{name}[i]"), 3));
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    private static string CompileObjectArrayBody(FieldDefinition field, bool nullable)
    {
        string name = field.Name;

        StringBuilder builder = new();
        builder.AppendLine(CompileLengthLocal(field, nullable));
        builder.AppendLine();
        builder.AppendLine(CompileLengthHeader(field));
        builder.AppendLine();
        builder.AppendLine($"if (g__{name}_Length != 0)");
        builder.AppendLine("{");
        builder.AppendLine($"    for (int i = 0; i < g__{name}_Length; i++)");
        builder.AppendLine("    {");
        builder.AppendLine($"        byte* g__{name}_LengthPos = offset;");
        builder.AppendLine("        offset += 2;");
        builder.AppendLine();
        builder.AppendLine($"        int g__{name}_ObjectLength = g__{name}[i].SerializeCore(offset);");
        builder.AppendLine();
        builder.AppendLine($"        if (g__{name}_ObjectLength > ushort.MaxValue)");
        builder.AppendLine("        {");
        builder.AppendLine($"            throw new InvalidOperationException(\"Field '{name}' exceeds the maximum encodable length of 65535.\");");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine($"        *((ushort*)g__{name}_LengthPos) = BitConverter.IsLittleEndian ? (ushort)g__{name}_ObjectLength : BinaryPrimitives.ReverseEndianness((ushort)g__{name}_ObjectLength);");
        builder.AppendLine($"        offset += g__{name}_ObjectLength;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    private static string CompileObjectBody(FieldDefinition field, string accessor)
    {
        string name = field.Name;

        StringBuilder builder = new();
        builder.AppendLine($"byte* g__{name}_LengthPos = offset;");
        builder.AppendLine("offset += 2;");
        builder.AppendLine();
        builder.AppendLine($"int g__{name}_ObjectLength = {accessor}.SerializeCore(offset);");
        builder.AppendLine();
        builder.AppendLine($"if (g__{name}_ObjectLength > ushort.MaxValue)");
        builder.AppendLine("{");
        builder.AppendLine($"    throw new InvalidOperationException(\"Field '{name}' exceeds the maximum encodable length of 65535.\");");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine($"*((ushort*)g__{name}_LengthPos) = BitConverter.IsLittleEndian ? (ushort)g__{name}_ObjectLength : BinaryPrimitives.ReverseEndianness((ushort)g__{name}_ObjectLength);");
        builder.AppendLine($"offset += g__{name}_ObjectLength;");
        return builder.ToString().TrimEnd();
    }

    private static string CompileScalarStore(FieldDefinition field, string accessor)
    {
        switch (field.Type)
        {
            case FieldType.Enum:
                return
                    $"*((int*)offset) = BitConverter.IsLittleEndian ? (int){accessor} : BinaryPrimitives.ReverseEndianness((int){accessor});\n" +
                    "offset += 4;";

            case FieldType.Primitive:
                switch (field.TypeName)
                {
                    case "bool":
                        return
                            $"*((bool*)offset) = {accessor};\n" +
                            "offset += 1;";

                    case "float":
                        return
                            $"float g__{field.Name}_Copy = {accessor};\n" +
                            $"*((uint*)offset) = BitConverter.IsLittleEndian ? *(uint*)&g__{field.Name}_Copy : BinaryPrimitives.ReverseEndianness(*(uint*)&g__{field.Name}_Copy);\n" +
                            "offset += 4;";

                    case "double":
                        return
                            $"double g__{field.Name}_Copy = {accessor};\n" +
                            $"*((ulong*)offset) = BitConverter.IsLittleEndian ? *(ulong*)&g__{field.Name}_Copy : BinaryPrimitives.ReverseEndianness(*(ulong*)&g__{field.Name}_Copy);\n" +
                            "offset += 8;";

                    default:
                        return
                            $"*(({field.TypeName}*)offset) = BitConverter.IsLittleEndian ? {accessor} : BinaryPrimitives.ReverseEndianness({accessor});\n" +
                            $"offset += {SizeOfPrimitive(field)};";
                }

            default:
                throw new NotSupportedException($"{field.TypeName} serialization is not supported.");
        }
    }

    private static string Indent(string text, int levels = 1)
    {
        string prefix = new(' ', levels * Nsd1Compiler.INDENT.Length);
        return prefix + text.Replace("\n", "\n" + prefix);
    }

    private static int GetElementSize(FieldDefinition field)
    {
        if (field.Type == FieldType.Enum)
        {
            return 4;
        }

        return field.TypeName switch
        {
            "float" => 4,
            "double" => 8,
            _ => SizeOfPrimitive(field),
        };
    }

    private static int GetMemcpyThreshold(int elementSize)
    {
        return Math.Max(1, MemcpyThresholdBytes / elementSize);
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
}
