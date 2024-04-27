namespace Needlefish.Schema;

internal enum TokenType
{
    Undefined = -1,
    Define,
    Number,
    Terminate,
    StringValue,
    Identifier,
    Whitespace,
    Array,
    Optional,
    Equals,
    OpenBrace,
    CloseBrace,
    Message,
    Enum,
    String,
    Int,
    Float,
    Double,
    Long,
    Uint,
    Bool,
    Byte,
    Ulong,
    Short,
    UShort,
    Namespace
}