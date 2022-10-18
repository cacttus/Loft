using System;
using System.Collections.Generic;
using PirateCraft;

namespace TestOtherNamespace
{
  public class Startup : IObjectScript
  {
    public void OnCreate()
    {
      var tc = new TestClass();
      tc.DoSomething();
      // var gearob = Gu.Lib.LoadModel(RName.WorldObject_Gear);
      // Gu.World.AddObject(gearob);
      // gearob.Position_Local = new vec3(2,8,10);
    }
    public void OnUpdate()
    {
    }
    public void OnDestroy()
    {
    }
  }
}
