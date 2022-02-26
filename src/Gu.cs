using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Desktop;

namespace PirateCraft
{
   public enum FileStorage
   {
      Disk,
      Embedded,
      Web,
      Generated,
   }
   
   /// <summary>
   /// FileLoc represents a virtual file location on disk, embed, or web
   /// </summary>
   public class FileLoc
   {
      //The name here has to be unique or it will cause conflicts.
      public static FileLoc Generated = new FileLoc("<generated>", FileStorage.Generated);
      
      public FileStorage FileStorage { get; private set; } = FileStorage.Disk;
      public string RawPath { get; private set; } ="";

      public Stream? GetStream()
      {
         string qualifiedPath = this.QualifiedPath;

         if (FileStorage == FileStorage.Embedded)
         {
            return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(qualifiedPath);
         }
         else if (FileStorage == FileStorage.Disk)
         {
            return File.OpenRead(qualifiedPath);
         }
         else
         {
            Gu.BRThrowNotImplementedException();
         }
         return null;
      }

      public string QualifiedPath
      {
         //Returns the full path with base storage location (disk/embed..)
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
            if (FileStorage == FileStorage.Embedded)
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

      public FileLoc(string path, FileStorage storage) { 
         RawPath= path; 
         FileStorage = storage;
      }
      public override bool Equals(object? obj)
      {
         return base.Equals(obj);
         FileLoc other = obj as FileLoc;
         if (other != null)
         {
            return other.RawPath.Equals(RawPath) && other.FileStorage.Equals(FileStorage);
         }
         else
         {
            Gu.BRThrowNotImplementedException();
         }
         return false;
      }
      public void AssertExists()
      {
         if (!Exists)
         {
            throw new Exception("File " + QualifiedPath + " does not exist.");
         }
      }
   }
   // Global Utils. static Class
   public static class Gu
   {
      private static Dictionary<GameWindow, WindowContext> Contexts = new Dictionary<GameWindow, WindowContext>();

      //This will be gotten via current context if we have > 1
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
            throw new Exception("Assertion failed: " + caller + ":" + lineNumber.ToString());
         }
      }
      public static void DebugBreak()
      {
         Debugger.Break();
      }

      #endregion




   }

}
