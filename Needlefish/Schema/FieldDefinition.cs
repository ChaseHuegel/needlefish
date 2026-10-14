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

    /// <summary>
    /// 0-based character offset of the field name in the source, or -1 when unknown.
    /// </summary>
    public int Position { get; }

    public int Length { get; }

    public FieldDefinition(
        FieldType type,
        string? typeName,
        string name,
        int? value,
        bool isOptional,
        bool isArray,
        int position = -1,
        int length = 0)
    {
        Type = type;
        TypeName = typeName;
        Name = name;
        Value = value;
        IsOptional = isOptional;
        IsArray = isArray;
        Position = position;
        Length = length;
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
