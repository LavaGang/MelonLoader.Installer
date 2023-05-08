using System.Collections.Generic;
using System.IO;

namespace MelonLoader
{
    internal static class TempFileCache
    {
        private static List<string> TempFiles = new List<string>();

        /// <summary>
        /// Creates a temporary file and adds its path to a list of temporary files.
        /// </summary>
        /// <returns>The path of the created temporary file.</returns>
        internal static string CreateFile()
        {
            string temppath = Path.GetTempFileName();
            TempFiles.Add(temppath);
            return temppath;
        }

        /// <summary>
        /// Clears the cache by deleting all temporary files.
        /// </summary>
        internal static void ClearCache()
        {
            if (TempFiles.Count <= 0)
                return;
            foreach (string file in TempFiles)
                if (File.Exists(file))
                    File.Delete(file);
        }
    }
}
