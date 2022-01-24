using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace PirateCraft
{
    // Global Utils. static lcass
    public static class Gu
    {
        private static bool _initialized = false;
        //This will be gotten via current context if we have > 1
        public static CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.Rhs;
        public static EngineConfig EngineConfig { get; set; } = new EngineConfig();
        public static Log Log { get; set; } = new Log("./logs/");

        //This will end up being a map to GameWindow->Context
        public static Context Context { get; private set; }

        public static void BRThrowNotImplementedException()
        {
            throw new NotImplementedException();
        }

        public static void Init(GameWindow g)
        {
            Gu.Log.Info("Initializing Globals");
            Context = new Context(g);
            _initialized = true;
        }
        public static Int64 Nanoseconds()
        {
            return DateTime.UtcNow.Ticks * 100;
        }
        public static Int64 Microseconds()
        {
            return Nanoseconds()/1000;
        }
        public static void CheckGpuErrorsRt()
        {
            ErrorCode c = GL.GetError();
            if (c != ErrorCode.NoError)
            {
                if (Gu.EngineConfig.LogErrors)
                {
                    Log.Error("OpenGL Error " + c.ToString());
                }
                if (Gu.EngineConfig.BreakOnOpenGLError)
                {
                    System.Diagnostics.Debugger.Break();
                }
            }
        }
        public static void CheckGpuErrorsDbg()
        {
#if DEBUG
            CheckGpuErrorsRt();
#endif
        }
        public static void Assert(bool x, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!x)
            {
                throw new Exception("Assertion failed: " + caller + ":" + lineNumber.ToString());
            }
        }
        public static void DebugBreak()
        {
            Debugger.Break();
        }
    }
}
