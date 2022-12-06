using System;
using System.Collections.Generic;
using System.ComponentModel;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Loft
{
  public class MainClass
  {
    public static void Main(string[] args)
    {
      try
      {
        Gu.InitGlobals();
        var win = new MainWindow(
          new ivec2(Gu.EngineConfig.WindowInitX, Gu.EngineConfig.WindowInitY),
          new ivec2(Gu.EngineConfig.WindowInitW, Gu.EngineConfig.WindowInitH),
          new vec2(Gu.EngineConfig.WindowInitScaleW, Gu.EngineConfig.WindowInitScaleH)
          );
        Gu.Run();
      }
      catch (Exception ex)
      {
        string strex = Gu.GetAllException(ex);
        if (Gu.Log != null)
        {
          Gu.Log.Error("Fatal Error: " + strex);
        }
        else
        {
          Console.WriteLine(strex);
        }
        System.Environment.Exit(0);
      }
    }
  }
}