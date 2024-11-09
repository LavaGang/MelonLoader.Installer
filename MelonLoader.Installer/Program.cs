using Avalonia;
using System.Diagnostics;

namespace MelonLoader.Installer;

internal static class Program
{
    private static FileStream processLock = null!;

    public static event Action? Exiting;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    private static void Main(string[] args)
    {
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
                Updater.WaitAndRemoveApp(args[1], pid);
            }
        }

        if (!CheckProcessLock())
            return;

        if (!Updater.UpdateIfPossible())
            MLManager.Init();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        Exiting?.Invoke();
    }

    private static bool CheckProcessLock()
    {
        var lockFile = Path.Combine(Config.CacheDir, "process.lock");
        if (File.Exists(lockFile))
        {
            // Try to delete the lock. It will fail if it's used by another instance.
            try
            {
                File.Delete(lockFile);
            }
            catch
            {
                try
                {
                    // Try to set focus on the existing instance.
                    var procIdRaw = File.ReadAllBytes(lockFile);
                    if (procIdRaw.Length != sizeof(int))
                        return false;

                    var procId = BitConverter.ToInt32(procIdRaw);

                    var proc = Process.GetProcessById(procId);
                    WindowsUtils.ShowWindow(proc.MainWindowHandle, 1);
                    WindowsUtils.SetForegroundWindow(proc.MainWindowHandle);
                }
                catch { }

                return false;
            }
        }

        Directory.CreateDirectory(Config.CacheDir);

        processLock = File.Create(lockFile);
        processLock.Write(BitConverter.GetBytes(Environment.ProcessId));
        processLock.Flush();
        processLock.Dispose();
        processLock = File.OpenRead(lockFile);

        return true;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

}
