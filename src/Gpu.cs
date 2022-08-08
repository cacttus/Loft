using OpenTK.Graphics.OpenGL4;

using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;

namespace PirateCraft
{
  public class GpuRenderState
  {
    //State switches to prevent unnecessary gpu context changes.
    private bool _depthTestEnabledLast = false;
    private bool _depthTestEnabled = true;
    private bool _cullFaceEnabledLast = false;
    private bool _cullFaceEnabled = true;
    private bool _scissorTestEnabledLast = false;
    private bool _scissorTestEnabled = true;
    private bool _blendEnabledLast = false;
    private bool _blendEnabled = false;
    private BlendEquationMode _blendFuncLast = BlendEquationMode.FuncAdd;
    private BlendEquationMode _blendFunc = BlendEquationMode.FuncAdd;
    private BlendingFactor _blendFactorLast = BlendingFactor.OneMinusSrcAlpha;
    private BlendingFactor _blendFactor = BlendingFactor.OneMinusSrcAlpha;

    public GpuRenderState Clone()
    {
      GpuRenderState clone = new GpuRenderState();
      clone._depthTestEnabledLast = _depthTestEnabledLast;
      clone._depthTestEnabled = _depthTestEnabled;
      clone._cullFaceEnabledLast = _cullFaceEnabledLast;
      clone._cullFaceEnabled = _cullFaceEnabled;
      clone._scissorTestEnabledLast = _scissorTestEnabledLast;
      clone._scissorTestEnabled = _scissorTestEnabled;
      clone._blendEnabled = _blendEnabled;
      clone._blendFunc = _blendFunc;
      clone._blendFactor = _blendFactor;
      return clone;
    }

    public bool CullFace { get { return _cullFaceEnabled; } set { _cullFaceEnabledLast = _cullFaceEnabled; _cullFaceEnabled = value; } }
    public bool DepthTest { get { return _depthTestEnabled; } set { _depthTestEnabledLast = _depthTestEnabled; _depthTestEnabled = value; } }
    public bool ScissorTest { get { return _scissorTestEnabled; } set { _scissorTestEnabledLast = _scissorTestEnabled; _scissorTestEnabled = value; } }
    public bool Blend { get { return _blendEnabled; } set { _blendEnabledLast = _blendEnabled; _blendEnabled = value; } }
    //public BlendEquationMode BlendFunc { get { return _blendFunc; } set { _blendFuncLast = _blendFunc; _blendFunc = value; } }
    //public BlendingFactor BlendingFactor { get { return _blendFactor; } set { _blendFactorLast = _blendFactor;  _blendFactor = value; } }

    public void SetState()
    {
      if (_blendEnabled != _blendEnabledLast)
      {
        if (_blendEnabled)
        {
          GL.Enable(EnableCap.Blend);
          //Just default to basic blending for now
          GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }
        else
        {
          GL.Disable(EnableCap.Blend);
        }
      }
      //if (_blendFunc != _blendFuncLast)
      //{
      //   if (_blendEnabled)
      //   {
      //      GL.Enable(EnableCap.Blend);
      //   }
      //   else
      //   {
      //      GL.Disable(EnableCap.Blend);
      //   }
      //}
      if (_depthTestEnabled != _depthTestEnabledLast)
      {
        if (_depthTestEnabled)
        {
          GL.Enable(EnableCap.DepthTest);
        }
        else
        {
          GL.Disable(EnableCap.DepthTest);
        }
      }
      if (_scissorTestEnabled != _scissorTestEnabledLast)
      {
        if (_scissorTestEnabled)
        {
          GL.Enable(EnableCap.ScissorTest);
        }
        else
        {
          GL.Disable(EnableCap.ScissorTest);
        }
      }
      if (_cullFaceEnabled != _cullFaceEnabledLast)
      {
        if (_cullFaceEnabled)
        {
          GL.Enable(EnableCap.CullFace);
        }
        else
        {
          GL.Disable(EnableCap.CullFace);
        }
      }
    }
  }
  public class GPULog
  {
    bool _bPrintingGPULog = false;
    int _maxMsgLen = -1;

    public GPULog()
    {
      GL.Enable(EnableCap.DebugOutput);
      GL.Enable(EnableCap.DebugOutputSynchronous);
    }

    public bool chkErrRt(bool bDoNotBreak, bool doNotLog, string shaderName, bool clearOnly)
    {
      if (Gu.EngineConfig.EnableRuntimeErrorChecking == true)
      {
        return handleErrors(true, bDoNotBreak, doNotLog, shaderName, clearOnly);
      }
      return false;
    }
    public bool chkErrDbg(bool bDoNotBreak, bool doNotLog, string shaderName, bool clearOnly)
    {
      if (Gu.EngineConfig.EnableDebugErrorChecking == true)
      {
        return handleErrors(true, bDoNotBreak, doNotLog, shaderName, clearOnly);
      }
      return false;
    }
    public void clearGPULog()
    {
      printAndFlushGpuLog(true, true, true, "", true);
    }

    #region Private OGLERR

    private enum GpuLogLevel
    {
      Err_,
      Wrn_,
      Inf_,
      Dbg_
    }
    private static string glErrToStr(ErrorCode err)
    {
      switch (err)
      {
        case ErrorCode.NoError:
          return "GL_NO_ERROR         ";
        case ErrorCode.InvalidEnum:
          return "GL_INVALID_ENUM     ";
        case ErrorCode.InvalidValue:
          return "GL_INVALID_VALUE    ";
        case ErrorCode.InvalidOperation:
          return "GL_INVALID_OPERATION";
        case (ErrorCode)0x0503:
          return "GL_STACK_OVERFLOW   ";
        case (ErrorCode)0x0504:
          return "GL_STACK_UNDERFLOW  ";
        case ErrorCode.OutOfMemory:
          return "GL_OUT_OF_MEMORY    ";
      }
      return " *GL Error code not recognized.";
    }
    private bool handleErrors(bool bShowNote, bool bDoNotBreak, bool doNotLog, string shaderName, bool clearOnly)
    {
      //SDLUtils::checkSDLErr(doNotLog || !clearOnly, clearOnly);

      //ErrorCode err = GL.GetError();
      //if (err != ErrorCode.NoError)
      //{
      //  int n = 0;
      //  n++;
      //}
      printAndFlushGpuLog(true, bDoNotBreak, doNotLog, shaderName, clearOnly);

      return checkOglErr(bShowNote, bDoNotBreak || clearOnly, doNotLog || !clearOnly, shaderName);
    }
    private bool checkOglErr(bool bShowNote, bool bDoNotBreak, bool doNotLog, string shaderName)
    {
      bool bError = false;

      //GPU Log -
      // This isn't the Application log it's the hardware log on the card.
      ErrorCode err = GL.GetError();
      if (err != ErrorCode.NoError)
      {
        if (doNotLog == false)
        {
          string errmsg = "GL Error: " + glErrToStr(err) + " (" + (int)err + ")";
          if (!string.IsNullOrEmpty(shaderName))
          {
            errmsg += Environment.NewLine + " -> shader: " + shaderName;
          }
          errmsg += Gpu.debugGetRenderState();
          Gu.Log.Error(errmsg);
        }

        if (Gu.EngineConfig.BreakOnGraphicsError == true)
        {
          if (bDoNotBreak == false)
          {
            Gu.DebugBreak();
          }
        }
        bError = true;
      }

      return bError;
    }
    private void printAndFlushGpuLog(bool bShowNote, bool bDoNotBreak, bool doNotLog, string shaderName, bool clearOnly)
    {
      if (_bPrintingGPULog)
      {
        //Prevent recursion.
        return;
      }
      _bPrintingGPULog = true;
      {
        printAndFlushGpuLog_Notrap(bShowNote, bDoNotBreak, doNotLog, shaderName, clearOnly);
      }
      _bPrintingGPULog = false;
    }
    private void printAndFlushGpuLog_Notrap(bool bShowNote, bool bDoNotBreak, bool doNotLog, string shaderName, bool clearOnly)
    {
      //Enable this in engine.cpp glEnable(GL_DEBUG_OUTPUT);
      //if (ctx == nullptr)
      //{
      //  BRLogWarn("Context not initialized (context isseu");
      //  return;
      //}
      //if (!ctx->glGetDebugMessageLog)
      //{
      //  BRLogWarn("Opengl log not initialized (context isseu");
      //  return;
      //}

      int numMsgs = 1;
      int numFound;

      if (_maxMsgLen == -1)
      {
        _maxMsgLen = GL.GetInteger((GetPName)0x9143 /*GL_MAX_DEBUG_MESSAGE_LENGTH*/);
      }
      if (_maxMsgLen <= 0)
      {
        Gu.Log.Error("GL_MAX_DEBUG_MESSAGE_LENGTH returned 0.");
        _maxMsgLen = -2;
        return;
      }

      bool graphicsLogHigh = true;// Gu::getEngineConfig()->getGraphicsErrorLogging_High();
      bool graphicsLogMed = true;//:getEngineConfig()->getGraphicsErrorLogging_Medium();
      bool graphicsLogLow = true;//:getEngineConfig()->getGraphicsErrorLogging_Low();
      bool graphicsLogInfo = true;//:getEngineConfig()->getGraphicsErrorLogging_Info();

      do
      {
        //char[] msgData = new char[numMsgs * _maxMsgLen];
        DebugSource[] sources = new DebugSource[numMsgs];
        DebugType[] types = new DebugType[numMsgs];
        DebugSeverity[] severities = new DebugSeverity[numMsgs];
        int[] ids = new int[numMsgs];
        int[] lengths = new int[numMsgs];

        //unsafe
       // {
          //Yeah i don't know what's up with opentk here.
          string msgcopy = "";

          numFound = GL.GetDebugMessageLog(numMsgs, numMsgs * _maxMsgLen, sources, types, ids, severities, lengths, out msgcopy);
          //fixed (char* ptr = msgcopy)
          //{
          //  for (int x = 0; x < msgData.Length; x++)
          //  {
          //    msgData[x] = *(ptr + x);
          //  }
          //}

       // }//

        if (numFound == 0)
        {
          return;
        }
        if (clearOnly)
        {
          continue;  //clear messages.
        }

        Array.Resize(ref sources, numFound);
        Array.Resize(ref types, numFound);
        Array.Resize(ref severities, numFound);
        Array.Resize(ref ids, numFound);
        Array.Resize(ref lengths, numFound);


        int currPos = 0;
        for (int iMsg = 0; iMsg < lengths.Length; ++iMsg)
        {
          int id = ids[iMsg];
          if (!skipNVIDIA(id) && !skipATI(id))
          {
            //string strMsg = "";
            //for (int xx = currPos; xx < lengths[iMsg] - 1; xx++)
            //{
            //  strMsg += msgData[xx];  //who care man
            //}
            //stromg strMsg = msgcopy;
            DebugSeverity severity = severities[iMsg];
            DebugType type = types[iMsg];
            DebugSource source = sources[iMsg];
            logGPUMessageText(msgcopy, id, shaderName, doNotLog, severity, type, source, graphicsLogHigh, graphicsLogMed, graphicsLogLow, graphicsLogInfo);
          }
          currPos = currPos + lengths[iMsg];
        }

      } while (numFound > 0);
    }
    private void logGPUMessageText(string cstrMsg, int msgId, string shaderName, bool doNotLog, DebugSeverity severity, DebugType type,
      DebugSource source, bool graphicsLogHigh, bool graphicsLogMed, bool graphicsLogLow, bool graphicsLogInfo)
    {
      string msg = "";
      string shaderMsg = "";

      if (!String.IsNullOrEmpty(shaderName))
      {
        shaderMsg = " -> shader: " + shaderName;
      }
      if (doNotLog == false)
      {
        string strId = "[id=" + msgId.ToString("X") + "]";

        //Skip if the config.xml has turned off this kind of logging.
        if (severity == DebugSeverity.DebugSeverityHigh && graphicsLogHigh == false)
        {
          return;
        }
        else if (severity == DebugSeverity.DebugSeverityMedium && graphicsLogMed == false)
        {
          return;
        }
        else if (severity == DebugSeverity.DebugSeverityLow && graphicsLogLow == false)
        {
          return;
        }
        else if (severity == DebugSeverity.DebugSeverityNotification && graphicsLogInfo == false)
        {
          return;
        }

        string strSev = "";
        string strType = "";
        string strSource = "";
        GpuLogLevel level = GpuLogLevel.Dbg_;
        getTypeSevSourceLevel(type, severity, source, ref strType, ref strSev, ref strSource, ref level);

        //Prevent infinite recursion to dump the rendering state.
        string strStackInfo = "";
        string strRenderState = "";
        //static bool _bPrintingGPULog = false;
        //if (_bPrintingGPULog == false)
        {
          _bPrintingGPULog = true;
          //This isn't necessary. We can just add it above. what's happening is calling renderstate() resets the glError.
          // Also the GL Error automatically resets.
          strRenderState = (severity == DebugSeverity.DebugSeverityNotification) ? "" : Gpu.debugGetRenderState(true, false, false);
          strStackInfo = "";//(type == GL_DEBUG_TYPE_ERROR || type == GL_DEBUG_SEVERITY_NOTIFICATION) ? "" : DebugHelper::getStackTrace();  //error prints stack.
          _bPrintingGPULog = false;
        }
        //else
        //{
        //  strRenderState = " RenderState: Gpu Log is currently in recursive call, no information can be displayed.";
        //  strStackInfo = " Stack: Gpu Log is currently in recursive call, no information can be displayed.";
        //}

        msg = "GPU_LOG_MSG" + strId + strType + strSev + strSource + Environment.NewLine +
              shaderMsg + Environment.NewLine +
              " MSG ID: " + strId + Environment.NewLine +
              " Msg: " + cstrMsg + Environment.NewLine +
              " Render: " + Environment.NewLine + strStackInfo + Environment.NewLine +
              strRenderState;

        if (type == DebugType.DebugTypeError)
        {
          Gu.Log.Error(msg);
        }
        else if (severity == DebugSeverity.DebugSeverityNotification)
        {
          Gu.Log.Info(msg);
        }
        else
        {
          Gu.Log.Warn(msg);
        }
      }
    }
    private bool skipNVIDIA(int id)
    {
      //NVidia - redundant messages / infos
      return id == 0x00020071     // GL_DYANMIC_DRAW or GL_STATIC_DRAW memory usgae
             || id == 0x00020084  // Texture state usage warning: Texture 0 is base level inconsistent. Check texture size.
                                  // else if (id == 0x00020061) {
                                  //   return true;
                                  // }  // Framebuffer detailed info: The driver allocated storage for renderbuffer 1.
                                  // else if (id == 0x00020004) {
                                  //   return true;
                                  // }  // Usage warning: Generic vertex attribute array ... uses a pointer with a small value (...). Is this intended to be used as an offset into a buffer object?
                                  // else if (id == 0x00020072) {
                                  //   return true;
                                  // }  // Buffer performance warning: Buffer object ... (bound to ..., usage hint is GL_STATIC_DRAW) is being copied/moved from VIDEO memory to HOST memory.
                                  // else if (id == 0x00020074) {
                                  //   return true;
                                  // }  // Buffer usage warning: Analysis of buffer object ... (bound to ...) usage indicates that the GPU is the primary producer and consumer of data for this buffer object.  The usage hint s upplied with this buffer object, GL_STATIC_DRAW, is inconsistent with this usage pattern.  Try using GL_STREAM_COPY_ARB, GL_STATIC_COPY_ARB, or GL_DYNAMIC_COPY_ARB instead.
                                  // else if (id == 0x00020070) {
                                  //   return true;
                                  // }  // Total VBO Usage in the system... (Useful information)
                                  // else if (id == 0x00020043) {
                                  //   return true;
                                  // }  // A non-Fullscreen clear caused a fallback from CSAA to MSAA; - probolem in clearing cube shadow buffers
                                  //Other (mom's house) driver
                                  // else if (id == 0x07) {
                                  //   return true;
                                  // }  // glLineWidth Deprecated (other driver)

          ;

      return false;
    }
    private bool skipATI(int id)
    {
      return false;
    }
    private static void getTypeSevSourceLevel(DebugType type, DebugSeverity severity, DebugSource source, ref string strType, ref string strSev, ref string strSource, ref GpuLogLevel level)
    {
      if (type == DebugType.DebugTypeError)
      {
        strType = "[type=ERROR]";
      }
      else if (type == DebugType.DebugTypeDeprecatedBehavior)
      {
        strType = "[type=DEPRECATED_BEHAVIOR]";
      }
      else if (type == DebugType.DebugTypeUndefinedBehavior)
      {
        strType = "[type=UNDEFINED_BEHAVIOR]";
      }
      else if (type == DebugType.DebugTypePortability)
      {
        strType = "[type=PORTABILITY]";
      }
      else if (type == DebugType.DebugTypePerformance)
      {
        strType = "[type=PERFORMANCE]";
      }
      else if (type == DebugType.DebugTypeOther)
      {
        strType = "[type=OTHER]";
      }
      else
      {
        strType = "[type=(undefined(" + type + "))]";
      }

      if (severity == DebugSeverity.DebugSeverityHigh)
      {
        strSev = "[severity=HIGH]";
        level = GpuLogLevel.Err_;
      }
      else if (severity == DebugSeverity.DebugSeverityMedium)
      {
        strSev = "[severity=MEDIUM]";
        level = GpuLogLevel.Wrn_;
      }
      else if (severity == DebugSeverity.DebugSeverityLow)
      {
        strSev = "[severity=LOW]";
        level = GpuLogLevel.Inf_;
      }
      else if (severity == DebugSeverity.DebugSeverityNotification)
      {
        strSev = "[severity=NOTIFICATION]";
        level = GpuLogLevel.Inf_;
      }
      else
      {
        strSev = "[severity=(undefined(" + severity + ")))]";
        level = GpuLogLevel.Inf_;
      }

      if (source == DebugSource.DebugSourceApi)
      {
        strSource = "[source=API]";
      }
      else if (source == DebugSource.DebugSourceWindowSystem)
      {
        strSource = "[source=WINDOW_SYSTEM]";
      }
      else if (source == DebugSource.DebugSourceShaderCompiler)
      {
        strSource = "[source=SHADER_COMPILER]";
      }
      else if (source == DebugSource.DebugSourceThirdParty)
      {
        strSource = "[source=THIRD_PARTY]";
      }
      else if (source == DebugSource.DebugSourceApplication)
      {
        strSource = "[source=APPLICATION]";
      }
      else if (source == DebugSource.DebugSourceOther)
      {
        strSource = "[source=OTHER]";
      }
    }

    #endregion

  }

  //This instance must be per-context.
  public class Gpu
  {
    private Dictionary<WindowContext, List<Action<WindowContext>>> RenderThreadActions = new Dictionary<WindowContext, List<Action<WindowContext>>>();
    private int _maxTextureSize = 1;
    //  public GpuRenderState GpuRenderState { get; set; } = new GpuRenderState();
    public int RenderThreadID { get; private set; } = -1;

    public Gpu()
    {
      //Initializes gpu info
      int[] maxTextureSize = new int[2];
      GL.GetInteger(GetPName.MaxTextureSize, maxTextureSize);
      _maxTextureSize = maxTextureSize[0];
      RenderThreadID = Thread.CurrentThread.ManagedThreadId;
    }
    public int GetMaxTextureSize()
    {
      return _maxTextureSize;
    }
    public static GpuDataPtr GetGpuDataPtr<T>(T[] data)
    {
      //takes an array of vertexes and marshals them for copying to the GPU
      GpuDataPtr p = null;
      if (data.Length == 0)
      {
        p = new GpuDataPtr(0, data.Length, data);
      }
      else
      {
        var size = Marshal.SizeOf(data[0]);
        p = new GpuDataPtr(size, data.Length, data);
      }
      return p;
    }
    public static unsafe T ByteArrayToStructure<T>(byte[] bytes) where T : struct
    {
      fixed (byte* ptr = &bytes[0])
      {
        return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
      }
    }
    public static GpuDataArray SerializeGpuData<T>(T[] data) where T : struct
    {
      var size = Marshal.SizeOf(data[0]);

      var bytes = new byte[size * data.Length];
      var ptr = Marshal.AllocHGlobal(size);
      for (int di = 0; di < data.Length; di++)
      {
        Marshal.StructureToPtr(data[di], ptr, false);
        Marshal.Copy(ptr, bytes, di * size, size);
      }
      Marshal.FreeHGlobal(ptr);
      GpuDataArray arr = new GpuDataArray(size, data.Length, bytes);

      return arr;
    }
    public void Post_To_RenderThread(WindowContext wc, Action<WindowContext> a)
    {
      //This is super important for disposing Render (opengl) stuff.
      //Posts this operation to a render thread to cleanup OpenGL stuff.
      //This is also for any async call that requires render thread synchronization.
      //These get executed after literally all rendering (right now)
      lock (RenderThreadActions)
      {
        //Register an action to delete GPU memory on the main thread.
        //This is for C# finalizers (called on the GC thread)
        List<Action<WindowContext>> actions = null;
        if (!RenderThreadActions.TryGetValue(wc, out actions))
        {
          RenderThreadActions.Add(wc, new List<Action<WindowContext>> { a });
        }
        else
        {
          actions.Add(a);
        }
      }
    }
    public void ExecuteCallbacks_RenderThread(WindowContext wc)
    {
      List<Action<WindowContext>> actions_cpy = null;
      lock (RenderThreadActions)
      {
        RenderThreadActions.TryGetValue(wc, out actions_cpy);
        RenderThreadActions.Remove(wc);
      }
      if (actions_cpy != null)
      {
        //Call this at the end of render thread (or beginning)
        foreach (var action in actions_cpy)
        {
          action(wc);
        }
        actions_cpy.Clear();
      }
    }
    private static GPULog GPULog = new GPULog();
    public static void CheckGpuErrorsRt(bool donotbreak = false, bool donotlog = false, string shadername = "", bool clearonly = false)
    {
      GPULog.chkErrRt(donotbreak, donotlog, shadername, clearonly);
    }
    public static void CheckGpuErrorsDbg(bool donotbreak = false, bool donotlog = false, string shadername = "", bool clearonly = false)
    {
#if DEBUG
      GPULog.chkErrDbg(donotbreak, donotlog, shadername, clearonly);
      //CheckGpuErrorsRt();
#endif
    }
    public static Img32 GetTextureDataFromGpu(int iGLTexId, TextureTarget eTexTargetBase, int iCubeMapSide = -1)
    {
      //Input image32 must be not nulll
      int iSavedTextureBinding;
      GetPName eTexBinding = texTargetToTexBindingQuery(eTexTargetBase);

      TextureTarget eTexTargetSide = eTexTargetBase;
      if (eTexTargetBase == TextureTarget.TextureCubeMap)
      {
        Gu.Assert(iCubeMapSide >= 0 && iCubeMapSide < 6);
        eTexTargetSide = TextureTarget.TextureCubeMapPositiveX + iCubeMapSide;
      }

      iSavedTextureBinding = GL.GetInteger(eTexBinding);
      Gpu.CheckGpuErrorsRt();

      GL.ActiveTexture(TextureUnit.Texture0);
      GL.BindTexture(eTexTargetBase, iGLTexId);
      Gpu.CheckGpuErrorsRt();

      int w = 0, h = 0, tmp = 0, iMipLevel = 0;
      PixelInternalFormat internalFormat = PixelInternalFormat.Rgba;
      GL.GetTexLevelParameter(eTexTargetSide, iMipLevel, GetTextureParameter.TextureWidth, out w);
      Gpu.CheckGpuErrorsRt();
      GL.GetTexLevelParameter(eTexTargetSide, iMipLevel, GetTextureParameter.TextureHeight, out h);
      Gpu.CheckGpuErrorsRt();
      GL.GetTexLevelParameter(eTexTargetSide, iMipLevel, GetTextureParameter.TextureInternalFormat, out tmp);
      Gpu.CheckGpuErrorsRt();
      internalFormat = (PixelInternalFormat)tmp;

      PixelFormat calculatedFmt = PixelFormat.Rgba;
      PixelType calculatedType = PixelType.UnsignedByte;
      int bufsiz_bytes = 0;

      if (internalFormat == PixelInternalFormat.Rgba)
      {
        calculatedFmt = PixelFormat.Rgba;
        calculatedType = PixelType.UnsignedByte;
        bufsiz_bytes = w * h * 4;
      }
      else if (internalFormat == PixelInternalFormat.Rgba8)
      {
        calculatedFmt = PixelFormat.Rgba;
        calculatedType = PixelType.UnsignedByte;
        bufsiz_bytes = w * h * 4;
      }
      else if (internalFormat == PixelInternalFormat.Rgba32f)
      {  //All color buffers
        calculatedFmt = PixelFormat.Rgba;
        calculatedType = PixelType.UnsignedByte;
        bufsiz_bytes = w * h * 4;
      }
      else if (internalFormat == PixelInternalFormat.Rgba32ui)
      {  //Pick buffer
        calculatedFmt = PixelFormat.RedInteger;
        calculatedType = PixelType.UnsignedInt;
        bufsiz_bytes = w * h * 4 * 4;
      }
      else if (internalFormat == PixelInternalFormat.Rgba16f)
      {
        calculatedFmt = PixelFormat.Rgba;
        calculatedType = PixelType.UnsignedByte;
        bufsiz_bytes = w * h * 4;
      }
      else if (internalFormat == PixelInternalFormat.R32f)
      {
        /*
        https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glGetTexImage.xhtml
        If the selected texture image does not contain four components, the following mappings are applied.
        Single-component textures are treated as RGBA buffers with red set to the single-component value,
        green set to 0, blue set to 0, and alpha set to 1.
        */
        calculatedFmt = PixelFormat.RedInteger;
        calculatedType = PixelType.Float;// valid ?
        bufsiz_bytes = w * h * 4;
      }
      else if (internalFormat == PixelInternalFormat.R16f)
      {
        calculatedFmt = PixelFormat.RedInteger; // ? Look at r32ui
        calculatedType = PixelType.Float; // valid ?
        bufsiz_bytes = w * h * 4;
      }
      else if (internalFormat == PixelInternalFormat.R32ui)
      {
        calculatedFmt = PixelFormat.RedInteger;
        calculatedType = PixelType.UnsignedInt;
        bufsiz_bytes = w * h * 4;//4 for ui? 
      }
      else
      {
        Gu.Log.Error("Invalid or Unsupported texture internal format when reading from GPU" + (int)internalFormat);
        Gu.DebugBreak();
      }

      if (false)
      {
        //Print all image values as floats (tests to see if buffer was written to
        byte[] ts = new byte[w * h * 4];
        int iNonZero = 0;
        byte lastr, lastg, lastb, lasta;
        var handle2 = GCHandle.Alloc(ts, GCHandleType.Pinned);
        GL.GetTexImage(eTexTargetSide, iMipLevel, calculatedFmt, calculatedType, handle2.AddrOfPinnedObject());
        handle2.Free();
        for (int ih = 0; ih < h; ++ih)
        {
          for (int iw = 0; iw < w; ++iw)
          {
            float r = ts[ih * (w * 4) + iw * 4 + 0];
            float g = ts[ih * (w * 4) + iw * 4 + 1];
            float b = ts[ih * (w * 4) + iw * 4 + 2];
            float a = ts[ih * (w * 4) + iw * 4 + 3];
            if (lastr != r || lastg != g || lastb != b || lasta != a)
            {
              Console.Write(" ,(" + r + "," + g + "," + b + "," + a + ")");
              iNonZero++;
            }
            lastr = (byte)r;
            lastg = (byte)g;
            lastb = (byte)b;
            lasta = (byte)a;
          }
        }
        int nnn = 0;
        nnn++;
      }

      // char* buf = new char[bufsiz_bytes];
      // glReadPixels(0, 0, w, h, GL_RGBA, GL_UNSIGNED_BYTE, (GLvoid*)bi.getData()->ptr());
      //glGetTexImage(GL_TEXTURE_2D, iMipLevel, GL_RGBA, GL_UNSIGNED_BYTE, (GLvoid*)bi.getData()->ptr());
      Img32 image = new Img32(w, h, null, Img32.PixelFormat.RGBA);
      var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
      GL.GetTexImage(eTexTargetSide, iMipLevel, calculatedFmt, calculatedType, handle.AddrOfPinnedObject());
      handle.Free();
      Gpu.CheckGpuErrorsRt();

      GL.BindTexture(eTexTargetBase, iSavedTextureBinding);
      Gpu.CheckGpuErrorsRt();
      return image;
    }
    private static GetPName texTargetToTexBindingQuery(TextureTarget target)
    {
      if (target == TextureTarget.Texture1D)
      {
        return GetPName.TextureBinding1D;
      }
      else if (target == TextureTarget.Texture2D)
      {
        return GetPName.TextureBinding2D;
      }
      else if (target == TextureTarget.Texture3D)
      {
        return GetPName.TextureBinding3D;
      }
      else if (target == TextureTarget.TextureRectangle)
      {
        return GetPName.TextureBindingRectangle;
      }
      else if (target == TextureTarget.TextureBuffer)
      {
        return GetPName.TextureBindingBuffer;
      }
      else if (target == TextureTarget.TextureCubeMap)
      {
        return GetPName.TextureBindingCubeMap;
      }
      else if (target == TextureTarget.Texture1DArray)
      {
        return GetPName.TextureBinding1DArray;
      }
      else if (target == TextureTarget.Texture2DArray)
      {
        return GetPName.TextureBinding2DArray;
      }
      else if (target == TextureTarget.TextureCubeMapArray)
      {
        Gu.BRThrowException("OpenTK doesn't have this parameter GL_TEXTURE_BINDING_CUBE_MAP_ARRAY");
        return GetPName.TextureBindingCubeMap;
      }
      else if (target == TextureTarget.Texture2DMultisample)
      {
        return GetPName.TextureBinding2DMultisample;
      }
      else if (target == TextureTarget.Texture2DMultisampleArray)
      {
        return GetPName.TextureBinding2DMultisampleArray;
      }
      return 0;
    }

    public static GPUBuffer CreateBuffer<T>(BufferTarget t, T[] verts)
    {
      var fmt = VertexFormat.GetVertexFormat<T>();
      GPUBuffer b = new GPUBuffer(fmt, t, Gpu.GetGpuDataPtr(verts));
      return b;
    }
    public static GPUBuffer CreateVertexBuffer<T>(T[] verts)
    {
      return CreateBuffer(BufferTarget.ArrayBuffer, verts);
    }
    public static GPUBuffer CreateIndexBuffer<T>(T[] inds)
    {
      return CreateBuffer(BufferTarget.ElementArrayBuffer, inds);
    }

    public static string debugGetRenderState(bool x = true, bool y = true, bool z = true)//udmmies
    {
      //TODO:
      return "todo: renderstate";
    }




  }
}
