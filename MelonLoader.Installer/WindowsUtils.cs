#if WINDOWS
using System.Runtime.InteropServices;

namespace MelonLoader.Installer;

internal static partial class WindowsUtils
{
    [LibraryImport("user32")]
    internal static partial nint SetForegroundWindow(nint hWnd);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(nint hWnd, int nCmdShow);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsIconic(nint hWnd);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool BringWindowToTop(nint hWnd);
}
#endif