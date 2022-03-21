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
    public static void ShowConsole()
    {
      ShowWindow(GetConsoleWindow(), SW_SHOW);
    }
    public static void HideConsole()
    {
      ShowWindow(GetConsoleWindow(), SW_HIDE);
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
