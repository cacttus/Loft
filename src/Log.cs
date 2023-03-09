using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Loft
{
  public class Log
  {
    private int _logLine = 0;
    private object _lock = new object();
    private string _fileLoc = "";
    public const string Red = "\033[1;31m";
    public const string Green = "\033[1;32m";
    public const string Yellow = "\033[1;33m";
    public const string Magenta = "\033[1;35m";
    public const string Cyan = "\033[1;36m";
    public const string White = "\033[1;37m";

    public bool WriteConsole { get; set; } = true;
    public bool WriteFile { get; set; } = true;

    //Loc is the path to the logs, not the logs.
    public Log(string loc)
    {
      _fileLoc = System.IO.Path.Combine(loc, "Log" + LogFileNameStr() + ".log");
      //Console.BackgroundColor = ConsoleColor.Black;
    }
    private string GetHeader(string iwd, bool dont)
    {
      if (dont == false)
      {
        return $"[{LogLineStr()}][{TimeStr()}][{iwd}]:";
      }
      return "";
    }
    public void Debug(string s, bool headless = false)
    {
      //headless = omit header information 
      LogString($"{GetHeader("D", headless)}{s}{Environment.NewLine}", ConsoleColor.Cyan);
    }
    public void Info(string s, bool headless = false, ConsoleColor? color = null)
    {
      LogString($"{GetHeader("I", headless)}{s}{Environment.NewLine}", color == null ? ConsoleColor.White : color.Value);
    }
    public void Warn(string s, bool headless = false)
    {
      LogString($"{GetHeader("W", headless)}{s}{Environment.NewLine}", ConsoleColor.Yellow);
    }
    public void Warn(string s, Exception ex, bool headless = false)
    {
      var msg = s + " " + Gu.GetAllException(ex);
      Warn(msg, headless);
    }
    public void Error(string msg, System.Exception? ex, string afterStackTrace, bool headless)
    {
      //headless = print just colored message without stack trace or info
      var except = "";
      if (!headless)
      {
        if (ex != null)
        {
          except += Environment.NewLine + GetSimpleStackTrace(ex.StackTrace, true, true, true);
          except += Environment.NewLine + Gu.GetAllException(ex);
        }
      }

      var s = $"{msg}{except}{afterStackTrace}";

      LogString($"{GetHeader("E", headless)}{s}{Environment.NewLine}", ConsoleColor.Red);
    }
    public void Error(string msg, bool headless = false)
    {
      Error(msg, null, "", headless);
    }
    public void Error(Exception ex, bool headless = false)
    {
      Error("", ex, "", headless);
    }
    public void Error(string info, Exception ex, bool headless = false)
    {
      Error(info, ex, "", headless);
    }
    private string GetSimpleStackTrace(string? stackTrace, bool removeParams, bool removeLineText, bool gridify)
    {
      //remove params from st
      if (removeParams)
      {
        stackTrace = System.Text.RegularExpressions.Regex.Replace(stackTrace, @"\(.*\)", ""); //stackTrace.Replace(":line ",":");
      }
      if (removeLineText)
      {
        //So it makes it easier to click on the file:line in vscode if it's file:line
        stackTrace = stackTrace.Replace(":line ", ":");
      }

      return stackTrace;
    }
    private bool CheckCycle(int frames)
    {
      return Gu.Context.FrameStamp < 60 || ((int)Gu.Context.FrameStamp % frames) == 0;
    }
    public void ErrorCycle(string s, int frames = 120)
    {
      if (CheckCycle(frames))
      {
        Error(s);
      }
    }
    public void WarnCycle(string s, int frames = 60)
    {
      if (CheckCycle(frames)) //<10 lets us see errors in the first 10 frames
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
              using (var fs = System.IO.File.Create(_fileLoc)) ;
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
  public class ClassLog
  {
    //log file is too big
    //  disables errors across different systems based on engine config params
    //  has internal log that can print all errors at once instead of each line
    private StringBuilder _errs;
    private bool _errors = false;
    private bool _warnings = false;
    private bool _log_buffer_enabled = false;
    private string _name;
    private bool _log_console_enabled = false;

    public ClassLog(string name, bool log_console_enabled, bool buffer_enabled = false)
    {
      _name = name;
      _log_console_enabled = log_console_enabled;
      _log_buffer_enabled = buffer_enabled;
      if (_log_buffer_enabled)
      {
        _errs = new StringBuilder();
      }
    }

    public void Append(string str = "") { _errs.Append(str); }
    public void AppendLine(string str = "") { _errs.AppendLine(str); }
    public void Error(string err)
    {
      string e = $"  Error: '{_name}': {err}";
      if (_log_buffer_enabled)
      {
        _errs.AppendLine(e);
      }
      // ** always show errors for now
      //   if (_log_console_enabled)
      //   {
      Gu.Log.Error(e);
      Gu.DebugBreak();
      // }
      _errors = true;
    }
    public void Debug(string err)
    {
      if (_log_console_enabled)
      {
        Gu.Log.Debug($"  Info: '{_name}': {err}");
      }
    }
    public void Info(string err)
    {
      if (_log_console_enabled)
      {
        Gu.Log.Debug($"  Info: '{_name}': {err}");
      }
    }
    public void Warn(string err)
    {
      string e = $"  Warning: '{_name}': {err}";
      if (_log_buffer_enabled)
      {
        _errs.AppendLine(e);
      }
      if (_log_console_enabled)
      {
        Gu.Log.Error(e);
      }
    }
    public bool Assert(bool condition, [CallerArgumentExpression("condition")] string? err = null)
    {
      if (!condition)
      {
        Error(err);
        return false;
      }
      return true;
    }
    public void Print()
    {
      if (_errors)
      {
        Gu.Log.Error(_errs.ToString());
      }
      else if (_warnings)
      {
        Gu.Log.Warn(_errs.ToString());
      }
      else
      {
        Gu.Log.Info(_errs.ToString());
      }
    }

  }//cls






}//ns

























