using Needlefish.Linting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Needlefish.Schema;

/// <summary>
/// Resolves field types and field identifiers for a set of type definitions. Resolution can see
/// types declared in the same document as well as types declared in the documents it includes.
/// </summary>
internal static class NsdProcessor
{
    private const string EnumKeyword = "enum";

    /// <summary>
    /// Assigns each field its implicit or explicit value, and classifies fields whose type is
    /// declared in <paramref name="visibleTypes"/> as an object or an enum. Fields whose type
    /// cannot be found remain <see cref="FieldType.Unknown"/> so linting can report them.
    /// </summary>
    public static TypeDefinition[] ResolveTypeDefinitions(
        IReadOnlyList<TypeDefinition> typeDefinitions,
        IReadOnlyList<TypeDefinition> visibleTypes)
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
                if (TryFindType(visibleTypes, field.TypeName, out TypeDefinition fieldTypeDefinition))
                {
                    if (fieldTypeDefinition.Keyword == EnumKeyword)
                    {
                        type = FieldType.Enum;
                    }
                    else if (type == FieldType.Unknown)
                    {
                        type = FieldType.Object;
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
                    field.Length);
            }

            resolved[typeIndex] = new TypeDefinition(
                definition.Keyword,
                definition.Name,
                fields,
                definition.Position,
                definition.Length);
        }

        return resolved;
    }

    /// <summary>
    /// Runs the nsd linter, throwing an aggregate exception when any error level issue is found.
    /// </summary>
    public static void ThrowOnIssues(IReadOnlyList<Define> defines, IReadOnlyList<TypeDefinition> typeDefinitions)
    {
        Issue[] issues = NsdLinter.Lint(defines, typeDefinitions);

        if (issues.Length == 0)
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

    private static bool TryFindType(
        IReadOnlyList<TypeDefinition> typeDefinitions,
        string? name,
        out TypeDefinition definition)
    {
        if (name != null)
        {
            for (int i = 0; i < typeDefinitions.Count; i++)
            {
                TypeDefinition candidate = typeDefinitions[i];
                if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }
        }

        definition = default;
        return false;
    }
}
