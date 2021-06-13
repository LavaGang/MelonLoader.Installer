using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonLoader.Interfaces
{
    internal static class UnityGame
    {
        internal enum PLATFORM_ENUM
        {
            None = 0,
            Windows_x86,
            Windows_x64,
        }
        internal static PLATFORM_ENUM Platform = PLATFORM_ENUM.None;

        internal static void OpenSelectionPrompt()
        {

        }

        internal static void SelectFile(string filepath)
        {

        }
    }
}
