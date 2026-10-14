using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Needlefish.Generators;
using NUnit.Framework;

namespace Needlefish.Tests;

internal class SourceGeneratorTests
{
    private const string ProjectDirKey = "build_property.ProjectDir";
    private const string PartialPropertyKey = "build_property.NeedlefishPartial";
    private const string PartialMetadataKey = "build_metadata.AdditionalFiles.NeedlefishPartial";
    private const string AllowUnsafeBlocksKey = "build_property.AllowUnsafeBlocks";

    private const string TypesNsd = """
        #version 1.0;
        #namespace Test;

        message Order
        {
            int Id;
        }
        """;

    private const string SYNTAX_ERROR_NSD = """
        #version 1.0;
        #namespace Test;

        message M
        {
            int x
        }
        """;

    [Test]
    public void GeneratesSameSourceAsNsdc()
    {
        string expected = new Nsd1Emitter(new EmitterOptions()).Emit("LexerTests.ValidNsd", LexerTests.VALID_NSD);

        GeneratorDriverRunResult result = Run(new[] { new TestFile("/project/LexerTests.ValidNsd.nsd", LexerTests.VALID_NSD) });

        Assert.That(result.Diagnostics, Is.Empty);

        GeneratedSourceResult source = result.Results[0].GeneratedSources.Single();
        Assert.That(source.HintName, Is.EqualTo("LexerTests.ValidNsd.g.cs"));
        Assert.That(source.SourceText.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void PartialOptionIsReadFromBuildProperty()
    {
        GeneratorDriverRunResult result = Run(
            new[] { new TestFile("/project/Types.nsd", TypesNsd) },
            new Dictionary<string, string> { [PartialPropertyKey] = "true" });

        StringAssert.Contains("public partial struct Order", GeneratedSource(result));
    }

    [Test]
    public void PerFileOptionOverridesGlobalOption()
    {
        var file = new TestFile(
            "/project/Types.nsd",
            TypesNsd,
            new Dictionary<string, string> { [PartialMetadataKey] = "false" });

        GeneratorDriverRunResult result = Run(
            new[] { file },
            new Dictionary<string, string> { [PartialPropertyKey] = "true" });

        StringAssert.Contains("public struct Order", GeneratedSource(result));
        StringAssert.DoesNotContain("public partial struct Order", GeneratedSource(result));
    }

    [Test]
    public void InvalidOptionValueReportsWarning()
    {
        GeneratorDriverRunResult result = Run(
            new[] { new TestFile("/project/Types.nsd", TypesNsd) },
            new Dictionary<string, string> { [PartialPropertyKey] = "yes" });

        Diagnostic diagnostic = result.Diagnostics.Single();
        Assert.That(diagnostic.Id, Is.EqualTo("NSD0001"));
        Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Warning));

        // Generation still happens with the default option value.
        StringAssert.Contains("public struct Order", GeneratedSource(result));
    }

    [Test]
    public void SyntaxErrorReportsLocation()
    {
        GeneratorDriverRunResult result = Run(new[] { new TestFile("/project/Broken.nsd", SYNTAX_ERROR_NSD) });

        Diagnostic diagnostic = result.Diagnostics.Single(d => d.Id == "NSD1001");
        Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));

        FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
        Assert.That(span.Path, Is.EqualTo("/project/Broken.nsd"));
        Assert.That(span.StartLinePosition.Line, Is.EqualTo(6));
        Assert.That(span.StartLinePosition.Character, Is.EqualTo(0));
        Assert.That(result.Results[0].GeneratedSources, Is.Empty);
    }

    [Test]
    public void LexerErrorReportsLocation()
    {
        GeneratorDriverRunResult result = Run(new[] { new TestFile("/project/Broken.nsd", "a + b") });

        Diagnostic diagnostic = result.Diagnostics.Single(d => d.Id == "NSD1001");
        FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
        Assert.That(span.StartLinePosition.Line, Is.EqualTo(0));
        Assert.That(span.StartLinePosition.Character, Is.EqualTo(2));
        Assert.That(span.EndLinePosition.Character - span.StartLinePosition.Character, Is.EqualTo(1));
    }

    [Test]
    public void DuplicateAdditionalFilesGenerateOnce()
    {
        GeneratorDriverRunResult result = Run(new[]
        {
            new TestFile("/project/Types.nsd", TypesNsd),
            new TestFile("/project/Types.nsd", TypesNsd),
        });

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Results[0].GeneratedSources.Length, Is.EqualTo(1));
    }

    [Test]
    public void HintNamesIncludeRelativeDirectory()
    {
        GeneratorDriverRunResult result = Run(new[]
        {
            new TestFile("/project/Models/Types.nsd", TypesNsd),
            new TestFile("/project/Schemas/Types.nsd", TypesNsd),
        });

        string[] hintNames = result.Results[0].GeneratedSources.Select(s => s.HintName).OrderBy(n => n).ToArray();
        Assert.That(hintNames, Is.EqualTo(new[] { "Models_Types.g.cs", "Schemas_Types.g.cs" }));
    }

    [Test]
    public void FilesOutsideProjectDirectoryUseHashedHintNames()
    {
        GeneratorDriverRunResult result = Run(new[] { new TestFile("/shared/Types.nsd", TypesNsd) });

        string hintName = result.Results[0].GeneratedSources.Single().HintName;
        StringAssert.StartsWith("Types_", hintName);
        StringAssert.EndsWith(".g.cs", hintName);
    }

    [Test]
    public void NonNsdAdditionalFilesAreIgnored()
    {
        GeneratorDriverRunResult result = Run(new[] { new TestFile("/project/readme.txt", "not an nsd") });

        Assert.That(result.Results[0].GeneratedSources, Is.Empty);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void DisabledUnsafeBlocksReportsError()
    {
        GeneratorDriverRunResult result = Run(
            new[] { new TestFile("/project/Types.nsd", TypesNsd) },
            new Dictionary<string, string> { [AllowUnsafeBlocksKey] = "false" });

        Diagnostic diagnostic = result.Diagnostics.Single(d => d.Id == "NSD0002");
        Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
    }

    [Test]
    public void MissingUnsafeBlocksOptionDoesNotReportError()
    {
        GeneratorDriverRunResult result = Run(new[] { new TestFile("/project/Types.nsd", TypesNsd) });

        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void ChangingOneFileOnlyRegeneratesThatFile()
    {
        var firstFile = new TestFile("/project/First.nsd", TypesNsd);
        var secondFile = new TestFile("/project/Second.nsd", TypesNsd);

        ImmutableArray<AdditionalText> additionalTexts = CreateAdditionalTexts(new[] { firstFile, secondFile });
        CSharpGeneratorDriver driver = CreateDriver(additionalTexts, new[] { firstFile, secondFile }, null);

        driver = (CSharpGeneratorDriver)driver.RunGenerators(CreateCompilation());
        GeneratorDriverRunResult firstResult = driver.GetRunResult();
        Assert.That(firstResult.Results[0].GeneratedSources.Length, Is.EqualTo(2));

        // Update one file and re-run; the other file's source should be cached.
        var updatedText = new InMemoryAdditionalText(
            firstFile.Path,
            TypesNsd.Replace("Order", "Invoice", StringComparison.Ordinal));

        driver = (CSharpGeneratorDriver)driver.ReplaceAdditionalText(additionalTexts[0], updatedText);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(CreateCompilation());
        GeneratorDriverRunResult secondResult = driver.GetRunResult();

        ImmutableArray<IncrementalGeneratorRunStep> steps = secondResult.Results[0].TrackedSteps["NsdSource"];
        IncrementalStepRunReason[] reasons = steps.SelectMany(step => step.Outputs).Select(output => output.Reason).ToArray();

        Assert.That(reasons, Does.Contain(IncrementalStepRunReason.Modified));
        Assert.That(reasons, Does.Contain(IncrementalStepRunReason.Cached));
        Assert.That(
            secondResult.Results[0].GeneratedSources.Select(s => s.HintName),
            Is.EquivalentTo(new[] { "First.g.cs", "Second.g.cs" }));
    }

    private static string GeneratedSource(GeneratorDriverRunResult result)
    {
        return result.Results[0].GeneratedSources.Single().SourceText.ToString();
    }

    private static GeneratorDriverRunResult Run(
        TestFile[] files,
        Dictionary<string, string>? globalOptions = null)
    {
        ImmutableArray<AdditionalText> additionalTexts = CreateAdditionalTexts(files);
        CSharpGeneratorDriver driver = CreateDriver(additionalTexts, files, globalOptions);

        return driver.RunGenerators(CreateCompilation()).GetRunResult();
    }

    private static ImmutableArray<AdditionalText> CreateAdditionalTexts(IReadOnlyList<TestFile> files)
    {
        return files
            .Select(file => (AdditionalText)new InMemoryAdditionalText(file.Path, file.Content))
            .ToImmutableArray();
    }

    private static CSharpGeneratorDriver CreateDriver(
        ImmutableArray<AdditionalText> additionalTexts,
        IReadOnlyList<TestFile> files,
        Dictionary<string, string>? globalOptions)
    {
        var options = new Dictionary<string, string>(globalOptions ?? new Dictionary<string, string>())
        {
            [ProjectDirKey] = "/project/",
        };

        var optionsProvider = new TestOptionsProvider(options, files);

        return (CSharpGeneratorDriver)CSharpGeneratorDriver.Create(
            generators: new[] { new NsdGenerator().AsSourceGenerator() },
            additionalTexts: additionalTexts,
            optionsProvider: optionsProvider,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
    }

    private static CSharpCompilation CreateCompilation()
    {
        return CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees: Array.Empty<SyntaxTree>(),
            references: Array.Empty<MetadataReference>(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
    }

    private sealed class TestFile
    {
        public string Path { get; }

        public string Content { get; }

        public Dictionary<string, string> Options { get; }

        public TestFile(string path, string content, Dictionary<string, string>? options = null)
        {
            Path = path;
            Content = content;
            Options = options ?? new Dictionary<string, string>();
        }
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _text = SourceText.From(content, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return _text;
        }
    }

    private sealed class TestOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _globalOptions;
        private readonly Dictionary<string, AnalyzerConfigOptions> _fileOptions;

        public TestOptionsProvider(
            Dictionary<string, string> globalOptions,
            IReadOnlyList<TestFile> files)
        {
            _globalOptions = new TestOptions(globalOptions);
            _fileOptions = new Dictionary<string, AnalyzerConfigOptions>();

            foreach (TestFile file in files)
            {
                _fileOptions[file.Path] = new TestOptions(file.Options);
            }
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return _globalOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return _fileOptions.TryGetValue(textFile.Path, out AnalyzerConfigOptions? options)
                ? options
                : _globalOptions;
        }
    }

    private sealed class TestOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values;

        public TestOptions(Dictionary<string, string> values)
        {
            _values = values;
        }

#pragma warning disable CS8765 // The nullability of the out parameter differs between Roslyn flavors.
        public override bool TryGetValue(string key, out string? value)
        {
            return _values.TryGetValue(key, out value);
        }
#pragma warning restore CS8765
    }
}
