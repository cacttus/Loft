using System;
namespace PirateCraft
{
  public class EngineConfig
  {
    public bool LogErrors = true;
    public bool BreakOnOpenGLError = true;
    public int MaxBakedCharSize = 64;
    public int MaxFontBitmapSize = 4096;
    public bool MSAA = false;
    public int MSAASamples = 4;
    public int ShadowMapResolution = 1024;
    public bool EnableDebugErrorChecking = true;
    public bool EnableRuntimeErrorChecking = true;
    public bool BreakOnGraphicsError = true;
    public bool ShowGuiBoxesAndDisableClipping = false;
    public bool SaveSTBFontImage = true; //saves raw generated font images
    public EngineConfig()
    {
    }
  }
}
