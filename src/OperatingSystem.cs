using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PirateCraft
{
    public static class OperatingSystem
    {
        public enum Platform
        {
            Unknown,
            Linux,
            Windows
        }
        //Yeah linux.
        const int SW_HIDE = 0;
        const int SW_SHOWNORMAL = 1;
        const int SW_NORMAL = 1;
        const int SW_SHOWMINIMIZED = 2;
        const int SW_SHOWMAXIMIZED = 3;
        const int SW_MAXIMIZE = 3;
        const int SW_SHOWNOACTIVATE = 4;
        const int SW_SHOW = 5;
        const int SW_MINIMIZE = 6;
        const int SW_SHOWMINNOACTIVE = 7;
        const int SW_SHOWNA = 8;
        const int SW_RESTORE = 9;
        const int SW_SHOWDEFAULT = 10;
        const int SW_FORCEMINIMIZE = 11;
        const int SW_MAX = 11;
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static bool _consoleisshow = true;//default

        public static OperatingSystem.Platform GetPlatform()
        {
            bool linux = System.Environment.OSVersion.VersionString.ToLower().Contains("unix");
            bool windows = System.Environment.OSVersion.VersionString.ToLower().Contains("windows");

            if (linux && windows)
            {
                return Platform.Unknown; // Buggy , way
            }
            if (linux)
            {
                return Platform.Linux;
            }
            if (windows)
            {
                return Platform.Windows;
            }
            return Platform.Unknown;
        }
        public static void ShowConsole()
        {
          //On linux the console window does not show, so this isn't really necessary, however
          //to see log, use tail -n or some other command.
            if (GetPlatform() == Platform.Windows)
            {
                ShowWindow(GetConsoleWindow(), SW_SHOW);
            }
            else
            {
                Gu.Log.Error("Console window: invalid platform.");
            }
        }
        public static void HideConsole()
        {
            if (GetPlatform() == Platform.Windows)
            {
                ShowWindow(GetConsoleWindow(), SW_HIDE);
            }
            else
            {
                Gu.Log.Error("Console window: invalid platform.");
            }
        }
        public static void ToggleShowConsole()
        {
            if (_consoleisshow)
            {
                ShowConsole();
            }
            else
            {
                HideConsole();
            }
            _consoleisshow = !_consoleisshow;
        }
    }
}
