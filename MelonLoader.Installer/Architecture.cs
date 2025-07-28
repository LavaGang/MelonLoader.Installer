using System.ComponentModel;

namespace MelonLoader.Installer;

/// <summary>
/// Supported architectures by MelonLoader.
/// </summary>
public enum Architecture
{
    [Description("unknown")]
    Unknown,
    [Description("win-x86")]
    WindowsX86,
    [Description("win-x64")]
    WindowsX64,
    [Description("linux-x86")]
    LinuxX86,
    [Description("linux-x64")]
    LinuxX64,
    [Description("osx-x64")]
    MacOSX64,
    [Description("osx-arm64")]
    MacOSArm64,
}
