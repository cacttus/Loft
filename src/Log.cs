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

        //Loc is the path to the logs, not the logs.
        public Log(string loc)
        {
            _fileLoc = loc;
            if (String.IsNullOrEmpty(System.IO.Path.GetFileName(_fileLoc)))
            {
                _fileLoc = System.IO.Path.Combine(_fileLoc, "Log" +
                DateTime.Now.Year + "." +
                DateTime.Now.Day + "." +
                    DateTime.Now.Hour + ":" +
                    DateTime.Now.Minute + ":" +
                    DateTime.Now.Second + ":" +
                    DateTime.Now.Millisecond);
            }

            Console.BackgroundColor = ConsoleColor.Black;
        }
        public void Debug(string s)
        {
            LogString("[" + LogLineStr() + "][" + TimeStr() + "][D]: " + s, ConsoleColor.Cyan);
        }
        public void Info(string s)
        {
            LogString("[" + LogLineStr() + "][" + TimeStr() + "][I]: " + s, ConsoleColor.White);
        }
        public void Warn(string s)
        {
            LogString("[" + LogLineStr() + "][" + TimeStr() + "][W]: " + s, ConsoleColor.Yellow);
        }
        public void Error(string s)
        {
            LogString("[" + LogLineStr() + "][" + TimeStr() + "][E]: " + s, ConsoleColor.Red);
        }
        private string TimeStr()
        {
            string st = DateTime.Now.Hour.ToString("{0,2}") + ":" +
                DateTime.Now.Minute.ToString("{0,2}") + ":" +
                DateTime.Now.Second.ToString("{0,2}") + ":" +
                DateTime.Now.Millisecond.ToString("{0,-6}");
            return st;
        }
        private string LogLineStr()
        {
            return String.Format("{0,9}", _logLine.ToString());
        }
        private void LogString(string s, ConsoleColor color)
        {
            if (WriteConsole)
            {
                Console.ForegroundColor = color;
                Console.Out.WriteLine(s);
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
