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

    //Log
    [DataMember(IsRequired = true)] public bool LogErrors = true;
    [DataMember(IsRequired = true)] public bool GraphicsErrorLogging_High = true;
    [DataMember(IsRequired = true)] public bool GraphicsErrorLogging_Medium = true;
    [DataMember(IsRequired = true)] public bool GraphicsErrorLogging_Low = true;
    [DataMember(IsRequired = true)] public bool GraphicsErrorLogging_Info = true;
    [DataMember(IsRequired = true)] public bool Debug_Print_Shader_Uniform_Details_Verbose_NotFound = false;  
    [DataMember(IsRequired = true)] public bool Debug_Print_Shader_Uniform_Details_Verbose_AlreadySet = false;
    [DataMember(IsRequired = true)] public bool Debug_PrintTranslationTable = false; 
    [DataMember(IsRequired = true)] public bool Debug_LogSerializationDetails = true;
    [DataMember(IsRequired = true)] public bool Debug_Log_GLTF_Details = true;

    //font
    [DataMember(IsRequired = true)] public int MaxBakedCharSize = 64;
    [DataMember(IsRequired = true)] public int MaxFontBitmapSize = 4096;
    [DataMember(IsRequired = true)] public bool UseLang_RU = true;
    [DataMember(IsRequired = true)] public bool UseLang_ZH = true;
    [DataMember(IsRequired = true)] public bool SaveSTBFontImage = true; //saves raw generated font images

    //render    
    [DataMember(IsRequired = true)] public bool Debug_EnableCompatibilityProfile = false;
    [DataMember(IsRequired = true)] public bool EnableShaderCaching = true; //cache shader binaries on disk.
    [DataMember(IsRequired = true)] public bool Debug_PickIDs = true;
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
    [DataMember(IsRequired = true)] public float WindowInitScaleH = 0.75f;
    [DataMember(IsRequired = true)] public ColorBitDepth ColorBitDepth = ColorBitDepth.FB_16_BIT;//16 or 32
    
    //system
    [DataMember(IsRequired = true)] public bool Debug_SaveDebuggShaderSource = true;
    [DataMember(IsRequired = true)] public bool BreakOnOpenGLError = true;
    [DataMember(IsRequired = true)] public bool ClearTmpOnStart = true;
    [DataMember(IsRequired = true)] public int AutoSaveTimeoutSeconds = 5;
    [DataMember(IsRequired = true)] public string UserSavePath = "";
    [DataMember(IsRequired = true)] public bool BreakOnGraphicsError = true;
    [DataMember(IsRequired = true)] public int MaxUndoHistoryItems = 256;
    [DataMember(IsRequired = true)] public bool ReleaseAllButtonsWhenWindowLosesFocus = true;
    [DataMember(IsRequired = true)] public int MaxUIEvents = 500;
    [DataMember(IsRequired = true)] public string ScriptDLLName = "Scripts.dll";
    [DataMember(IsRequired = true)] public bool Debug_ShowFailedShaderSourceInVSCOode = true;

    public static EngineConfig LoadEngineConfig(FileLoc loc)
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
              Gu.Log.Error($"config JSON Member '{e.ErrorContext.Member.ToString()}' was not found. Setting to default.");
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
    /*
      "fonts" : [
        {
          "name": "NotoSerif",
          "langs": {
              "all": {
                "regular" : { "file" : "NotoSerifSC-Regular.ttf", "storage" : "embedded" },
                "bold" : { "file" : "NotoSerifSC-Bold.ttf", "storage" : "embedded" }
              },
              "zh": {
                "regular" : { "file" : "NotoSerifSC-Regular.ttf", "storage" : "embedded" },
                "bold" : { "file" : "NotoSerifSC-Bold.ttf", "storage" : "embedded" }
              }
            }
        },
        {
          "name": "Parisienne",
          "en": {
            "regular" : { "file" : "Parisienne-Regular.ttf", "storage" : "embedded"}
          }
        },
        {
          "name": "RobotoMono",
          "en": {
            "regular" : { "file" : "RobotoMono-Regular.ttf", "storage" : "embedded" }
          }
        },
        {
          "name": "PressStart2P",
          "en": {
            "regular" : { "file" : "PressStart2P-Regular.ttf", "storage" : "embedded" }
          }
        },
        {
          "name": "Entypo",
          "en": {
            "regular" : { "file" : "Entypo.ttf", "storage" : "embedded" }
          }
        },
        {
          "name": "Calibri",
          "en": {
            "regular" : { "file" : "calibri.ttf", "storage" : "embedded" }
          }
        },
        {
          "name": "EmilysCandy",
          "en": {
            "regular" : { "file" : "EmilysCandy-Regular.ttf", "storage" : "embedded" }
          }
        }
      ]

    */
  }
}
