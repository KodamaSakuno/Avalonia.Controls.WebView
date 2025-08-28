using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MicroCom.CodeGenerator;
using NuGet.Configuration;
using NuGet.Versioning;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.PowerShell;
using NukeExtensions;
using Semver;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    public static int Main() => IsLocalBuild ?
        Execute<Build>(x => x.CopyPackagesToNuGetCache) :
        Execute<Build>(x => x.CreateNugetPackages);

    [NuGetPackage("dotnet-ilrepack", "ILRepackTool.dll", Framework = "net8.0")] readonly Tool IlRepackTool;

    [NuGetPackage("Babel.Obfuscator.Tool", "babel.dll", Framework = "net9.0")] readonly Tool Babel;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = Configuration.Release;
    [Parameter]
    readonly AbsolutePath Output = RootDirectory / "artifacts" / "packages";

    readonly AbsolutePath SolutionFile = RootDirectory / "Avalonia.Controls.WebView.ci.slnf";

    Target OutputParameters => _ => _
        .Executes(() =>
        {
            Log.Information("Configuration: {Configuration}", Configuration);
            Log.Information("Output: {AbsolutePath}", Output);
            Log.Information("Version: {GetVersion}", GetVersion());
        });

    Target Compile => _ => _
        .DependsOn(OutputParameters)
        .DependsOn(RunTests)
        .Executes(() => DotNetBuild(c => c
            .SetProjectFile(SolutionFile)
            .SetVersion(GetVersion())
            .AddProperty("ILMergeBuild", true)
            .SetConfiguration(Configuration)
        ));

    Target RunTests => _ => _
        .DependsOn(OutputParameters)
        .Executes(() => DotNetTest(c => c
            .SetProjectFile(SolutionFile)
            .SetVerbosity(DotNetVerbosity.minimal)
            .SetConfiguration(Configuration)
        ));

    Target IlMerge => _ => _
        .DependsOn(Compile)
        .DependsOn(RunTests)
        .Executes(() =>
        {
            var mergeRootProjects = (RootDirectory / "src").GlobFiles("**/*.csproj").Where(p =>
                p.Name.Contains("Avalonia.Controls.WebView.csproj") ||
                p.Name.Contains("Avalonia.Xpf.Controls.WebView"));

            var libs = string.Join(' ', GetExtraDepLibs().Select(l => $"/lib:{l}"));
            var coreProjectPublicApi = (RootDirectory / "src").GlobFiles("**/Avalonia.Controls.WebView.Core.csproj")
                .First().Parent / "public-api.txt";

            foreach (var mergeRootProject in mergeRootProjects)
            {
                var projectName = Path.GetFileNameWithoutExtension(mergeRootProject);
                var mergeRootDlls = mergeRootProject.Parent
                    .GlobFiles(Path.Combine("bin", Configuration, "**", projectName + ".dll"));
                foreach (var mergeRootDll in mergeRootDlls)
                {
                    string[] depNamesToMerge = ["Avalonia.Controls.WebView.Core.dll", "AvaloniaUI.Licensing.dll"];
                    var dependenciesToMerge = mergeRootDll.Parent
                        .GlobFiles("*.dll")
                        .Where(f => Array.IndexOf(depNamesToMerge, f.Name) >= 0);

                    var dependenciesArg = string.Join(" ", dependenciesToMerge.Select(dll => '"' + dll + '"'));
                    var signParams = $"/keyfile:{Statics.AvaloniaStrongNameKey}";

                    IlRepackTool.Invoke(
                        $"""/internalize:{coreProjectPublicApi} /renameinternalized /parallel /ndebug {libs:nq} {signParams} /out:"{mergeRootDll}" "{mergeRootDll}" {dependenciesArg} """,
                        mergeRootDll.Parent);
                }
            }
        });

    Target Obfuscate => _ => _
        .DependsOn(Compile)
        .DependsOn(IlMerge)
        .Executes(() =>
        {
            string[] projectsToObfuscate =
            [
                "Avalonia.Controls.WebView",
                "Avalonia.Xpf.Controls.WebView"
            ];
            foreach (var project in (RootDirectory / "src").GlobFiles("**/*.csproj")
                     .Where(p => projectsToObfuscate.Contains(p.NameWithoutExtension)))
            {
                var tfms = (project.Parent / "bin" / Configuration).GetDirectories();
                NukeExtensions.Babel.Obfuscate(
                    Babel,
                    assemblyName: project.NameWithoutExtension,
                    targets: tfms.Select(tfm => new Babel.ObfuscationTargetFramework(tfm, [])),
                    signKey: Statics.AvaloniaStrongNameKey,
                    licenseFile: Statics.BabelLicense,
                    rulesFiles: [
                        Statics.BabelRules,
                        RootDirectory / "build" / "BabelWebView.rules"
                    ]);
            }
        });

    Target CreateNugetPackages => _ => _
        .DependsOn(OutputParameters)
        .DependsOn(RunTests)
        .DependsOn(Compile)
        .DependsOn(IlMerge)
        .DependsOn(Obfuscate)
        .Executes(() =>
        {
            var srcRootDirectory = RootDirectory / "src";
            foreach (var srcProject in srcRootDirectory.GlobFiles("**/*.csproj"))
            {
                DotNetPack(c => c
                    .SetProject(srcProject)
                    .SetNoBuild(true)
                    .SetNoRestore(true)
                    .SetContinuousIntegrationBuild(true)
                    .AddProperty("PackageVersion", GetVersion())
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(Output)
                );
            }
        });

    Target CopyPackagesToNuGetCache => _ => _
        .DependsOn(CreateNugetPackages)
        .Executes(() => NugetCache.InstallLibraryToNuGetCache(
            Output.GlobFiles("*.nupkg"),
            RootDirectory,
            GetVersion()));

    string GetVersion() => VersionResolver
        .GetGitHubVersion(
            baseVersionNumber: new Version(11, 3, 999),
            isPackingToLocalCache: RunningTargets.Concat(ScheduledTargets)
                .Any(t => t.Name == nameof(CopyPackagesToNuGetCache)))
        .ToString();

    static IEnumerable<string> GetExtraDepLibs()
    {
        // See https://github.com/gluck/il-repack/issues/399
        var androidSdk = NuGetPackageResolver.GetGlobalInstalledPackage("Microsoft.Android.Ref.34",
            new VersionRange(new NuGetVersion(1, 0, 0)), null)?.Directory;
        if (androidSdk is null)
        {
            throw new DirectoryNotFoundException("Unable to find installed \"Microsoft.Android.Ref.34\" nuget package.");
        }

        var androidRefs = androidSdk / "ref" / "net8.0";
        yield return androidRefs;
    }
}
