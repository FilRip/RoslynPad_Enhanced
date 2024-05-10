using System.Composition;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Microsoft.Win32;

using NuGet.Versioning;

using RoslynPad.Build;
using RoslynPad.UI;

namespace RoslynPad;

[Export(typeof(IPlatformsFactory))]
internal class PlatformsFactory : IPlatformsFactory
{
    IReadOnlyList<ExecutionPlatform>? _executionPlatforms;
    private (string dotnetExe, string sdkPath) _dotnetPaths;

    public IReadOnlyList<ExecutionPlatform> GetExecutionPlatforms() =>
        _executionPlatforms ??= GetNetVersions().Concat(GetNetFrameworkVersions()).ToArray().AsReadOnly();

    public string DotNetExecutable => FindNetSdk().dotnetExe;

    private IEnumerable<ExecutionPlatform> GetNetVersions()
    {
        var (_, sdkPath) = FindNetSdk();

        if (string.IsNullOrEmpty(sdkPath))
        {
            return Array.Empty<ExecutionPlatform>();
        }

        var versions = new List<(string name, string tfm, NuGetVersion version)>();

        foreach (var directory in IOUtilities.EnumerateDirectories(sdkPath))
        {
            var versionName = Path.GetFileName(directory);
            if (NuGetVersion.TryParse(versionName, out var version) && version.Major > 1)
            {
                var name = version.Major < 5 ? ".NET Core" : ".NET";
                var tfm = version.Major < 5 ? $"netcoreapp{version.Major}.{version.Minor}" : $"net{version.Major}.{version.Minor}";
                versions.Add((name, tfm, version));
            }
        }

        return versions.OrderBy(c => c.version.IsPrerelease).ThenByDescending(c => c.version)
            .Select(version => new ExecutionPlatform(version.name, version.tfm, version.version, Architecture.X64, isDotNet: true));
    }

    private IEnumerable<ExecutionPlatform> Get1To45VersionFromRegistry()
    {
        // Opens the registry key for the .NET Framework entry.
        using RegistryKey? ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\");
        if (ndpKey == null)
            return [];
        List<ExecutionPlatform> list = [];
        foreach (string versionKeyName in ndpKey.GetSubKeyNames())
        {
            // Skip .NET Framework 4.5 version information.
            if (versionKeyName == "v4")
            {
                continue;
            }

            if (versionKeyName.StartsWith("v", StringComparison.InvariantCultureIgnoreCase))
            {
                RegistryKey? versionKey = ndpKey.OpenSubKey(versionKeyName);
                if (versionKey == null)
                    break;
                // Get the .NET Framework version value.
                string name = (string)versionKey.GetValue("Version", "");
                // Get the service pack (SP) number.
                string? sp = versionKey.GetValue("SP", "").ToString();

                // Get the installation flag, or an empty string if there is none.
                string? install = versionKey.GetValue("Install", "").ToString();
                if (string.IsNullOrEmpty(install)) // No install info; it must be in a child subkey.
                    list.AddRange(AddNetFrameworkVersion(name));
                else
                {
                    if (!(string.IsNullOrEmpty(sp)) && install == "1")
                        list.AddRange(AddNetFrameworkVersion(name, sp));
                }
                if (!string.IsNullOrEmpty(name))
                {
                    continue;
                }
                foreach (string subKeyName in versionKey.GetSubKeyNames())
                {
                    RegistryKey? subKey = versionKey.OpenSubKey(subKeyName);
                    if (subKey != null)
                    {
                        name = (string)subKey.GetValue("Version", "");
                        if (!string.IsNullOrEmpty(name))
                            sp = subKey.GetValue("SP", "").ToString();

                        install = subKey.GetValue("Install", "").ToString();
                        if (string.IsNullOrEmpty(install)) //No install info; it must be later.
                            list.AddRange(AddNetFrameworkVersion(name));
                        else
                        {
                            if (!(string.IsNullOrEmpty(sp)) && install == "1")
                            {
                                list.AddRange(AddNetFrameworkVersion(name, sp));
                            }
                            else if (install == "1")
                            {
                                list.AddRange(AddNetFrameworkVersion(name));
                            }
                        }
                    }
                }
            }
        }
        return list;
    }

    private static List<ExecutionPlatform> AddNetFrameworkVersion(string version, string? servicePack = null)
    {
        string name = ".NET Framework";
        if (!string.IsNullOrWhiteSpace(servicePack))
            name += " Service Pack " + servicePack;
        List<ExecutionPlatform> list = [];
        list.Add(new ExecutionPlatform(name, version, null, Architecture.X86, false));
        if (Environment.Is64BitOperatingSystem)
            list.Add(new ExecutionPlatform(name, version, null, Architecture.X64, false));
        return list;
    }

    private IEnumerable<ExecutionPlatform> Get45PlusFromRegistry()
    {
        const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

        using RegistryKey? ndpKey = Registry.LocalMachine.OpenSubKey(subkey);
        if (ndpKey == null)
            return [];

        List<ExecutionPlatform> list = [];
        //First check if there's an specific version indicated
        if (ndpKey.GetValue("Version") != null)
        {
            AddNetFrameworkVersion(ndpKey.GetValue("Version").ToString()!);
        }
        else
        {
            if (ndpKey.GetValue("Release") != null)
            {
                AddNetFrameworkVersion(
                    CheckFor45PlusVersion(
                            (int)ndpKey.GetValue("Release")!
                        )
                );
            }
        }

        return list;

        // Checking the version using >= enables forward compatibility.
        string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 533320)
                return "4.8.1";
            if (releaseKey >= 528040)
                return "4.8";
            if (releaseKey >= 461808)
                return "4.7.2";
            if (releaseKey >= 461308)
                return "4.7.1";
            if (releaseKey >= 460798)
                return "4.7";
            if (releaseKey >= 394802)
                return "4.6.2";
            if (releaseKey >= 394254)
                return "4.6.1";
            if (releaseKey >= 393295)
                return "4.6";
            if (releaseKey >= 379893)
                return "4.5.2";
            if (releaseKey >= 378675)
                return "4.5.1";
            if (releaseKey >= 378389)
                return "4.5";
            // This code should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "";
        }
    }

    private IEnumerable<ExecutionPlatform> GetNetFrameworkVersions()
    {
        IEnumerable<ExecutionPlatform> list = Get1To45VersionFromRegistry();
        list = list.Concat(Get45PlusFromRegistry());
        return list;
        /*var targetFrameworkName = GetNetFrameworkName();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.X64)
        {
            yield return new ExecutionPlatform(".NET Framework x64", targetFrameworkName, null, Architecture.X64, isDotNet: false);
        }
        yield return new ExecutionPlatform(".NET Framework x86", targetFrameworkName, null, Architecture.X86, isDotNet: false);*/
    }

    private (string dotnetExe, string sdkPath) FindNetSdk()
    {
        if (_dotnetPaths.dotnetExe is not null)
        {
            return _dotnetPaths;
        }

        List<string> dotnetPaths = [];
        if (Environment.GetEnvironmentVariable("DOTNET_ROOT") is var dotnetRoot && !string.IsNullOrEmpty(dotnetRoot))
        {
            dotnetPaths.Add(dotnetRoot);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            dotnetPaths.Add(Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432")!, "dotnet"));
        }
        else
        {
            dotnetPaths.AddRange([
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet"),
                "/usr/lib64/dotnet",
                "/usr/share/dotnet",
                "/usr/local/share/dotnet",
            ]);
        }

        var dotnetExe = GetDotnetExe();
        var paths = (from path in dotnetPaths
                     let exePath = Path.Combine(path, dotnetExe)
                     let fullPath = Path.Combine(path, "sdk")
                     where File.Exists(exePath) && Directory.Exists(fullPath)
                     select (exePath, fullPath)).FirstOrDefault<(string exePath, string fullPath)>();

        if (paths.exePath is null)
        {
            paths = (string.Empty, string.Empty);
        }

        _dotnetPaths = paths;
        return paths;
    }

    private static string GetDotnetExe() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

    private static string GetNetFrameworkName()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return string.Empty;
        }

        const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

        using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
        {
            var release = ndpKey?.GetValue("Release") as int?;
            if (release != null)
            {
                return GetNetFrameworkTargetName(release.Value);
            }
        }

        return string.Empty;
    }

    private static string GetNetFrameworkTargetName(int releaseKey) => releaseKey switch
    {
        >= 528040 => "net48",
        >= 461808 => "net472",
        >= 461308 => "net471",
        >= 460798 => "net47",
        >= 394802 => "net462",
        >= 394254 => "net461",
        >= 393295 => "net46",
        _ => throw new ArgumentOutOfRangeException(nameof(releaseKey))
    };
}
