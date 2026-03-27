using Semver;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using ELF = ELFSharp.ELF;
using MachO = ELFSharp.MachO;

namespace MelonLoader.Installer;

public class MLVersion
{
    public string? DownloadUrlWin { get; init; }
    public string? DownloadUrlWinX86 { get; init; }
    public string? DownloadUrlLinux { get; init; }
    public string? DownloadUrlMacOS { get; init; }
    public required SemVersion Version { get; init; }
    public bool IsLocalPath { get; init; }

    public static SemVersion? GetMelonLoaderVersion(string gameDir, out Architecture architecture, out string? errorMessage)
    {
        architecture = Architecture.Unknown;
        errorMessage = null;
        
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

        SemVersion? version;
        try
        {
            ReadVersionInfo(mlAssemblyPath, out Version? fileVersion);
            if (fileVersion == null)
                return null;
            version = SemVersion.ParsedFrom(fileVersion.Major, fileVersion.Minor, fileVersion.Build,
                fileVersion.Revision <= 0 ? string.Empty : $"ci.{fileVersion.Revision}");
        }
        catch (Exception ex)
        {
            errorMessage = ex.ToString();
            return null;
        }
        if (version == null) 
            return null;

        var proxyName = MLManager.proxyNames.FirstOrDefault(x => File.Exists(Path.Combine(gameDir, x)));
        if (proxyName == null)
            return null;
        string proxyPath = Path.Combine(gameDir, proxyName);
        try
        {
            ReadArchitecture(proxyPath, out architecture);
        }
        catch (Exception ex)
        {
            architecture = Architecture.Unknown;
            errorMessage = ex.ToString();
            return null;
        }
        if (architecture == Architecture.Unknown)
            return null;

        return version;
    }

    public override string ToString()
    {
        var name = Version.ToString();
        if (IsLocalPath)
            name += " (Local Build)";

        return name;
    }

    private static void ReadVersionInfo(string filePath, out Version? version)
    {
        var fileVersionRaw = FileVersionInfo.GetVersionInfo(filePath).FileVersion!;
        version = System.Version.Parse(fileVersionRaw);
    }

    public static Architecture ReadFromPE(string filePath)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            var pe = new PEReader(fs, PEStreamOptions.Default, (int)fs.Length);
            Architecture architecture = pe.PEHeaders.CoffHeader.Machine switch
            {
                Machine.I386 => Architecture.WindowsX86,
                Machine.Amd64 => Architecture.WindowsX64,
                _ => Architecture.Unknown
            };
            pe.Dispose();
            fs.Dispose();
            return architecture;
        }
        catch { }
        return Architecture.Unknown;
    }

    public static Architecture ReadFromELF(string filePath)
    {
        try
        {
            return ELF.ELFReader.CheckELFType(filePath) switch
            {
                ELF.Class.Bit32 => Architecture.LinuxX86,
                ELF.Class.Bit64 => Architecture.LinuxX64,
                _ => Architecture.Unknown,
            };
        }
        catch { }
        return Architecture.Unknown;
    }

    public static Architecture ReadFromMachO(string filePath)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            return MachO.MachOReader.TryLoadFat(fs, true, out var machOs) switch
            {
                MachO.MachOResult.OK => machOs[0].Machine switch
                {
                    MachO.Machine.X86_64 => Architecture.MacOSX64,
                    MachO.Machine.Arm64 => Architecture.MacOSArm64,
                    _ => Architecture.Unknown
                },
                MachO.MachOResult.FatMachO =>
                    machOs.Any(x => x.Machine == MachO.Machine.X86_64)
                        ? Architecture.MacOSX64
                        : machOs.Any(x => x.Machine == MachO.Machine.Arm64)
                            ? Architecture.MacOSArm64
                            : Architecture.Unknown,
                _ => Architecture.Unknown
            };
        }
        catch { }
        return Architecture.Unknown;
    }

    private static void ReadArchitecture(string filePath, out Architecture architecture)
    {
        architecture = Path.GetExtension(filePath) switch
        {
            ".so" => ReadFromELF(filePath),
            ".dylib" => ReadFromMachO(filePath),
            _ => ReadFromPE(filePath)
        };
    }
}
