# Playwright Install Tool

[![CI](https://github.com/damianh/playwright-install-tool/actions/workflows/ci.yml/badge.svg)](https://github.com/damianh/playwright-install-tool/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DamianH.Playwright.InstallTool.svg)](https://www.nuget.org/packages/DamianH.Playwright.InstallTool)
[![NuGet Downloads](https://img.shields.io/nuget/dt/DamianH.Playwright.InstallTool.svg)](https://www.nuget.org/packages/DamianH.Playwright.InstallTool)
[![License](https://img.shields.io/github/license/damianh/playwright-install-tool.svg)](https://github.com/damianh/playwright-install-tool/blob/main/LICENSE)

A .NET tool to install Playwright browser dependencies for a project — without needing to find and run `playwright.ps1` from your build output.

## Installation

```bash
dotnet tool install DamianH.Playwright.InstallTool
```

## Usage

Navigate to a directory containing a project that references `Microsoft.Playwright` and run:

```bash
playwright-install install
```

To uninstall browsers:

```bash
playwright-install uninstall
```

### Options

| Option | Description |
|---|---|
| `--project <path>` | Path to a specific `.csproj` file. Defaults to scanning the current directory. |

Any additional arguments are passed through to Playwright's CLI.

## How it works

1. Finds the `.csproj` in the current directory (or the one specified via `--project`)
2. Reads `obj/project.assets.json` to determine the resolved `Microsoft.Playwright` version (runs `dotnet restore` if needed)
3. Loads `Microsoft.Playwright.dll` from the NuGet package cache
4. Invokes Playwright's install/uninstall command

## License

MIT
