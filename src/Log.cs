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
    public void Warn(string s, Exception ex)
    {
      string e_all = Gu.GetAllException(ex);
      LogString("[" + LogLineStr() + "][" + TimeStr() + "][W]: " + s + " " + e_all + Newline, ConsoleColor.Yellow);
    }
    public void Error(string msg)
    {
      Error(msg, "", "");
    }
    public void Error(Exception ex)
    {
      string e_all = Gu.GetAllException(ex);
      Error("", e_all, "");
    }    
    public void Error(string pred, Exception ex)
    {
      string e_all = Gu.GetAllException(ex);
      Error(pred, e_all, "");
    }
    public void Error(string msg, string ex, string afterStackTrace = "")
    {
      string stackTrace = GetBeautifulStackTrace(true, true, true);
      LogString("[" + LogLineStr() + "][" + TimeStr() + "][E]: " +
      msg + Newline +
      stackTrace + Newline +
      (StringUtil.IsNotEmpty(ex) ? (ex + Newline) : "") +
      afterStackTrace + Newline,
      ConsoleColor.Red);
    }
    private string GetBeautifulStackTrace(bool removeParams, bool removeLineText, bool gridify)
    {
      var stackTrace = Environment.StackTrace;

      //remove params from st
      if (removeParams)
      {
        stackTrace = System.Text.RegularExpressions.Regex.Replace(stackTrace, @"\(.*\) in ", " "); //stackTrace.Replace(":line ",":");
      }
      if (removeLineText)
      {
        //So it makes it easier to click on the file:line in vscode if it's file:line
        stackTrace = stackTrace.Replace(":line ", ":");
      }
      if (gridify)
      {
        //Make it in a grid. This might be annoying to some people but i like it
        var ss = stackTrace.Split("\n").ToList(); ;

        //Remove top two
        ss.RemoveRange(0, 2);

        int midspace = 2;
        char spacingChar = ' ';
        int gridformat = 0;//0 = Align left, 1 = Block align

        int maxsp = 0;
        for (int i = 0; i < ss.Count; i++)
        {
          ss[i] = System.Text.RegularExpressions.Regex.Replace(ss[i], @"^\s+at\s+", ""); //stackTrace.Replace(":line ",":");

          int val = 1;
          if (gridformat == 0)
          {
            val = ss[i].IndexOf(" ");
          }
          else if (gridformat == 1)
          {
            val = ss[i].Length;
          }

          maxsp = Math.Max(maxsp, val);
        }
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < ss.Count; i++)
        {
          var line = ss[i];
          var firstsp = line.IndexOf(" ");
          if (firstsp >= 0)
          {
            var spaces = 0;
            if (gridformat == 0)
            {
              spaces = maxsp - firstsp + midspace;
            }
            else if (gridformat == 1)
            {
              spaces = maxsp - line.Length + midspace;
            }
            var s1 = line.Substring(0, firstsp);
            var sp = new string(spacingChar, spaces);
            var s2 = line.Substring(firstsp + 1, line.Length - firstsp - 1);
            sb.Append(s1 + sp + s2 + Environment.NewLine);
          }
          else
          {
            sb.Append(line);
          }
        }
        stackTrace = sb.ToString();
      }

      return stackTrace;
    }
    public void ErrorCycle(string s, int frames = 120)
    {
      int md = (int)Gu.Context.FrameStamp % frames;
      if (md == 0)
      {
        Error(s);
      }
    }
    public void WarnCycle(string s, int frames = 60)
    {
      int md = (int)Gu.Context.FrameStamp % frames;
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
            using (FileStream file = new FileStream(_fileLoc, System.IO.FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter writer = new StreamWriter(file, Encoding.ASCII))
            {
              writer.Write(s);
            }
          }
          catch (Exception ex)
          {
            Console.WriteLine(Gu.GetAllException(ex));
            Debugger.Break();
          }
        }
      }
      _logLine++;
    }
  }
}
