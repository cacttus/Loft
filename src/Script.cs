using System.Diagnostics;
using System.Text;
namespace PirateCraft
{
  public interface IObjectScript
  {
    public void OnCreate();
    public void OnUpdate();
    public void OnDestroy();
  }
  public interface IFunctionScript
  {
    public object? DoThing(object? param);
  }
  //So you need to pass ALL used files to mcs, it can't find it for you.
  // We have 2 dll's one for embedded scripts and one for external scripts. 
  // hm.
  //This pretty much works. Would requires some tinkering with dll references as we'll need a lot more of them.
  //
  //Ubuntu - requires mono-devel
  //    https://www.mono-project.com/download/stable/#download-lin
  //
  public enum ScriptType
  {
    Function,
    Object
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

  public class CSharpScript : DynamicFileLoader
  {
    private ScriptStatus ScriptStatus { get { return _scriptStatus; } }
    private ScriptStatus _scriptStatus = ScriptStatus.None;
    public override bool ShouldCheck => true;
    private string _outputPath = "";
    private ScriptType _scriptType = ScriptType.Object;
    private Type? _scriptObjectType = null;
    private object? _scriptObject = null;
    private System.Reflection.Assembly? _loadedAssembly = null;
    private static Dictionary<FileLoc, CSharpScript> _loadedScripts = new Dictionary<FileLoc, CSharpScript>(new FileLoc.EqualityComparer());

    private List<FileLoc> _files;
    protected override List<FileLoc> Files { get { return _files; } }

    private int _totalLoadedScriptAssemblyBytes = 0;

    public static object? Call(string scriptname, object? param)
    {
      return Call(new FileLoc(Gu.WorkspaceDataPath, scriptname, FileStorage.Disk), param);
    }
    public static object? Call(FileLoc scriptname, object? param)
    {
      if (!_loadedScripts.TryGetValue(scriptname, out var ss))
      {
        ss = new CSharpScript(scriptname, ScriptType.Function);
        _loadedScripts.Add(scriptname, ss);
      }
      ss.CheckSourceChanged();
      return ss.Call(null);
    }

    public CSharpScript(FileLoc loc, ScriptType type)
    {
      _files = _files.ConstructIfNeeded();
      _scriptType = type;
      _files.Add(loc);
    }
    protected override void OnSourceChanged(List<FileLoc> changed)
    {
      Compile();
    }
    public bool Compile()
    {
      _outputPath = System.IO.Path.Combine(Gu.ExePath, "Scripts.dll");
      Gu.Log.Info($"{Name}:{_outputPath}: Compiling Script.");


      try
      {

        List<String> refs = new List<string>(){
          "System.dll",
          "System.Runtime.dll",
          "System.Collections.dll",
          System.IO.Path.Combine(Gu.ExePath, "PirateCraft.dll"),
          System.IO.Path.Combine(Gu.ExePath, "OpenTK.Core.dll"),
        };
        List<String> libs = new List<string>(){
        "/usr/lib/mono/4.8-api/Facades/",//Mono facade
         "/usr/lib/mono/4.8-api/",//Mono facade
        // "/usr/share/dotnet/shared/Microsoft.NETCore.App/6.0.9/",//MS .NET
        };

        string debug = "-debug:full";
        //-debug:{full|pdbonly|portable|embedded}
        //-pdb:<file>   

        string files = "";
        foreach (var f in _files)
        {
          files += $" {f.QualifiedPath}";
        }


        _scriptStatus = ScriptStatus.Compiling;

        //csc worked. mcs did not work.. idk why
        var compiler = "csc";
        var args = $"-out:{_outputPath} -t:library -r:{String.Join(" -r:", refs.ToArray())} -lib:{String.Join(" -lib:", libs.ToArray())} {files} {debug}";
        Gu.Log.Info($"{compiler} {args}");

        if (Gu.LaunchProgram(compiler, args, out var output))
        {
          StringBuilder sb = new StringBuilder();
          foreach (var line in output)
          {
            if (line.Contains("error"))
            {
              _scriptStatus = ScriptStatus.Error;
            }
            sb.AppendLine(line);
          }
          if (sb.Length > 0)
          {
            Gu.Log.Info(sb.ToString());
          }

          if (_scriptStatus != ScriptStatus.Error)
          {
            if (System.IO.File.Exists(_outputPath))
            {
              //Load ASM in bytes to just duplicate the assembly. We can't unload assemblies, but, it's not an issue right now.
              var asmbytes = System.IO.File.ReadAllBytes(_outputPath);

              _totalLoadedScriptAssemblyBytes += asmbytes.Length;
              Gu.Log.Debug($"Total Runtime Script Bytes: {_totalLoadedScriptAssemblyBytes}");

              var asm = System.Reflection.Assembly.Load(asmbytes);
              if (LoadInterface(asm))
              {
                Gu.Log.Error($"{Name}:{_outputPath}: Script loaded.");
                _scriptStatus = ScriptStatus.CompileSuccess;
              }
              else
              {
                Gu.Log.Error($"{Name}:{_outputPath}: Interface load failed ");
                _scriptStatus = ScriptStatus.Error;
              }
            }
            else
            {
              Gu.Log.Error($"{Name}:{_outputPath}: Compiled file did not exist.");
              _scriptStatus = ScriptStatus.Error;
            }
          }
        }
        else
        {
          _scriptStatus = ScriptStatus.Error;
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error($"{Name}:{_outputPath}", ex);
        _scriptStatus = ScriptStatus.Error;
      }

      return _scriptStatus == ScriptStatus.CompileSuccess;
    }
    private bool LoadInterface(System.Reflection.Assembly? newAsm)
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

        Type? needType = null;
        if (_scriptType == ScriptType.Object)
        {
          needType = typeof(IObjectScript);
        }
        else if (_scriptType == ScriptType.Function)
        {
          needType = typeof(IFunctionScript);
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }

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
          Gu.Log.Error($"{Name}:{_outputPath}: Could not find type implementing '{needType.ToString()}' interface. Script class must implement '{needType.ToString()}'.");
          res = false;
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error($"{Name}:{_outputPath}", ex);
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
    public bool Run()
    {
      if (_scriptObject != null)
      {
        var obo = (_scriptObject as IObjectScript);
        if (obo != null)
        {
          obo.OnCreate();
          obo.OnUpdate();
          obo.OnDestroy();
          return true;
        }
      }
      return false;
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


  }
}