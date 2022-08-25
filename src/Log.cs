using System;
using System.IO;
using System.Text;
using System.Diagnostics;
namespace PirateCraft
{
  public class Log
  {
    private int _logLine = 0;
    private object _lock = new object();
    private string _fileLoc = "";
    private const string Red = "\033[1;31m";
    private const string Green = "\033[1;32m";
    private const string Yellow = "\033[1;33m";
    private const string Magenta = "\033[1;35m";
    private const string Cyan = "\033[1;36m";
    private const string White = "\033[1;37m";

    public bool WriteConsole { get; set; } = true;
    public bool WriteFile { get; set; } = true;
    
    private string Newline = "\n";

    //Loc is the path to the logs, not the logs.
    public Log(string loc)
    {
      _fileLoc = System.IO.Path.Combine(loc, "Log" + LogFileNameStr() + ".log");
      //Console.BackgroundColor = ConsoleColor.Black;
    }
    public void Debug(string s)
    {
      LogString("[" + LogLineStr() + "][" + TimeStr() + "][D]: " + s + Newline, ConsoleColor.Cyan);
    }
    public void Info(string s)
    {
      LogString("[" + LogLineStr() + "][" + TimeStr() + "][I]: " + s + Newline, ConsoleColor.White);
    }
    public void Warn(string s)
    {
      LogString("[" + LogLineStr() + "][" + TimeStr() + "][W]: " + s + Newline, ConsoleColor.Yellow);
    }
    public void Error(string s)
    {
      string stackTrace = Environment.StackTrace;

      LogString("[" + LogLineStr() + "][" + TimeStr() + "][E]: " + s + Newline + stackTrace + Newline, ConsoleColor.Red);
    }
    public void ErrorCycle(string s)
    {
      int md = (int)Gu.Context.FrameStamp % 60;
      if (md == 0)
      {
        Error(s);
      }
    }
    public void WarnCycle(string s, int frames = 60)
    {
      int md = (int)Gu.Context.FrameStamp % 60;
      if (Gu.Context.FrameStamp < 10 || md == 0) //<10 lets us see errors in the first 10 frames
      {
        Warn(s);
      }
    }
    private string TimeStr()
    {
      string st = DateTime.Now.ToString("yyyy.MM.dd.HH:mm:ss:fff");
      return st;
    }
    private string LogFileNameStr()
    {
      string st = DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss.fff");
      return st;
    }
    private string LogLineStr()
    {
      string r = String.Format("{0,9}", _logLine.ToString());
      return r;
    }
    private void LogString(string s, ConsoleColor color)
    {
      if (WriteConsole)
      {
        Console.ForegroundColor = color;
        Console.Out.Write(s); //WriteLine I think has been using /r/n We specifically want /n
        Console.ForegroundColor = ConsoleColor.White;
      }

      if (WriteFile)
      {
        lock (_lock)
        {
          try
          {
            if (!System.IO.File.Exists(_fileLoc))
            {
              var dir = System.IO.Path.GetDirectoryName(_fileLoc);
              System.IO.Directory.CreateDirectory(dir);
              using (var fs = System.IO.File.Create(_fileLoc))
              {
              }
            }
            using (FileStream file = new FileStream(_fileLoc, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter writer = new StreamWriter(file, Encoding.ASCII))
            {
              writer.Write(s);
            }
          }
          catch (Exception ex)
          {
            Console.WriteLine(ex.ToString());
            Debugger.Break();
          }
        }
      }
      _logLine++;
    }
  }
}
