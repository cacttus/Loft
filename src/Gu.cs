using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Desktop;

namespace PirateCraft
{
  public static class Gu
  {
public static bool BreakRenderState=false;

    // Global Utils. static Class
    #region Public: Constants

    public const int c_intMaxWhileTrueLoop = 100000;//dummy infinite loop blocker

    #endregion
    #region Public: Static Members

    public static bool AllowOpenTKFaults = false;//OpenTK's GL isn't fully implemented in a lot of places

    public static string ExePath
    {
      get
      {
        if (String.IsNullOrEmpty(_strExePath))
        {
          var assemblyLoc = System.Reflection.Assembly.GetExecutingAssembly().Location;
          _strExePath = System.IO.Path.GetDirectoryName(assemblyLoc);
        }
        return _strExePath;
      }
    }
    public static Dictionary<UiWindowBase, WindowContext> Contexts { get; private set; } = new Dictionary<UiWindowBase, WindowContext>();
    public static CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.Rhs;
    public static float CoordinateSystemMultiplier { get { return (Gu.CoordinateSystem == CoordinateSystem.Lhs ? -1 : 1); } }
    public static EngineConfig EngineConfig { get; set; } = new EngineConfig();
    public static Log Log { get; set; } = null;
    public static WindowContext Context { get; set; } = null;
    public static readonly string EmbeddedDataPath = "PirateCraft.data.";
    public static World World = null;
    public static PCMouse Mouse { get { return Context.PCMouse; } }
    public static PCKeyboard Keyboard { get { return Context.PCKeyboard; } }
    public static ResourceManager Resources { get; private set; } = null;
    public static AudioManager Audio { get; private set; } = null;
    public static string LocalCachePath { get; private set; } = "";
    public static string SavePath { get; private set; } = "";

    #endregion
    #region Private: Static Members

    private static string _strExePath = "";
    private static List<UiWindowBase> toClose = new List<UiWindowBase>();

    #endregion
    #region Public: Static Methods

    public static void InitGlobals()
    {
      //Paths
      LocalCachePath = System.IO.Path.Combine(ExePath, "./cache");
      SavePath = System.IO.Path.Combine(ExePath, "./save");

      //Config
      EngineConfig.Load();

      //Log
      Log = new Log(Gu.LocalCachePath);
      Gu.Log.Info("Initializing Globals");
      Gu.Log.Info("CurrentDirectory =" + System.IO.Directory.GetCurrentDirectory());
      Resources = new ResourceManager();

      //Cache
      var dir = Path.GetDirectoryName(LocalCachePath);
      if (!Directory.Exists(dir))
      {
        Directory.CreateDirectory(dir);
      }
      if (EngineConfig.ClearCacheOnStart)
      {
        ResourceManager.ClearCache();
      }

      //Audio
      Audio = new AudioManager();
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
            //Not sure how to typically do this
            List<UiWindowBase> wins = new List<UiWindowBase>();
            foreach (var ct in Contexts)
            {
              wins.Add(ct.Key);
            }

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

          }
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Fatal error: " + ex.ToString());
      }
    }
    public static void CloseWindow(UiWindowBase win)
    {
      toClose.Add(win);

    }
    public static void CreateContext(UiWindowBase uw)
    {
      Gu.Log.Info("Registering Context: " + uw.Name);
      var ct = Gu.Context;
      var wd = new WindowContext(uw);
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

    #endregion



  }

}
