using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using System;

namespace Loft
{
  public class WindowContext
  {
    //WindowContext
    //  Handle to window and data specific to GL context.

    public string Name { get; private set; } = Lib.UnsetName;
    public Int64 FrameStamp { get { return ContextFrameTimer.FrameStamp; } }
    public double UpTime { get { return ContextFrameTimer.UpTime; } }
    public double FpsFrame { get { return ContextFrameTimer.FpsAvg; } }
    public double FpsAvg { get { return ContextFrameTimer.FpsAvg; } }
    public double FrameDelta { get { return ContextFrameTimer.Delta; } }
    public Gpu Gpu { get; private set; } = null;
    public AppWindowBase GameWindow { get; set; } = null;
    public PCKeyboard PCKeyboard = new PCKeyboard();
    public PCMouse PCMouse = new PCMouse();
    public FrameDataTimer ContextFrameTimer = new FrameDataTimer();
    public Renderer Renderer { get; private set; } = null;
    public WindowContext? SharedContext { get; private set; } = null;

    public WindowContext(string name, AppWindowBase g, WindowContext? shared = null)
    {
      Name = name;
      GameWindow = g;
      SharedContext = shared;
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
      ContextFrameTimer.Update();
      PCKeyboard.Update();
      PCMouse.Update();
    }
  }
}
