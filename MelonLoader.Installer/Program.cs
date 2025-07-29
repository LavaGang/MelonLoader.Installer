using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Diagnostics;

namespace MelonLoader.Installer;

internal static class Program
{
    private static FileStream processLock = null!;
    private static readonly string processLockPath = Path.Combine(Config.CacheDir, "process.lock");

    public static event Action? Exiting;

    public static Version Version { get; } = typeof(Program).Assembly.GetName().Version!;

    public static string VersionName { get; } =
        $"v{Version.Major}.{Version.Minor}.{Version.Build}{(Version.Revision > 0 ? $"-ci.{Version.Revision}" : string.Empty)}";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    private static void Main(string[] args)
    {
        if (!Directory.Exists(Config.CacheDir))
            Directory.CreateDirectory(Config.CacheDir);

        if (args.Length >= 3)
        {
            if (!int.TryParse(args[2], out var pid))
                return;

            if (args[0] == "-handleupdate")
            {
                Updater.HandleUpdate(args[1], pid);
                return;
            }

            if (args[0] == "-cleanup")
            {
                Updater.WaitAndRemoveApp(args[1], pid
#if OSX
                    , true
#endif
                    );
            }
            else if (args[0] == "-wait")
            {
                try
                {
                    Process.GetProcessById(pid).WaitForExit();
                }
                catch { }
            }
        }

#if WINDOWS
        if (Updater.CheckLegacyUpdate())
            return;
#endif

        if (!CheckProcessLock())
            return;

        try
        {

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        }
        catch (Exception ex)
        {
            LogCrashException(ex);
        }

        Exiting?.Invoke();

        processLock.Dispose();
        File.Delete(processLockPath);
    }

#if OSX
    public static void OpenFolderInExplorer(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "open",
            ArgumentList = { path },
            UseShellExecute = false
        });
    }
#endif

    public static void OpenFolderInExplorer(Visual? visual, string path)
    {
#if OSX
        Process.Start(new ProcessStartInfo
        {
            FileName = "open",
            ArgumentList = { path },
            UseShellExecute = false
        });
#else
        TopLevel.GetTopLevel(visual)!.Launcher.LaunchDirectoryInfoAsync(new(path));
#endif
    }

    public static void LogCrashException(Exception ex)
    {
        try
        {
#if OSX
            string logLoc = Config.CacheDir;
#else
            string logLoc = Path.GetDirectoryName(Config.ProcessPath)!;
#endif

            var logPath = Path.Combine(logLoc, "melonloader-installer-crash.log");
            File.WriteAllText(logPath, ex.ToString());
        }
        catch { }
    }

    public static void RestartWithElevatedPrivileges()
    {
        try
        {
#if WINDOWS
            Process.Start(new ProcessStartInfo(Config.ProcessPath!, ["-wait", Environment.ProcessId.ToString()])
            {
                UseShellExecute = true,
                Verb = "runas"
            });
#elif LINUX
            Process.Start("pkexec", [Config.ProcessPath!, "-wait", Environment.ProcessId.ToString()]);
#elif OSX
            Process.Start("osascript", ["-e", "do shell script \"open '{Config.ProcessPath!}' --args -wait {Environment.ProcessId.ToString()}\" with prompt \"MelonLoader Installer\" with administrator privileges"]);
#endif
        }
        catch
        {
            // This may happen if the user refuses to start the new process as admin
            return;
        }

        Environment.Exit(0);
    }

    private static bool CheckProcessLock()
    {
        if (File.Exists(processLockPath))
        {
#if WINDOWS
            // Try to delete the lock. It will fail if it's used by another instance.
            try
            {
                File.Delete(processLockPath);
            }
            catch
            {
                try
                {
                    // Try to set focus on the existing instance.
                    var procIdRaw = File.ReadAllBytes(processLockPath);
                    if (procIdRaw.Length != sizeof(int))
                        return false;

                    var procId = BitConverter.ToInt32(procIdRaw);
                    var proc = Process.GetProcessById(procId);
                    GrabAttention(proc);
                    return false;
                }
                catch
                {
                    return false;
                }
            }
#else
            var procIdRaw = File.ReadAllBytes(processLockPath);
            if (procIdRaw.Length == sizeof(int))
            {
                var procId = BitConverter.ToInt32(procIdRaw);

                try
                {
                    Process.GetProcessById(procId);
                    return false;
                }
                catch { }
            }
#endif
        }

        processLock = File.Create(processLockPath);
        processLock.Write(BitConverter.GetBytes(Environment.ProcessId));
        processLock.Flush();

#if WINDOWS
        // On Windows, we need to reopen a new stream with only read perms, otherwise other processes will fail to read it
        processLock.Dispose();
        processLock = File.OpenRead(processLockPath);
#endif

        return true;
    }

#if WINDOWS

    internal static void GrabAttention()
        => GrabAttention(Process.GetCurrentProcess());
    private static void GrabAttention(Process process)
    {
        var processHandle = process.MainWindowHandle;
        if (WindowsUtils.IsIconic(processHandle))
            WindowsUtils.ShowWindow(processHandle, 9);

        WindowsUtils.SetForegroundWindow(processHandle);
        WindowsUtils.BringWindowToTop(processHandle);
    }

#endif

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

}
