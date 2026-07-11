using Needlefish.Schema;
using System;
using System.Linq;
using System.Text;

namespace Needlefish.Compile;

internal class Nsd1MessageCompiler(in CompilerOptions compilerOptions) : INsdTypeCompiler
{
    internal const string KEYWORD = "message";

    private readonly CompilerOptions _compilerOptions = compilerOptions;
    
    private readonly INsdTypeCompiler[] _subcompilers = [
        new Nsd1FieldIdentifiersCompiler(),
        new Nsd1FieldsCompiler(),
        new Nsd1ConstructorCompiler(),
        new Nsd1GetSizeCompiler(),
        new Nsd1SerializeCompiler(),
        new Nsd1DeserializeCompiler(),
    ];

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

        return BuildMessage(typeDefinition);
    }

    private StringBuilder BuildMessage(TypeDefinition typeDefinition)
    {
        StringBuilder builder = new();
        string modifierStr = _compilerOptions.Partial ? "partial " : string.Empty;
        builder.AppendLine($"public {modifierStr}struct {typeDefinition.Name}");
        builder.AppendLine("{");

        foreach (INsdTypeCompiler subcompiler in _subcompilers.Where(c => c.CanCompile(typeDefinition)))
        {
            StringBuilder subcompilerBuilder = subcompiler.Compile(typeDefinition);
            subcompilerBuilder.Replace("\n", "\n" + Nsd1Compiler.INDENT);

            builder.Append(Nsd1Compiler.INDENT);
            builder.Append(subcompilerBuilder);
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder;
    }
}
