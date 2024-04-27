using Needlefish.Schema;
using System.Text;

namespace Needlefish.Compile;

internal class Nsd1FieldsCompiler : INsdTypeCompiler
{
    public bool CanCompile(TypeDefinition typeDefinition)
    {
        return typeDefinition.Keyword == Nsd1MessageCompiler.Keyword;
    }

    public StringBuilder Compile(TypeDefinition typeDefinition)
    {
        StringBuilder builder = new();

        foreach (FieldDefinition fieldDefinition in typeDefinition.FieldDefinitions)
        {
            string nameStr = fieldDefinition.Name;
            builder.AppendLine($"public {fieldDefinition.GetFullyQualifiedType()} {nameStr};");
        }

        return builder;
    }
}