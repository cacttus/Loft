using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
namespace Loft
{
  public enum ColorBitDepth
  {
    FB_16_BIT = 0,
    FB_32_BIT = 1
  }
  [DataContract]
  public class EngineConfig
  {
    //Performance
    [DataMember(IsRequired = true)] public bool ClearCacheOnStart = false;//This cause significant performance problem
    [DataMember(IsRequired = true)] public bool Debug_AlwaysCompileAndReloadGpuUniformData = true;
    [DataMember(IsRequired = true)] public bool Debug_SaveFBOsEveryFrame = false;
    [DataMember(IsRequired = true)] public bool EnableDebugErrorChecks = true;
    [DataMember(IsRequired = true)] public bool EnableRuntimeErrorChecks = true;

    //Logging
    [DataMember(IsRequired = true)] public bool ShowConsoleWindow = true;
    [DataMember(IsRequired = true)] public bool LogErrors = true;
    [DataMember(IsRequired = true)] public bool GraphicsErrorLogging_High = true;
    [DataMember(IsRequired = true)] public bool GraphicsErrorLogging_Medium = true;
    [DataMember(IsRequired = true)] public bool GraphicsErrorLogging_Low = true;
    [DataMember(IsRequired = true)] public bool GraphicsErrorLogging_Info = true;
    [DataMember(IsRequired = true)] public bool Debug_Print_Shader_Uniform_Details_Verbose_NotFound = false;
    [DataMember(IsRequired = true)] public bool Debug_Print_Shader_Uniform_Details_Verbose_AlreadySet = false;
    [DataMember(IsRequired = true)] public bool Debug_PrintTranslationTable = false;
    [DataMember(IsRequired = true)] public bool Debug_LogSerializationDetails = false;
    [DataMember(IsRequired = true)] public bool Debug_Log_GLTF_Details = true;
    [DataMember(IsRequired = true)] public bool Debug_LogToFile = true;
    [DataMember(IsRequired = true)] public bool Debug_LogToConsole = true;

    //font
    [DataMember(IsRequired = true)] public int Font_MaxBakedCharSize = 92;
    [DataMember(IsRequired = true)] public int Font_MinBakedCharSize = 2;
    [DataMember(IsRequired = true)] public int Font_Mipmaps = 10;
    [DataMember(IsRequired = true)] public int Font_MaxBitmapSize = 4096;
    [DataMember(IsRequired = true)] public int Font_MinBitmapSize = 64;
    [DataMember(IsRequired = true)] public bool Font_Lang_RU = true;
    [DataMember(IsRequired = true)] public bool Font_Lang_ZH = true;
    [DataMember(IsRequired = true)] public bool Debug_Font_SaveImage = true; //saves raw generated font images

    //render    
    [DataMember(IsRequired = true)] public bool Debug_EnableCompatibilityProfile = false;
    [DataMember(IsRequired = true)] public bool EnableShaderCaching = true; //cache shader binaries on disk.
    [DataMember(IsRequired = true)] public uint Debug_PickIDIncrement = 100; //set to 100 to see pick ids
    [DataMember(IsRequired = true)] public bool Renderer_UseAlias = true;//-1;//320;//-1=Disable, 640, 320
    [DataMember(IsRequired = true)] public int AliasScreenWidthPixels = 430;//-1;//320;//-1=Disable, 640, 320
    [DataMember(IsRequired = true)] public bool EnableMSAA = false;
    [DataMember(IsRequired = true)] public int MSAASamples = 4;
    [DataMember(IsRequired = true)] public int ShadowMapResolution = 1024;
    [DataMember(IsRequired = true)] public bool StartInEditMode = true;//Set this to false to start the engine in game mode.
    [DataMember(IsRequired = true)] public int WindowInitX = 200;
    [DataMember(IsRequired = true)] public int WindowInitY = 200;
    [DataMember(IsRequired = true)] public int WindowInitW = 1920;
    [DataMember(IsRequired = true)] public int WindowInitH = 1080;
    [DataMember(IsRequired = true)] public float WindowInitScaleW = 0.75f;
    [DataMember(IsRequired = true)] public float WindowInitScaleH = 1;//0.75f;
    [DataMember(IsRequired = true)] public ColorBitDepth ColorBitDepth = ColorBitDepth.FB_16_BIT;//16 or 32

    //game system
    [DataMember(IsRequired = true)] public bool Debug_SaveDebuggShaderSource = true;
    [DataMember(IsRequired = true)] public bool BreakOnOpenGLError = true;
    [DataMember(IsRequired = true)] public bool ClearTmpOnStart = true;
    [DataMember(IsRequired = true)] public int AutoSaveTimeoutSeconds = 5;
    [DataMember(IsRequired = true)] public string UserSavePath = "";
    [DataMember(IsRequired = true)] public bool BreakOnGraphicsError = true;
    [DataMember(IsRequired = true)] public int MaxUndoHistoryItems = 256;
    [DataMember(IsRequired = true)] public bool ReleaseAllButtonsWhenWindowLosesFocus = true;
    [DataMember(IsRequired = true)] public int MaxUIEvents = 500;
    [DataMember(IsRequired = true)] public string ScriptDLLBaseName = "Scripts";
    [DataMember(IsRequired = true)] public bool Debug_ShowFailedShaderSourceInVSCOode = true;

    //Scripts
    [DataMember(IsRequired = true)] public string CSCPath = "";//leave blank for default
    [DataMember(IsRequired = true)] public bool Script_Optimize = false;//may cause slow compile
    [DataMember(IsRequired = true)] public bool Script_Debug = true;//Generate PDB 
    [DataMember(IsRequired = true)] public bool Script_ParallelBuild = true;
    [DataMember(IsRequired = true)] public FileLoc EditGuiScript = new FileLoc("EditGuiScript.cs", EmbeddedFolder.Script); //= new FileLoc("EditGuiScript.cs", PathRoot.Src);
    [DataMember(IsRequired = true)] public FileLoc TestGuiScript = new FileLoc("TestGuiScript.cs", EmbeddedFolder.Script); //= new FileLoc("TestGuiScript.cs", PathRoot.Src);
    [DataMember(IsRequired = true)] public FileLoc UIControls_Script = new FileLoc("UiControls.cs", EmbeddedFolder.Script); //= new FileLoc("UiControls.cs"   , PathRoot.Src);
    [DataMember(IsRequired = true)] public FileLoc Gui2d_Script = new FileLoc("Gui2d.cs", EmbeddedFolder.Script); //= new FileLoc("Gui2d.cs"        , PathRoot.Src);
    [DataMember(IsRequired = true)] public FileLoc TestWorldScript = new FileLoc("MyWorldScript.cs", EmbeddedFolder.Script); //= new FileLoc("MyWorldScript.cs", PathRoot.Src);
    [DataMember(IsRequired = true)] public bool Debug_RainbowMegatexture = true;

    //Shader
    [DataMember(IsRequired = true)] public int ShaderCV_MaxLights = 32;
    [DataMember(IsRequired = true)] public int ShaderCV_MaxMaterials = 256;
    [DataMember(IsRequired = true)] public int ShaderCV_MaxObjects = 1024;
    [DataMember(IsRequired = true)] public int ShaderCV_MaxInstances = 32;
    [DataMember(IsRequired = true)] public int ShaderCV_MaxSampler2Ds = 64;
    [DataMember(IsRequired = true)] public int ShaderCV_MaxCameras = 32;
    [DataMember(IsRequired = true)] public int ShaderCV_MaxArmatures = 128;
    [DataMember(IsRequired = true)] public int ShaderCV_MaxMeshes = 256;
    [DataMember(IsRequired = true)] public int ShaderCV_MaxCubeShadowSamples = 4;
    [DataMember(IsRequired = true)] public int ShaderCV_MaxFrusShadowSamples = 4;
    [DataMember(IsRequired = true)] public int ShaderCV_GpuDataSizeMB = 16;


    public static EngineConfig LoadEngineConfig(FileLoc loc)
    {
      //Dont load just return this one for debugging purposes

      Gu.Log.Warn("Disabled saveing/loading engine config for deubggign");

      return new EngineConfig();

      return Load(loc);
    }
    private static EngineConfig Load(FileLoc loc)
    {
      EngineConfig ret = null;
      bool missing = false;
      if (!loc.Exists)
      {
        ret = new EngineConfig();
        Gu.Log.Warn($"Config '{loc.QualifiedPath}' did not exist, saving new config.");
        Save(ret, new FileLoc(Gu.WorkspaceDataPath, loc.FileName, FileStorage.Disk));
      }
      else
      {
        Gu.Log.Info($"Loading config {loc.QualifiedPath}");
        try
        {
          string text = loc.ReadAllText();

          var settings = new JsonSerializerSettings()
          {
            MissingMemberHandling = MissingMemberHandling.Error,
            Error = (s, e) =>
            {
              string member = "";
              if (e != null && e.ErrorContext != null && e.ErrorContext.Member != null)
              {
                member = e.ErrorContext.Member.ToString();
              }
              else
              {
                member = "member was not set (Newtonsoft .json)";
              }
              Gu.Log.Error($"config JSON Member '{member}' was not found. Setting to default.");
              missing = true;
              e.ErrorContext.Handled = true;
            }
          };

          //will throw if fails
          ret = JsonConvert.DeserializeObject<EngineConfig>(text, settings);
          ret.Sanitize();
        }
        catch (Exception ex)
        {
          Gu.Log.Error(ex);
          Gu.DebugBreak();
          throw ex;//we must fail if config fails.
        }

        if (missing)
        {
          //missing stuff - save
          Save(ret, new FileLoc(Gu.WorkspaceDataPath, loc.FileName, FileStorage.Disk));
        }
      }
      return ret;
    }
    private static void Save(EngineConfig c, FileLoc fl)
    {
      try
      {
        var ob = JsonConvert.SerializeObject(c, Formatting.Indented);
        Gu.Log.Info($"Saving new config {fl.QualifiedPath}");
        fl.WriteAllText(ob);
      }
      catch (Exception ex)
      {
        Gu.Log.Error($"config, JSON - Could not save {fl.QualifiedPath}", ex);
      }
    }

    public EngineConfig()
    {
    }

    private bool Sanitize()
    {
      bool valuesChanged = false;


      if (AutoSaveTimeoutSeconds < 1)
      {
        AutoSaveTimeoutSeconds = 1;
        Gu.Log.Info("AutoSaveTimeout was invalid, setting to 1");
        valuesChanged = true;
      }


      return valuesChanged;
    }

  }
}
