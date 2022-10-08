using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Desktop;
using System.Reflection;
using System.Diagnostics;
using System.Linq.Expressions;

namespace PirateCraft
{
  public static class Gu
  {
    public static bool BreakRenderState = false;

    private static bool SkipRequiredTests = false;
    public static void MustTest()
    {
      //Must test this method.
      if (!SkipRequiredTests)
      {
        Gu.DebugBreak();
      }
    }
    // Global Utils. static Class
    #region Public: Constants

    public const int c_intMaxWhileTrueLoopBinarySearch64Bit = 64;//dummy infinite loop blocker
    public const int c_intMaxWhileTrueLoopSmall = 1000;//dummy infinite loop blocker
    public const int c_intMaxWhileTrueLoop = 100000;//dummy infinite loop blocker
    public const int c_intMaxWhileTrueLoopLONG = 100000000;//dummy infinite loop blocker


    #endregion
    #region Public: Static Members

    public static bool AllowOpenTKFaults = false;//OpenTK's GL isn't fully implemented in a lot of places
    public static Dictionary<UiWindowBase, WindowContext> Contexts { get; private set; } = new Dictionary<UiWindowBase, WindowContext>();
    public static CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.Rhs;
    public static float CoordinateSystemMultiplier { get { return (Gu.CoordinateSystem == CoordinateSystem.Lhs ? -1 : 1); } }
    public static EngineConfig EngineConfig { get; set; } = null;
    public static Log Log { get; set; } = null;
    public static WindowContext Context { get; set; } = null;
    public static World World = null;
    public static WorldLoader WorldLoader = null;
    public static PCMouse Mouse { get { return Context.PCMouse; } }
    public static PCKeyboard Keyboard { get { return Context.PCKeyboard; } }
    public static string ExePath { get; private set; } = "";
    public static string LocalCachePath { get; private set; } = "";//megatex..
    public static string LocalTmpPath { get; private set; } = "";//logs..debug..shaderdebug..
    public static string WorkspacePath { get; private set; } = "";
    public static string WorkspaceDataPath { get; private set; } = "";// the ./Data directory. This is not present on embedded versions.
    public static string SavePath { get; private set; } = "";
    public static string BackupPath { get; private set; } = "";
    public static readonly string EmbeddedDataPath = "PirateCraft.data.";
    public static Library?  Lib {get; private set;}=null;
    public static AudioManager?  Audio { get; private set; } = null;
    public static Gui2dManager?  Gui2dManager { get; private set; } = null;
    public static FrameDataTimer?  GlobalTimer { get; private set; } = null;//Global frame timer, for all windows;
    public static Translator?  Translator { get; private set; } = null;
    public static UiWindowBase? FocusedWindow { get; set; } = null;

    #endregion
    #region Private: Static Members

    private static bool _exitPosted = false;
    private static List<UiWindowBase> toClose = new List<UiWindowBase>();
    private static bool _customDebugBreak = false;

    #endregion
    #region Public: Static Methods

    public static void InitGlobals()
    {
      //Paths
      var assemblyLoc = System.Reflection.Assembly.GetExecutingAssembly().Location;
      ExePath = System.IO.Path.GetDirectoryName(assemblyLoc);
      LocalTmpPath = System.IO.Path.Combine(ExePath, "./tmp");
      LocalCachePath = System.IO.Path.Combine(ExePath, "./cache");

      //This is for debug files and file changes. 
      WorkspacePath = System.IO.Path.Combine(ExePath, "../../../");
      WorkspaceDataPath = System.IO.Path.Combine(WorkspacePath, "./data");//This may change .. uh

      //Log
      Log = new Log(Gu.LocalTmpPath);
      Gu.Log.Info("Initializing Globals");
      Gu.Log.Info("CurrentDirectory =" + System.IO.Directory.GetCurrentDirectory());

      BListTest_GonnaUseItQuestionMark();
      //Config
      EngineConfig = new EngineConfig(new FileLoc("config.json", FileStorage.Embedded));

      if (StringUtil.IsNotEmpty(EngineConfig.UserSavePath))
      {
        try
        {
          SavePath = System.IO.Path.Combine(ExePath, EngineConfig.UserSavePath);
        }
        catch (Exception ex)
        {
          Gu.Log.Error(ex);
        }
      }

      if (StringUtil.IsEmpty(SavePath))
      {
        SavePath = System.IO.Path.Combine(ExePath, "./save");
      }

      if (StringUtil.IsEmpty(BackupPath))
      {
        BackupPath = System.IO.Path.Combine(ExePath, "./backup");
      }

      Gu.Log.Info($"ExePath: {ExePath}");
      Gu.Log.Info($"SavePath: {SavePath}");
      Gu.Log.Info($"BackupPath: {BackupPath}");
      Gu.Log.Info($"LocalTmpPath: {LocalTmpPath}");
      Gu.Log.Info($"LocalCachePath: {LocalCachePath}");
      Gu.Log.Info($"WorkspacePath: {WorkspacePath}");
      Gu.Log.Info($"WorkspaceDataPath: {WorkspaceDataPath}");
      if (
        !SavePath.Contains(ExePath) ||
        !BackupPath.Contains(ExePath) ||
        !LocalTmpPath.Contains(ExePath) ||
        !LocalCachePath.Contains(ExePath) ||
        !WorkspacePath.Contains(ExePath) ||
        !WorkspaceDataPath.Contains(ExePath)
        )
      {
        Gu.Log.Warn("Paths may be invalid. Make sure not root.");
        Gu.DebugBreak();
      }


      //Manager
      Translator = new Translator();
      Lib = new Library();
      GlobalTimer = new FrameDataTimer();
      Audio = new AudioManager();
      Gui2dManager = new Gui2dManager();
    }
    public static void BListTest_GonnaUseItQuestionMark()
    {
      System.Text.StringBuilder sb = new System.Text.StringBuilder();

      // sb.AppendLine("");
      // sb.AppendLine("");
      // sb.AppendLine("");
      var x = new BList<int>() { };
      // sb.AppendLine(x.ToString());
      // x.Add(955);
      // sb.AppendLine(x.ToString());
      // x.Remove(3);
      // x.Remove(9);
      // x.Remove(955);
      // sb.AppendLine(x.ToString());

      //check non dupe adds
      // sb.AppendLine("non dupes");
      x = new BList<int>() { 19, 0, 1, 5, 4, 3, 6, 8, 2, 12, 7, 13, 10, 14, 11, 16, 15, 17, 18, 1853 };
      for (var i = 0; i < 5000; ++i)
      {
        var yy = Random.NextInt(0, 2000);
        if (!x.Contains(yy)) { x.Add(yy); }
      }
      sb.AppendLine(x.ToString());

      //check dupe ads
      sb.AppendLine("dupes");
      x = new BList<int>() { 13, 8, 14, 14, 2, 19, 5, 30, 1, 1, 30, 14 };
      x = new BList<int>() { };
      for (var i = 0; i < 50; ++i)
      {
        x.Add(Random.NextInt(0, 20));
      }
      sb.AppendLine(x.ToString());

      for (int ct = x.Count - 1; ct >= 0; ct--)
      {
        var ccc = x[ct];
        x.Remove(ccc);
      }
      sb.AppendLine(x.ToString());
      Gu.Log.Info(sb.ToString());

    }
    public static void Run()
    {
      try
      {
        while (true)
        {
          if (Contexts.Count == 0)
          {
            throw new Exception("No window has been created yet. Create window before calling Run()");
          }
          else
          {
            //Grab all windows from their given contexts so we can loop over windows only.
            //Not sure how to typically do this easily in C#
            List<UiWindowBase> wins = new List<UiWindowBase>();
            foreach (var ct in Contexts)
            {
              wins.Add(ct.Key);
            }

            GlobalTimer.Update();

            foreach (var win in wins)
            {
              Gu.SetContext(win);
              if (!win.IsLoaded)
              {
                win.Load();
              }
              win.ProcessEvents();
              Gu.Context.Update();

              if (Gu.World.UpdateContext == Gu.Context)
              {
                Gu.World.Update(Gu.Context.Delta);
              }

              win.UpdateAsync();
              win.RenderAsync();
            }

            CheckExit();

            _customDebugBreak = false;
          }
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Fatal error: ", ex);
      }
    }
    private static void CheckExit()
    {
      //Remove/Destroy windows / main window destroys app, or force
      foreach (var win in toClose)
      {
        win.Context.MakeNoneCurrent();
        Contexts.Remove(win);
        win.Close();
        win.IsVisible = false;

        if (Contexts.Count == 0 || win.IsMain)
        {
          Environment.Exit(0);
          break;
        }
      }
      toClose.Clear();

      if (_exitPosted)
      {
        //Basically Exit() failed, maybe there was a window created since Exit() was called..
        Environment.Exit(0);
      }
    }
    public static void Exit()
    {
      _exitPosted = true;
      foreach (var ct in Contexts)
      {
        CloseWindow(ct.Key);
      }
    }
    public static void CloseWindow(UiWindowBase win)
    {
      if (win != null)
      {
        if (!toClose.Contains(win))
        {
          toClose.Add(win);
        }
      }
    }
    public static void CreateContext(string name, UiWindowBase uw)
    {
      //***TODO: context names must be unique because we use them to save, and store state.

      Gu.Log.Info("Registering Context name='" + name + "', window= " + uw.Name);
      var ct = Gu.Context;
      var wd = new WindowContext(name, uw);
      Contexts.Add(uw, wd);
      Gu.SetContext(uw);
      wd.Init();
      if (ct != null)
      {
        //Set back in case we are not main window
        Gu.SetContext(ct.GameWindow);
      }
    }
    public static void SetContext(UiWindowBase g)
    {
      if (Contexts.TryGetValue(g, out var ct))
      {
        Context = ct;
        g.Context.MakeCurrent();
      }
    }
    public static WindowContext GetContextForWindow(UiWindowBase g)
    {
      if (Contexts.TryGetValue(g, out var ct))
      {
        return ct;
      }
      return null;
    }
    public static Int64 Nanoseconds()
    {
      return DateTime.UtcNow.Ticks * 100;
    }
    public static Int64 Microseconds()
    {
      return Nanoseconds() / 1000;
    }
    public static Int64 Milliseconds()
    {
      return Microseconds() / 1000;
    }
    public static double RotationPerSecond(double seconds)
    {
      var f = (Context.UpTime % seconds) / seconds;
      f *= Math.PI * 2;
      return f;
    }
    public static void SaveFBOs()
    {
      foreach (var ct in Contexts)
      {
        ct.Value.Renderer.SaveFBOs();
      }
    }
    public static void BRThrowException(string msg)
    {
      throw new Exception("Error: " + msg);
    }
    public static void BRThrowNotImplementedException()
    {
      Gu.DebugBreak();
      throw new NotImplementedException();
    }
    public static void AssertDebug(bool x, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
    {
      Assert(x, string.Empty, lineNumber, caller, true);
    }
    public static void Assert(bool x, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null, bool debugOnly = false)
    {
      Assert(x, string.Empty, lineNumber, caller);
    }
    public static void Assert(bool x, string emsg, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null, bool debugOnly = false)
    {
      if (!x)
      {
        string msg = $"Assertion failed {emsg}: {caller}:{lineNumber.ToString()}";
        if (emsg.IsNotEmpty())
        {
          emsg = $"'{emsg}'";
        } 
        Gu.Log.Error(emsg);
        Gu.DebugBreak();

        if (debugOnly == false)
        {
          System.Diagnostics.Debug.Assert(x, msg);
          throw new Exception(msg);
        }
      }
    }
    public static void DebugBreak()
    {
      Debugger.Break();
    }
    public static void Trap()
    {
      int n = 0;
      n++;
    }
    public static byte[] Compress(byte[] data)
    {
      MemoryStream output = new MemoryStream();
      using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
      {
        dstream.Write(data, 0, data.Length);
      }
      return output.ToArray();
    }
    public static byte[] Decompress(byte[] data)
    {
      MemoryStream input = new MemoryStream(data);
      MemoryStream output = new MemoryStream();
      using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
      {
        dstream.CopyTo(output);
      }
      return output.ToArray();
    }
    public static void TryLock(object ob, Action<object> act)
    {
      if (Monitor.TryEnter(ob))
      {
        try
        {
          act(ob);
        }
        finally
        {
          Monitor.Exit(ob);
        }
      }
    }
    public static string GetAllException(Exception ex)
    {
      string ret = "";
      Exception tmp = ex;
      while (tmp != null)
      {
        ret += tmp.ToString() + Environment.NewLine;
        tmp = tmp.InnerException;
        if (tmp != null)
        {
          ret += "InnerException:" + Environment.NewLine;
        }
      }
      return ret;
    }
    public static string GetFilenameDateTimeNOW()
    {
      //return a windows safe filename with datenow
      return DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss.fff");
    }
    public static ulong HashStringArray(List<string> strrings)
    {
      //I don't claim to know anything about maths
      //https://stackoverflow.com/questions/19250374/fastest-way-to-make-a-hashkey-of-multiple-strings
      unchecked
      {
        ulong hash = 17;
        foreach (var s in strrings)
        {
          hash = hash * 23 + s == null ? 0 : (ulong)s.GetHashCode();
        }
        return hash;
      }
    }
    public static UInt64 HashByteArray(List<byte[]> datas)
    {
      //https://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c
      unchecked
      {
        const UInt64 p = 16777619;
        UInt64 hash = (UInt64)2166136261;

        foreach (var data in datas)
        {
          Gu.Assert(data != null);
          for (UInt64 i = 0; i < (UInt64)data.Length; i++)
          {
            hash = (hash ^ data[i]) * p;
          }
        }

        hash += hash << 13;
        hash ^= hash >> 7;
        hash += hash << 3;
        hash ^= hash >> 17;
        hash += hash << 5;
        return hash;
      }
    }
    public static int HashIntArray(int[] intlist)
    {
      //https://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c
      unchecked
      {
        const int p = 16777619;
        int hash = (int)2166136261;

        foreach (var i in intlist)
        {
          hash = (hash ^ i) * p; //Not sure if this will work but.
        }

        hash += hash << 13;
        hash ^= hash >> 7;
        hash += hash << 3;
        hash ^= hash >> 17;
        hash += hash << 5;
        return hash;
      }
    }
    public static string GetAssemblyVersion()
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
      string version = fileVersionInfo.ProductVersion;
      return version;
    }
    public static bool WhileTrueGuard(int whileloop_index, int whileloop_maxlen)
    {
      //simple while loop guard used in a for loop to prevent infinite execution
      //log when we take too long.
      //the while true loop should always break 
      if (whileloop_index > 1000)
      {
        int n = 0; n++;
      }
      if (whileloop_index < whileloop_maxlen)
      {
        return true;
      }
      Gu.Log.Error("while(true) loop out of bounds.");
      Gu.DebugBreak();
      return false;
    }
    public static Line3f? CastRayFromScreen(vec2 screen_pt)
    {
      Line3f? v = null;
      if (Gu.Context.GameWindow.ActiveViewCamera != null)
      {
        if (Gu.Context.GameWindow.ActiveViewCamera.Frustum != null)
        {
          v = Gu.Context.GameWindow.ActiveViewCamera.Frustum.RaycastWorld(screen_pt);
        }
      }
      return v;
    }
    public static void PostCustomDebugBreak()
    {
      _customDebugBreak = true;
    }
    public static void CustomDebugBreak()
    {
      if (_customDebugBreak)
      {
        Gu.DebugBreak();
      }
    }
    public static void Debug_IntegrityTestGPUMemory()
    {
      int obcount = 1000;
      //Integrity test of GPU memory management.
      // for (int i = 0; i < obcount; ++i)
      // {
      //   Gu.World.CreateAndAddObject("BoxMesh", MeshData.GenBox(1, 1, 1), Material.DefaultFlatColor);
      // }
      // for (int i = 1; i < obcount; ++i)
      // {
      //   Gu.World.DestroyObject("BoxMesh-" + i.ToString());
      // }
    }

    public static ushort QuantitizeUShortFloat(float in_value, float min_float_value, float max_float_value, ushort max_ushort_value)
    {
      //return a short representing the input
      Gu.Assert(in_value >= min_float_value);
      Gu.Assert(in_value <= max_float_value);

      double iv = (double)min_float_value;
      double av = (double)max_float_value;
      double v = (double)in_value;

      ushort s = 0;
      s = (ushort)((double)max_ushort_value * ((v - iv) / (av - iv)));
      return s;
    }
    public static float UnQuantitizeUShortFloat(ushort in_value, float min_float_value, float max_float_value, ushort max_ushort_value)
    {
      //return a short representing the input

      double iv = (double)min_float_value;
      double av = (double)max_float_value;
      double v = (double)in_value;

      double ev = iv + (v / (double)max_ushort_value) * (max_float_value - min_float_value);

      Gu.Assert((float)ev >= min_float_value);
      Gu.Assert((float)ev <= max_float_value);

      return (float)ev;
    }

    public static T? ParseEnum<T>(string s, bool ignorecase = false, bool bthrow = true)
    {
      T? ret = default(T);
      object? obj = null;
      if (Enum.TryParse(typeof(T), s.Trim(), ignorecase, out obj))
      {
        if (obj != null)
        {
          ret = (T)obj;
        }
        else
        {
          var msg = $"Could not cast enum '{typeof(T).ToString()}'";
          if (bthrow)
          {
            Gu.BRThrowException(msg);
          }
          else
          {
            Gu.Log.Error(msg);
          }
        }
      }
      return ret;
    }
    public static void AssertDistinctEnum<T>()
    {
      var enums = (T[])Enum.GetValues(typeof(T));
      Gu.Assert(enums.Count() == enums.Distinct().Count());
    }
    public static void ClearDirectory(string dir)
    {
      if (System.IO.Directory.Exists(dir))
      {
        Gu.Log.Info($"Clearing dir {dir}");
        var fs = System.IO.Directory.GetFiles(dir);
        foreach (var f in fs)
        {
          try
          {
            System.IO.File.Delete(f);
          }
          catch (Exception ex)
          {
            Gu.Log.Error("Clear: Could not delete '" + f + "'", ex);
          }
        }
      }
    }
    public static T? DeepClone<T>(T? obj) where T : ICopy<T>, IClone, new()
    {
      return Gu.Clone<T>(obj, false);
    }
    public static T? Clone<T>(T? obj, bool? shallow = null) where T : ICopy<T>, IClone, new()
    {
      T? t = new T();
      t.CopyFrom(obj, shallow);
      return t;
    }
    public static bool BackupFile(FileLoc fl, int maxbackups_size_MB = 200)
    {
      if (!fl.Exists)
      {
        return false;
      }
      long totalsize = 0;
      string[] files = System.IO.Directory.GetFiles(fl.QualifiedPath);
      SortedDictionary<DateTime, FileInfo> sorted = new SortedDictionary<DateTime, FileInfo>();
      foreach (var f in files)
      {
        FileInfo fi = new FileInfo(f);
        totalsize += fi.Length;
        sorted.Add(fi.CreationTime, fi);
      }
      if (totalsize > maxbackups_size_MB)
      {
        Gu.Log.Info("Backup files are over the maximum length for backups, consider deleting them.");
        Gu.DebugBreak();
        //Automatic delete of files  not a good thing, but I'll leave this here
        // Gu.MustTest();
        // List<FileInfo> toDelete = new List<FileInfo>();
        // foreach (var f in sorted)
        // {
        //   if (totalsize < maxbackups_size_MB)
        //   {
        //     break;
        //   }
        //   totalsize -= f.Value.Length;
        //   toDelete.Add(f.Value);
        // }
        // Gu.Log.Info($"Deleting {toDelete.Count} backup files");
        // foreach (var f in toDelete)
        // {
        //   try
        //   {
        //     System.IO.File.Delete(f.FullName);
        //   }
        //   catch (Exception ex)
        //   {
        //     Gu.Log.Error($"Failed to delete backup file {f.FullName}", ex);
        //   }
        // }
      }

      //This should be more of a global backup system.
      string dt = Gu.GetFilenameDateTimeNOW();
      bool b = fl.CopyFile(new FileLoc(Gu.BackupPath, dt + " " + fl.RawPath, FileStorage.Disk));
      return b;
    }
    public static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> memberAccess)
    {
      return ((MemberExpression)memberAccess.Body).Member.Name;
    }

    public static bool TryGetSelectedView(out RenderView? view)
    {
      //selected view - the view that the user is interacting with
      //active view - the view being rendered to/updated      
      //get renderview underneath mouse
      view = null;
      if (Gu.FocusedWindow != null)
      {
        view = Gu.FocusedWindow.SelectedView;
      }
      return view != null;
    }
    public static bool TryGetSelectedViewOverlay(out ViewportOverlay? over)
    {
      over = null;
      if (TryGetSelectedView(out var vv))
      {
        over = vv.Overlay;
      }
      return over != null;
    }
    public static bool TryGetSelectedViewCamera(out Camera3D? cam)
    {
      cam = null;
      if (TryGetSelectedView(out var vv))
      {
        if (vv.Camera.TryGetTarget(out var cm))
        {
          cam = cm;
        }
      }
      return cam != null;
    }
    public static bool SaneFloat(float f)
    {
      bool a = Single.IsInfinity(f);
      bool b = Single.IsNaN(f);

      return !a && !b;
    }
    #endregion



  }

}
