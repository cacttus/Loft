using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Desktop;
using System.Reflection;
using System.Diagnostics;

namespace PirateCraft
{
  public static class Gu
  {
    public static bool BreakRenderState = false;

    // Global Utils. static Class
    #region Public: Constants

    public const int c_intMaxWhileTrueLoopSmall = 1000;//dummy infinite loop blocker
    public const int c_intMaxWhileTrueLoop = 100000;//dummy infinite loop blocker
    public const int c_intMaxWhileTrueLoopLONG = 100000000;//dummy infinite loop blocker
    public const string UnsetName = "<unset>";
    public const string CopyName = "-copy";
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
    public static PCMouse Mouse { get { return Context.PCMouse; } }
    public static PCKeyboard Keyboard { get { return Context.PCKeyboard; } }
    public static string ExePath { get; private set; } = "";
    public static string LocalCachePath { get; private set; } = "";//megatex..
    public static string LocalTmpPath { get; private set; } = "";//logs..debug..shaderdebug..
    public static string WorkspacePath { get; private set; } = "";
    public static string WorkspaceDataPath { get; private set; } = "";// the ./Data directory. This is not present on embedded versions.
    public static readonly string EmbeddedDataPath = "PirateCraft.data.";
    public static string SavePath { get; private set; } = "";
    public static ResourceManager Resources { get; private set; } = null;
    public static AudioManager Audio { get; private set; } = null;
    public static Gui2dManager Gui2dManager { get; private set; } = null;
    public static FrameDataTimer GlobalTimer { get; private set; } = null;//Global frame timer, for all windows;
    public static Translator Translator { get; private set; } = null;

    #endregion
    #region Private: Static Members

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
      SavePath = System.IO.Path.Combine(ExePath, "./save");

      //This is for debug files and file changes. 
      WorkspacePath = System.IO.Path.Combine(ExePath, "../../../");
      WorkspaceDataPath = System.IO.Path.Combine(WorkspacePath, "./data");//This may change .. uh

      //Log
      Log = new Log(Gu.LocalTmpPath);
      Gu.Log.Info("Initializing Globals");
      Gu.Log.Info("CurrentDirectory =" + System.IO.Directory.GetCurrentDirectory());

      //Config
      EngineConfig = new EngineConfig(new FileLoc("config.json", FileStorage.Embedded));

      //Manager
      Translator = new Translator();
      Resources = new ResourceManager();
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
            List<UiWindowBase> wins = new List<UiWindowBase>();
            foreach (var ct in Contexts)
            {
              wins.Add(ct.Key);
            }

            GlobalTimer.Update();
            Resources.Update(GlobalTimer.Delta);

            foreach (var win in wins)
            {
              Gu.SetContext(win);
              if (!win.IsLoaded)
              {
                win.Load();
              }
              win.ProcessEvents();
              //win.SetActiveView();//make sure the view<->camera is updated to the active view of the window.
              win.SetActiveView();
              Gu.Context.Update();

              if (Gu.World.UpdateContext == Gu.Context)
              {
                Gu.World.Update(Gu.Context.Delta);
              }

              win.UpdateAsync();
              win.RenderAsync();
            }

            //Remove/Destroy windows / main window destroys app
            foreach (var win in toClose)
            {
              win.Context.MakeNoneCurrent();
              Contexts.Remove(win);
              win.Close();
              win.IsVisible = false;


              if (Contexts.Count == 0 || win.IsMain)
              {
                break;
              }
            }
            toClose.Clear();

            _customDebugBreak = false;
          }
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Fatal error: ", ex);
      }
    }
    public static void CloseWindow(UiWindowBase win)
    {
      if (win != null)
      {
        toClose.Add(win);
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
    public static void Assert(bool x, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
    {
      if (!x)
      {
        Gu.DebugBreak();//First catch before we can't change the FOE
        throw new Exception("Assertion failed: " + caller + ":" + lineNumber.ToString());
      }
    }
    public static void DebugBreak()
    {
      Debugger.Break();
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
          v = Gu.Context.GameWindow.ActiveViewCamera.Frustum.ScreenToWorld(screen_pt);
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
      for (int i = 0; i < obcount; ++i)
      {
        Gu.World.CreateAndAddObject("BoxMesh", MeshData.GenBox(1, 1, 1), Material.DefaultFlatColor);
      }
      for (int i = 1; i < obcount; ++i)
      {
        Gu.World.DestroyObject("BoxMesh-" + i.ToString());
      }
    }

    #endregion



  }

}
