namespace Needlefish.Schema;

public readonly struct TypeDefinition
{
    public string Keyword { get; }

    public string Name { get; }

    public FieldDefinition[] FieldDefinitions { get; }

    public TypeDefinition(string keyword, string name, FieldDefinition[] fieldDefinitions)
    {
        Keyword = keyword;
        Name = name;
        FieldDefinitions = fieldDefinitions;
    }
}
