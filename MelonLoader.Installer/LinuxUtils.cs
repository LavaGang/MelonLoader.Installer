#if LINUX
using Avalonia;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.InteropServices;

namespace MelonLoader.Installer;

internal static partial class LinuxUtils{
    public static event InstallProgressEventHandler? Progress;
    public static string DownloadUrlVCx64 = "https://aka.ms/vs/16/release/vc_redist.x64.exe";
    public static string DownloadUrlVCx86 = "https://aka.ms/vs/16/release/vc_redist.x86.exe";
    public static string TempDestination = "/tmp";
    public static bool CheckIfBinaryExists(string name){
        ProcessResult whichResult = RunCommand("which", name);
        if (whichResult.ErrorLevel == 1){
            return false;
        }
        return true;
    }
    public static bool CheckIfFlatpakExists(string name){
        ProcessResult flatpakResult = RunCommand("flatpak", $"info {name}");
        if (flatpakResult.ErrorLevel == 1){
            return false;
        }
        return true;
    }
    public static bool CheckIfProtonTricksExists(){
        if(CheckIfBinaryExists("protontricks")){
            return true;
        }
        if(CheckIfFlatpakExists("com.github.Matoking.protontricks")){
            return true;
        }
        return false;
    }
    public static void InstallProtonDependencies(string appId){
        bool useFlatpak = false;
        string command = "protontricks";
        string commandLaunch = "protontricks-launch";
        string launchArgPrefix = "--appid";
        string argPrefix = "";
        if(CheckIfFlatpakExists("com.github.Matoking.protontricks")){
            useFlatpak = true;
        }
        if(CheckIfBinaryExists("protontricks")){
            useFlatpak = false;
        }
        if(useFlatpak){
            command = "flatpak";
            commandLaunch = "flatpak";
            argPrefix = "run com.github.Matoking.protontricks ";
            launchArgPrefix = "run com.github.Matoking.protontricks --command=protontricks-launch --appid";
        }
        try{
            string downloadPath = $"{TempDestination}/vc_redistx86.exe";
            DownloadFile(DownloadUrlVCx86, downloadPath);
            RunCommand(commandLaunch, $"{launchArgPrefix} {appId} {downloadPath}");
            File.Delete(downloadPath);
            downloadPath = $"{TempDestination}/vc_redistx64.exe";
            DownloadFile(DownloadUrlVCx64, downloadPath);
            RunCommand(commandLaunch, $"{launchArgPrefix} {appId} {downloadPath}");
            File.Delete(downloadPath);
        }
        catch{
            return;
        }
        RunCommand(command, $"{argPrefix}{appId} -q dotnetdesktop6");
        
    }
    public static void OpenSteamGameProperties(string appId){
        Process.Start($"steam://gameproperties/{appId}");
    }
    public static async void DownloadFile(string url, string destination){
        var newStr = File.OpenWrite(destination);
        var result = await InstallerUtils.DownloadFileAsync(url, newStr, (progress, newStatus) => Progress?.Invoke(progress, newStatus));
        if (result != null)
        {
            throw new Exception($"Failed to download {url}: " + result);
        }
    }
    public static ProcessResult RunCommand(string command, string arguments){
        var escapedArgs = arguments.Replace("\"", "\\\"");
        var process = new Process{
            StartInfo = new ProcessStartInfo{
                FileName = command,
                Arguments = escapedArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
        string standardError = process.StandardError.ToString() ?? "";
        string StandardOutput = process.StandardOutput.ToString() ?? "";
        return new ProcessResult(process.ExitCode, standardError, StandardOutput);
    }
}

internal class ProcessResult{
    public int ErrorLevel;
    public string ErrorOutput;
    public string StandardOutput;
    public ProcessResult(int ErrorLevel, string ErrorOutput, string StandardOutput){
        this.ErrorLevel = ErrorLevel;
        this.ErrorOutput = ErrorOutput;
        this.StandardOutput = StandardOutput;
    }
}

#endif