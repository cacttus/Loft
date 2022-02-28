using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenTK.Windowing.Desktop;

namespace PirateCraft
{
  // Global Utils. static Class
  public static class Gu
  {
    private static Dictionary<GameWindow, WindowContext> Contexts = new Dictionary<GameWindow, WindowContext>();

    //This will be gotten via current context if we have > 1
    private static string _strExePath = "";
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
    public static CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.Rhs;
    public static float CoordinateSystemMultiplier { get { return (Gu.CoordinateSystem == CoordinateSystem.Lhs ? -1 : 1); } }
    public static EngineConfig EngineConfig { get; set; } = new EngineConfig();
    public static Log Log { get; set; } = null;
    public static WindowContext Context { get; private set; }
    public static readonly string EmbeddedDataPath = "PirateCraft.data.";
    public static World World = new World();
    public static PCMouse Mouse { get { return Context.PCMouse; } }
    public static PCKeyboard Keyboard { get { return Context.PCKeyboard; } }
    public static ResourceManager ResourceManager { get; private set; } = new ResourceManager();

    public static string LocalCachePath = "";
    public static string SavePath = "";

    public static void Init(GameWindow g)
    {
      LocalCachePath = System.IO.Path.Combine(ExePath, "./data/cache");
      SavePath = System.IO.Path.Combine(ExePath, "./save");

      //Create cache
      var dir = Path.GetDirectoryName(LocalCachePath);
      if (!Directory.Exists(dir))
      {
        Directory.CreateDirectory(dir);
      }

      Log = new Log(Gu.LocalCachePath);
      Gu.Log.Info("Initializing Globals");

      Gu.Log.Info("Base Dir=" + System.IO.Directory.GetCurrentDirectory());

      Gu.Log.Info("Register Context");
      RegisterContext(g);
      SetContext(g);
    }
    private static void RegisterContext(GameWindow g)
    {
      Contexts.Add(g, new WindowContext(g));
    }
    public static void SetContext(GameWindow g)
    {
      WindowContext c = null;
      if (Contexts.TryGetValue(g, out c))
      {
        Context = c;
      }
      else
      {
        Gu.BRThrowException("Context for game window " + g.Title + " not found.");
      }
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
    #region Debugging

    public static void BRThrowException(string msg)
    {
      throw new Exception("Error: " + msg);
    }
    public static void BRThrowNotImplementedException()
    {
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

    #endregion

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


  }

}
