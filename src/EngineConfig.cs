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
    [JSONXIgnore()] public bool Debug_Print_Shader_Uniform_Details_Verbose = false; //So this is to be turned on for debug
    [JSONXIgnore()] public bool Debug_ShowPipelineClearMessage = false;
    [JSONXIgnore()] public bool Debug_CheckSortedLists_Slow = true;
    [JSONXIgnore()] public bool SaveFBOsEveryFrame = false;
    [JSONXIgnore()] public bool SaveAllFBOsEveryStageOfPipeline = false;

    //Debug Configs
    [JSONXIgnore()] public bool UseLang_RU = true;//russian
    [JSONXIgnore()] public bool UseLang_ZH = true;//Mandarin

    //Basic configs
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


    }

  }
}
