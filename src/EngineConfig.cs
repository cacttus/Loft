using System;
namespace PirateCraft
{
  public class EngineConfig
  {

    //SUPER DEBUG stuff
    [JSONXIgnore()] public bool ClearCacheOnStart = false;//Setting this true may make the thing SLOW
    [JSONXIgnore()] public bool ClearTmpOnStart = true;//logs..debug..
    [JSONXIgnore()] public bool BreakOnOpenGLError = true;
    [JSONXIgnore()] public bool Debug_AlwaysCompileAndReloadGpuUniformData = true; //Don't try to optimize out modification, and always compile the structs (for debugging)
    [JSONXIgnore()] public bool Debug_Print_Shader_Uniform_Details_Verbose_NotFound = true; //So this is to be turned on for debug
    [JSONXIgnore()] public bool Debug_Print_Shader_Uniform_Details_Verbose_AlreadySet = false; //So this is to be turned on for debug
    [JSONXIgnore()] public bool Debug_ShowPipelineClearMessage = false;
    [JSONXIgnore()] public bool Debug_EnableCompatibilityProfile = true;
    [JSONXIgnore()] public bool SaveFBOsEveryFrame = false;
    [JSONXIgnore()] public bool SaveAllFBOsEveryStageOfPipeline = false;

    //Debug Configs
    [JSONXIgnore()] public bool UseLang_RU = true;//russian
    [JSONXIgnore()] public bool UseLang_ZH = true;//Mandarin
    [JSONXIgnore()] public bool RenderDebug_ShowNormals = true;
    [JSONXIgnore()] public bool RenderDebug_ShowTangents = true;
    [JSONXIgnore()] public bool RenderDebug_ShowOrigins = true;

    //Basic configs
    public string UserSavePath = "";
    public int AutoSaveTimeoutSeconds = 5;
    public bool LogErrors = true;
    public bool ShaderCaching = true; //cache shader binaries on disk.
    public int MaxCharactersPerBitmap = 2048;//may not need this, MaxFontBitmapSize seems better
    public int MaxBakedCharSize = 64;
    public int MaxFontBitmapSize = 4096;
    public bool EnableMSAA = false;
    public int MSAASamples = 4;
    public int ShadowMapResolution = 1024;
    public bool EnableDebugErrorChecking = true;
    public bool EnableRuntimeErrorChecking = true;
    public bool BreakOnGraphicsError = true;
    public bool ShowGuiBoxesAndDisableClipping = false;
    public bool SaveSTBFontImage = true; //saves raw generated font images
    public bool StartInEditMode = true;//Set this to false to start the engine in game mode.
    public int WindowInitX = 200;
    public int WindowInitY = 200;
    public int WindowInitW = 1920;
    public int WindowInitH = 1080;
    public float WindowInitScaleW = 0.75f;
    public float WindowInitScaleH = 0.75f;
    [JSONXIgnore()] public int MaxUndoHistoryItems = 256;
    [JSONXIgnore()] public bool GraphicsErrorLogging_High = true;
    [JSONXIgnore()] public bool GraphicsErrorLogging_Medium = true;
    [JSONXIgnore()] public bool GraphicsErrorLogging_Low = true;
    [JSONXIgnore()] public bool GraphicsErrorLogging_Info = true;
    [JSONXIgnore()] public bool Renderer_UseAlias = true;//-1;//320;//-1=Disable, 640, 320
    [JSONXIgnore()] public int AliasScreenWidthPixels = 320;//-1;//320;//-1=Disable, 640, 320
    [JSONXIgnore()] public bool ReleaseAllButtonsWhenWindowLosesFocus = true;

    public EngineConfig(FileLoc loc)
    {
      Gu.Log.Info($"Loading Engine config {loc.QualifiedPath}");

      JSONXFile jsf = new JSONXFile(loc);
      jsf.Load();
      jsf.FillOutClass(this);

      //TODO: for multiple fonts figure out how to deserialize a class from this data

      if (jsf.PrintErrors())
      {
        Gu.DebugBreak();
      }
      //Sanitize
      if (AutoSaveTimeoutSeconds < 1)
      {
        AutoSaveTimeoutSeconds = 1;
        Gu.Log.Info("AutoSaveTimeout was invalid, setting to 1");
      }


    }

  }
}
