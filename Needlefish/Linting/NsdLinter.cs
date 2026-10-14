using System.Collections.Generic;
using System.Linq;
using Needlefish.Schema;

namespace Needlefish.Linting;

internal class NsdLinter
{
    private static void ValidateVersion(List<Issue> issues, IReadOnlyList<Define> defines)
    {
        Define versionDefinition = defines.FirstOrDefault(define => define.Key == "version");
        if (!float.TryParse(versionDefinition.Value, out _))
        {
            issues.Add(new Issue("Missing version definition."));
        }
    }

    private static void ValidateFieldDefinitions(List<Issue> issues, IReadOnlyList<TypeDefinition> typeDefinitions)
    {
        foreach (FieldDefinition field in typeDefinitions.SelectMany(def => def.FieldDefinitions))
        {
            if (field.Type == FieldType.Unknown)
            {
                issues.Add(new Issue(string.Format("Unknown type ({0}) for field ({1}). Is there a missing include?", field.TypeName, field.Name), position: field.Position, length: field.Length));
            }
        }
    }

    private static void ValidateNoValueDuplicates(List<Issue> issues, TypeDefinition type)
    {
        FieldDefinition[] duplicates = type.FieldDefinitions.GroupBy(f => f.Value).Where(g => g.Count() > 1).SelectMany(g => g).ToArray();

        if (duplicates.Length > 0)
        {
            for (int duplicateIndex = 0; duplicateIndex < duplicates.Length; duplicateIndex++)
            {
                FieldDefinition duplicate = duplicates[duplicateIndex];
                issues.Add(new Issue(string.Format("Duplicate field value. TypeName: ({0}), Field: ({1}), Value: ({2}).", type.Name, duplicate.Name, duplicate.Value), position: duplicate.Position, length: duplicate.Length));
            }
        }
    }

    private static void ValidateNoFieldDuplicates(List<Issue> issues, TypeDefinition type)
    {
        FieldDefinition[] duplicates = type.FieldDefinitions.GroupBy(f => f.Name).Where(g => g.Count() > 1).SelectMany(g => g).ToArray();

        if (duplicates.Length > 0)
        {
            for (int i = 0; i < duplicates.Length; i++)
            {
                FieldDefinition fieldDuplicate = duplicates[i];
                issues.Add(new Issue(string.Format("Duplicate field. TypeName: ({0}), Field: ({1}).", type.Name, fieldDuplicate.Name), position: fieldDuplicate.Position, length: fieldDuplicate.Length));
            }
        }
    }

    private static void ValidateEnumFieldDefinitions(List<Issue> issues, TypeDefinition type)
    {
        for (int i = 0; i < type.FieldDefinitions.Length; i++)
        {
            FieldDefinition field = type.FieldDefinitions[i];
            if (field.TypeName != null)
            {
                issues.Add(new Issue(string.Format("Enums may not contain typed fields. Enum: ({0}), Value: ({1}).", type.Name, field.Name), position: field.Position, length: field.Length));
            }

            if (field.IsOptional)
            {
                issues.Add(new Issue(string.Format("Enums fields may not be optional. Enum: ({0}), Value: ({1}).", type.Name, field.Name), position: field.Position, length: field.Length));
            }

            if (field.IsArray)
            {
                issues.Add(new Issue(string.Format("Enums fields may not be arrays. Enum: ({0}), Value: ({1}).", type.Name, field.Name), position: field.Position, length: field.Length));
            }
        }
    }

    private static void ValidateEnumDefinitions(List<Issue> issues, IReadOnlyList<TypeDefinition> typeDefinitions)
    {
        foreach (TypeDefinition type in typeDefinitions.Where(def => def.Keyword == "enum"))
        {
            ValidateNoFieldDuplicates(issues, type);
            ValidateNoValueDuplicates(issues, type);
            ValidateEnumFieldDefinitions(issues, type);
        }
    }

    private static void ValidateMessageDefinitions(List<Issue> issues, IReadOnlyList<TypeDefinition> typeDefinitions)
    {
        foreach (TypeDefinition type in typeDefinitions.Where(def => def.Keyword == "message"))
        {
            ValidateNoFieldDuplicates(issues, type);
            ValidateNoValueDuplicates(issues, type);
        }
    }

    private static void ValidateNoTypeDuplicates(List<Issue> issues, IEnumerable<TypeDefinition> typeDefinitions)
    {
        TypeDefinition[] duplicates = typeDefinitions.GroupBy(typeDefinition => typeDefinition.Name).Where(g => g.Count() > 1).SelectMany(g => g).ToArray();

        if (duplicates.Length > 0)
        {
            for (int i = 0; i < duplicates.Length; i++)
            {
                TypeDefinition typeDuplicate = duplicates[i];
                issues.Add(new Issue(string.Format("Duplicate type. TypeName: ({0}).", typeDuplicate.Name), position: typeDuplicate.Position, length: typeDuplicate.Length));
            }
        }
    }

    public static Issue[] Lint(IReadOnlyList<Define> defines, IReadOnlyList<TypeDefinition> typeDefinitions)
    {
        var issues = new List<Issue>();

        ValidateVersion(issues, defines);
        ValidateNoTypeDuplicates(issues, typeDefinitions);
        ValidateFieldDefinitions(issues, typeDefinitions);
        ValidateMessageDefinitions(issues, typeDefinitions);
        ValidateEnumDefinitions(issues, typeDefinitions);

        return issues.ToArray();
    }
}