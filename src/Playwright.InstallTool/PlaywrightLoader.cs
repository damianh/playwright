using System.Reflection;
using System.Runtime.Loader;

namespace Playwright.InstallTool;

internal sealed class PlaywrightLoader
{
    public int Load(ProjectInfo projectInfo, string[] playwrightArgs)
    {
        var packageRoot = Path.Combine(
            projectInfo.PackagesPath,
            "microsoft.playwright",
            projectInfo.PlaywrightVersion);

        Console.WriteLine($"NuGet package cache: {projectInfo.PackagesPath}");
        Console.WriteLine($"Package path: {packageRoot}");

        if (!Directory.Exists(packageRoot))
        {
            throw new InvalidOperationException(
                $"Microsoft.Playwright {projectInfo.PlaywrightVersion} not found in NuGet cache at: {packageRoot}" +
                $"{Environment.NewLine}Try running 'dotnet restore' in your project directory first.");
        }

        var dllPath = FindPlaywrightDll(packageRoot);
        Console.WriteLine($"Playwright assembly: {dllPath}");

        // Set the driver search path to the package root — this is where .playwright/ lives
        Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", packageRoot);
        Console.WriteLine($"PLAYWRIGHT_DRIVER_SEARCH_PATH: {packageRoot}");
        Console.WriteLine($"Invoking Playwright with args: {string.Join(" ", playwrightArgs)}");

        var alc = new AssemblyLoadContext("PlaywrightContext", isCollectible: false);
        var assembly = alc.LoadFromAssemblyPath(dllPath);

        var programType = assembly.GetType("Microsoft.Playwright.Program")
            ?? throw new InvalidOperationException("Could not find Microsoft.Playwright.Program type.");

        var mainMethod = programType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static, [typeof(string[])])
            ?? throw new InvalidOperationException("Could not find Microsoft.Playwright.Program.Main(string[]) method.");

        var result = mainMethod.Invoke(null, [playwrightArgs]);
        return result is int exitCode ? exitCode : 0;
    }

    private static string FindPlaywrightDll(string packageRoot)
    {
        var libDir = Path.Combine(packageRoot, "lib");
        if (!Directory.Exists(libDir))
        {
            throw new InvalidOperationException($"lib directory not found in package: {libDir}");
        }

        // Search for Microsoft.Playwright.dll across any TFM folder
        var dll = Directory.EnumerateFiles(libDir, "Microsoft.Playwright.dll", SearchOption.AllDirectories)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"Microsoft.Playwright.dll not found under {libDir}");

        return dll;
    }
}
