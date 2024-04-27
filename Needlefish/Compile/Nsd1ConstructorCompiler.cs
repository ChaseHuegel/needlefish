using Needlefish.Schema;
using System;
using System.Linq;
using System.Text;

namespace Needlefish.Compile;

internal class Nsd1ConstructorCompiler : INsdTypeCompiler
{
    private const string ConstructorTemplate =
@"public $type($parameters)
{
    $fields
}";

    private const string FieldTemplate =
@"$field:name = $field:parameter;";

    public bool CanCompile(TypeDefinition typeDefinition)
    {
        return typeDefinition.Keyword == Nsd1MessageCompiler.Keyword;
    }

    public StringBuilder Compile(TypeDefinition typeDefinition)
    {
        string parameters = string.Join(", ", typeDefinition.FieldDefinitions.Select(CompileFieldParameter));

        StringBuilder fieldsBuilder = new();
        foreach (FieldDefinition field in typeDefinition.FieldDefinitions)
        {
            string fieldStr = FieldTemplate.Replace("$field:name", field.Name).Replace("$field:parameter", $"_{field.Name}");
            fieldsBuilder.AppendLine(fieldStr);
        }
        fieldsBuilder.Replace("\n", "\n" + Nsd1Compiler.Indent);

        StringBuilder builder = new();
        builder.AppendLine(ConstructorTemplate);
        builder.Replace("$parameters", parameters);
        builder.Replace("$fields", fieldsBuilder.ToString());
        builder.Replace("$type", typeDefinition.Name);
        builder.Replace("$fields:count", typeDefinition.FieldDefinitions.Length.ToString());
        return builder;
    }

    private static string CompileFieldParameter(FieldDefinition fieldDefinition)
    {
        return $"{fieldDefinition.GetFullyQualifiedType()} _{fieldDefinition.Name}";
    }
}