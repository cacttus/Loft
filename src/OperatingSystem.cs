using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

namespace Loft
{
  public enum Platform
  {
    Unknown,
    Linux,
    Windows
  }
  public static class OperatingSystem
  {
    public static bool ConsoleVisible { get; private set; } = false;
    private static Platform? _platform = null;

    public static string VersionString { get { return System.Environment.OSVersion.VersionString; } }
    public static Platform Platform
    {
      get
      {
        if (_platform == null)
        {
          bool linux = VersionString.ToLower().Contains("unix");
          bool windows = VersionString.ToLower().Contains("windows");

          if (linux && windows)
          {
            _platform = Loft.Platform.Unknown; // Buggy , way
          }
          else if (linux)
          {
            _platform = Loft.Platform.Linux;
          }
          else if (windows)
          {
            _platform = Loft.Platform.Windows;
          }
          else
          {
            _platform = Loft.Platform.Unknown;
          }
        }
        return _platform.Value;
      }
    }
    public static void ShowConsole(bool show)
    {
      //On linux the console window does not show, so this isn't really necessary, however
      //to see log, use tail -n or some other command.
      if (Platform == Loft.Platform.Windows)
      {
        Interop.ShowWindow(Interop.GetConsoleWindow(), show ? Interop.SW_SHOW : Interop.SW_HIDE);
        ConsoleVisible = show;
      }
      else
      {
        Gu.Log.Error("Console window: invalid platform.");
      }
    }

    //[DllImport("advapi32.dll", CharSet = CharSet.Auto)]
    //public static extern int RegOpenKeyEx(  UIntPtr hKey,  string subKey,  int ulOptions, int samDesired,  out UIntPtr hkResult);
    
    //[DllImport("advapi32.dll", SetLastError = true)]
    //public static extern int RegCloseKey(IntPtr hKey);

    //public static UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002u);
    //public static UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001u);
    //[Flags]
    //public enum RegOption
    //{
    //  NonVolatile = 0x0,
    //  Volatile = 0x1,
    //  CreateLink = 0x2,
    //  BackupRestore = 0x4,
    //  OpenLink = 0x8
    //}
    //[Flags]
    //public enum RegSAM
    //{
    //  QueryValue = 0x0001,
    //  SetValue = 0x0002,
    //  CreateSubKey = 0x0004,
    //  EnumerateSubKeys = 0x0008,
    //  Notify = 0x0010,
    //  CreateLink = 0x0020,
    //  WOW64_32Key = 0x0200,
    //  WOW64_64Key = 0x0100,
    //  WOW64_Res = 0x0300,
    //  Read = 0x00020019,
    //  Write = 0x00020006,
    //  Execute = 0x00020019,
    //  AllAccess = 0x000f003f
    //}

    //public enum RegResult
    //{
    //  CreatedNewKey = 0x00000001,
    //  OpenedExistingKey = 0x00000002
    //}
    //public static string GetRegistryValue(UIntPtr rootKey, string keyPath, string valueName)
    //{
    //  //HKEY_LOCAL_MACHINE, @"Software\SomeCompany\OurProduct", "InstalledVersion"

    //  if (RegOpenKeyEx(rootKey, keyPath, 0, KEY_READ, out hKey) == 0)
    //  {
    //    uint size = 1024;
    //    uint type;
    //    string keyValue = null;
    //    StringBuilder keyBuffer = new StringBuilder((int)size);

    //    if (RegQueryValueEx(hKey, valueName, IntPtr.Zero, out type, keyBuffer, ref size) == 0)
    //      keyValue = keyBuffer.ToString();

    //    RegCloseKey(hKey);

    //    return (keyValue);
    //  }

    //  return null;
    //}



//// Checking the version using >= enables forward compatibility.
//string CheckFor45PlusVersion(int releaseKey)
//{
//  if (releaseKey >= 528040)
//    return "4.8 or later";
//  if (releaseKey >= 461808)
//    return "4.7.2";
//  if (releaseKey >= 461308)
//    return "4.7.1";
//  if (releaseKey >= 460798)
//    return "4.7";
//  if (releaseKey >= 394802)
//    return "4.6.2";
//  if (releaseKey >= 394254)
//    return "4.6.1";
//  if (releaseKey >= 393295)
//    return "4.6";
//  if (releaseKey >= 379893)
//    return "4.5.2";
//  if (releaseKey >= 378675)
//    return "4.5.1";
//  if (releaseKey >= 378389)
//    return "4.5";
//  // This code should never execute. A non-null release key should mean
//  // that 4.5 or later is installed.
//  return "No 4.5 or later version detected";
//}

  }//cls
  public class SystemInfo_Fast
  {
    //faster.. This class can call "query" every so many frames as these methods tend to be on the slow side
    public static long _memUsedBytes = 0;
    public static long _vmemUsedBytes = 0;
    public static string _asmVersion = "";
    public static long _start = 0;

    public static void Query(long millis)
    {
      long cms = Gu.Milliseconds();
      if (cms - _start > millis)
      {
        _start = cms;

        _memUsedBytes = System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64;
        _vmemUsedBytes = System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64;

        if (string.IsNullOrEmpty(_asmVersion))
        {
          //Note: this is kind of slow and takes significant frame slice
          System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
          System.Diagnostics.FileVersionInfo fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
          _asmVersion = fileVersionInfo.ProductVersion;
        }
      }
    }

    public static string AssemblyVersion { get { return _asmVersion; } }
    public static long MemUsedBytes { get { return _memUsedBytes; } }
    public static long VMemUsedBytes { get { return _vmemUsedBytes; } }

    public static float BToMB(long b)
    {
      var b_to_mb = 1024 * 1024;
      var bc = b / b_to_mb;
      var bm = b % b_to_mb;
      return (float)(bc + (float)bm / (float)b_to_mb);
    }
  }//cls
}//ns
