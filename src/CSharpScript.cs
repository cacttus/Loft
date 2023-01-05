using System.Diagnostics;
using System.Text;
namespace Loft
{
  #region Script Interfaces

  //Script-side interfaces to implement in the .cs script file
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
  public interface IUIScript
  {
    public string GetName();
    public List<FileLoc> GetResources();

    public void OnCreate(Gui2d g);
    public void OnUpdate(RenderView rv);
  }
  public interface IWorldScript
  {
    public void OnLoad(World w);
    public void OnUpdate(World w, double delta);
    public void OnExit(World w);
  }

  #endregion
  #region Engine Interfaces

  public class UIScript : CSharpScript
  {
    private List<RenderView> _views = new List<RenderView>();
    private IUIScript Script { get { return (_scriptObject as IUIScript); } }

    public UIScript(List<FileLoc> loc) : base(loc, typeof(IUIScript))
    {
    }
    public void LinkView(RenderView rv)
    {
      _views.Add(rv);
      LoadForView(rv);
    }
    protected override void ScriptChanged()
    {
      foreach (var rv in _views)
      {
        LoadForView(rv);
      }
    }
    public void UpdateForView(RenderView rv)
    {
      if (Script != null)
      {
        Script.OnUpdate(rv);
      }
    }
    private void LoadForView(RenderView rv)
    {
      //TODO: we need to update this again when failed compile actually succeeds

      if (rv.Gui != null)
      {
        rv.Gui = null;
        //*** Possibly needed here. 
        GC.Collect();
        //***
      }

      if (Script == null)
      {
        Compile();
      }

      if (Script != null)
      {
        var rsc = Script.GetResources();

        //TODO: remove the Gui2dShared if the resources changed. 

        var gdat = Gu.Gui2dManager.GetOrCreateGui2dShared(Script.GetName(), rsc);
        rv.Gui = new Gui2d(gdat, rv);
        Script.OnCreate(rv.Gui);
      }
    }

  }
  public class WorldScript : CSharpScript, IWorldScript
  {
    //Thunk class for runtime interface

    public WorldScript(FileLoc loc) : base(new List<FileLoc>(){loc}, typeof(IWorldScript))
    {
    }
    protected override void ScriptChanged() { }
    public void OnLoad(World w)
    {
      if (_scriptObject != null)
      {
        (_scriptObject as IWorldScript).OnLoad(w);
      }
    }
    public void OnUpdate(World w, double delta)
    {
      if (_scriptObject != null)
      {
        (_scriptObject as IWorldScript).OnUpdate(w, delta);
      }
    }
    public void OnExit(World w)
    {
      if (_scriptObject != null)
      {
        (_scriptObject as IWorldScript).OnExit(w);
      }
    }
  }

  #endregion

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
    //public FileLoc File { get { return _files[0]; } }

    private ScriptStatus _scriptStatus = ScriptStatus.None;
    private string _outputPath = "";
    protected Type? _scriptObjectType = null;
    protected object? _scriptObject = null;
    private System.Reflection.Assembly? _loadedAssembly = null;
    private static Dictionary<FileLoc, CSharpScript> _loadedScripts = new Dictionary<FileLoc, CSharpScript>(new FileLoc.EqualityComparer());
    private List<FileLoc> _files;
    private List<string> _scriptMessages = new List<string>();
    private int _compileCount = 0;
    private FileWatcher? _watcher = null;
    protected Type? _scriptObjectInterfaceType = null;
    private bool _initialized = false;

    #endregion
    #region Public:Methods

    protected virtual void ScriptChanged() { }

    public static object? Call(string scriptname, object? param)
    {
      //Shortcut to call a "function script"
      return Call(new FileLoc(Gu.WorkspaceDataPath, scriptname, FileStorage.Disk), param);
    }
    public static object? Call(FileLoc scriptname, object? param)
    {
      if (!_loadedScripts.TryGetValue(scriptname, out var ss))
      {
        ss = new CSharpScript(new List<FileLoc>() { scriptname }, typeof(IFunctionScript));
        _loadedScripts.Add(scriptname, ss);
      }
      return ss.Call(null);
    }

    public CSharpScript(List<FileLoc> locs, Type interface_type)
    {
      _scriptObjectInterfaceType = interface_type;

      _files = _files.ConstructIfNeeded();
      _files.AddRange(locs);

      _watcher = new FileWatcher(_files, (f) =>
      {
        Compile();
        return this._scriptStatus == ScriptStatus.CompileSuccess;
      });
    }
    private string GetScriptPath(FileLoc f)
    {
      string fn = "";
      if (f.FileStorage == FileStorage.Disk)
      {
        fn = f.QualifiedPath;
      }
      else if (f.FileStorage == FileStorage.Embedded)
      {
        fn = f.WorkspacePath;
        ScriptWarn($"Embedded scripts can't be sent to the compiler (for now), using workspace path ({f.WorkspacePath})");
      }
      return fn;
    }
    public bool Compile()
    {
      //Root compile method, reset everything and try compiling.
      _scriptStatus = ScriptStatus.None;
      _outputPath = System.IO.Path.Combine(Gu.ExePath, Gu.EngineConfig.ScriptDLLName);

      ScriptInfo($"Loading..");

      try
      {
        string files = "";
        foreach (var f in _files)
        {
          var fn = GetScriptPath(f);

          if (!System.IO.File.Exists(fn))
          {
            ScriptError($"File {fn} does not exist.");
            Gu.DebugBreak();
            _scriptStatus = ScriptStatus.Error;
          }
          else
          {
            files += $" {fn}";
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

      _initialized = true;

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
          "System.Linq.dll",//IEnumerable.ToArray
          System.IO.Path.Combine(Gu.ExePath, "Loft.dll"),
          System.IO.Path.Combine(Gu.ExePath, "OpenTK.Core.dll"),
          System.IO.Path.Combine(Gu.ExePath, "OpenTK.Graphics.dll"),
          System.IO.Path.Combine(Gu.ExePath, "OpenTK.Windowing.Common.dll"),
          System.IO.Path.Combine(Gu.ExePath, "OpenTK.Windowing.Desktop.dll"),
          System.IO.Path.Combine(Gu.ExePath, "OpenTK.Windowing.GraphicsLibraryFramework.dll"),
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
      //-parallel[+|-]                Concurrent build.
      //-optimize[+|-]                Enable optimizations (Short form: -o)

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
            ScriptChanged();
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
      Gu.Assert(newAsm != null);
      Gu.Assert(_scriptObjectInterfaceType != null);
      var newObjType = GetInterfaceTypeFromAssembly(newAsm, _scriptObjectInterfaceType);

      //Only set data if the entire thing succeeds.
      if (newAsm != null && newObjType != null)
      {
        _loadedAssembly = newAsm;
        _scriptObjectType = newObjType;
        _scriptObject = Activator.CreateInstance(_scriptObjectType);
        return true;
      }
      return false;
    }
    private Type? GetInterfaceTypeFromAssembly(System.Reflection.Assembly? newAsm, Type? ifaceType)
    {
      Gu.Assert(newAsm != null);
      Gu.Assert(ifaceType != null);

      Type? ret = null;
      var types = newAsm.GetTypes();
      foreach (var tt in types)
      {
        foreach (var iface in tt.GetInterfaces())
        {
          if (iface == ifaceType)
          {
            ret = tt;
            break;
          }
        }
        if (ret != null)
        {
          break;
        }
      }

      if (ret == null)
      {
        ScriptError($"Could not find type implementing '{ifaceType.ToString()}' interface. Script class must implement '{ifaceType.ToString()}'.");
      }

      return ret;
    }
    private string GetFilename()
    {
      string fn = "";
      string app = "";
      foreach (var f in _files)
      {
        fn += app + f.FileName;
        app = ",";
      }
      return fn;
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
        if (_initialized == false)
        {
          Gu.DebugBreak();
        }
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