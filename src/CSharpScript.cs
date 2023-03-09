using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

namespace Loft
{
  #region Engine Interfaces

  public class UIScript : CSharpScript
  {
    private List<RenderView> _views = new List<RenderView>();
    public IUiControls Script { get { return (_scriptObject as IUiControls); } }
    private Action<IUiControls>? _onChanged = null;

    public UIScript(List<FileLoc> loc, Action<IUiControls> on_changed) : base(loc, typeof(IUiControls))
    {
      _onChanged = on_changed;
    }
    public void LinkView(RenderView rv)
    {
      _views.Add(rv);
      UpdateUIForView(rv);
    }
    protected override void OnCompile()
    {
      _onChanged?.Invoke(Script);
      foreach (var rv in _views)
      {
        rv.Gui = null;
      }

      GC.Collect();

      foreach (var rv in _views)
      {
        UpdateUIForView(rv);
      }
    }
    private void UpdateUIForView(RenderView rv)
    {
      //TODO: we need to update this again when failed compile actually succeeds
      if (Script != null)
      {
        rv.Gui = Script.CreateForView(rv);
      }
    }

  }
  public class WorldScript : CSharpScript, IWorldScript
  {
    public WorldScript(FileLoc loc) : base(new List<FileLoc>() { loc }, typeof(IWorldScript))
    {
    }
    protected override void OnCompile() { }
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

    #region Members

    public static int TotalLoadedScriptAssemblyBytes { get; private set; } = 0;
    public ScriptStatus ScriptStatus { get { return _scriptStatus; } protected set { _scriptStatus = value; } }
    public string Name { get; private set; }
    protected Type? _scriptObjectType = null;
    protected object? _scriptObject = null;
    protected Type? _scriptObjectInterfaceType = null;

    private static int _compileCount = 0;
    private static Dictionary<FileLoc, CSharpScript> _loadedScripts = new Dictionary<FileLoc, CSharpScript>(new FileLoc.EqualityComparer());
    private ScriptStatus _scriptStatus = ScriptStatus.None;
    private string _outputPath = "";
    private System.Reflection.Assembly? _loadedAssembly = null;
    private List<FileLoc> _files;
    private FileWatcher? _watcher = null;
    private bool _initialized = false;

    #endregion
    #region Public:Methods

    public CSharpScript(List<FileLoc> locs, Type interface_type)
    {
      Name = this.GetType().Name;
      _scriptObjectInterfaceType = interface_type;

      _files = _files.ConstructIfNeeded();
      _files.AddRange(locs);

      _watcher = new FileWatcher(_files, (f) =>
      {
        Compile().Wait();
        return _scriptStatus == ScriptStatus.CompileSuccess;
      });
    }
    public string ScriptDLLName
    {
      get
      {
        //In Mono we can just write to the PDB for some reason, but in MS .NET this is impossible as the PDB gets locked
        //https://stackoverflow.com/questions/26651293/runtime-code-compilation-gives-error-process-cannot-access-the-file/26699609#26699609
        string dl = $"{Gu.EngineConfig.ScriptDLLBaseName}{_compileCount}.dll";
        return dl;
      }
    }
    public EngineTask Compile()
    {
      //Root compile method, reset everything and try compiling.
      _scriptStatus = ScriptStatus.None;
      _outputPath = System.IO.Path.Combine(Gu.ExePath, ScriptDLLName);
      _compileCount++;

      ScriptInfo(DebugUtils.HeaderString($"SCRIPT {this.Name}"));

      ScriptInfo(
        $"Loft Script Compiler" + ", " +
        $".NET Version: {Environment.Version}"
      );

      ScriptInfo($"Loading..");

      _initialized = true;

      try
      {
        List<string> files = new List<string>();
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
            files.Add(fn);
          }
        }

        if (_scriptStatus != ScriptStatus.Error)
        {
          return CompileInternal(files);
        }
      }
      catch (Exception ex)
      {
        ScriptError(Gu.GetAllException(ex));
        _scriptStatus = ScriptStatus.Error;
        PrintErrors();
      }

      return EngineTask.Fail;
    }

    #endregion
    #region Private/Protected: Methods

    protected virtual void OnCompile() { }

    private EngineTask CompileInternal(List<string> files)
    {
      _scriptStatus = ScriptStatus.Compiling;
      ScriptInfo($"Compiling ({_compileCount}): {this.GetFilename()}");

      var compiler = FindCSC();
      var quot = "\"";
      string cmdstr = $"{quot}{compiler}{quot} {String.Join(" ", GetArgs(files, quot))}";

      ScriptDebug($"Executing: {cmdstr}");
      ScriptInfo(String.Join($"{Environment.NewLine}", GetArgs(files, quot)), true, ConsoleColor.Green);

      ExternalProgram ep = new ExternalProgram(compiler, String.Join(" ", GetArgs(files, "")));
      var etask = EngineTask.Start((et) =>
      {
        return ep.Launch();
      })
      .SyncThen((et) =>
      {
        if (et.Success)
        {
          PrintCompilerOutput(ep.Output);
          LoadAssembly();
        }
        else
        {
          _scriptStatus = ScriptStatus.Error;
        }

        PrintErrors();

        return _scriptStatus == ScriptStatus.CompileSuccess;
      });

      return etask;
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
    private string FindCSC()
    {
      string compiler = "";
      //"C:/Program Files/Mono/Lib/Mono4.5/csc.exe";
      //var ver = System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion();

      if (StringUtil.IsNotEmpty(Gu.EngineConfig.Script_CSCPath))
      {
        compiler = Gu.EngineConfig.Script_CSCPath;
      }
      else if (OperatingSystem.Platform == Platform.Windows)
      {
        //Windows: dir /s %WINDIR%\CSC.EXE
        //var frameworkPath = RuntimeEnvironment.GetRuntimeDirectory();//this path didn't contain csc.
        //compiler = Path.Combine(frameworkPath, "csc.exe");
        //compiler = "C:\\Program Files\\Mono\\bin\\csc.bat"; //wrong version
        //compiler = "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\csc.exe";\ //wrong version. We must use the same version we compile the app with.

        //Visual Studio
        //compiler = @"C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\Roslyn\csc.exe";

        //This is the omnisharp VSCode compielr
        compiler = @"c:\Users\scriplez\.vscode\extensions\ms-dotnettools.csharp-1.24.1\.omnisharp\1.38.1\.msbuild\Current\Bin\Roslyn\csc.exe";

        if (!System.IO.File.Exists(compiler))
        {
          Gu.Log.Error($"Compiler path '{compiler}' was not found.");
          Gu.DebugBreak();
        }


      }
      else if (OperatingSystem.Platform == Platform.Linux)
      {
        compiler = "csc";
      }

      if (StringUtil.IsEmpty(compiler))
      {
        Gu.BRThrowException($"Could not get csc compiler (version={OperatingSystem.VersionString}) for scripts.\n Mono may not be installed.\n Set manually in config: MANUAL_CSC_PATH");
      }

      return compiler;
    }
    private List<string> GetLibPaths()
    {
      List<String> libs = null;
      if (OperatingSystem.Platform == Platform.Windows)
      {
        libs = new List<string>()
        {
          //if compiling with roslyn
          //@"C:\Windows\Assembly\",
          // "\"C:/Program Files/dotnet/packs/Microsoft.NETCore.App.Ref/6.0.2/ref/net6.0/\"",
          //"C:/Program Files/Mono/lib/mono/4.8-api/"
        };
      }
      else if (OperatingSystem.Platform == Platform.Linux)
      {
        //Mono facades
        libs = new List<string>()
        {
          @"/usr/lib/mono/4.8-api/Facades/",
          @"/usr/lib/mono/4.8-api/",
        };
      }
      return libs;
    }
    private List<string> GetRefs()
    {
      //TODO: figure out how to do this dynamically
      var refs = new List<string>(){
          "System.dll",
          "System.Runtime.dll",
          "System.Collections.dll",
          "System.Linq.dll",
          "System.Console.dll",
          "System.Threading.dll",
          "System.Threading.ThreadPool.dll",
          "System.Runtime.InteropServices.dll",
          Path.Combine(Gu.ExePath, "Loft.dll"),
          Path.Combine(Gu.ExePath, "OpenTK.Core.dll"),
          Path.Combine(Gu.ExePath, "OpenTK.Graphics.dll"),
          Path.Combine(Gu.ExePath, "OpenTK.Windowing.Common.dll"),
          Path.Combine(Gu.ExePath, "OpenTK.Windowing.Desktop.dll"),
          Path.Combine(Gu.ExePath, "OpenTK.Windowing.GraphicsLibraryFramework.dll"),
        };
      return refs;
    }
    private List<string> GetArgs(List<string> files, string quot)
    {
      //If quot is specified This returns the command prompt string not Process.Start string

      var libpaths = GetLibPaths();
      var refs = GetRefs();

      Gu.Assert(files != null && files.Count > 0);

      var csw = "";
      if (OperatingSystem.Platform == Platform.Windows) { csw = "/"; }
      else { csw = "-"; }

      //-debug:{full|pdbonly|portable|embedded}
      //-pdb:<file>   
      //-parallel[+|-]                
      var debugEnabled = Gu.EngineConfig.Script_Debug ? "+" : "-";
      var debugInfo = "full";
      var optimize = Gu.EngineConfig.Script_Optimize ? "+" : "-";
      var nullable = "+";
      var @unsafe = "+";
      var parallel = Gu.EngineConfig.Script_ParallelBuild ? "+" : "-"; //Concurrent build.

      var scriptTemp = Gu.RootPath("tmp/script" + (Gu.EngineConfig.Script_Debug ? "/debug" : "/release"), PathRoot.Exe);
      if (!System.IO.Directory.Exists(scriptTemp))
      {
        System.IO.Directory.CreateDirectory(scriptTemp);
      }
      //Others
      //-recurse:<wildcard>
      //-moduleassemblyname:<string>  Name of the assembly which this module will be      a part of
      //- modulename:< string > Specify the name of the source module

      var args = new List<string>() {
        $"{csw}nologo",
        //$"{csw}nostdlib-", 
        //$"{csw}nosdkpath", // disable default sdk search
        //$"{csw}platform:x64",
        $"{csw}debug{debugEnabled}",
        $"{csw}debug:{debugInfo}",
        $"{csw}nullable{nullable}",
        $"{csw}unsafe{@unsafe}",
        $"{csw}parallel{parallel}",
        $"{csw}optimize{optimize}",

        $"{csw}out:{quot}{_outputPath}{quot}",
        $"{csw}generatedfilesout:{quot}{scriptTemp}{quot}",
        $"{csw}t:library",
        };
      if (refs != null && refs.Count > 0)
      {
        foreach (var rr in refs)
        {
          args.Add($"{csw}r:{rr}");
        }
      }
      if (libpaths != null && libpaths.Count > 0)
      {
        foreach (var rr in libpaths)
        {
          args.Add($"{csw}lib:{rr}");
        }
      }
      if (files != null && files.Count > 0)
      {
        foreach (var rr in files)
        {
          args.Add($"{quot}{rr}{quot}");
        }
      }

      return args;
    }
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
            ScriptInfo($"{ScriptDLLName}: Assembly Loaded, (Total LSR={TotalLoadedScriptAssemblyBytes}).");
            OnCompile();
            _scriptStatus = ScriptStatus.CompileSuccess;
          }
          else
          {
            ScriptError($"{ScriptDLLName}: Interface load failed ");
            _scriptStatus = ScriptStatus.Error;
          }
        }
        else
        {
          ScriptError($"Could not load assembly, '{_outputPath}' did not exist.");
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
        app = ", ";
      }
      return fn;
    }


    private void ScriptError(string msg, bool headless = false)
    {
      //print a local script info without extra header stuff
      Gu.Log.Error(GetLogStr("Error: ", msg, headless), true);
    }
    private void ScriptWarn(string msg, bool headless = false)
    {
      Gu.Log.Warn(GetLogStr("Warning: ", msg, headless), true);
    }
    private void ScriptDebug(string msg, bool headless = false)
    {
      Gu.Log.Debug(GetLogStr("Debug: ", msg, headless), true);
    }
    private void ScriptInfo(string msg, bool headless = false, ConsoleColor? color = null)
    {
      Gu.Log.Info(GetLogStr("", msg, headless), true, color);
    }
    private string GetLogStr(string ewi, string msg, bool headless = false)
    {
      string _global_log_tab = "  ";

      var header = headless ? "" : $"{_global_log_tab}{ewi}";
      return $"{_global_log_tab}{header}{msg}".Replace(Environment.NewLine, $"{Environment.NewLine}{_global_log_tab}");
    }
    private void PrintErrors()
    {
      if (_scriptStatus == ScriptStatus.Error)
      {
        ScriptError("One or more compiles failed");
      }
      else if (_scriptStatus == ScriptStatus.CompileSuccess)
      {
        ScriptInfo("All compiles succeeded");
      }
      else
      {
        Gu.BRThrowNotImplementedException();//invalid enum
      }

      if (_scriptStatus != ScriptStatus.CompileSuccess)
      {
        if (_initialized == false)
        {
          Gu.DebugBreak();
        }
      }

    }
    private void PrintCompilerOutput(List<string>? output)
    {
      //make the output readable
      Gu.Assert(output != null);
      ScriptInfo(DebugUtils.HeaderString($"COMPILE OUTPUT {this.Name}", false, '*'));
      ScriptInfo("");
      if (!Gu.EngineConfig.Script_ShowWarnings)
      {
        ScriptInfo("(Script warnings disabled)");
        ScriptInfo("");
      }
      ScriptInfo("Output:");

      //skip warnings
      bool hide_8601 = true; // Possible null reference assignment.
      bool hide_8602 = true; // Dereference of a possibly null reference.
      bool hide_8603 = true; // Possible null reference return.
      bool hide_8604 = true; // Possible null reference argument for parameter
      bool hide_8625 = false; // Cannot convert null literal to non-nullable reference type.
      bool hide_8629 = true; // Nullable value type may be null.
      bool hide_0414 = true; // The field '' is assigned but its value is never used

      string tabbing = "     ";

      bool has_warnings = false;

      foreach (var line in output)
      {
        var tab = $"{tabbing}{line}";

        if (Gu.EngineConfig.Script_ShowWarnings)
        {
          if (line.Contains("warn"))
          {
            if (
              (hide_8601 && line.Contains("CS8601:")) ||
              (hide_8602 && line.Contains("CS8602:")) ||
              (hide_8603 && line.Contains("CS8603:")) ||
              (hide_8604 && line.Contains("CS8604:")) ||
              (hide_8625 && line.Contains("CS8625:")) ||
              (hide_8629 && line.Contains("CS8629:")) ||
              (hide_0414 && line.Contains("CS0414:"))
            )
            {
            }
            else
            {
              has_warnings = true;
              ScriptWarn(tab, true);
            }
          }
        }
        if (line.Contains("error"))
        {
          _scriptStatus = ScriptStatus.Error;
          ScriptError(tab, true);
        }
      }
      ScriptInfo("");


      if (_scriptStatus == ScriptStatus.Error)
      {
        ScriptError($"{tabbing}Compiled with errors.", true);
      }
      else if (has_warnings)
      {
        ScriptWarn($"{tabbing}Compiled with warnings.", true);
      }
      else
      {
        ScriptInfo($"{tabbing}Compile success.", true);
      }

      ScriptInfo("");
      ScriptInfo(DebugUtils.HeaderString($"SCRIPT {this.Name}", true));


    }
    #endregion

  }
}