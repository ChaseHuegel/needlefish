using System.Text;

namespace Needlefish.Schema;

public readonly struct FieldDefinition
{
    public FieldType Type { get; }
    public string? TypeName { get; }

    /// <summary>
    /// The C# type reference to emit for the field, for example <c>global::Other.Namespace.Foo</c>
    /// for a type declared in another namespace. Falls back to <see cref="TypeName"/> when null.
    /// </summary>
    public string? TypeReference { get; }

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
        int length = 0,
        string? typeReference = null)
    {
        Type = type;
        TypeName = typeName;
        TypeReference = typeReference;
        Name = name;
        Value = value;
        IsOptional = isOptional;
        IsArray = isArray;
        Position = position;
        Length = length;
    }

    /// <summary>
    /// The C# type reference of the field's element type.
    /// </summary>
    public string GetTypeReference()
    {
        return TypeReference ?? TypeName ?? string.Empty;
    }

    public string GetFullyQualifiedType()
    {
        StringBuilder builder = new(GetTypeReference());

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
