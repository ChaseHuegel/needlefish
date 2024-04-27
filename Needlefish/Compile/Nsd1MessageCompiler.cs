using Needlefish.Schema;
using System;
using System.Linq;
using System.Text;

namespace Needlefish.Compile;

internal class Nsd1MessageCompiler : INsdTypeCompiler
{
    internal const string Keyword = "message";

    private readonly INsdTypeCompiler[] Subcompilers = [
        new Nsd1FieldIdentifiersCompiler(),
        new Nsd1FieldsCompiler(),
        new Nsd1ConstructorCompiler(),
        new Nsd1GetSizeCompiler(),
        new Nsd1SerializeCompiler(),
        new Nsd1DeserializeCompiler(),
    ];

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

        return BuildMessage(typeDefinition);
    }

    private StringBuilder BuildMessage(TypeDefinition typeDefinition)
    {
        StringBuilder builder = new();
        builder.AppendLine($"public struct {typeDefinition.Name}");
        builder.AppendLine("{");

        foreach (INsdTypeCompiler subcompiler in Subcompilers.Where(c => c.CanCompile(typeDefinition)))
        {
            StringBuilder subcompilerBuilder = subcompiler.Compile(typeDefinition);
            subcompilerBuilder.Replace("\n", "\n" + Nsd1Compiler.Indent);

            builder.Append(Nsd1Compiler.Indent);
            builder.Append(subcompilerBuilder);
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder;
    }
}
