using OpenTK.Windowing.Desktop;
using System;

namespace PirateCraft
{
  //GL stuff that should be "static" in the context. Global textures..
  public class StaticContextData<T> where T : class
  {
    Dictionary<WindowContext, T> _data = new Dictionary<WindowContext, T>();
    public T Get()
    {
      var ct = Gu.Context;
      _data.TryGetValue(ct, out var x);
      return x;
    }
    public void Set(T x)
    {
      var ct = Gu.Context;
      _data.Add(ct, x);
    }
  }

  //Graphics Contxt + Window Frame Sync
  //Window specific data to the given context
  //Buffers & screen &c not shared among other contexts
  public class WindowContext
  {
    private long _lastTime = Gu.Nanoseconds();

    public string Name { get; private set; } = "ct-not-set";

    public Gpu Gpu { get; private set; } = null;
    public UiWindowBase GameWindow { get; set; } = null;
    public PCKeyboard PCKeyboard = new PCKeyboard();
    public PCMouse PCMouse = new PCMouse();
    public Int64 FrameStamp { get; private set; }
    public double UpTime { get; private set; } = 0; //Time since engine started.
    private DateTime _startTime = DateTime.Now;
    public double Fps { get; private set; } = 60;
    public double Delta { get; private set; } = 1 / 60;
    public Renderer Renderer { get; private set; } = null;
    public DebugDraw DebugDraw { get; private set; } = new DebugDraw();
    //public SynchronizationContext UpdateSyncContext{ get; private set; }

    public WindowContext(string name, UiWindowBase g)
    {
      Name = name;
      GameWindow = g;
    }
    public void Init()
    {
      Gpu = new Gpu();
      Renderer = new Renderer();
      Renderer.init(GameWindow.Width, GameWindow.Height, null);
    }
    public void Update()
    {
      //For first frame run at a smooth time.
      long curTime = Gu.Nanoseconds();
      if (FrameStamp > 0)
      {
        Delta = (double)((decimal)(curTime - _lastTime) / (decimal)(1000000000));
      }
      _lastTime = curTime;
      FrameStamp++;

      Fps = 1 / Delta;

      UpTime = (DateTime.Now - _startTime).TotalSeconds;

      PCKeyboard.Update();
      PCMouse.Update();
      DebugDraw.BeginFrame();
    }
  }
}
