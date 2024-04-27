﻿using Needlefish.Schema;
using System.Linq;
using System.Text;

namespace Needlefish.Compile;

internal class Nsd1Compiler : INsdCompiler
{
    internal const string Indent = "    ";

    private static readonly string[] Prepends = [
        "#pragma warning disable CS0219 // Variable is assigned but its value is never used",
        "#pragma warning disable CS8602 // Dereference of a possibly null reference.",
        "using System;",
        "using System.Buffers.Binary;",
    ];

    private readonly INsdTypeCompiler[] TypeCompilers = [
        new Nsd1MessageCompiler(),
        new Nsd1EnumCompiler()
    ];

    public float Version => 1;

    public string Compile(Nsd nsd, string? sourceName = null)
    {
        StringBuilder builder = new();

        builder.AppendLine("/// <auto-generated>");

        builder.AppendLine("/// Generated by the nsd compiler. Do not modify!");
        builder.AppendLine($"/// Language version: {Version}");

        if (!string.IsNullOrWhiteSpace(sourceName))
        {
            builder.AppendLine($"/// Source: {sourceName}");
        }

        builder.AppendLine("/// </auto-generated>");
        builder.AppendLine();

        foreach (string usingStr in Prepends)
        {
            builder.AppendLine(usingStr);
        }

        builder.AppendLine();

        Define namespaceDefine = nsd.Defines.FirstOrDefault(define => define.Key == "namespace");
        bool hasNamespace = !string.IsNullOrWhiteSpace(namespaceDefine.Value);
        if (hasNamespace)
        {
            builder.AppendLine($"namespace {namespaceDefine.Value}");
            builder.AppendLine("{");
        }

        foreach (TypeDefinition typeDefinition in nsd.TypeDefinitions.OrderBy(d => d.Keyword))
        {
            foreach (INsdTypeCompiler? typeCompiler in TypeCompilers.Where(c => c.CanCompile(typeDefinition)))
            {
                StringBuilder typeBuilder = typeCompiler.Compile(typeDefinition);

                if (hasNamespace)
                {
                    typeBuilder.Insert(0, Indent);
                    typeBuilder.Replace("\n", "\n" + Indent);
                }

                builder.Append(typeBuilder);
                builder.AppendLine();
            }
        }

        if (hasNamespace)
        {
            builder.AppendLine("}");
        }

        return builder.ToString();
    }
}