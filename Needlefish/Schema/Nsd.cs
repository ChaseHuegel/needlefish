namespace Needlefish.Schema;

public readonly struct Nsd
{
    public string Version { get; }

    public Define[] Defines { get; }

    public TypeDefinition[] TypeDefinitions { get; }

    public Nsd(string version, Define[] defines, TypeDefinition[] typeDefinitions)
    {
        Version = version;
        Defines = defines;
        TypeDefinitions = typeDefinitions;
    }
}