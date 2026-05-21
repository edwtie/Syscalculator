param(
    [string]$AppAssembly = "src/syscalculator/bin/CodexCheck/Syscalculator.dll",
    [string]$PackagePath = "web/packages/languages/Syscalculator.Language.ned.lngpdk"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assemblyPath = (Resolve-Path (Join-Path $repoRoot $AppAssembly)).Path
$languagePackagePath = (Resolve-Path (Join-Path $repoRoot $PackagePath)).Path
$workRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("SyscalcLangSmoke_" + [guid]::NewGuid().ToString("N"))
$baseDirectory = Join-Path $workRoot "app"
$projectDirectory = Join-Path $workRoot "runner"
New-Item -ItemType Directory -Path $baseDirectory, $projectDirectory | Out-Null

try {
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
"@ | Set-Content -Encoding ASCII (Join-Path $projectDirectory "Smoke.csproj")

    @'
using System.Reflection;
using System.Runtime.Loader;

if (args.Length != 3)
    throw new InvalidOperationException("Usage: <Syscalculator.dll> <package.lngpdk> <baseDirectory>");

var assemblyPath = Path.GetFullPath(args[0]);
var packagePath = Path.GetFullPath(args[1]);
var baseDirectory = Path.GetFullPath(args[2]);
var assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;

AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    var candidate = Path.Combine(assemblyDirectory, name.Name + ".dll");
    return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
};

var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
var service = assembly.GetType("Syscalculator.UI.WinForms.LanguagePackageService", throwOnError: true)!;
var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

var manifest = service.GetMethod("Install", flags)!.Invoke(null, new object?[] { baseDirectory, packagePath })!;
var packageKey = (string)manifest.GetType().GetProperty("PackageKey")!.GetValue(manifest)!;
var languageCode = (string)manifest.GetType().GetProperty("LanguageCode")!.GetValue(manifest)!;

var packages = (System.Collections.ICollection)service.GetMethod("ListInstalled", flags)!.Invoke(null, new object?[] { baseDirectory })!;

object?[] languageArgs = [baseDirectory, packageKey, languageCode + ".lng", "", ""];
var languageOk = (bool)service.GetMethod("TryReadLanguageFile", flags)!.Invoke(null, languageArgs)!;
var languageText = (string)languageArgs[3]!;
var resolvedLanguageFile = (string)languageArgs[4]!;

object?[] toolEditorArgs = [baseDirectory, packageKey, "tool-editor/overview.html", ""];
var toolEditorHelpOk = (bool)service.GetMethod("TryReadHelpContentFile", flags)!.Invoke(null, toolEditorArgs)!;
var toolEditorHelpText = (string)toolEditorArgs[3]!;

object?[] nodArgs = [baseDirectory, packageKey, "nod/full/guide.html", ""];
var nodHelpOk = (bool)service.GetMethod("TryReadHelpContentFile", flags)!.Invoke(null, nodArgs)!;

object?[] formulaArgs = [baseDirectory, packageKey, "formula-card.html", ""];
var formulaHelpOk = (bool)service.GetMethod("TryReadHelpContentFile", flags)!.Invoke(null, formulaArgs)!;

Console.WriteLine($"PackageKey={packageKey}");
Console.WriteLine($"LanguageCode={languageCode}");
Console.WriteLine($"InstalledPackages={packages.Count}");
Console.WriteLine($"LanguageOk={languageOk}");
Console.WriteLine($"ResolvedLanguageFile={resolvedLanguageFile}");
Console.WriteLine($"LanguageHasName={languageText.Contains("language.name=")}");
Console.WriteLine($"ToolEditorHelpOk={toolEditorHelpOk}");
Console.WriteLine($"ToolEditorHelpHasText={toolEditorHelpText.Contains("ToolEditor", StringComparison.OrdinalIgnoreCase)}");
Console.WriteLine($"NodHelpOk={nodHelpOk}");
Console.WriteLine($"FormulaHelpOk={formulaHelpOk}");

if (!languageOk || !toolEditorHelpOk || !nodHelpOk || !formulaHelpOk)
    Environment.Exit(1);
'@ | Set-Content -Encoding ASCII (Join-Path $projectDirectory "Program.cs")

    dotnet run --project (Join-Path $projectDirectory "Smoke.csproj") -- $assemblyPath $languagePackagePath $baseDirectory
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    if (Test-Path $workRoot) {
        Remove-Item -LiteralPath $workRoot -Recurse -Force
    }
}
