using System;
using Newtonsoft.Json;
namespace PirateCraft
{
  public class EngineConfig
  {
    //SUPER DEBUG stuff
    public bool ClearCacheOnStart = true;
    public bool LogErrors = true;
    public bool BreakOnOpenGLError = true;
    public bool AlwaysCompileAndReloadGpuUniformData = true; //Don't try to optimize out modification, and always compile the structs (for debugging)
    public bool SaveFBOsEveryFrame = false;
    public bool SaveAllFBOsEveryStageOfPipeline = false;

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
      //TODO:
    }
  }
}
