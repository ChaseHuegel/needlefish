using System.CommandLine;
using Needlefish;
using Needlefish.Compiler;
using Needlefish.Schema;

var inputOption = new Option<DirectoryInfo?>(
            name: "--input",
            description: "The directory containing nsd files to use as input.");
inputOption.AddAlias("-i");

var recursiveOption = new Option<bool>(
            name: "-r",
            description: "Search the input directory recursively.");

var outputOption = new Option<DirectoryInfo?>(
            name: "--output",
            description: "The directory to output generated files to.");
outputOption.AddAlias("-o");

var partialOption = new Option<bool>(
    name: "--partial",
    description: "The generated types will have the `partial` keyword.");
partialOption.AddAlias("-p");

var rootCommand = new RootCommand("Compile nsd files.");
rootCommand.AddOption(inputOption);
rootCommand.AddOption(recursiveOption);
rootCommand.AddOption(outputOption);
rootCommand.AddOption(partialOption);

rootCommand.SetHandler(Compile, inputOption, outputOption, recursiveOption, partialOption);

await rootCommand.InvokeAsync(args);

return Environment.ExitCode;

void Compile(DirectoryInfo? inputDir, DirectoryInfo? outputDir, bool recursive, bool partial)
{
    string inputPath = inputDir?.FullName ?? Environment.CurrentDirectory;
    string outputPath = outputDir?.FullName ?? Environment.CurrentDirectory;

    if (!Path.IsPathFullyQualified(inputPath))
    {
        inputPath = Path.Combine(Environment.CurrentDirectory, inputPath);
    }

    if (!Path.IsPathFullyQualified(outputPath))
    {
        outputPath = Path.Combine(Environment.CurrentDirectory, outputPath);
    }

    string[] allFiles = Directory.GetFiles(inputPath, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
    string[] nsdFiles = allFiles.Where(x => Path.GetExtension(x).Equals(".nsd", StringComparison.OrdinalIgnoreCase)).ToArray();

    foreach (string filePath in nsdFiles)
    {
        Console.WriteLine($"Found nsd: {Path.GetRelativePath(Environment.CurrentDirectory, filePath)}");
    }

    var compilerOptions = new CompilerOptions
    {
        Partial = partial,
    };

    var options = new EmitterOptions
    {
        CompilerOptions = compilerOptions
    };

    Nsd1Emitter generator = new(options);
    var resolver = new FileSystemNsdFileResolver();
    string[] rootPaths = nsdFiles.Select(NsdPaths.Normalize).ToArray();

    var diagnostics = new DiagnosticConsole();
    bool hasErrors = false;

    Directory.CreateDirectory(outputPath);

    foreach (string filePath in nsdFiles)
    {
        string canonicalPath = NsdPaths.Normalize(filePath);
        string content = File.ReadAllText(canonicalPath);
        diagnostics.Register(canonicalPath, content);

        string name = Path.GetFileNameWithoutExtension(canonicalPath);
        NsdEmitResult result = generator.TryEmit(canonicalPath, content, resolver, rootPaths, name);

        foreach (NsdDiagnostic diagnostic in result.Diagnostics)
        {
            diagnostics.Report(diagnostic);

            if (diagnostic.Level == NsdDiagnosticLevel.Error)
            {
                hasErrors = true;
            }
        }

        if (result.Source == null)
        {
            continue;
        }

        string filePathOut = Path.Combine(outputPath, name + ".cs");
        File.WriteAllText(filePathOut, result.Source);

        Console.WriteLine($"Generated: {Path.GetRelativePath(Environment.CurrentDirectory, filePathOut)}");
    }

    if (hasErrors)
    {
        Environment.ExitCode = 1;
    }
}
