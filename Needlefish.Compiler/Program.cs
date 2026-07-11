using System.CommandLine;
using Needlefish;

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

return await rootCommand.InvokeAsync(args);

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

    var sources = nsdFiles.Select(x => new KeyValuePair<string, string>(Path.GetFileNameWithoutExtension(x), File.ReadAllText(x))).ToArray();

    Directory.CreateDirectory(outputPath);

    foreach (var source in sources)
    {
        string result = generator.Emit(source.Key, source.Value);

        string filePath = Path.Combine(outputPath, source.Key + ".cs");
        File.WriteAllText(filePath, result);

        Console.WriteLine($"Generated: {Path.GetRelativePath(Environment.CurrentDirectory, filePath)}");
    }
}
