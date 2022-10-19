
using System;
using System.Collections.Generic;
using PirateCraft;

namespace PirateCraft
{
  public class MyWorldScript : IWorldScript
  {
    public void OnLoad(World w)
    {
      Gu.Log.Info("Init script");
    }
    public void OnUpdate(World w, double delta)
    {
      if (Gu.Context.FrameStamp % 100==0)
      {
        Gu.Log.Info("Doing script");
      }
    }
    public void OnExit(World w)
    {
    }
  }
}
