using Semver;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using ELF = ELFSharp.ELF;
using MachO = ELFSharp.MachO;

namespace MelonLoader.Installer;

public class MLVersion
{
    public string? DownloadUrlWinX86 { get; internal set; }
    public string? DownloadUrlWinX64 { get; internal set; }
    public string? DownloadUrlWinArm64 { get; internal set; }
    public string? DownloadUrlLinuxX86 { get; internal set; }
    public string? DownloadUrlLinuxX64 { get; internal set; }
    public string? DownloadUrlLinuxArm64 { get; internal set; }
    public string? DownloadUrlMacOSX64 { get; internal set; }
    public string? DownloadUrlMacOSArm64 { get; internal set; }
    public string? DownloadUrlAndroidArm64 { get; internal set; }
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
            var fs = File.OpenRead(filePath);
            var pe = new PEReader(fs, PEStreamOptions.Default, (int)fs.Length);
            Architecture architecture = pe.PEHeaders.CoffHeader.Machine switch
            {
                Machine.I386 => Architecture.WindowsX86,
                Machine.Amd64 => Architecture.WindowsX64,
                Machine.Arm64 => Architecture.WindowsArm64,
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
            var elf = ELF.ELFReader.Load(filePath);
            
            var elfMachine = elf.Machine;
            switch (elfMachine)
            {
                case ELF.Machine.AArch64:
                    return Architecture.LinuxArm64;
            }
            
            var elfClass = elf.Class;
            switch (elfClass)
            {
                case ELF.Class.Bit32:
                    return Architecture.LinuxX86;
                case ELF.Class.Bit64:
                    return Architecture.LinuxX64;
            }
            
            elf.Dispose();
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
    
    public string? GetDownload(Architecture architecture)
        => architecture switch
        {
            Architecture.WindowsX86 => DownloadUrlWinX86,
            Architecture.WindowsX64 => DownloadUrlWinX64,
            Architecture.WindowsArm64 => DownloadUrlWinArm64,
            
            Architecture.LinuxX86 => DownloadUrlLinuxX86,
            Architecture.LinuxX64 => DownloadUrlLinuxX64,
            Architecture.LinuxArm64 => DownloadUrlLinuxArm64,
            
            Architecture.MacOSX64 => DownloadUrlMacOSX64,
            Architecture.MacOSArm64 => DownloadUrlMacOSArm64,
            
            Architecture.AndroidArm64 => DownloadUrlAndroidArm64,
            
            _ => null
        };

    public string? GetDownloadOrDefault(Architecture architecture)
    {
        var attempt = GetDownload(architecture);
        if (!string.IsNullOrEmpty(attempt))
            return attempt;

#if WINDOWS
        return DownloadUrlWinX64,
#elif LINUX
        return DownloadUrlLinuxX64;
#elif OSX
        return DownloadUrlMacOSX64;
#elif ANDROID
        return DownloadUrlAndroidArm64;
#endif
    }
    
    public bool IsDownloadEmpty()
        => ((DownloadUrlWinX86 == null)
            && (DownloadUrlWinX64 == null)
            && (DownloadUrlWinArm64 == null)
            && (DownloadUrlLinuxX86 == null)
            && (DownloadUrlLinuxX64 == null)
            && (DownloadUrlLinuxArm64 == null)
            && (DownloadUrlMacOSX64 == null)
            && (DownloadUrlMacOSArm64 == null)
            && (DownloadUrlAndroidArm64 == null));

    public void ApplyLocalPathDownload(Architecture arch, string path)
    {
        DownloadUrlWinX86 = arch == Architecture.WindowsX86 ? path : null;
        DownloadUrlWinX64 = arch == Architecture.WindowsX64 ? path : null;
        DownloadUrlWinArm64 = arch == Architecture.WindowsArm64 ? path : null;

        DownloadUrlLinuxX86 = arch == Architecture.LinuxX86 ? path : null;
        DownloadUrlLinuxX64 = arch == Architecture.LinuxX64 ? path : null;
        DownloadUrlLinuxArm64 = arch == Architecture.LinuxArm64 ? path : null;

        DownloadUrlMacOSX64 = arch == Architecture.MacOSX64 ? path : null;
        DownloadUrlMacOSArm64 = arch == Architecture.MacOSArm64 ? path : null;

        DownloadUrlAndroidArm64 = arch == Architecture.AndroidArm64 ? path : null;
    }
    
    public void ApplyURLDownload(string fileName, string downloadUrl)
    {
        string fileNameLower = fileName.ToLower();
        
        // Windows
        if (fileNameLower.StartsWith("melonloader.windows.x64")
            || fileNameLower.StartsWith("melonloader.x64"))
            DownloadUrlWinX64 = downloadUrl;
        if (fileNameLower.StartsWith("melonloader.windows.x86")
            || fileNameLower.StartsWith("melonloader.x86"))
            DownloadUrlWinX86 = downloadUrl;
        if (fileNameLower.StartsWith("melonloader.windows.arm64")
            || fileNameLower.StartsWith("melonloader.arm64"))
            DownloadUrlWinArm64 = downloadUrl;

        // Linux
        if (fileNameLower.StartsWith("melonloader.linux.x86"))
            DownloadUrlLinuxX86 = downloadUrl;
        if (fileNameLower.StartsWith("melonloader.linux.x64"))
            DownloadUrlLinuxX64 = downloadUrl;
        if (fileNameLower.StartsWith("melonloader.linux.arm64"))
            DownloadUrlLinuxArm64 = downloadUrl;

        // MacOS
        if (fileNameLower.StartsWith("melonloader.macos.x64")
            || fileNameLower.StartsWith("melonloader.macos"))
            DownloadUrlMacOSX64 = downloadUrl;
        if (fileNameLower.StartsWith("melonloader.macos.arm64")
            || fileNameLower.StartsWith("melonloader.macos"))
            DownloadUrlMacOSArm64 = downloadUrl;
        
        // Android
        if (fileNameLower.StartsWith("melonloader.android.arm64"))
            DownloadUrlAndroidArm64 = downloadUrl;
    }
}
