using Playwright.InstallTool;

var (command, projectPath, remainingArgs) = ParseArgs(args);

if (command is null)
{
    PrintUsage();
    return 1;
}

try
{
    var discovery = new ProjectDiscovery();
    var projectInfo = projectPath is not null
        ? discovery.DiscoverFromCsproj(projectPath)
        : discovery.Discover(Directory.GetCurrentDirectory());

    Console.WriteLine($"Found project: {Path.GetFileName(projectInfo.CsprojPath)}");
    Console.WriteLine($"Detected Microsoft.Playwright version: {projectInfo.PlaywrightVersion}");
    Console.WriteLine("Loading Playwright...");

    var loader = new PlaywrightLoader();
    var playwrightArgs = new[] { command }.Concat(remainingArgs).ToArray();
    return loader.Load(projectInfo, playwrightArgs);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static (string? command, string? projectPath, string[] remainingArgs) ParseArgs(string[] args)
{
    string? command = null;
    string? projectPath = null;
    var remaining = new List<string>();

    var i = 0;

    // First positional arg is the command
    if (i < args.Length && !args[i].StartsWith('-'))
    {
        var candidate = args[i].ToLowerInvariant();
        if (candidate is "install" or "uninstall")
        {
            command = candidate;
            i++;
        }
        else
        {
            Console.Error.WriteLine($"Error: Unknown command '{args[i]}'. Expected 'install' or 'uninstall'.");
            return (null, null, []);
        }
    }

    while (i < args.Length)
    {
        if (args[i] == "--project" && i + 1 < args.Length)
        {
            projectPath = args[i + 1];
            i += 2;
        }
        else
        {
            remaining.Add(args[i]);
            i++;
        }
    }

    return (command, projectPath, remaining.ToArray());
}

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: playwright-install <command> [--project <path>] [options]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  install    Install Playwright browsers");
    Console.Error.WriteLine("  uninstall  Uninstall Playwright browsers");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options:");
    Console.Error.WriteLine("  --project <path>  Path to .csproj file (default: scan current directory)");
}
