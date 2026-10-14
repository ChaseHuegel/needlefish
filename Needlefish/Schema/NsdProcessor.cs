using Needlefish.Linting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Needlefish.Schema;

/// <summary>
/// Resolves field types and field identifiers for a set of type definitions. Resolution can see
/// types declared in the same document as well as types declared in the documents it includes,
/// including types declared in other namespaces.
/// </summary>
internal static class NsdProcessor
{
    private const string EnumKeyword = "enum";

    private enum TypeResolution
    {
        Resolved,
        NotFound,
        Ambiguous,
    }

    /// <summary>
    /// Assigns each field its implicit or explicit value and resolves its type. Fields whose type
    /// is declared in <paramref name="visibleTypes"/> are classified as an object or an enum and
    /// receive the C# type reference to emit. Problems are appended to <paramref name="issues"/>.
    /// </summary>
    public static TypeDefinition[] ResolveTypeDefinitions(
        IReadOnlyList<TypeDefinition> typeDefinitions,
        IReadOnlyList<TypeDefinition> visibleTypes,
        string currentNamespace,
        ICollection<Issue> issues)
    {
        var resolved = new TypeDefinition[typeDefinitions.Count];

        for (int typeIndex = 0; typeIndex < typeDefinitions.Count; typeIndex++)
        {
            TypeDefinition definition = typeDefinitions[typeIndex];

            int currentValue = -1;
            var fields = new FieldDefinition[definition.FieldDefinitions.Length];
            for (int fieldIndex = 0; fieldIndex < definition.FieldDefinitions.Length; fieldIndex++)
            {
                FieldDefinition field = definition.FieldDefinitions[fieldIndex];
                if (field.Value.HasValue)
                {
                    currentValue = field.Value.Value;
                }
                else
                {
                    currentValue++;
                }

                FieldType type = field.Type;
                string? typeReference = field.TypeName;

                if (field.TypeName != null && field.Type == FieldType.Unknown)
                {
                    switch (ResolveType(visibleTypes, field.TypeName, currentNamespace, out TypeDefinition resolvedType, out List<TypeDefinition>? ambiguousMatches))
                    {
                        case TypeResolution.Resolved:
                            type = resolvedType.Keyword == EnumKeyword ? FieldType.Enum : FieldType.Object;
                            typeReference = GetTypeReference(resolvedType, currentNamespace);
                            break;

                        case TypeResolution.Ambiguous:
                            issues.Add(new Issue(FormatAmbiguous(field, ambiguousMatches!), position: field.Position, length: field.Length));
                            break;

                        default:
                            issues.Add(new Issue(
                                string.Format("Unknown type ({0}) for field ({1}). Is there a missing include?", field.TypeName, field.Name),
                                position: field.Position,
                                length: field.Length));
                            break;
                    }
                }

                fields[fieldIndex] = new FieldDefinition(
                    type,
                    field.TypeName,
                    field.Name,
                    currentValue,
                    field.IsOptional,
                    field.IsArray,
                    field.Position,
                    field.Length,
                    typeReference);
            }

            resolved[typeIndex] = new TypeDefinition(
                definition.Keyword,
                definition.Name,
                fields,
                definition.Position,
                definition.Length,
                definition.Namespace);
        }

        return resolved;
    }

    /// <summary>
    /// Throws an aggregate exception when any error level issue is present.
    /// </summary>
    public static void ThrowOnIssues(IReadOnlyList<Issue> issues)
    {
        if (issues.Count == 0)
        {
            return;
        }

        List<Exception> exceptions = new List<Exception>();
        foreach (Issue issue in issues.Where(x => x.Level == IssueLevel.Error))
        {
            exceptions.Add(new NsdException(issue.Message, issue.Position, issue.Length));
        }

        throw new NsdException("There were one or more syntax errors.", exceptions);
    }

    private static TypeResolution ResolveType(
        IReadOnlyList<TypeDefinition> visibleTypes,
        string reference,
        string currentNamespace,
        out TypeDefinition resolved,
        out List<TypeDefinition>? ambiguousMatches)
    {
        ambiguousMatches = null;

        //  A dotted reference names an exact namespace, for example "Other.Foo" or "A.B.Foo".
        if (reference.IndexOf('.') >= 0)
        {
            for (int i = 0; i < visibleTypes.Count; i++)
            {
                TypeDefinition candidate = visibleTypes[i];
                if (string.Equals(candidate.GetQualifiedName(), reference, StringComparison.Ordinal))
                {
                    resolved = candidate;
                    return TypeResolution.Resolved;
                }
            }

            resolved = default;
            return TypeResolution.NotFound;
        }

        //  A simple name prefers the current namespace.
        for (int i = 0; i < visibleTypes.Count; i++)
        {
            TypeDefinition candidate = visibleTypes[i];
            if (string.Equals(candidate.Name, reference, StringComparison.Ordinal)
                && string.Equals(candidate.Namespace, currentNamespace, StringComparison.Ordinal))
            {
                resolved = candidate;
                return TypeResolution.Resolved;
            }
        }

        int matchCount = 0;
        TypeDefinition match = default;
        List<TypeDefinition>? matches = null;

        for (int i = 0; i < visibleTypes.Count; i++)
        {
            TypeDefinition candidate = visibleTypes[i];
            if (!string.Equals(candidate.Name, reference, StringComparison.Ordinal))
            {
                continue;
            }

            matchCount++;
            if (matchCount == 1)
            {
                match = candidate;
            }
            else
            {
                matches ??= new List<TypeDefinition> { match };
                matches.Add(candidate);
            }
        }

        if (matchCount == 0)
        {
            resolved = default;
            return TypeResolution.NotFound;
        }

        if (matchCount == 1)
        {
            resolved = match;
            return TypeResolution.Resolved;
        }

        resolved = default;
        ambiguousMatches = matches;
        return TypeResolution.Ambiguous;
    }

    private static string GetTypeReference(TypeDefinition type, string currentNamespace)
    {
        if (string.Equals(type.Namespace, currentNamespace, StringComparison.Ordinal))
        {
            return type.Name;
        }

        return string.IsNullOrEmpty(type.Namespace)
            ? "global::" + type.Name
            : "global::" + type.Namespace + "." + type.Name;
    }

    private static string FormatAmbiguous(FieldDefinition field, List<TypeDefinition> matches)
    {
        string candidates = string.Join(" and ", matches.Select(match => "'" + match.GetQualifiedName() + "'"));
        return string.Format(
            "Ambiguous type ({0}) for field ({1}). Declared as {2}. Qualify the type with its namespace.",
            field.TypeName,
            field.Name,
            candidates);
    }
}
