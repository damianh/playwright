using System.Text.Json;
using System.Text.Json.Nodes;

namespace Playwright.InstallTool;

internal sealed record ProjectInfo(string CsprojPath, string PlaywrightVersion, string PackagesPath);

internal sealed class ProjectDiscovery
{
    public ProjectInfo Discover(string directory)
    {
        var csprojPath = FindCsproj(directory);
        return DiscoverFromCsproj(csprojPath);
    }

    public ProjectInfo DiscoverFromCsproj(string csprojPath)
    {
        if (!File.Exists(csprojPath))
        {
            throw new InvalidOperationException($"Project file not found: {csprojPath}");
        }

        var projectDir = Path.GetDirectoryName(csprojPath)!;
        var assetsFile = Path.Combine(projectDir, "obj", "project.assets.json");

        if (!File.Exists(assetsFile))
        {
            Console.WriteLine("No obj/project.assets.json found. Running dotnet restore...");
            RunRestore(csprojPath);
            Console.WriteLine("Restore completed.");
        }

        if (!File.Exists(assetsFile))
        {
            throw new InvalidOperationException(
                $"obj/project.assets.json not found after restore: {assetsFile}");
        }

        Console.WriteLine($"Reading assets file: {assetsFile}");
        return ParseAssetsFile(csprojPath, assetsFile);
    }

    private static string FindCsproj(string directory)
    {
        var csprojFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly);

        if (csprojFiles.Length == 0)
        {
            throw new InvalidOperationException(
                $"No .csproj file found in {directory}. " +
                "Use --project to specify a project file explicitly.");
        }

        if (csprojFiles.Length > 1)
        {
            var list = string.Join(Environment.NewLine + "  ", csprojFiles.Select(Path.GetFileName));
            throw new InvalidOperationException(
                $"Multiple .csproj files found in {directory}:{Environment.NewLine}  {list}" +
                $"{Environment.NewLine}Use --project to specify which project to use.");
        }

        return csprojFiles[0];
    }

    private static void RunRestore(string csprojPath)
    {
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"restore \"{csprojPath}\"",
            UseShellExecute = false,
        });

        process?.WaitForExit();

        if (process?.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet restore failed with exit code {process?.ExitCode}.");
        }
    }

    private static ProjectInfo ParseAssetsFile(string csprojPath, string assetsFile)
    {
        using var stream = File.OpenRead(assetsFile);
        var root = JsonNode.Parse(stream) ?? throw new InvalidOperationException("Failed to parse project.assets.json.");

        var libraries = root["libraries"]?.AsObject();
        if (libraries is null)
        {
            throw new InvalidOperationException("project.assets.json has no 'libraries' section.");
        }

        string? playwrightVersion = null;
        foreach (var (key, _) in libraries)
        {
            if (key.StartsWith("Microsoft.Playwright/", StringComparison.OrdinalIgnoreCase))
            {
                playwrightVersion = key.Split('/')[1];
                break;
            }
        }

        if (playwrightVersion is null)
        {
            throw new InvalidOperationException(
                "Microsoft.Playwright package not found in project.assets.json. " +
                "Ensure the project has a PackageReference on Microsoft.Playwright.");
        }

        // Use the first packageFolders entry as the NuGet packages path
        var packageFolders = root["packageFolders"]?.AsObject();
        var packagesPath = packageFolders?.Select(kvp => kvp.Key).FirstOrDefault()
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

        return new ProjectInfo(csprojPath, playwrightVersion, packagesPath);
    }
}
