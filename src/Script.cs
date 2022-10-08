using System.Diagnostics;

namespace PirateCraft
{
  /*
    pseudocode script for the csharp script dll 
    should use the same "change" system that we are using for shaders
  */
  public class Script
  {
    public FileLoc loc;
    public FileLoc dll_loc;
    public Script(FileLoc path)
    {
      loc = path;
    }
    public void Compile()
    {
      Process proc = new Process
      {
        StartInfo = new ProcessStartInfo
        {
          FileName = "msbuild",
          Arguments = "filename...",
          UseShellExecute = false,
          RedirectStandardOutput = true,
          CreateNoWindow = true
        }
      };
      proc.Start();
      while (!proc.StandardOutput.EndOfStream)
      {
        //read errors etc
        string line = proc.StandardOutput.ReadLine();
      }
    }


  }
}