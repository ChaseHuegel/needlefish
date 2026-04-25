using Needlefish.Schema;
using System;
using System.Text;

namespace Needlefish.Compile;

internal class Nsd1EnumCompiler : INsdTypeCompiler
{
    internal const string KEYWORD = "enum";

    public bool CanCompile(TypeDefinition typeDefinition)
    {
        return typeDefinition.Keyword == KEYWORD;
    }

    public StringBuilder Compile(TypeDefinition typeDefinition)
    {
        if (typeDefinition.Keyword != KEYWORD)
        {
            throw new InvalidOperationException(string.Format(
                "Invalid {0}. Expected keyword \"{1}\" but got \"{2}\".",
                nameof(TypeDefinition),
                KEYWORD,
                typeDefinition.Keyword)
            );
        }

        return BuildEnum(typeDefinition);
    }

    private static StringBuilder BuildEnum(TypeDefinition typeDefinition)
    {
        StringBuilder builder = new();
        builder.AppendLine($"public enum {typeDefinition.Name}");
        builder.AppendLine("{");

        foreach (FieldDefinition fieldDefinition in typeDefinition.FieldDefinitions)
        {
            builder.Append(Nsd1Compiler.INDENT);
            builder.AppendLine($"{fieldDefinition.Name} = {fieldDefinition.Value},");
        }

        builder.AppendLine("}");
        return builder;
    }
}
