using System.Text;

namespace Needlefish.Schema;

public readonly struct FieldDefinition
{
    public FieldType Type { get; }
    public string? TypeName { get; }
    public string Name { get; }
    public int? Value { get; }
    public bool IsOptional { get; }
    public bool IsArray { get; }

    public FieldDefinition(FieldType type, string? typeName, string name, int? value, bool isOptional, bool isArray)
    {
        Type = type;
        TypeName = typeName;
        Name = name;
        Value = value;
        IsOptional = isOptional;
        IsArray = isArray;
    }

    public string GetFullyQualifiedType()
    {
        StringBuilder builder = new(TypeName);

        if (IsArray)
        {
            builder.Append("[]");
        }

        if (IsOptional)
        {
            builder.Append("?");
        }

        return builder.ToString();
    }
}