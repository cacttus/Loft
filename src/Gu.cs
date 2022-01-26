using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private static Dictionary<GameWindow, Context> Contexts = new Dictionary<GameWindow, Context>();

        //This will be gotten via current context if we have > 1
        public static CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.Rhs;
        public static EngineConfig EngineConfig { get; set; } = new EngineConfig();
        public static Log Log { get; set; } = new Log("./logs/");
        public static Context Context { get; private set; }
        public static readonly string EmbeddedDataPath = "PirateCraft.data.";

        private static void RegisterContext(GameWindow g)
        {
            Contexts.Add(g, new Context(g));
        }
        public static void SetContext(GameWindow g)
        {
            Context c = null;
            if (Contexts.TryGetValue(g, out c))
            {
                Context = c;
            }
            else
            {
                Gu.BRThrowException("Context for game window " + g.Title + " not found.");
            }
        }
        public static void BRThrowException(string msg)
        {
            throw new Exception("Error: " + msg);
        }

        public static void BRThrowNotImplementedException()
        {
            throw new NotImplementedException();
        }

        public static void Init(GameWindow g)
        {
            Gu.Log.Info("Initializing Globals");
            RegisterContext(g);
            SetContext(g);
            _initialized = true;
        }
        public static string ReadTextFile(string location, bool embedded)
        {
            string data = "";

            if (embedded)
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(location))
                using (StreamReader reader = new StreamReader(stream))
                {
                    data = reader.ReadToEnd();
                }
            }
            else
            {
                Assert(System.IO.File.Exists(location));
                using (Stream stream = File.Open(location, FileMode.Open, FileAccess.Read, FileShare.None))
                using (StreamReader reader = new StreamReader(stream))
                {
                    data = reader.ReadToEnd();
                }
            }

            return data;
        }
        public static Bitmap LoadBitmap(string path, bool embedded)
        {
            Bitmap b = null;
            if (embedded)
            {
                using (var fs = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
                {
                    if (fs != null)
                    {
                        b = new Bitmap(fs);
                    }
                }
            }
            else
            {
                if (!System.IO.File.Exists(path))
                {
                    throw new Exception("File " + path + " does not exist.");
                }
                b = new Bitmap(path);
            }
            if (b == null)
            {
                Gu.BRThrowException("Failed to load image file " + path);
            }
            return b;
        }
        //public static string ReadTextFileSafe(string location)
        //{
        //    string data = "";
        //    byte[] bytes = null;
        //    int readed = 0;
        //    using (var fs = File.Open(location, FileMode.Open, FileAccess.Read, FileShare.None))
        //    {
        //        if (fs != null)
        //        {
        //            bytes = new byte[fs.Length];
        //            readed = fs.Read(bytes, 0, (int)fs.Length);
        //        }
        //    }
        //    if (bytes != null && readed > 0)
        //    {
        //        data = Encoding.Default.GetString(bytes);
        //    }
        //    else
        //    {
        //        Gu.BRThrowException("Failed to load file " + location);
        //    }
        //    return data;
        //}
        public static Int64 Nanoseconds()
        {
            return DateTime.UtcNow.Ticks * 100;
        }
        public static Int64 Microseconds()
        {
            return Nanoseconds() / 1000;
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
        public static T GetRef<T>(WeakReference<T> v) where T : class
        {
            if (v == null)
            {
                return null;
            }
            T wr = default(T);
            v.TryGetTarget(out wr);
            return wr;
        }

    }
}
