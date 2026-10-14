namespace Needlefish.Schema;

public readonly struct TypeDefinition
{
    public string Keyword { get; }

    public string Name { get; }

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
        int length = 0)
    {
        Keyword = keyword;
        Name = name;
        FieldDefinitions = fieldDefinitions;
        Position = position;
        Length = length;
    }
}
