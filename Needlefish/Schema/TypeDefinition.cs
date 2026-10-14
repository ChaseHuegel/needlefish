namespace Needlefish.Schema;

public readonly struct TypeDefinition
{
    public string Keyword { get; }

    public string Name { get; }

    /// <summary>
    /// The namespace the type is declared in, or an empty string for the global namespace.
    /// </summary>
    public string Namespace { get; }

    public FieldDefinition[] FieldDefinitions { get; }

    /// <summary>
    /// 0-based character offset of the type name in the source, or -1 when unknown.
    /// </summary>
    public int Position { get; }

    public int Length { get; }

    public TypeDefinition(
        string keyword,
        string name,
        FieldDefinition[] fieldDefinitions,
        int position = -1,
        int length = 0,
        string namespaceName = "")
    {
        Keyword = keyword;
        Name = name;
        Namespace = namespaceName;
        FieldDefinitions = fieldDefinitions;
        Position = position;
        Length = length;
    }

    /// <summary>
    /// The type name qualified with its namespace, for example <c>Other.Namespace.Foo</c>.
    /// </summary>
    public string GetQualifiedName()
    {
        return string.IsNullOrEmpty(Namespace) ? Name : Namespace + "." + Name;
    }
}
