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
using Newtonsoft.Json;
using System.Runtime.Versioning;

namespace Loft
{
  public static class Gu
  {
    // Global utils static singleton
    #region Public: Constants

    public const int c_intMaxWhileTrueLoopBinarySearch64Bit = 64;//dummy infinite loop blocker
    public const int c_intMaxWhileTrueLoopSmall = 9999;//dummy infinite loop blocker
    public const int c_intMaxWhileTrueLoop = 999999;//dummy infinite loop blocker
    public const int c_intMaxWhileTrueLoopLONG = 999999999;//dummy infinite loop blocker

    private const string EmbeddedDataPathRoot = "Loft.data";
    private const string EmbeddedFolder_Root = "";
    private const string EmbeddedFolder_Font = "font";
    private const string EmbeddedFolder_Icon = "icon";
    private const string EmbeddedFolder_Image = "image";
    private const string EmbeddedFolder_Model = "model";
    private const string EmbeddedFolder_Script = "script";
    private const string EmbeddedFolder_Sfx = "sfx";
    private const string EmbeddedFolder_Shader = "shader";


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
    public static string SrcPath { get; private set; } = "";
    public static string WorkspacePath { get; private set; } = "";
    public static string WorkspaceDataPath { get; private set; } = "";// the ./Data directory. This is not present on embedded versions.
    public static string SavePath { get; private set; } = "";
    public static string BackupPath { get; private set; } = "";
    public static Lib Lib { get; private set; }
    public static AudioManager Audio { get; private set; }
    public static FrameDataTimer GlobalTimer { get; private set; }//Global frame timer, for all windows;
    public static Translator Translator { get; private set; }
    public static AppWindowBase? FocusedWindow { get; set; } = null;
    public static FrameStats FrameStats { get; private set; } = new FrameStats();
    public static Profiler Profiler { get; private set; } = new Profiler();
    public static IUiControls Controls { get; private set; }

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
    private static UIScript? _uiScript = null;

    #endregion
    #region Static Methods

    #region App/Window

    public static void InitGlobals()
    {
      //Root Paths
      var assemblyLoc = System.Reflection.Assembly.GetExecutingAssembly().Location;
      ExePath = System.IO.Path.GetDirectoryName(assemblyLoc);
      LocalTmpPath = System.IO.Path.Combine(ExePath, "./tmp");
      LocalCachePath = System.IO.Path.Combine(ExePath, "./cache");
      WorkspacePath = System.IO.Path.Combine(ExePath, "../../../");
      WorkspaceDataPath = System.IO.Path.Combine(WorkspacePath, "./data");//This may change .. uh
      SrcPath = System.IO.Path.Combine(WorkspacePath, "src");

      //Log
      Log = new Log(Gu.LocalTmpPath);
      Gu.Log.Info("Initializing Globals");
      Gu.Log.Info("CurrentDirectory =" + System.IO.Directory.GetCurrentDirectory());

      //Config
      EngineConfig = EngineConfig.LoadEngineConfig(new FileLoc("config.json", EmbeddedFolder.Root));
      Log.WriteConsole = EngineConfig.Debug_LogToConsole;
      Log.WriteFile = EngineConfig.Debug_LogToFile;
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
    }
    public static void InitScripts()
    {
      //Clean up scripts
      var files = System.IO.Directory.GetFiles(ExePath);
      foreach (var f in files)
      {
        if (System.IO.Path.GetFileName(f).StartsWith(Gu.EngineConfig.ScriptDLLBaseName))
        {
          if (System.IO.Path.GetExtension(f) == "dll" || System.IO.Path.GetExtension(f) == "pdb")
          {
            System.IO.File.Delete(f);
          }
        }
      }

      //make gui  script
      _uiScript = new UIScript(
        new List<FileLoc>(){
          Gu.EngineConfig.TestGuiScript,
          Gu.EngineConfig.EditGuiScript,
          Gu.EngineConfig.UIControls_Script,
          Gu.EngineConfig.Gui2d_Script,
        }
        , (sc) => { Controls = sc; }
      );
      var e = _uiScript.Compile();
      e.Wait();
      Controls = _uiScript.Script;
      Gu.Assert(Controls != null, "Ui Controls was null, possibly failed to compile.");
    }
    public static void CreateUIForView(RenderView rv)
    {
      _uiScript.LinkView(rv);
    }
    public static void Run()
    {
      try
      {
        while (true)
        {
          SystemInfo_Fast.Query(5000);

          Gu.Profiler.Reset();

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
              Gu.Prof("window");

              Gu.Context.Update();
              Gu.Prof("update");

              //TODO: we're going to move this to keymap, but also
              //keymap update should come here (which it isnt)
              if (win.IsVisible && win.WindowState != WindowState.Minimized)
              {
                win.OnUpdateInput();
                Gu.Prof("input");
              }

              //user mouse moved now pick the back buffer
              Gu.World.Pick();
              Gu.Prof("pick");

              if (Gu.World.UpdateContext == Gu.Context)
              {
                Gu.World.UpdateWorld(Gu.Context.FrameDelta);
                Gu.Prof("world");

                Gu.World.UpdateWorldEditor(Gu.Context.FrameDelta);
                Gu.Prof("edit");
              }

              win.CullAllViews();//adds objects for rendering
              Gu.Prof("cull");

              win.UpdateSelectedView();//must come after editor update
              Gu.Prof("view");
              //may end up being a problem.
              if (win.IsVisible && win.WindowState != WindowState.Minimized)
              {
                //Culling happens here.
                win.RenderAllViews();
                Gu.Prof("render");
              }

            }

            //** Post - Render
            Gu.World.WorldProps.EndAllRenders();

            //Update the picked object after all windows have used it.
            foreach (var ct in Contexts)
            {
              ct.Value?.Picker?.ResetPickedObject();
            }

            CheckExit();

            _customDebugBreak = false;

            Gu.Prof("window update");

          }
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Fatal error: ", ex);
      }
    }
    public static void Prof(string label = "", [CallerMemberName] string memberName = "")
    {
      //, [CallerMemberName]string memberName = ""
      Gu.Profiler.Section(label, memberName);//3);
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

    #endregion
    #region File/Path

    public static string RootPath(string path, PathRoot root)
    {
      //root a path relative to the project
      string r = path;

      if (root == PathRoot.None) { }
      else if (root == PathRoot.Exe) { r = System.IO.Path.Combine(ExePath, path); }
      else if (root == PathRoot.Project) { r = System.IO.Path.Combine(WorkspacePath, path); }
      else if (root == PathRoot.Src) { r = System.IO.Path.Combine(SrcPath, path); }
      else { Gu.BRThrowNotImplementedException(); }

      return r;
    }
    public static string GetWorkspacePath(EmbeddedFolder folder, string file)
    {
      return System.IO.Path.Combine(Gu.WorkspaceDataPath, GetEmbeddedFolder(folder), file);
    }
    public static string GetEmbeddedPath(EmbeddedFolder folder, string file)
    {
      string stfolder = GetEmbeddedFolder(folder);

      file = file.Replace("\\", ".");
      file = file.Replace("/", ".");

      if (!string.IsNullOrEmpty(stfolder))
      {
        return $"{Gu.EmbeddedDataPathRoot}.{stfolder}.{file}";
      }
      else
      {
        return $"{Gu.EmbeddedDataPathRoot}.{file}";
      }
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

    #endregion
    #region Time

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
    public static string GetFilenameDateTimeNOW()
    {
      //return a windows safe filename with datenow
      return DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss.fff");
    }

    #endregion
    #region Debug

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
    public static void MustTest()
    {
      //Must test this method.
      if (!SkipRequiredTests)
      {
        Gu.DebugBreak();
      }
    }
    public static void DebugGetLoadedAssemblies()
    {
      var sb = new System.Text.StringBuilder();

      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        bool isSystem = assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company == "Microsoft Corporation";

        if (!isSystem)
        {
          Gu.Trap();
        }

        sb.AppendLine(assembly.FullName);
        sb.AppendLine(assembly.GetName().Name);
        sb.AppendLine(assembly.GetName().Version.ToString());
        sb.AppendLine(assembly.Location);
        sb.AppendLine(isSystem.ToString());
        sb.AppendLine(assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration);
        sb.AppendLine(assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName);


        // var str = JsonSerializer.Serialize(ob);
        // options: jsonOptions

        //Console.WriteLine(metadataJson);
      }
      Gu.Log.Debug(sb.ToString());
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
    public static string GetAllException(Exception ex, string inner_exception_indent = "  ")
    {
      var sb = new System.Text.StringBuilder();
      Exception tmp = ex;
      for (int i = 0; tmp != null && Gu.WhileTrueGuard(i, Gu.c_intMaxWhileTrueLoopSmall); i++)
      {
        var estr = "";
        if (i > 0)
        {
          estr = $"InnerException:{Environment.NewLine}{tmp.ToString()}";
          estr.Replace(Environment.NewLine, Environment.NewLine + Enumerable.Repeat(inner_exception_indent, i));
        }
        else
        {
          estr = tmp.ToString();
        }
        sb.AppendLine(estr);
        tmp = tmp.InnerException;
      }
      return sb.ToString();
    }

    #endregion
    #region Number

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
    public static bool SaneFloat(float f)
    {
      bool a = Single.IsInfinity(f);
      bool b = Single.IsNaN(f);

      return !a && !b;
    }

    #endregion
    #region Loft

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
    public static bool TryGetSelectedViewGui(out IGui2d? g2)
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
    public static bool IsGuiFocused()
    {
      //currnelty we update object components before updating the picked element so use last picked
      var ob = Gu.Context.Picker.PickedObjectFrameLast;

      bool f = (ob != null) && (ob is IUiElement);

      return f;
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
          IUiWindow win = Gu.Controls.CreateWindow(title, pos, wh);
          win.Content.Text = msg;
          win.TopMost = true;
          v.Gui.AddChild(win);
        }
      }
    }

    #endregion
    #region Other

    //**TODO: rmeove Compress / Decompress replace with SerializeTools
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
    public static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> memberAccess)
    {
      return ((MemberExpression)memberAccess.Body).Member.Name;
    }


    #endregion

    #endregion
  }//gu
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
  }//cls
  public class ExternalProgram
  {
    public bool Success { get { return _success; } }
    public long ExecutionTime { get { return _executionTime; } }
    public List<string>? Output { get { return _output; } }
    public bool TimedOut { get { return _timedOut; } }
    public bool FailedToStart { get { return _failedToStart; } }

    private string _filename = "";
    private string _args = "";
    private bool _create_no_window = false;
    private bool _use_shellexec = false;
    private bool _redirect_output = true;
    private bool _success = false;
    private long _executionTime = 0;
    private List<string>? _output = null;
    private int _timeout_ms = -1;
    private bool _timedOut = false;
    private long _start_ms = 0;
    private bool _failedToStart = false;

    public ExternalProgram(string filename, string args, int timeout_ms = -1, bool create_no_window = false, bool use_shellexec = false, bool redirect_output = true)
    {
      _filename = filename;
      _timeout_ms = timeout_ms;
      _args = args;
      _create_no_window = create_no_window;
      _use_shellexec = use_shellexec;
      _redirect_output = redirect_output;
    }
    public bool Launch()
    {
      try
      {
        _success = false;
        _executionTime = 0;
        _output = new List<string>();

        var proc = new Process
        {
          StartInfo = new ProcessStartInfo
          {
            FileName = _filename,
            Arguments = _args,
            UseShellExecute = _use_shellexec,
            RedirectStandardOutput = _redirect_output,
            CreateNoWindow = _create_no_window,
          }
        };
        _success = proc.Start();
        proc.PriorityBoostEnabled = true;
        proc.EnableRaisingEvents = true;

        _start_ms = Gu.Milliseconds();
        if (_timeout_ms <= 0)
        {
          proc.WaitForExit();
        }
        else if (!proc.WaitForExit(_timeout_ms))
        {
          _timedOut = true;
        }
        _executionTime = Gu.Milliseconds() - _start_ms;

        if (_success)
        {
          if (_redirect_output)
          {
            while (!proc.StandardOutput.EndOfStream)
            {
              var line = proc.StandardOutput.ReadLine();
              _output.Add(line);
            }
          }
        }
        else
        {
          _failedToStart = true;
          Gu.DebugBreak();
        }

        //This is supposed to be non-blocking, e.g. faster? Not sure
        // return proc.WaitForExitAsyncTimeout(new TimeSpan(0, 0, 0, 0, _timeout_ms)).ContinueWith((ts, ob) =>
        //  {
        //    Finish(proc, ts.Result, fn_args);
        //    return ts.Result;
        //  }, null);

      }
      catch (Exception ex)
      {
        var fn_args = $"filename='{_filename}',args='{_args}'";
        Gu.Log.Error($"Exception: {fn_args}:", ex);
        Gu.DebugBreak();
      }

      return _success;
    }

  }//cls
  public enum AsyncResult
  {
    Unset,
    Success,
    Fail
  }
  public class EngineTask
  {
    //like Task but allows us to run on render thread / GL context only
    //"lift off" from the rneder thread, run on threadpool, "land" back to render thread.
    // Start().Then().Then().SyncThen().Then().SyncThen().Then().SyncThen()
    //    async thread 1 -> then << runs independently
    //    async thread 2 -> then << runs after thread 2 completes
    //    sync (GPU) -> then << will execute when we process gpu events, after all other stuff
    //    async thread 3 -> then
    //    sync (GPU)  << will execute when we process gpu events, after all other stuff
    //thread "groups" would work like some kind of promise.

    public static EngineTask Fail { get { return new EngineTask() { _success = false }; } }
    public bool Success { get { return _success; } set { _success = value; } }
    public long RunTimeMS { get { return _runTimeUS / 1000; } }

    private bool _success = false;
    private Task _lastTask = null; //last task in entire chain until any continuation
    private Task _continueTask = null; //continuation task for if we must break the task chain
    private Task _firstTask = null;//first task in the entire chain and is shared among spawned enginetasks
    private WindowContext? _context = null;
    private AsyncResult _result = AsyncResult.Unset;
    private bool _is_sync_continue = false; //We are a duplicate EngineTask because we must wait for the synchronous thread to pump events.
    private bool _started = false;
    private EngineTask _previousEngineTask = null;
    private long _startTimeUS = 0;
    private long _runTimeUS = 0;
    private long _taskCount = 0;

    public static EngineTask Start(Func<EngineTask, bool> act)
    {
      //start an async task not necessarily on the render thread
      //task starts immediately, calling Then() or SyncThen() will append further tasks while the task is running
      var et = new EngineTask(act, Gu.Context);
      et.Start();
      return et;
    }
    public static void Test()
    {

      var tmpact = () =>
      {
        Thread.Sleep(1000);
        Console.Write("[Thread=" + (System.Threading.Thread.CurrentThread.ManagedThreadId) + "] ");
      };
      EngineTask ett =
      //launch 6 tasks that complete async
      EngineTask.Start((t) => { tmpact(); Console.WriteLine("Doing complex stuff 1/6."); return true; })
      .Then((t) => { tmpact(); Console.WriteLine("Doing complex stuff 2/6.."); return true; })
      .Then((t) => { tmpact(); Console.WriteLine("Doing complex stuff 3/6.."); return true; })
      .Then((t) => { tmpact(); Console.WriteLine("Doing complex stuff 4/6.."); return true; })
      .Then((t) => { tmpact(); Console.WriteLine("Doing complex stuff 5/6.."); return true; })
      .Then((t) => { tmpact(); Console.WriteLine("Doing complex stuff 6/6.."); return true; })
      //pause here until we process synchronously
      .SyncThen((t) => { tmpact(); Console.WriteLine("On GPU, doing stuff.."); return true; })
      //continue again asynchronously
      .Then((t) => { tmpact(); Console.WriteLine("MORE complex stuff 1/2.."); return true; })
      .Then((t) => { tmpact(); Console.WriteLine("MORE complex stuff 2/2.."); return true; })
      .SyncThen((t) => { tmpact(); Console.WriteLine("Back on GPU.."); return true; })
      .Then((t) => { tmpact(); Console.WriteLine("EVEN MORE complex stuff 1/3.."); return true; })
      .Then((t) => { tmpact(); Console.WriteLine("EVEN MORE complex stuff 2/3.."); return true; })
      .Then((t) => { tmpact(); Console.WriteLine("EVEN MORE complex stuff 3/3.."); return true; })
      .SyncThen((t) => { tmpact(); Console.WriteLine("Back on GPU again, done.."); return true; })
      ;

      //Pause a bit to test that the first n tasks completed asynchronously (until SyncThen).
      //  Thread.Sleep(1000);

      //Process all remaining tasks.
      // ett.Wait();

      int n = 0;
      n++;
    }

    private EngineTask() { }
    public EngineTask(Func<EngineTask, bool>? act, WindowContext? context)
    {
      _context = context;
      if (act != null)
      {
        _firstTask = _lastTask = new Task(async () =>
        {
          _success = act(this);
        });
        _taskCount++;
      }
    }
    public EngineTask Start()
    {
      Gu.Assert(_firstTask != null);
      if (!_started)
      {
        _firstTask.Start();
        _started = true;
        _startTimeUS = Gu.Microseconds();
      }
      return this;
    }
    public EngineTask Then(Func<EngineTask, bool> act)
    {
      if (_is_sync_continue && _continueTask == null)
      {
        _continueTask = _lastTask = new Task(async () =>
        {
          _success = act(this);
        });
      }
      else
      {
        _lastTask = _lastTask.ContinueWith(
          (t, o) =>
          {
            this._success = this._success && act(this);
            CalcRunTime();
          },
          null);

        if (_firstTask == null)
        {
          _firstTask = _lastTask;
        }
      }
      _taskCount += 1;
      return this;
    }
    public EngineTask SyncThen(Func<EngineTask, bool> act)
    {
      //execute on the given context thread (GPU)
      //Execution of the async continuation chain will stop until the render thread processes this request.
      Gu.Assert(_context != null);

      //Return a new EngineTask that will continue after the sync code is processed.
      EngineTask afterSync = new EngineTask(null, this._context);
      afterSync._previousEngineTask = this;
      afterSync._is_sync_continue = true;
      afterSync._started = this._started;
      afterSync._firstTask = this._firstTask;
      afterSync._startTimeUS = this._startTimeUS;
      afterSync._firstTask = this._firstTask;
      afterSync._taskCount = this._taskCount + 1;
      afterSync._lastTask = _lastTask = _lastTask.ContinueWith((tt, o) =>
        {
          _context.Gpu.Post_To_RenderThread(() =>
          {
            afterSync._success = this._success && act(this);
            if (afterSync._continueTask != null)
            {
              afterSync._continueTask.Start();
            }
            else
            {
              afterSync.CalcRunTime();
            }
          });
        },
        null);
      if (_firstTask == null)
      {
        afterSync._firstTask = _firstTask = _lastTask;
      }

      return afterSync;
    }
    public EngineTask Wait(int timeout_ms = -1)
    {
      //Without Wait() we can fire off a series of events that go back to the GPU and synchronize.
      if (!_started)
      {
        Start();
      }
      if (!_is_sync_continue)
      {
        _success = _success && _lastTask.Wait(timeout_ms);
      }
      else
      {
        //If we have synchronization code, pump all synchronous actions pending on the GPU.
        //We can't randomly change context here (we could, but it would be sub-optimal and no point doing it)
        Gu.Assert(_context != null);
        Gu.Assert(Gu.Context == _context);

        var st = Gu.Milliseconds();
        while (true)
        {
          _context.Gpu.ExecuteCallbacks_RenderThread();
          if (timeout_ms != -1)
          {
            if (Gu.Milliseconds() - st >= timeout_ms)
            {
              break;
            }
          }
          if (_lastTask.Status == TaskStatus.RanToCompletion)
          {
            break;
          }

          if (_lastTask.Status == TaskStatus.Canceled || _lastTask.Status == TaskStatus.Faulted)
          {
            _success = false;
            break;
          }

          //System.Threading.Thread.Sleep(1);
        }
      }
      return this;
    }
    private void CalcRunTime()
    {
      _runTimeUS = Gu.Microseconds() - this._startTimeUS;
    }

  }


}//ns
