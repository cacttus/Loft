using System.Diagnostics;
using System.Text;
namespace PirateCraft
{
  //Script interfaces to implement in the .cs script file
  public interface IFunctionScript
  {
    public object? DoThing(object? param);
  }
  public interface IObjectScript
  {
    public void OnCreate();
    public void OnUpdate(double delta, WorldObject? ob);
    public void OnDestroy();
  }
  public interface IWorldScript
  {
    public void OnLoad(World w);
    public void OnUpdate(World w, double delta);
    public void OnExit(World w);
  }
  public class WorldScript : CSharpScript, IWorldScript
  {
    //Thunk class for runtime interface

    private IWorldScript? _script = null;

    public WorldScript(FileLoc loc) : base(loc, typeof(IWorldScript))
    {
    }
    public void OnLoad(World w)
    {
      CheckUpdate();
      if (_script != null)
      {
        _script.OnLoad(w);
      }
    }
    public void OnUpdate(World w, double delta)
    {
      CheckUpdate();
      if (_script != null)
      {
        _script.OnUpdate(w, delta);
      }
    }
    public void OnExit(World w)
    {
      CheckUpdate();
      if (_script != null)
      {
        _script.OnExit(w);
      }
    }

    private void CheckUpdate()
    {
      if (_scriptObject != null && _script != _scriptObject)
      {
        Gu.Assert(_scriptObject is IWorldScript, $"Loaded script '{_scriptObject.GetType().ToString()}' is not a '{typeof(IWorldScript).ToString()}'!");
        _script = _scriptObject as IWorldScript; //script changed
      }
    }
  }

  public enum ScriptStatus
  {
    None,
    Compiling,
    Loading,
    Processing,
    Error,
    CompileSuccess
  }
  public class CSharpScript
  {
    //@class Script
    //@brief C sharp scripts built into assemblies, dynamic loaded (LSR).
    //@note Ubuntu - requires mono-devel https://www.mono-project.com/download/stable/#download-lin
    //      We have 2 dll's one for embedded scripts and one for external scripts. 
    //      This pretty much works. Would requires some tinkering with dll references as we'll need a lot more of them.

    #region Public:Members

    public static int TotalLoadedScriptAssemblyBytes { get; private set; } = 0;

    #endregion
    #region Private/Protected Members

    public ScriptStatus ScriptStatus { get { return _scriptStatus; } protected set { _scriptStatus = value; } }
    private ScriptStatus _scriptStatus = ScriptStatus.None;

    private string _outputPath = "";
    protected Type? _scriptObjectType = null;
    protected object? _scriptObject = null;
    private System.Reflection.Assembly? _loadedAssembly = null;
    private static Dictionary<FileLoc, CSharpScript> _loadedScripts = new Dictionary<FileLoc, CSharpScript>(new FileLoc.EqualityComparer());
    private List<FileLoc> _files;
    private List<string> _scriptMessages = new List<string>();
    private int _compileCount = 0;
    private DynamicFileLoader? _loader = null;
    protected Type? _scriptObjectInterfaceType = null;
    
    #endregion
    #region Public:Methods

    public static object? Call(string scriptname, object? param)
    {
      //Shortcut to call a "function script"
      return Call(new FileLoc(Gu.WorkspaceDataPath, scriptname, FileStorage.Disk), param);
    }
    public static object? Call(FileLoc scriptname, object? param)
    {
      if (!_loadedScripts.TryGetValue(scriptname, out var ss))
      {
        ss = new CSharpScript(scriptname, typeof(IFunctionScript));
        _loadedScripts.Add(scriptname, ss);
      }
      return ss.Call(null);
    }

    public CSharpScript(FileLoc loc, Type interface_type)
    {
      _files = _files.ConstructIfNeeded();
      _scriptObjectInterfaceType = interface_type;
      _files.Add(loc);
      _loader = new DynamicFileLoader(_files, (f) =>
      {
        Compile();
      });
    }
    public bool Compile()
    {
      _outputPath = System.IO.Path.Combine(Gu.ExePath, Gu.EngineConfig.ScriptDLLName);

      ScriptInfo($"Loading..");

      try
      {
        string files = "";
        foreach (var f in _files)
        {
          if (f.FileStorage == FileStorage.Embedded)
          {
            ScriptWarn($"Embedded scripts can't be sent to the compiler (for now), using workspace path ({f.WorkspacePath})");
          }
          files += $" {f.WorkspacePath}";
          if (!f.ExistsOnDisk())
          {
            ScriptError($"File {f.WorkspacePath} does not exist.");
            _scriptStatus = ScriptStatus.Error;
          }
        }

        if (_scriptStatus != ScriptStatus.Error)
        {
          DoCompile(files);
        }
      }
      catch (Exception ex)
      {
        ScriptError(Gu.GetAllException(ex));
        _scriptStatus = ScriptStatus.Error;
      }

      PrintErrors();

      return _scriptStatus == ScriptStatus.CompileSuccess;
    }
    private void DoCompile(string files)
    {
      _compileCount++;
      ScriptInfo($"Compiling. (compiles={_compileCount})");

      //TODO: figure out how to do this dynamically
      List<String> refs = new List<string>(){
          "System.dll",
          "System.Runtime.dll",
          "System.Collections.dll",
          System.IO.Path.Combine(Gu.ExePath, "PirateCraft.dll"),
          System.IO.Path.Combine(Gu.ExePath, "OpenTK.Core.dll"),
        };
      //TODO: rel paths / PATH .. 
      List<String> libs = new List<string>(){
        "/usr/lib/mono/4.8-api/Facades/",//Mono facade
         "/usr/lib/mono/4.8-api/",//Mono facade
        // "/usr/share/dotnet/shared/Microsoft.NETCore.App/6.0.9/",//MS .NET
        };

      //if(_scriptStatus != ScriptStatus.Error){
      _scriptStatus = ScriptStatus.Compiling;

      //-debug:{full|pdbonly|portable|embedded}
      //-pdb:<file>   

      //csc worked. mcs did not work.. idk why
      var compiler = "csc";
      var debug = "-debug:full";
      var args = $"-out:{_outputPath} -t:library -r:{String.Join(" -r:", refs.ToArray())} -lib:{String.Join(" -lib:", libs.ToArray())} {files} {debug}";
      ScriptInfo($"Executing:{compiler} {args}");

      if (Gu.LaunchProgram(compiler, args, out var output))
      {
        PrintOutput(output);
        LoadAssembly();
      }
      else
      {
        _scriptStatus = ScriptStatus.Error;
      }
    }
    public object? Call(object? param)
    {
      object? ret = null;
      if (_scriptObject != null)
      {
        var obo = (_scriptObject as IFunctionScript);
        if (obo != null)
        {
          try
          {
            ret = obo.DoThing(param);
          }
          catch (Exception ex)
          {
            Gu.Log.Error(ex);
          }
        }
      }
      return ret;
    }

    #endregion
    #region Private/Protected: Methods

    private void LoadAssembly()
    {
      //load interface,  create instance
      if (_scriptStatus != ScriptStatus.Error)
      {
        if (System.IO.File.Exists(_outputPath))
        {
          //Load ASM in bytes to just duplicate the assembly. We can't unload assemblies, but, it's not an issue right now.
          var asmbytes = System.IO.File.ReadAllBytes(_outputPath);

          TotalLoadedScriptAssemblyBytes += asmbytes.Length;
          ScriptInfo($"Total Runtime Size: {StringUtil.FormatPrec((double)TotalLoadedScriptAssemblyBytes / (double)1000000, 2)}MB");

          var asm = System.Reflection.Assembly.Load(asmbytes);
          if (LoadTypeAndCreateInstance(asm))
          {
            ScriptInfo($"Assembly Loaded, (Total LSR={TotalLoadedScriptAssemblyBytes}).");
            _scriptStatus = ScriptStatus.CompileSuccess;
          }
          else
          {
            ScriptError($"Interface load failed ");
            _scriptStatus = ScriptStatus.Error;
          }
        }
        else
        {
          ScriptError($"Compiled file did not exist.");
          _scriptStatus = ScriptStatus.Error;
        }
      }
    }
    private bool LoadTypeAndCreateInstance(System.Reflection.Assembly? newAsm)
    {
      var res = false;
      Type? newObjType = null;
      if (newAsm == null)
      {
        return false;
      }
      try
      {
        Gu.Assert(newAsm != null);

        Type? needType = _scriptObjectInterfaceType;

        if (needType == null)
        {
          Gu.BRThrowNotImplementedException();
        }

        var tpes = newAsm.GetTypes();
        foreach (var tt in tpes)
        {
          foreach (var xy in tt.GetInterfaces())
          {
            if (xy == needType)
            {
              //Don't set _scriptObject and _asembly until everyth
              newObjType = tt;
              res = true;
              break;
            }
          }
          if (newObjType != null)
          {
            break;
          }
        }

        if (newObjType == null)
        {
          ScriptError($"Could not find type implementing '{needType.ToString()}' interface. Script class must implement '{needType.ToString()}'.");
          res = false;
        }
      }
      catch (Exception ex)
      {
        ScriptError(Gu.GetAllException(ex));
        res = false;
      }

      //Only set data if the entire thing succeeds.
      if (newAsm != null && newObjType != null)
      {
        _loadedAssembly = newAsm;
        _scriptObjectType = newObjType;
        _scriptObject = Activator.CreateInstance(_scriptObjectType);
      }

      return res;
    }
    private string GetFilename()
    {
      if (_files != null && _files.Count > 0)
      {
        return _files[0].FileName;
      }
      return "ERROR!!! - file was not set.";
    }
    private void ScriptWarn(string msg)
    {
      _scriptMessages.Add($"  [{GetFilename()}][W]: {msg}");
    }
    private void ScriptError(string msg)
    {
      _scriptMessages.Add($"  [{GetFilename()}][E]: {msg}");
    }
    private void ScriptInfo(string msg)
    {
      _scriptMessages.Add($"  [{GetFilename()}][I]: {msg}");
    }
    private void PrintErrors()
    {
      if (_scriptMessages.Count > 0)
      {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("");
        foreach (var m in _scriptMessages)
        {
          sb.AppendLine(m);
        }
        if (_scriptStatus == ScriptStatus.Error)
        {
          Gu.Log.Error(sb.ToString());
        }
        else if (_scriptStatus == ScriptStatus.CompileSuccess)
        {
          Gu.Log.Info(sb.ToString());
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
        _scriptMessages.Clear();
      }

      if (_scriptStatus != ScriptStatus.CompileSuccess)
      {
        //TODO: check if system is currently loading, or world is loading (not running)
        Gu.DebugBreak();
      }

    }
    private void PrintOutput(List<string>? output)
    {
      Gu.Assert(output != null);
      StringBuilder sb = new StringBuilder();
      bool warnings = false;
      sb.AppendLine("-------------------------------------------------------------------------------");
      sb.AppendLine("");
      sb.AppendLine("Output:");
      string tabbing = "     ";
      foreach (var line in output)
      {
        if (line.Contains("warn"))
        {
          warnings = true;
        }
        if (line.Contains("error"))
        {
          _scriptStatus = ScriptStatus.Error;
        }
        sb.AppendLine($"{tabbing}{line}");
      }
      sb.AppendLine("");
      sb.AppendLine("-------------------------------------------------------------------------------");
      if (sb.Length > 0)
      {
        _scriptMessages.Add(sb.ToString());
      }

      if (_scriptStatus == ScriptStatus.Error)
      {
        ScriptError("Compiled with errors.");
      }
      else if (warnings)
      {
        ScriptWarn("Compiled with warnings.");
      }
      else
      {
        ScriptInfo("Compile success.");
      }


    }
    #endregion

  }
}