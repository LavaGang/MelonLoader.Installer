using Semver;
using System.Diagnostics;
using System.Reflection.PortableExecutable;

namespace MelonLoader.Installer;

public class MLVersion
{
    public string? DownloadUrl { get; init; }
    public string? DownloadX86Url { get; init; }
    public required SemVersion Version { get; init; }
    public bool IsLocalPath { get; init; }

    public static SemVersion? GetMelonLoaderVersion(string gameDir)
    {
        var mlDir = Path.Combine(gameDir, "MelonLoader");
        if (!Directory.Exists(mlDir))
            return null;

        var mlAssemblyPath = Path.Combine(mlDir, "MelonLoader.dll");
        if (!File.Exists(mlAssemblyPath))
        {
            mlAssemblyPath = Path.Combine(mlDir, "MelonLoader.ModHandler.dll");
            if (!File.Exists(mlAssemblyPath))
            {
                mlAssemblyPath = Path.Combine(mlDir, "net35", "MelonLoader.dll");
                if (!File.Exists(mlAssemblyPath))
                {
                    mlAssemblyPath = Path.Combine(mlDir, "net6", "MelonLoader.dll");
                    if (!File.Exists(mlAssemblyPath))
                        mlAssemblyPath = null;
                }
            }
        }
        if (mlAssemblyPath == null)
            return null;

        try
        {
            var fileVersionRaw = FileVersionInfo.GetVersionInfo(mlAssemblyPath).FileVersion!;
            var fileVersion = System.Version.Parse(fileVersionRaw);
            return SemVersion.ParsedFrom(fileVersion.Major, fileVersion.Minor, fileVersion.Build, fileVersion.Revision == 0 ? string.Empty : $"ci.{fileVersion.Revision}");
        }
        catch { }

        return null;
    }

    public static SemVersion? GetMelonLoaderVersion(string gameDir, out bool x86)
    {
        x86 = false;
        var ver = GetMelonLoaderVersion(gameDir);
        if (ver == null)
            return null;

        var proxyPath = MLManager.proxyNames.FirstOrDefault(x => File.Exists(Path.Combine(gameDir, x + ".dll")));
        if (proxyPath == null)
            return null;

        proxyPath = Path.Combine(gameDir, proxyPath + ".dll");

        try
        {
            using var proxyStr = File.OpenRead(proxyPath);
            var pe = new PEReader(proxyStr);
            x86 = pe.PEHeaders.CoffHeader.Machine != Machine.Amd64;
            return ver;
        }
        catch { }

        return null;
    }

    public override string ToString()
    {
        var name = Version.ToString();
        if (IsLocalPath)
            name += " (Local Build)";

        return name;
    }
}
