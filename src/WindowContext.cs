using OpenTK;
using System;

namespace PirateCraft
{
   //Graphics Contxt + Window Frame Sync
   public class WindowContext
   {
      private long _lastTime = Gu.Nanoseconds();

      public Gpu Gpu { get; private set; } = null;
      public GameWindow GameWindow { get; set; } = null;
      public double Delta { get; private set; } = 0;
      public PCKeyboard PCKeyboard = new PCKeyboard();
      public UInt64 FrameStamp { get; private set; }
      public double UpTime { get; private set; } = 0; //Time since engine started.
      private DateTime _startTime = DateTime.Now;

      public WindowContext(GameWindow g)
      {
         GameWindow = g;
         Gpu = new Gpu();
      }
      public void Update()
      {
         //For first frame run at a smooth time.
         Delta = 1 / 60;
         long curTime = Gu.Nanoseconds();
         if (FrameStamp > 0)
         {
            Delta = (double)((decimal)(curTime - _lastTime) / (decimal)(1000000000));
         }
         _lastTime = curTime;
         FrameStamp++;

         UpTime = (DateTime.Now - _startTime).TotalSeconds; 

         PCKeyboard.Update();
      }
   }
}
