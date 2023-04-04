using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Windows.Forms;
using MelonLoader.Tomlyn;
using MelonLoader.Tomlyn.Model;
using MelonLoader.Tomlyn.Syntax;

namespace MelonLoader
{
    internal static class OperationHandler
    {
        private static string[] ProxyNames = { "version", "winmm", "winhttp" };

        internal enum Operations
        {
            NONE,
            INSTALLER_UPDATE,
            INSTALL,
            UNINSTALL,
            REINSTALL,
            UPDATE,
            DOWNGRADE,
        }
        internal static Operations CurrentOperation = Operations.NONE;
        internal static string CurrentOperationName
        {
            get
            {
                string opname = null;
                switch (CurrentOperation)
                {
                    case Operations.DOWNGRADE:
                        opname = "DOWNGRADE";
                        break;
                    case Operations.UPDATE:
                        opname = "UPDATE";
                        break;
                    case Operations.UNINSTALL:
                        opname = "UN-INSTALL";
                        break;
                    case Operations.REINSTALL:
                        opname = "RE-INSTALL";
                        break;
                    case Operations.INSTALL:
                    case Operations.INSTALLER_UPDATE:
                    case Operations.NONE:
                    default:
                        opname = "INSTALL";
                        break;
                }
                return opname;
            }
        }

        internal static void Automated_Install(string destination, string selected_version, bool is_x86, bool legacy_version)
        {
            Program.SetCurrentOperation("Downloading MelonLoader...");
            string downloadurl = Config.Download_MelonLoader + "/" + selected_version + "/MelonLoader." + ((!legacy_version && is_x86) ? "x86" : "x64") + ".zip";
            string temp_path = TempFileCache.CreateFile();
            try { Program.webClient.DownloadFileAsync(new Uri(downloadurl), temp_path); while (Program.webClient.IsBusy) { } }
            catch (Exception ex)
            {
                Program.LogError(ex.ToString());
                return;
            }
            Program.SetTotalPercentage(50);
            if (Program.Closing)
                return;
            string repo_hash_url = Config.Download_MelonLoader + "/" + selected_version + "/MelonLoader." + ((!legacy_version && is_x86) ? "x86" : "x64") + ".sha512";
            string repo_hash = null;
            try { repo_hash = Program.webClient.DownloadString(repo_hash_url); } catch { repo_hash = null; }
            if (string.IsNullOrEmpty(repo_hash))
            {
                Program.LogError("Failed to get SHA512 Hash from Repo!");
                return;
            }
            if (Program.Closing)
                return;
            SHA512Managed sha512 = new SHA512Managed();
            byte[] checksum = sha512.ComputeHash(File.ReadAllBytes(temp_path));
            if ((checksum == null) || (checksum.Length <= 0))
            {
                Program.LogError("Failed to get SHA512 Hash from Temp File!");
                return;
            }
            string file_hash = BitConverter.ToString(checksum).Replace("-", string.Empty);
            if (string.IsNullOrEmpty(file_hash))
            {
                Program.LogError("Failed to get SHA512 Hash from Temp File!");
                return;
            }
            if (!file_hash.Equals(repo_hash))
            {
                Program.LogError("SHA512 Hash from Temp File does not match Repo Hash!");
                return;
            }
            Program.SetCurrentOperation("Extracting MelonLoader...");
            try
            {
                string MelonLoader_Folder = Path.Combine(destination, "MelonLoader");
                if (Directory.Exists(MelonLoader_Folder))
                {
                    ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                    {
                        try { Directory.Delete(MelonLoader_Folder, true); }
                        catch (Exception ex)
                        {
                            if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                                && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                                throw ex;
                            DialogResult result = MessageBox.Show($"Unable to remove Existing MelonLoader Folder! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                            if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                        }
                    });
                }
                string proxy_path = null;
                if (GetExistingProxyPath(destination, out proxy_path))
                {
                    ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                    {
                        try { File.Delete(proxy_path); }
                        catch (Exception ex)
                        {
                            if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                                && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                                throw ex;
                            DialogResult result = MessageBox.Show($"Unable to remove Existing Proxy Module! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                            if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                        }
                    });
                }
                using FileStream stream = new FileStream(temp_path, FileMode.Open, FileAccess.Read);
                using ZipArchive zip = new ZipArchive(stream);
                int total_entry_count = zip.Entries.Count;
                for (int i = 0; i < total_entry_count; i++)
                {
                    if (Program.Closing)
                        break;
                    int percentage = ((i / total_entry_count) * 100);
                    Program.SetCurrentPercentage(percentage);
                    Program.SetTotalPercentage((50 + (percentage / 2)));
                    ZipArchiveEntry entry = zip.Entries[i];
                    string fullPath = Path.Combine(destination, entry.FullName);
                    if (!fullPath.StartsWith(destination))
                        throw new IOException("Extracting Zip entry would have resulted in a file outside the specified destination directory.");
                    string filename = Path.GetFileName(fullPath);
                    if (filename.Length != 0)
                    {
                        if (!legacy_version && filename.Equals("version.dll"))
                        {
                            foreach (string proxyname in ProxyNames)
                            {
                                string new_proxy_path = Path.Combine(destination, (proxyname + ".dll"));
                                if (File.Exists(new_proxy_path))
                                    continue;
                                fullPath = new_proxy_path;
                                break;
                            }
                        }
                        string directorypath = Path.GetDirectoryName(fullPath);
                        if (!Directory.Exists(directorypath))
                            Directory.CreateDirectory(directorypath);
                        using FileStream targetStream = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Write);
                        using Stream entryStream = entry.Open();
                        ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                        {
                            try { entryStream.CopyTo(targetStream); }
                            catch (Exception ex)
                            {
                                if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                                    && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                                    throw ex;
                                DialogResult result = MessageBox.Show($"Couldn't extract file {filename}! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                                if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                            }
                        });
                        continue;
                    }
                    if (entry.Length != 0)
                        throw new IOException("Zip entry name ends in directory separator character but contains data.");
                    if (!Directory.Exists(fullPath))
                        Directory.CreateDirectory(fullPath);
                }
                ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                {
                    try { DowngradeMelonPreferences(destination, legacy_version); }
                    catch (Exception ex)
                    {
                        if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                            && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                            throw ex;
                        DialogResult result = MessageBox.Show($"Unable to Downgrade MelonLoader Preferences! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                        if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                    }
                });
                ExtraDirectoryChecks(destination);
                ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                {
                    try { ExtraCleanupCheck(destination); }
                    catch (Exception ex)
                    {
                        if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                            && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                            throw ex;
                        DialogResult result = MessageBox.Show($"Couldn't do Extra File Cleanup! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                        if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                    }
                });
            }
            catch (Exception ex)
            {
                Program.LogError(ex.ToString());
                return;
            }
            if (Program.Closing)
                return;
            TempFileCache.ClearCache();
            Program.OperationSuccess();
            Program.FinishingMessageBox((CurrentOperationName + " was Successful!"), MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        internal static void ManualZip_Install(string zip_path, string destination)
        {
            Program.SetCurrentOperation("Extracting Zip Archive...");
            try
            {
                string MelonLoader_Folder = Path.Combine(destination, "MelonLoader");
                if (Directory.Exists(MelonLoader_Folder))
                {
                    ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                    {
                        try { Directory.Delete(MelonLoader_Folder, true); }
                        catch (Exception ex)
                        {
                            if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                                && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                                throw ex;
                            DialogResult result = MessageBox.Show($"Unable to remove Existing MelonLoader Folder! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                            if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                        }
                    });
                }
                string proxy_path = null;
                if (GetExistingProxyPath(destination, out proxy_path))
                {
                    ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                    {
                        try { File.Delete(proxy_path); }
                        catch (Exception ex)
                        {
                            if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                                && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                                throw ex;
                            DialogResult result = MessageBox.Show($"Unable to remove Existing Proxy Module! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                            if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                        }
                    });
                }
                using FileStream stream = new FileStream(zip_path, FileMode.Open, FileAccess.Read);
                using ZipArchive zip = new ZipArchive(stream);
                int total_entry_count = zip.Entries.Count;
                for (int i = 0; i < total_entry_count; i++)
                {
                    if (Program.Closing)
                        break;
                    int percentage = ((i / total_entry_count) * 100);
                    Program.SetCurrentPercentage(percentage);
                    Program.SetTotalPercentage(percentage);
                    ZipArchiveEntry entry = zip.Entries[i];
                    string fullPath = Path.Combine(destination, entry.FullName);
                    if (!fullPath.StartsWith(destination))
                        throw new IOException("Extracting Zip entry would have resulted in a file outside the specified destination directory.");
                    string filename = Path.GetFileName(fullPath);
                    if (filename.Length != 0)
                    {
                        string directorypath = Path.GetDirectoryName(fullPath);
                        if (!Directory.Exists(directorypath))
                            Directory.CreateDirectory(directorypath);
                        using FileStream targetStream = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Write);
                        using Stream entryStream = entry.Open();
                        ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                        {
                            try { entryStream.CopyTo(targetStream); }
                            catch (Exception ex)
                            {
                                if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                                    && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                                    throw ex;
                                DialogResult result = MessageBox.Show($"Couldn't extract file {filename}! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                                if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                            }
                        });
                        continue;
                    }
                    if (entry.Length != 0)
                        throw new IOException("Zip entry name ends in directory separator character but contains data.");
                    if (!Directory.Exists(fullPath))
                        Directory.CreateDirectory(fullPath);
                }
                ExtraDirectoryChecks(destination);
                ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                {
                    try { ExtraCleanupCheck(destination); } catch (Exception ex)
                    {
                        if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                            && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                            throw ex;
                        DialogResult result = MessageBox.Show($"Couldn't do Extra File Cleanup! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                        if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                    }
                });
            }
            catch (Exception ex)
            {
                Program.LogError(ex.ToString());
                return;
            }
            if (Program.Closing)
                return;
            Program.OperationSuccess();
            Program.FinishingMessageBox((CurrentOperationName + " was Successful!"), MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        internal static void Uninstall(string destination)
        {
            Program.SetCurrentOperation("Uninstalling MelonLoader...");
            try
            {
                string MelonLoader_Folder = Path.Combine(destination, "MelonLoader");
                if (Directory.Exists(MelonLoader_Folder))
                {
                    ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                    {
                        try { Directory.Delete(MelonLoader_Folder, true); }
                        catch (Exception ex)
                        {
                            if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                                && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                                throw ex;
                            DialogResult result = MessageBox.Show($"Unable to remove Existing MelonLoader Folder! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                            if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                        }
                    });
                }

                string proxy_path = null;
                if (GetExistingProxyPath(destination, out proxy_path))
                {
                    ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                    {
                        try { File.Delete(proxy_path); }
                        catch (Exception ex)
                        {
                            if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                                && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                                throw ex;
                            DialogResult result = MessageBox.Show($"Unable to remove Existing Proxy Module! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                            if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                        }
                    });
                }
                ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                {
                    try { ExtraCleanupCheck(destination); }
                    catch (Exception ex)
                    {
                        if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                            && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                            throw ex;
                        DialogResult result = MessageBox.Show($"Couldn't do Extra File Cleanup! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                        if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                    }
                });

                string dobby_path = Path.Combine(destination, "dobby.dll");
                if (File.Exists(dobby_path))
                {
                    ThreadHandler.RecursiveFuncRun(delegate (ThreadHandler.RecursiveFuncRecurse recurse)
                    {
                        try { File.Delete(dobby_path); }
                        catch (Exception ex)
                        {
                            if (!ex.GetType().IsAssignableFrom(typeof(UnauthorizedAccessException))
                                && !ex.GetType().IsAssignableFrom(typeof(IOException)))
                                throw ex;
                            DialogResult result = MessageBox.Show($"Unable to remove dobby.dll! Make sure the Unity Game is not running or try running the Installer as Administrator.", BuildInfo.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                            if (result == DialogResult.Retry) recurse.Invoke(); else throw ex;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Program.LogError(ex.ToString());
                return;
            }
            if (Program.Closing)
                return;
            Program.CurrentInstalledVersion = null;
            Program.OperationSuccess();
            Program.FinishingMessageBox((CurrentOperationName + " was Successful!"), MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private static bool GetExistingProxyPath(string destination, out string proxy_path)
        {
            proxy_path = null;
            foreach (string proxy in ProxyNames)
            {
                string new_proxy_path = Path.Combine(destination, (proxy + ".dll"));
                if (!File.Exists(new_proxy_path))
                    continue;
                FileVersionInfo fileinfo = FileVersionInfo.GetVersionInfo(new_proxy_path);
                if (fileinfo == null)
                    continue;
                if (!string.IsNullOrEmpty(fileinfo.LegalCopyright) && fileinfo.LegalCopyright.Contains("Microsoft"))
                    continue;
                proxy_path = new_proxy_path;
                break;
            }
            return !string.IsNullOrEmpty(proxy_path);
        }

        private static void DowngradeMelonPreferences(string destination, bool legacy_version)
        {
            if (!legacy_version || (Program.CurrentInstalledVersion == null) || (Program.CurrentInstalledVersion.CompareTo(new Version("0.3.0")) < 0))
                return;
            string userdatapath = Path.Combine(destination, "UserData");
            if (!Directory.Exists(userdatapath))
                return;
            string oldfilepath = Path.Combine(userdatapath, "MelonPreferences.cfg");
            if (!File.Exists(oldfilepath))
                return;
            string filestr = File.ReadAllText(oldfilepath);
            if (string.IsNullOrEmpty(filestr))
                return;
            DocumentSyntax docsyn = Toml.Parse(filestr);
            if (docsyn == null)
                return;
            TomlTable model = docsyn.ToModel();
            if (model.Count <= 0)
                return;
            string newfilepath = Path.Combine(userdatapath, "modprefs.ini");
            if (File.Exists(newfilepath))
                File.Delete(newfilepath);
            IniFile iniFile = new IniFile(newfilepath);
            foreach (KeyValuePair<string, object> keypair in model)
            {
                string category_name = keypair.Key;
                TomlTable tbl = (TomlTable)keypair.Value;
                if (tbl.Count <= 0)
                    continue;
                foreach (KeyValuePair<string, object> tblkeypair in tbl)
                {
                    string name = tblkeypair.Key;
                    if (string.IsNullOrEmpty(name))
                        continue;
                    TomlObject obj = TomlObject.ToTomlObject(tblkeypair.Value);
                    if (obj == null)
                        continue;
                    switch(obj.Kind)
                    {
                        case ObjectKind.String:
                            iniFile.SetString(category_name, name, ((TomlString)obj).Value);
                            break;
                        case ObjectKind.Boolean:
                            iniFile.SetBool(category_name, name, ((TomlBoolean)obj).Value);
                            break;
                        case ObjectKind.Integer:
                            iniFile.SetInt(category_name, name, (int)((TomlInteger)obj).Value);
                            break;
                        case ObjectKind.Float:
                            iniFile.SetFloat(category_name, name, (float)((TomlFloat)obj).Value);
                            break;
                        default:
                            break;
                    }
                }
            }
            File.Delete(oldfilepath);
        }

        private static void ExtraDirectoryChecks(string destination)
        {
            string pluginsDirectory = Path.Combine(destination, "Plugins");
            if (!Directory.Exists(pluginsDirectory))
                Directory.CreateDirectory(pluginsDirectory);
            string modsDirectory = Path.Combine(destination, "Mods");
            if (!Directory.Exists(modsDirectory))
                Directory.CreateDirectory(modsDirectory);
            string userdataDirectory = Path.Combine(destination, "UserData");
            if (!Directory.Exists(userdataDirectory))
                Directory.CreateDirectory(userdataDirectory);
        }

        private static void ExtraCleanupCheck(string destination)
        {
            string main_dll = Path.Combine(destination, "MelonLoader.dll");
            if (File.Exists(main_dll))
                File.Delete(main_dll);
            main_dll = Path.Combine(destination, "Mods", "MelonLoader.dll");
            if (File.Exists(main_dll))
                File.Delete(main_dll);
            main_dll = Path.Combine(destination, "Plugins", "MelonLoader.dll");
            if (File.Exists(main_dll))
                File.Delete(main_dll);
            main_dll = Path.Combine(destination, "UserData", "MelonLoader.dll");
            if (File.Exists(main_dll))
                File.Delete(main_dll);
            string main2_dll = Path.Combine(destination, "MelonLoader.ModHandler.dll");
            if (File.Exists(main2_dll))
                File.Delete(main2_dll);
            main2_dll = Path.Combine(destination, "Mods", "MelonLoader.ModHandler.dll");
            if (File.Exists(main2_dll))
                File.Delete(main2_dll);
            main2_dll = Path.Combine(destination, "Plugins", "MelonLoader.ModHandler.dll");
            if (File.Exists(main2_dll))
                File.Delete(main2_dll);
            main2_dll = Path.Combine(destination, "UserData", "MelonLoader.ModHandler.dll");
            if (File.Exists(main2_dll))
                File.Delete(main2_dll);
            string logs_path = Path.Combine(destination, "Logs");
            if (Directory.Exists(logs_path))
                Directory.Delete(logs_path, true);
        }
    }
}