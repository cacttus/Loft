using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace PirateCraft
{
   public enum FileStorage
   {
      Disk,
      Embedded,
      Web,
      Fake,//File does not exist (on purpose)
   }
   
   /// <summary>
   /// FileLoc represents a virtual file location on disk, embed, or web
   /// </summary>
   public class FileLoc
   {
      public FileStorage FileStorage { get; private set; } = FileStorage.Disk;
      public string RawPath { get; private set; } ="";
      
      public FileLoc(string path, FileStorage storage) { 
         RawPath= path; 
         FileStorage = storage;
      }
      public void AssertExists()
      {
         if (!Exists)
         {
            throw new Exception("File " + QualifiedPath + " does not exist.");
         }
      }
      public string QualifiedPath 
      {
         get
         {
            string path = RawPath;
            if (FileStorage == FileStorage.Embedded)
            {
               path = Gu.EmbeddedDataPath + path;
            }
            else if (FileStorage == FileStorage.Disk)
            {
               //noop
            }
            else
            {
               Gu.BRThrowNotImplementedException();
            }
            return path;
         }
      }
      public bool Exists
      {
         get
         {
            if (FileStorage== FileStorage.Embedded)
            {
               bool exist = Assembly.GetExecutingAssembly().GetManifestResourceNames().Contains(QualifiedPath);
               return exist;
            }
            else if (FileStorage == FileStorage.Disk)
            {
               return File.Exists(QualifiedPath);
            }
            else
            {
               Gu.BRThrowNotImplementedException();
            }
            return false;
         }
      }
   }
   // Global Utils. static Class
   public static class Gu
   {
      private static Dictionary<GameWindow, WindowContext> Contexts = new Dictionary<GameWindow, WindowContext>();

      //This will be gotten via current context if we have > 1
      public static CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.Rhs;
      public static EngineConfig EngineConfig { get; set; } = new EngineConfig();
      public static Log Log { get; set; } = null;
      public static WindowContext CurrentWindowContext { get; private set; }
      public static readonly string EmbeddedDataPath = "PirateCraft.data.";
      public static World World = new World();

      public static string LocalCachePath = "./data/cache";
      public static string SavePath = "./save";

      public static void Init(GameWindow g)
      {
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
            CurrentWindowContext = c;
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
      public static double RotationPerSecond(double seconds)
      {
         var f = (CurrentWindowContext.UpTime % seconds) / seconds;
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
            throw new Exception("Assertion failed: " + caller + ":" + lineNumber.ToString());
         }
      }
      public static void DebugBreak()
      {
         Debugger.Break();
      }

      #endregion

      #region FileOps

      public static string ReadTextFile(FileLoc loc)
      {
         //Returns empty string when failSilently is true.
         string data = "";
         loc.AssertExists();

         if (loc.FileStorage == FileStorage.Embedded)
         {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(loc.QualifiedPath))
            {
               using (StreamReader reader = new StreamReader(stream))
               {
                  data = reader.ReadToEnd();
               }
            }
         }
         else if (loc.FileStorage == FileStorage.Disk)
         {
            if (!System.IO.File.Exists(loc.RawPath))
            {
               Gu.BRThrowException("File '" + loc.RawPath + "' does not exist.");
            }

            using (Stream stream = File.Open(loc.RawPath, FileMode.Open, FileAccess.Read, FileShare.None))
            using (StreamReader reader = new StreamReader(stream))
            {
               data = reader.ReadToEnd();
            }
         }
         else
         {
            Gu.BRThrowNotImplementedException();
         }

         return data;
      }
      public static void SaveImage(string path, Img32 image)
      {
         Bitmap b = image.ToBitmap();
         var dir = Path.GetDirectoryName(path);
         if (!Directory.Exists(dir))
         {
            Directory.CreateDirectory(dir);
         }
         b.Save(path);
      }
      public static Img32 LoadImage(FileLoc loc)
      {
         //Load an image in the form of Img32
         Bitmap b = LoadBitmap(loc);
         Img32 ret = new Img32(b);
         return ret;
      }
      public static Bitmap LoadBitmap(FileLoc loc)
      {
         Bitmap b = null;

         loc.AssertExists();

         if (loc.FileStorage == FileStorage.Embedded)
         {
            using (var fs = Assembly.GetExecutingAssembly().GetManifestResourceStream(loc.QualifiedPath))
            {
               if (fs != null)
               {
                  b = new Bitmap(fs);
               }
            }
         }
         else if (loc.FileStorage == FileStorage.Disk)
         {
            b = new Bitmap(loc.QualifiedPath);
         }
         else
         {
            Gu.BRThrowNotImplementedException();
         }
         if (b == null)
         {
            Gu.BRThrowException("Failed to load image file " + loc.QualifiedPath);
         }
         return b;
      }
      public static unsafe byte[] Serialize<T>(T[] data) where T : struct
      {
         //This is .. terrible.
         var size = Marshal.SizeOf(data[0]);
         var bytes = new byte[size * data.Length];
         for (int di = 0; di < data.Length; di++)
         {
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data[di], ptr, true);
            Marshal.Copy(ptr, bytes, di * size, size);
            Marshal.FreeHGlobal(ptr);
         }

         return bytes;
      }
      public static T[] Deserialize<T>(byte[] data) where T : struct
      {
         var tsize = Marshal.SizeOf(default(T));

         //Must be a multiple of the struct.
         Gu.Assert(data.Length % tsize == 0);

         var count = data.Length / tsize;
         T[] ret = new T[count];

         for (int di = 0; di < data.Length; di+=tsize)
         {
            var ptr_struct = Marshal.AllocHGlobal(tsize);
            Marshal.StructureToPtr(data[di], ptr_struct, true);
            ret[di / tsize] = (T)Marshal.PtrToStructure(ptr_struct, typeof(T));
            Marshal.FreeHGlobal(ptr_struct);
         }

         return ret;
      }


   }

   #endregion
}
