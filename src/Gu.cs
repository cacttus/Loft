using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Desktop;
using System.Reflection;
using System.Diagnostics;
using System.Linq.Expressions;
using OpenTK.Windowing.Common;

namespace Loft
{
  public class SystemInfo
  {
    public static long MemUsedBytes
    {
      get
      {
        return System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64;
      }
    }
    public static long VMemUsedBytes
    {
      get
      {
        return System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64;
      }
    }
    public static float BToMB(long b)
    {
      var b_to_mb = 1024 * 1024;
      var bc = b / b_to_mb;
      var bm = b % b_to_mb;
      return (float)(bc + (float)bm / (float)b_to_mb);
    }
  }
  public class FrameStats
  {
    public int NumDrawElements_Frame = 0;
    public int NumDrawArrays_Frame = 0;
    public int NumElements_Frame = 0;
    public int NumArrayElements_Frame = 0;
    public int NumTriangles_Frame = 0;
    public int NumDrawElements_Frame_Last = 0;
    public int NumDrawArrays_Frame_Last = 0;
    public int NumElements_Frame_Last = 0;
    public int NumArrayElements_Frame_Last = 0;
    public int NumTriangles_Frame_Last = 0;
    public void Reset()
    {
      NumDrawElements_Frame_Last = NumDrawElements_Frame;
      NumDrawArrays_Frame_Last = NumDrawArrays_Frame;
      NumElements_Frame_Last = NumElements_Frame;
      NumArrayElements_Frame_Last = NumArrayElements_Frame;
      NumTriangles_Frame_Last = NumTriangles_Frame;

      NumDrawElements_Frame = 0;
      NumDrawArrays_Frame = 0;
      NumElements_Frame = 0;
      NumArrayElements_Frame = 0;
      NumTriangles_Frame = 0;

    }
    public string ToString()
    {
      System.Text.StringBuilder sb = new System.Text.StringBuilder();

      sb.Append($" Triangles:{(NumElements_Frame_Last + NumArrayElements_Frame_Last) / 3}");
      sb.Append($" DrawArrays:{NumDrawArrays_Frame_Last}");
      sb.Append($" DrawElements:{NumDrawElements_Frame_Last}");

      return sb.ToString();
    }
  }
  public static class Gu
  {
    // Global Utils. static Class
    // Controls systems, game update loop(s), and windows
    #region Public: Constants

    public const int c_intMaxWhileTrueLoopBinarySearch64Bit = 64;//dummy infinite loop blocker
    public const int c_intMaxWhileTrueLoopSmall = 1000;//dummy infinite loop blocker
    public const int c_intMaxWhileTrueLoop = 100000;//dummy infinite loop blocker
    public const int c_intMaxWhileTrueLoopLONG = 100000000;//dummy infinite loop blocker

    private const string EmbeddedDataPathRoot = "Loft.data";
    private const string EmbeddedFolder_Root = "";
    private const string EmbeddedFolder_Font = "font";
    private const string EmbeddedFolder_Icon = "icon";
    private const string EmbeddedFolder_Image = "image";
    private const string EmbeddedFolder_Model = "model";
    private const string EmbeddedFolder_Script = "script";
    private const string EmbeddedFolder_Sfx = "sfx";
    private const string EmbeddedFolder_Shader = "shader";
    public static string GetEmbeddedPath(EmbeddedFolder folder, string file)
    {
      string stfolder = GetEmbeddedFolder(folder);
      if (!string.IsNullOrEmpty(stfolder))
      {
        return $"{Gu.EmbeddedDataPathRoot}.{stfolder}.{file}";
      }
      else
      {
        return $"{Gu.EmbeddedDataPathRoot}.{file}";
      }
    }
    public static string GetWorkspacePath(EmbeddedFolder folder, string file)
    {
      return System.IO.Path.Combine(Gu.WorkspaceDataPath, GetEmbeddedFolder(folder), file);
    }
    public static string GetEmbeddedFolder(EmbeddedFolder folder)
    {
      if (folder == EmbeddedFolder.Root) { return $""; }
      else if (folder == EmbeddedFolder.Font) { return $"{Gu.EmbeddedFolder_Font}"; }
      else if (folder == EmbeddedFolder.Icon) { return $"{Gu.EmbeddedFolder_Icon}"; }
      else if (folder == EmbeddedFolder.Image) { return $"{Gu.EmbeddedFolder_Image}"; }
      else if (folder == EmbeddedFolder.Model) { return $"{Gu.EmbeddedFolder_Model}"; }
      else if (folder == EmbeddedFolder.Script) { return $"{Gu.EmbeddedFolder_Script}"; }
      else if (folder == EmbeddedFolder.Sfx) { return $"{Gu.EmbeddedFolder_Sfx}"; }
      else if (folder == EmbeddedFolder.Shader) { return $"{Gu.EmbeddedFolder_Shader}"; }
      Gu.BRThrowNotImplementedException();
      return "";
    }

    #endregion
    #region Public: Static Members

    public static Dictionary<AppWindowBase, WindowContext> Contexts { get; private set; } = new Dictionary<AppWindowBase, WindowContext>();
    public static CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.Rhs;
    public static float CoordinateSystemMultiplier { get { return (Gu.CoordinateSystem == CoordinateSystem.Lhs ? -1 : 1); } }
    public static EngineConfig EngineConfig { get; set; } = null;
    public static Log Log { get; set; } = null;
    public static WindowContext Context { get; set; } = null;
    public static World World = null;
    public static WorldLoader WorldLoader = null;
    public static string ExePath { get; private set; } = "";
    public static string LocalCachePath { get; private set; } = "";//megatex..
    public static string LocalTmpPath { get; private set; } = "";//logs..debug..shaderdebug..
    public static string WorkspacePath { get; private set; } = "";
    public static string WorkspaceDataPath { get; private set; } = "";// the ./Data directory. This is not present on embedded versions.
    public static string SavePath { get; private set; } = "";
    public static string BackupPath { get; private set; } = "";
    public static Lib Lib { get; private set; }
    public static AudioManager Audio { get; private set; }
    public static Gui2dManager Gui2dManager { get; private set; }
    public static FrameDataTimer GlobalTimer { get; private set; }//Global frame timer, for all windows;
    public static Translator Translator { get; private set; }
    public static AppWindowBase? FocusedWindow { get; set; } = null;
    public static FrameStats FrameStats { get; private set; } = new FrameStats();

    //Debug
    public static bool BreakRenderState = false;
    public static string SaveRenderStateFile = "";
    private static bool SkipRequiredTests = false;
    public static bool AllowOpenTKFaults = false;

    #endregion
    #region Private: Static Members

    private static bool _exitPosted = false;
    private static List<AppWindowBase> toClose = new List<AppWindowBase>();
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

      //Config
      EngineConfig = EngineConfig.LoadEngineConfig(new FileLoc("config.json", EmbeddedFolder.Root));

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
      Lib = new Lib();
      GlobalTimer = new FrameDataTimer();
      Audio = new AudioManager();
      Gui2dManager = new Gui2dManager();
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
            List<AppWindowBase> wins = new List<AppWindowBase>();
            foreach (var ct in Contexts)
            {
              wins.Add(ct.Key);
            }

            GlobalTimer.Update();
            FrameStats.Reset();

            //Update all windows Sync
            //TODO: we should update the world main context first
            foreach (var win in wins)
            {
              Gu.SetContext(win);
              if (!win.IsLoaded)
              {
                win.Load();
              }
              win.ProcessEvents();

              Gu.Context.Update();

              //TODO: we're going to move this to keymap, but also
              //keymap update should come here (which it isnt)
              if (win.IsVisible && win.WindowState != WindowState.Minimized)
              {
                win.OnUpdateInput();
              }

              //user mouse moved now pick the back buffer
              Gu.World.Pick();

              if (Gu.World.UpdateContext == Gu.Context)
              {
                Gu.World.UpdateWorld(Gu.Context.FrameDelta);

                Gu.World.UpdateWorldEditor(Gu.Context.FrameDelta);
              }

              win.CullAllViews();//adds objects for rendering

              win.UpdateSelectedView();//must come after editor update

              //may end up being a problem.
              if (win.IsVisible && win.WindowState != WindowState.Minimized)
              {
                //Culling happens here.
                win.RenderAllViews();
              }

            }

            //** Post - Render
            Gu.World.WorldProps.EndAllRenders();

            //Update the picked object after all windows have used it.
            foreach (var ct in Contexts)
            {
              ct.Value?.Renderer?.Picker?.ResetPickedObject();
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
        DestroyWindowSafe(win);
      }
      toClose.Clear();

      //Check if exit was posted.
      if (_exitPosted)
      {
        //Basically Exit() failed, maybe there was a window created since Exit() was called..
        Environment.Exit(0);
      }
    }
    private static void DestroyWindowSafe(AppWindowBase? win)
    {
      try
      {
        win.Context.MakeNoneCurrent();
        Contexts.Remove(win);
        win.Close();
        win.IsVisible = false;
      }
      catch (Exception ex)
      {
        Gu.Log.Error(ex);
      }
      if (Contexts.Count == 0 || win.IsMain)
      {
        Environment.Exit(0);
      }
    }
    public static void Exit(bool abort_immediately)
    {
      //only call immediately if we are aborting /on error
      _exitPosted = true;
      foreach (var ct in Contexts)
      {
        if (abort_immediately)
        {
          DestroyWindowSafe(ct.Key);
        }
        else
        {
          CloseWindow(ct.Key);
        }
      }
    }
    public static void CloseWindow(AppWindowBase win)
    {
      if (win != null)
      {
        if (!toClose.Contains(win))
        {
          toClose.Add(win);
        }
      }
    }
    public static void CreateContext(string name, AppWindowBase uw, IGLFWGraphicsContext? glshared = null)
    {
      //try get shared context
      WindowContext? sharedCT = null;
      if (glshared != null)
      {
        foreach (var w in Contexts)
        {
          if (w.Key.Context == glshared)
          {
            sharedCT = w.Value;
            break;
          }
        }
        Gu.Assert(sharedCT != null);
      }

      var last_ct = Gu.Context;
      var wd = new WindowContext(name, uw, sharedCT);
      Contexts.Add(uw, wd);
      Gu.SetContext(uw);
      wd.Init();
      if (last_ct != null)
      {
        //Set back in case we are not main window
        Gu.SetContext(last_ct.GameWindow);
      }
    }
    public static void SetContext(WindowContext wc)
    {
      Context = wc;
      wc.GameWindow.Context.MakeCurrent();
    }
    public static void SetContext(AppWindowBase g)
    {
      if (Contexts.TryGetValue(g, out var ct))
      {
        Context = ct;
        g.Context.MakeCurrent();
      }
    }
    public static WindowContext GetContextForWindow(AppWindowBase g)
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
    public static bool AssertDebug(bool x, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
    {
      Assert(x, string.Empty, lineNumber, caller, true);

      return x;
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
    public static void Trap(int n = -1)
    {
      //Ctrl+Shift+B -> Break Traps
      if (Gu.Context != null &&
      Gu.Context.PCKeyboard != null &&
      Gu.Context.PCKeyboard.IsInitialized &&
      Gu.Context.PCKeyboard.PressOrDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftControl) &&
      Gu.Context.PCKeyboard.PressOrDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftShift) &&
      Gu.Context.PCKeyboard.PressOrDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.B) &&
      (n == -1 || Gu.Context.PCKeyboard.PressOrDown(PCKeyboard.IntToDigitKey(n)))
      )
      {
        Gu.DebugBreak();
      }
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
    public static string GetAssemblyVersion()
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
      string version = fileVersionInfo.ProductVersion;
      return version;
    }
    public static bool WhileTrueGuard(int whileloop_index, int whileloop_maxlen)
    {
      //guard infinite execution
      //log when we take too long.
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
      //selected view - the view that is under the mouse cursor
      //active view - the view being rendered to/updated      
      //get renderview underneath mouse
      view = null;
      if (Gu.FocusedWindow != null)
      {
        view = Gu.FocusedWindow.SelectedView;
      }
      return view != null;
    }
    public static bool TryGetSelectedViewOrDefault(out RenderView? view)
    {
      view = null;
      if (TryGetSelectedView(out var ss))
      {
        view = ss;
        return true;
      }
      else if (Gu.TryGetMainwWindow(out var mw))
      {
        //get the first view in the default window.
        if (mw.RenderViews.Count > 0)
        {
          view = mw.RenderViews[0];
        }
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
        cam = vv.Camera;
      }
      return cam != null;
    }
    public static bool TryGetSelectedViewGui(out Gui2d? g2)
    {
      g2 = null;
      if (TryGetSelectedView(out var vv))
      {
        g2 = vv.Gui;
      }
      return g2 != null;
    }
    public static Line3f? TryCastRayFromScreen(vec2 screen_pt)
    {
      Line3f? v = null;
      if (Gu.TryGetSelectedViewCamera(out var cm))
      {
        if (cm.Frustum != null)
        {
          v = cm.Frustum.RaycastWorld(screen_pt);
        }
      }
      return v;
    }
    public static bool TryGetMainwWindow(out MainWindow? w)
    {
      w = null;
      foreach (var p in Contexts)
      {
        if (p.Key.IsMain)
        {
          Gu.Assert(p.Key is MainWindow);
          w = (MainWindow)p.Key;
          break;
        }
      }
      return w != null;
    }
    public static bool TryGetWindowByName(string name, out AppWindowBase? win)
    {
      //Note: Window name is not the window Title
      win = null;
      foreach (var p in Contexts)
      {
        if (StringUtil.Equals(p.Key.Name, name))
        {
          win = p.Key;
          break;
        }
      }
      return win != null;
    }
    public static bool TryGetFocusedWindow(out AppWindowBase? win)
    {
      win = null;
      foreach (var p in Contexts)
      {
        if (p.Key.IsFocused)
        {
          win = p.Key;
          break;
        }
      }
      return win != null;
    }


    public static bool SaneFloat(float f)
    {
      bool a = Single.IsInfinity(f);
      bool b = Single.IsNaN(f);

      return !a && !b;
    }
    public static void MustTest()
    {
      //Must test this method.
      if (!SkipRequiredTests)
      {
        Gu.DebugBreak();
      }
    }
    public static bool LaunchProgram(string filename, string args, bool create_no_window = false, bool use_shellexec = false, bool redirect_output = true)
    {
      return LaunchProgram(filename, args, out var output, create_no_window, use_shellexec, redirect_output);
    }
    public static bool LaunchProgram(string filename, string args, out List<string>? output, bool create_no_window = false, bool use_shellexec = false, bool redirect_output = true)
    {
      string fn_args = $"filename='{filename}',args='{args}'";
      output = null;
      bool success = false;

      try
      {
        Process proc = new Process
        {
          StartInfo = new ProcessStartInfo
          {
            FileName = filename,
            Arguments = args,
            UseShellExecute = use_shellexec,
            RedirectStandardOutput = redirect_output,
            CreateNoWindow = create_no_window
          }
        };
        success = proc.Start();

        output = new List<string>();
        if (redirect_output)
        {
          if (success)
          {
            while (!proc.StandardOutput.EndOfStream)
            {
              var line = proc.StandardOutput.ReadLine();
              output.Add(line);
            }
          }
          else
          {
            Gu.Log.Error($"Process.Start failed: {fn_args}:");
          }
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error($"Exception: {fn_args}:", ex);
      }

      return success;
    }
    public static void FindRelativeFile(string root, string to_find, ref bool found)
    {
      if (found == false)
      {
        if (System.IO.Directory.Exists(root))
        {
          var files = System.IO.Directory.GetFiles(root);
          foreach (var file in files)
          {
            string fn = System.IO.Path.GetFileName(file);
            if (fn.Equals(to_find, StringComparison.OrdinalIgnoreCase))
            {
              found = true;
              break;
            }
          }

          var dirs = System.IO.Directory.GetDirectories(root);
          foreach (var dir in dirs)
          {
            FindRelativeFile(dir, to_find, ref found);
          }
        }
        else
        {
          found = false;
        }
      }

    }
    public static void SafeDeleteLocalFile(string fileloc)
    {
      //Make sure that the deleting file is in a subdirectory relative to this appliation
      if (System.IO.File.Exists(fileloc))
      {
        string fn = System.IO.Path.GetFileName(fileloc);

        bool found = false;
        FindRelativeFile(Gu.ExePath, fn, ref found);
        if (!found)
        {
          Gu.BRThrowException($"'{fileloc}': Could not delete file - specified file was not relative to application path '{Gu.ExePath}'.");
        }
        else
        {
          System.IO.File.Delete(fileloc);
        }
      }
      else
      {
        Gu.BRThrowException($"'{fileloc}': Could not delete file - not found.");
      }
    }
    public static void MessageBox(string title, string msg)
    {
      if (Gu.TryGetSelectedViewOrDefault(out var v))
      {
        if (v.Gui != null)
        {
          vec2 wh = new vec2(650, 500);

          vec2 pos = new vec2(
            v.Viewport.Width / 2 - wh.width / 2,
            v.Viewport.Height / 2 - wh.height / 2
            );
          UiWindow win = new UiWindow(title, pos, wh, msg);
          win.TopMost = true;
          v.Gui.AddChild(win);
        }
      }



    }
    #endregion



  }//gu

}
