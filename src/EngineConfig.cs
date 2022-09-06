using System;
using Newtonsoft.Json;
namespace PirateCraft
{
  public class EngineConfig
  {
    //SUPER DEBUG stuff
    public bool ClearCacheOnStart = false;//Setting this true may make the thing SLOW
    public bool ClearTmpOnStart = true;//logs..debug..
    public bool LogErrors = true;
    public bool BreakOnOpenGLError = true;
    public bool Debug_AlwaysCompileAndReloadGpuUniformData = true; //Don't try to optimize out modification, and always compile the structs (for debugging)
    public bool Debug_Print_Shader_Uniform_Details_Verbose = false; //So this is to be turned on for debug
    public bool Debug_ShowPipelineClearMessage = false;
    public bool SaveFBOsEveryFrame = false;
    public bool SaveAllFBOsEveryStageOfPipeline = false;

    public bool ShaderCaching = true; //cache shader binaries on disk.
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

    public EngineConfig()
    {
    }
    public void Load()
    {
      //TODO: this is all hard coded now. We can easily load from a file if needed.
    }
  }
}
