using Needlefish.Schema;
using System;
using System.Text;

namespace Needlefish.Compile;

internal class Nsd1EnumCompiler : INsdTypeCompiler
{
    internal const string Keyword = "enum";

    public bool CanCompile(TypeDefinition typeDefinition)
    {
        return typeDefinition.Keyword == Keyword;
    }

    public StringBuilder Compile(TypeDefinition typeDefinition)
    {
        if (typeDefinition.Keyword != Keyword)
        {
            throw new InvalidOperationException(string.Format(
                "Invalid {0}. Expected keyword \"{1}\" but got \"{2}\".",
                nameof(TypeDefinition),
                Keyword,
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
            builder.Append(Nsd1Compiler.Indent);
            builder.AppendLine($"{fieldDefinition.Name} = {fieldDefinition.Value},");
        }

        builder.AppendLine("}");
        return builder;
    }
}
