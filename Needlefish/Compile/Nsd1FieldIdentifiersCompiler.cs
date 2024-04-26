using Needlefish.Schema;
using System.Text;

namespace Needlefish.Compile;

internal class Nsd1FieldIdentifiersCompiler : INsdTypeCompiler
{
    public bool CanCompile(TypeDefinition typeDefinition)
    {
        return typeDefinition.Keyword == Nsd1MessageCompiler.Keyword;
    }

    public StringBuilder Compile(TypeDefinition typeDefinition)
    {
        StringBuilder builder = new();

        builder.AppendLine("#region Field identifiers");
        foreach (FieldDefinition fieldDefinition in typeDefinition.FieldDefinitions)
        {
            string nameStr = fieldDefinition.Name;
            string idStr = fieldDefinition.Value.ToString()!;

            builder.AppendLine($"private const ushort {nameStr}_ID = {idStr};");
        }
        builder.AppendLine("#endregion");

        return builder;
    }
}