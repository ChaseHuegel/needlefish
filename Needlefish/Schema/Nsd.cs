namespace Needlefish.Schema;

public readonly struct Nsd
{
    public float Version { get; }

    public Define[] Defines { get; }

    public TypeDefinition[] TypeDefinitions { get; }

    public Nsd(float version, Define[] defines, TypeDefinition[] typeDefinitions)
    {
        Version = version;
        Defines = defines;
        TypeDefinitions = typeDefinitions;
    }
}