using Needlefish;
using Needlefish.Schema;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Needlefish.Tests;

internal class IncludeTests
{
    private const string RootPath = "/project/Root.nsd";

    [Test]
    public void IncludedTypesAreVisibleAndOnlyOwnTypesAreEmitted()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Shared.nsd";

            message Root
            {
                SharedMessage Child;
                SharedEnum[] Values;
            }
            """;

        const string shared = """
            #version 1.0;
            #namespace Test;

            message SharedMessage
            {
                int Id;
            }

            enum SharedEnum
            {
                A;
                B;
            }
            """;

        NsdEmitResult result = Emit(root, (SharedPath, shared));

        Assert.That(result.Source, Is.Not.Null);
        Assert.That(result.Diagnostics.Where(d => d.Level == NsdDiagnosticLevel.Error), Is.Empty);

        StringAssert.Contains("public SharedMessage Child;", result.Source);
        StringAssert.Contains("public SharedEnum[] Values;", result.Source);
        StringAssert.Contains("(Values.Length * enumLen)", result.Source);

        // Same namespace references stay unqualified.
        StringAssert.DoesNotContain("global::", result.Source);

        // Included types are compiled as their own document.
        StringAssert.DoesNotContain("struct SharedMessage", result.Source);
        StringAssert.DoesNotContain("enum SharedEnum", result.Source);
    }

    [Test]
    public void IncludesResolveRelativeToIncludingFile()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Models/Parent.nsd";

            message Root
            {
                Child Child;
            }
            """;

        const string parent = """
            #version 1.0;
            #namespace Test;
            #include "Child.nsd";

            message Parent
            {
                Child Child;
            }
            """;

        const string child = """
            #version 1.0;
            #namespace Test;

            message Child
            {
                int Id;
            }
            """;

        NsdEmitResult result = Emit(
            root,
            ("/project/Models/Parent.nsd", parent),
            ("/project/Models/Child.nsd", child));

        Assert.That(result.Source, Is.Not.Null);
        Assert.That(result.Diagnostics, Is.Empty);
        StringAssert.Contains("public Child Child;", result.Source);
    }

    [Test]
    public void DiamondIncludesAreLoadedOnce()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Left.nsd";
            #include "Right.nsd";

            message Root
            {
                Shared Left;
                Shared Right;
            }
            """;

        NsdEmitResult result = Emit(
            root,
            ("/project/Left.nsd", IncludeSharedNsd),
            ("/project/Right.nsd", IncludeSharedNsd),
            (SharedPath, Shared));

        Assert.That(result.Source, Is.Not.Null);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void MissingIncludeReportsErrorAtDirective()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Missing.nsd";

            message Root
            {
                int Id;
            }
            """;

        NsdEmitResult result = Emit(root);

        Assert.That(result.Source, Is.Null);

        NsdDiagnostic diagnostic = result.Diagnostics.Single(d => d.Level == NsdDiagnosticLevel.Error);
        Assert.That(diagnostic.FilePath, Is.EqualTo(NsdPaths.Normalize(RootPath)));
        Assert.That(diagnostic.Position, Is.EqualTo(root.IndexOf("\"Missing.nsd\"", StringComparison.Ordinal)));
        Assert.That(diagnostic.Length, Is.EqualTo("\"Missing.nsd\"".Length));
        StringAssert.Contains("Missing.nsd", diagnostic.Message);
    }

    [Test]
    public void IncludeCycleReportsError()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Other.nsd";

            message Root
            {
                int Id;
            }
            """;

        const string other = """
            #version 1.0;
            #namespace Test;
            #include "Root.nsd";

            message Other
            {
                int Id;
            }
            """;

        NsdEmitResult result = Emit(root, ("/project/Other.nsd", other));

        Assert.That(result.Source, Is.Null);
        Assert.That(
            result.Diagnostics.Any(d => d.Message.Contains("Root.nsd -> Other.nsd -> Root.nsd", StringComparison.Ordinal)),
            Is.True);

        NsdDiagnostic cycle = result.Diagnostics.First(d =>
            d.FilePath == NsdPaths.Normalize("/project/Other.nsd") && d.Message.Contains("cycle", StringComparison.Ordinal));
        StringAssert.Contains("Root.nsd -> Other.nsd -> Root.nsd", cycle.Message);
    }

    [Test]
    public void CrossNamespaceIncludeQualifiesGeneratedReferences()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Shared.nsd";

            message Root
            {
                Shared Value;
                Shared[] Items;
                Status State;
            }
            """;

        const string shared = """
            #version 1.0;
            #namespace Other;

            message Shared
            {
                int Id;
            }

            enum Status
            {
                Active;
                Disabled;
            }
            """;

        NsdEmitResult result = Emit(root, (SharedPath, shared));

        Assert.That(result.Source, Is.Not.Null);
        Assert.That(result.Diagnostics, Is.Empty);

        StringAssert.Contains("public global::Other.Shared Value;", result.Source);
        StringAssert.Contains("public global::Other.Shared[] Items;", result.Source);
        StringAssert.Contains("global::Other.Status _State", result.Source);
        StringAssert.Contains("new global::Other.Shared[", result.Source);
        StringAssert.Contains("(global::Other.Status)", result.Source);
    }

    [Test]
    public void QualifiedReferenceDisambiguatesDuplicateNames()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Left.nsd";
            #include "Right.nsd";

            message Root
            {
                Left.Shared LeftItem;
                Right.Shared RightItem;
            }
            """;

        NsdEmitResult result = Emit(
            root,
            ("/project/Left.nsd", NamespaceLeftNsd),
            ("/project/Right.nsd", NamespaceRightNsd));

        Assert.That(result.Source, Is.Not.Null);
        Assert.That(result.Diagnostics, Is.Empty);

        StringAssert.Contains("public global::Left.Shared LeftItem;", result.Source);
        StringAssert.Contains("public global::Right.Shared RightItem;", result.Source);
    }

    [Test]
    public void AmbiguousSimpleNameReportsError()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Left.nsd";
            #include "Right.nsd";

            message Root
            {
                Shared Value;
            }
            """;

        NsdEmitResult result = Emit(
            root,
            ("/project/Left.nsd", NamespaceLeftNsd),
            ("/project/Right.nsd", NamespaceRightNsd));

        Assert.That(result.Source, Is.Null);

        NsdDiagnostic diagnostic = result.Diagnostics.Single(d => d.Level == NsdDiagnosticLevel.Error);
        StringAssert.Contains("Ambiguous type (Shared)", diagnostic.Message);
        StringAssert.Contains("'Left.Shared'", diagnostic.Message);
        StringAssert.Contains("'Right.Shared'", diagnostic.Message);
    }

    [Test]
    public void SimpleNamePrefersCurrentNamespace()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Shared.nsd";

            message Shared
            {
                int Id;
            }

            message Root
            {
                Shared Value;
            }
            """;

        const string shared = """
            #version 1.0;
            #namespace Other;

            message Shared
            {
                int Id;
            }
            """;

        NsdEmitResult result = Emit(root, (SharedPath, shared));

        Assert.That(result.Source, Is.Not.Null);
        Assert.That(result.Diagnostics, Is.Empty);

        StringAssert.Contains("public Shared Value;", result.Source);
        StringAssert.DoesNotContain("global::Other.Shared", result.Source);
    }

    [Test]
    public void GlobalNamespaceIncludeQualifiesGeneratedReferences()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Shared.nsd";

            message Root
            {
                Shared Value;
            }
            """;

        const string shared = """
            #version 1.0;

            message Shared
            {
                int Id;
            }
            """;

        NsdEmitResult result = Emit(root, (SharedPath, shared));

        Assert.That(result.Source, Is.Not.Null);
        Assert.That(result.Diagnostics, Is.Empty);

        StringAssert.Contains("public global::Shared Value;", result.Source);
    }

    [Test]
    public void DottedNamespaceQualifiedReference()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Shared.nsd";

            message Root
            {
                Other.Types.Shared Value;
            }
            """;

        const string shared = """
            #version 1.0;
            #namespace Other.Types;

            message Shared
            {
                int Id;
            }
            """;

        NsdEmitResult result = Emit(root, (SharedPath, shared));

        Assert.That(result.Source, Is.Not.Null);
        Assert.That(result.Diagnostics, Is.Empty);

        StringAssert.Contains("public global::Other.Types.Shared Value;", result.Source);
    }

    [Test]
    public void DuplicateTypeAcrossFilesReportsError()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Shared.nsd";

            message Shared
            {
                int Id;
            }
            """;

        const string shared = """
            #version 1.0;
            #namespace Test;

            message Shared
            {
                int Other;
            }
            """;

        NsdEmitResult result = Emit(root, (SharedPath, shared));

        Assert.That(result.Source, Is.Null);

        NsdDiagnostic diagnostic = result.Diagnostics.Single(d => d.Level == NsdDiagnosticLevel.Error);
        StringAssert.Contains("Duplicate type (Test.Shared)", diagnostic.Message);
        StringAssert.Contains("Root.nsd", diagnostic.Message);
        StringAssert.Contains("Shared.nsd", diagnostic.Message);
        Assert.That(diagnostic.FilePath, Is.EqualTo(NsdPaths.Normalize(RootPath)));
    }

    [Test]
    public void DuplicateTypeBetweenIncludedFilesReportsErrorAtInclude()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Left.nsd";
            #include "Right.nsd";

            message Root
            {
                Left Value;
            }
            """;

        const string left = """
            #version 1.0;
            #namespace Test;

            message Shared
            {
                int Id;
            }

            message Left
            {
                int Id;
            }
            """;

        const string right = """
            #version 1.0;
            #namespace Test;

            message Shared
            {
                int Other;
            }

            message Right
            {
                int Id;
            }
            """;

        NsdEmitResult result = Emit(
            root,
            ("/project/Left.nsd", left),
            ("/project/Right.nsd", right));

        Assert.That(result.Source, Is.Null);

        NsdDiagnostic diagnostic = result.Diagnostics.First(d => d.Message.Contains("Duplicate type (Test.Shared)", StringComparison.Ordinal));
        Assert.That(diagnostic.FilePath, Is.EqualTo(NsdPaths.Normalize(RootPath)));
        Assert.That(diagnostic.Position, Is.EqualTo(root.IndexOf("\"Right.nsd\"", StringComparison.Ordinal)));
        StringAssert.Contains("Left.nsd", diagnostic.Message);
        StringAssert.Contains("Right.nsd", diagnostic.Message);
    }

    [Test]
    public void UnknownTypeWithoutIncludeReportsError()
    {
        const string root = """
            #version 1.0;
            #namespace Test;

            message Root
            {
                Missing Value;
            }
            """;

        const string shared = """
            #version 1.0;
            #namespace Test;

            message Missing
            {
                int Id;
            }
            """;

        NsdEmitResult result = Emit(root, (SharedPath, shared));

        Assert.That(result.Source, Is.Null);

        NsdDiagnostic diagnostic = result.Diagnostics.Single(d => d.Level == NsdDiagnosticLevel.Error);
        StringAssert.Contains("Unknown type (Missing)", diagnostic.Message);
        StringAssert.Contains("missing include", diagnostic.Message);
    }

    [Test]
    public void IncludedFileThatIsNotCompiledReportsWarning()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Shared.nsd";

            message Root
            {
                Shared Shared;
            }
            """;

        NsdEmitResult result = Emit(root, new[] { RootPath }, (SharedPath, Shared));

        Assert.That(result.Source, Is.Not.Null);

        NsdDiagnostic warning = result.Diagnostics.Single();
        Assert.That(warning.Level, Is.EqualTo(NsdDiagnosticLevel.Warning));
        StringAssert.Contains("will not be generated", warning.Message);
        Assert.That(warning.FilePath, Is.EqualTo(NsdPaths.Normalize(RootPath)));
    }

    [Test]
    public void IncludedFileMustBeAStandaloneDocument()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Shared.nsd";

            message Root
            {
                Shared Shared;
            }
            """;

        const string shared = """
            #namespace Test;

            message Shared
            {
                int Id;
            }
            """;

        NsdEmitResult result = Emit(root, (SharedPath, shared));

        Assert.That(result.Source, Is.Null);
        Assert.That(result.Diagnostics.Any(d => d.Message.Contains("Missing version definition", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void IncludesWithoutResolverReportError()
    {
        const string root = """
            #version 1.0;
            #namespace Test;
            #include "Shared.nsd";

            message Root
            {
                int Id;
            }
            """;

        NsdEmitResult result = new Nsd1Emitter(new EmitterOptions()).TryEmit("Root", root);

        Assert.That(result.Source, Is.Null);

        NsdDiagnostic diagnostic = result.Diagnostics.Single();
        Assert.That(diagnostic.Level, Is.EqualTo(NsdDiagnosticLevel.Error));
        StringAssert.Contains("Shared.nsd", diagnostic.Message);
    }

    private const string SharedPath = "/project/Shared.nsd";

    private const string NamespaceLeftNsd = """
        #version 1.0;
        #namespace Left;

        message Shared
        {
            int Id;
        }
        """;

    private const string NamespaceRightNsd = """
        #version 1.0;
        #namespace Right;

        message Shared
        {
            int Id;
        }
        """;

    private const string Shared = """
        #version 1.0;
        #namespace Test;

        message Shared
        {
            int Id;
        }
        """;

    private const string IncludeSharedNsd = """
        #version 1.0;
        #namespace Test;
        #include "Shared.nsd";
        """;

    private static NsdEmitResult Emit(
        string root,
        params (string Path, string Content)[] includes)
    {
        return Emit(root, (IReadOnlyList<string>?)null, includes);
    }

    private static NsdEmitResult Emit(
        string root,
        IReadOnlyList<string>? knownRoots,
        params (string Path, string Content)[] includes)
    {
        var files = new Dictionary<string, string>(NsdPaths.Comparer)
        {
            [NsdPaths.Normalize(RootPath)] = root,
        };

        foreach ((string path, string content) in includes)
        {
            files[NsdPaths.Normalize(path)] = content;
        }

        var resolver = new InMemoryNsdFileResolver(files);

        return new Nsd1Emitter(new EmitterOptions()).TryEmit(
            NsdPaths.Normalize(RootPath),
            root,
            resolver,
            knownRoots?.Select(NsdPaths.Normalize).ToArray());
    }

    private sealed class InMemoryNsdFileResolver(IReadOnlyDictionary<string, string> files) : INsdFileResolver
    {
        public NsdFileResolution Resolve(string includingFilePath, string includePath)
        {
            string path = NsdPaths.ResolveIncludePath(includingFilePath, includePath);

            return files.TryGetValue(path, out string? content)
                ? NsdFileResolution.Resolved(path, content)
                : NsdFileResolution.Failed(
                    path,
                    string.Format("Included file '{0}' was not found (looked for '{1}').", includePath, path));
        }
    }
}
