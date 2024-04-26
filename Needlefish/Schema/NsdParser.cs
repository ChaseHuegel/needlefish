using Needlefish.Lexing;
using Needlefish.Linting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Needlefish.Schema;

internal partial class NsdParser
{
    private static readonly TokenType[] FieldTokenTypes =
    [
        TokenType.Int,
        TokenType.Uint,
        TokenType.String,
        TokenType.Float,
        TokenType.Double,
        TokenType.Long,
        TokenType.Ulong,
        TokenType.Short,
        TokenType.UShort,
        TokenType.Bool,
        TokenType.Byte,
        TokenType.Identifier,
    ];

    private readonly Stack<Token<TokenType>> _tokenStack = new();
    private Token<TokenType> _lookaheadFirst;
    private Token<TokenType> _lookaheadSecond;

    private float _version;
    private readonly List<Define> _defines = new();
    private readonly List<TypeDefinition> _typeDefinitions = new();

    private void LoadStack(List<Token<TokenType>> tokens)
    {
        _tokenStack.Clear();

        int count = tokens.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            _tokenStack.Push(tokens[i]);
        }
    }

    private void CreateLookaheads()
    {
        _lookaheadFirst = _tokenStack.Pop();
        _lookaheadSecond = _tokenStack.Pop();
    }

    private bool TryReadToken(TokenType tokenType, out Token<TokenType> token)
    {
        if (_lookaheadFirst.Type != tokenType)
        {
            token = default;
            return false;
        }

        token = _lookaheadFirst;
        DiscardToken();
        return true;
    }

    private Token<TokenType> ReadToken(TokenType tokenType)
    {
        if (!TryReadToken(tokenType, out Token<TokenType> token))
        {
            throw new NsdException(string.Format("Expected ({0}) but found ({1}).", tokenType.ToString(), _lookaheadFirst.Value));
        }

        return token;
    }

    private void DiscardToken()
    {
        _lookaheadFirst = _lookaheadSecond;

        if (_tokenStack.Any())
        {
            _lookaheadSecond = _tokenStack.Pop();
        }
        else
        {
            _lookaheadSecond = new Token<TokenType>(TokenType.Undefined, string.Empty);
        }
    }

    private bool TryDiscardToken(TokenType tokenType)
    {
        if (_lookaheadFirst.Type != tokenType)
        {
            return false;
        }

        DiscardToken();
        return true;
    }

    private void DiscardToken(TokenType tokenType)
    {
        if (!TryDiscardToken(tokenType))
        {
            throw new NsdException(string.Format("Expected ({0}) but found ({1}).", tokenType.ToString(), _lookaheadFirst.Value));
        }
    }

    private void Terminate()
    {
        TryDiscardToken(TokenType.Whitespace);
        DiscardToken(TokenType.Terminate);
    }

    private void OpenBrace()
    {
        TryDiscardToken(TokenType.Whitespace);
        DiscardToken(TokenType.OpenBrace);
    }

    private void CloseBrace()
    {
        TryDiscardToken(TokenType.Whitespace);
        DiscardToken(TokenType.CloseBrace);
    }

    private bool TryCloseBrace()
    {
        TryDiscardToken(TokenType.Whitespace);
        return TryDiscardToken(TokenType.CloseBrace);
    }

    private Token<TokenType> ReadNameIdentifier()
    {
        Token<TokenType> identifier = ReadToken(TokenType.Identifier);
        if (identifier.Value.Contains('.'))
        {
            throw new NsdException(string.Format("Expected a name but found a namespace: \"{0}\".", identifier.Value));
        }

        return identifier;
    }

    /// <summary>
    ///     TypeSyntaxRequirements: enum identifier{values...}
    /// </summary>
    private bool TryCollectEnumDefinition()
    {
        if (!TryReadToken(TokenType.Enum, out Token<TokenType> keyword))
        {
            return false;
        }

        DiscardToken(TokenType.Whitespace);
        Token<TokenType> identifier = ReadNameIdentifier();
        OpenBrace();

        var valueDefinitions = new List<FieldDefinition>();
        bool closed = false;
        while (true)
        {
            if (TryCloseBrace())
            {
                closed = true;
                break;
            }

            if (TryReadEnumValueDefinition(out FieldDefinition valueDefinition))
            {
                valueDefinitions.Add(valueDefinition);
            }
            else
            {
                break;
            }
        }

        if (!closed)
        {
            CloseBrace();
        }

        var typeDefinition = new TypeDefinition(keyword.Value, identifier.Value, valueDefinitions.ToArray());
        _typeDefinitions.Add(typeDefinition);
        return true;
    }

    /// <summary>
    ///     TypeSyntaxRequirements: identifier; OR identifier=value;
    /// </summary>
    private bool TryReadEnumValueDefinition(out FieldDefinition fieldDefinition)
    {
        TryDiscardToken(TokenType.Whitespace);
        Token<TokenType> identifier = ReadNameIdentifier();

        TryDiscardToken(TokenType.Whitespace);

        int? value = null;
        if (TryReadToken(TokenType.Equals, out _))
        {
            TryDiscardToken(TokenType.Whitespace);
            Token<TokenType> assignment = ReadToken(TokenType.Number);
            if (int.TryParse(assignment.Value, out int parsedValue))
            {
                value = parsedValue;
            }
            else
            {
                throw new NsdException(string.Format("Enum values must be integers ranging {0} to {1} ({2}).", int.MinValue, int.MaxValue, identifier.Value));
            }
        }

        Terminate();

        fieldDefinition = new FieldDefinition(FieldType.None, null, identifier.Value, value, false, false);
        return true;
    }

    /// <summary>
    ///     TypeSyntaxRequirements: Message identifier{} OR Message identifier{fields...}
    /// </summary>
    private bool TryCollectMessageDefinition()
    {
        if (!TryReadToken(TokenType.Message, out Token<TokenType> keyword))
        {
            return false;
        }

        DiscardToken(TokenType.Whitespace);
        Token<TokenType> identifier = ReadNameIdentifier();
        OpenBrace();

        var fieldDefinitions = new List<FieldDefinition>();
        bool closed = false;
        while (true)
        {
            if (TryCloseBrace())
            {
                closed = true;
                break;
            }

            if (TryReadFieldDefinition(out FieldDefinition fieldDefinition))
            {
                fieldDefinitions.Add(fieldDefinition);
            }
            else
            {
                break;
            }
        }

        if (!closed)
        {
            CloseBrace();
        }

        var typeDefinition = new TypeDefinition(keyword.Value, identifier.Value, fieldDefinitions.ToArray());
        _typeDefinitions.Add(typeDefinition);
        return true;
    }

    /// <summary>
    ///     TypeSyntaxRequirements: token identifier=value; OR token[] identifier=value; OR token? identifier=value; OR token[]? identifier=value;
    /// </summary>
    private bool TryReadFieldDefinition(out FieldDefinition fieldDefinition)
    {
        TryDiscardToken(TokenType.Whitespace);
        Token<TokenType> token = ReadFieldType();

        bool hasWhitespace = false;

        hasWhitespace |= TryDiscardToken(TokenType.Whitespace);
        bool isArray = TryReadToken(TokenType.Array, out _);

        hasWhitespace |= TryDiscardToken(TokenType.Whitespace);
        bool isOptional = TryReadToken(TokenType.Optional, out _);

        hasWhitespace |= TryDiscardToken(TokenType.Whitespace);
        if (!hasWhitespace)
        {
            throw new NsdException(string.Format("Expected ({0}) but found ({1}).", TokenType.Whitespace, _lookaheadFirst.Value));
        }

        Token<TokenType> identifier = ReadNameIdentifier();
        TryDiscardToken(TokenType.Whitespace);

        ushort? id = null;
        if (TryReadToken(TokenType.Equals, out _))
        {
            TryDiscardToken(TokenType.Whitespace);
            Token<TokenType> assignment = ReadToken(TokenType.Number);
            if (ushort.TryParse(assignment.Value, out ushort parsedID))
            {
                id = parsedID;
            }
            else
            {
                throw new NsdException(string.Format("Field ({2}) must be an integer ranging {0} to {1}.", ushort.MinValue, ushort.MaxValue, identifier.Value));
            }
        }

        Terminate();

        FieldType fieldType = token.Type == TokenType.Identifier ? FieldType.Unknown : FieldType.Primitive;

        fieldDefinition = new FieldDefinition(fieldType, token.Value, identifier.Value, id, isOptional, isArray);
        return true;
    }

    private Token<TokenType> ReadFieldType()
    {
        Token<TokenType> fieldType;
        for (int i = 0; i < FieldTokenTypes.Length; i++)
        {
            TokenType fieldTokenType = FieldTokenTypes[i];
            if (TryReadToken(fieldTokenType, out fieldType))
            {
                return fieldType;
            }
        }

        throw new NsdException(string.Format("Unknown field token \"{0}\".", _lookaheadFirst.Value));
    }

    /// <summary>
    ///     TypeSyntaxRequirements: #identifier "text"|0.0|name.space; OR #identifier;
    /// </summary>
    private bool TryCollectDefine()
    {
        if (!TryReadToken(TokenType.Define, out _))
        {
            return false;
        }

        Token<TokenType> identifier = ReadNameIdentifier();
        Token<TokenType> value = ReadDefineValue(identifier);
        Terminate();

        var define = new Define(identifier.Value, value.Value);
        _defines.Add(define);
        return true;
    }

    private Token<TokenType> ReadDefineValue(Token<TokenType> identifier)
    {
        switch (identifier.Value)
        {
            case "version":
                DiscardToken(TokenType.Whitespace);
                return ReadToken(TokenType.Number);

            case "namespace":
                DiscardToken(TokenType.Whitespace);
                return ReadToken(TokenType.Identifier);

            case "include":
                DiscardToken(TokenType.Whitespace);
                var includeValue = ReadToken(TokenType.StringValue);
                return new Token<TokenType>(TokenType.StringValue, includeValue.Value.Trim('"'));

            default:
                throw new NsdException(string.Format("Unknown identifier \"{0}\".", identifier.Value));
        }
    }

    private void CollectVersion()
    {
        Define versionDefinition = _defines.FirstOrDefault(define => define.Key == "version");
        if (!float.TryParse(versionDefinition.Value, out _version) || _version == default)
        {
            throw new NsdException("Missing version definition.");
        }
    }

    private void PostProcess()
    {
        for (int typeIndex = 0; typeIndex < _typeDefinitions.Count; typeIndex++)
        {
            TypeDefinition definition = _typeDefinitions[typeIndex];

            int currentValue = -1;
            for (int fieldIndex = 0; fieldIndex < definition.FieldDefinitions.Length; fieldIndex++)
            {
                //  Set field values, incrementing as we go.
                FieldDefinition fieldDefinition = definition.FieldDefinitions[fieldIndex];
                if (fieldDefinition.Value.HasValue)
                {
                    currentValue = fieldDefinition.Value.Value;
                }
                else
                {
                    currentValue++;
                }

                //  Identify fields that have enum or object types.
                TypeDefinition fieldTypeDefinition = _typeDefinitions.FirstOrDefault(x => x.Name == fieldDefinition.TypeName);
                FieldType type = fieldDefinition.Type;
                if (fieldTypeDefinition.Keyword == "enum")
                {
                    type = FieldType.Enum;
                }
                else if (type == FieldType.Unknown)// if (!string.IsNullOrEmpty(fieldTypeDefinition.Keyword)) TODO re-introduce when supporting includes
                {
                    type = FieldType.Object;
                }

                definition.FieldDefinitions[fieldIndex] = new FieldDefinition(
                    type,
                    fieldDefinition.TypeName,
                    fieldDefinition.Name,
                    currentValue,
                    fieldDefinition.IsOptional,
                    fieldDefinition.IsArray
                );
            }
        }
    }

    private void Validate()
    {
        Issue[] issues = NsdLinter.Lint(_defines, _typeDefinitions);

        if (issues.Length > 0)
        {
            List<Exception> exceptions = new List<Exception>();
            foreach (Issue issue in issues.Where(x => x.Level == IssueLevel.Error))
            {
                exceptions.Add(new NsdException(issue.Message));
            }

            throw new NsdException("There were one or more syntax errors.", exceptions);
        }
    }

    private void ParseInternal()
    {
        while (_tokenStack.Count > 0 || !TryDiscardToken(TokenType.Undefined))
        {
            bool readAny = false;
            readAny |= TryDiscardToken(TokenType.Whitespace);
            readAny |= TryCollectDefine();

            readAny |= TryDiscardToken(TokenType.Whitespace);
            readAny |= TryCollectMessageDefinition();

            readAny |= TryDiscardToken(TokenType.Whitespace);
            readAny |= TryCollectEnumDefinition();

            if (!readAny)
            {
                throw new NsdException(string.Format("Encountered unexpected token: ({0}).", _lookaheadFirst.Value));
            }
        }
    }

    public Nsd Parse(List<Token<TokenType>> tokens)
    {
        if (tokens == null || tokens.Count == 0)
        {
            throw new ArgumentException("No nsd tokens to parse.", nameof(tokens));
        }

        _version = default;
        _defines.Clear();
        _typeDefinitions.Clear();

        LoadStack(tokens);

        CreateLookaheads();

        ParseInternal();

        PostProcess();

        Validate();

        CollectVersion();

        return new Nsd(_version, _defines.ToArray(), _typeDefinitions.ToArray());
    }
}