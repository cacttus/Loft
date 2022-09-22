using OpenTK.Graphics.OpenGL4;

using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using System.Text;

namespace PirateCraft
{
  public class GpuDataPtr
  {
    //these could be one class
    //Sizing information from GPU data, and object pointer destined for gpu
    private bool _locked = false;
    private GCHandle pinnedArray;
    private object _pt;

    public int ItemSizeBytes { get; private set; } = 0;
    public int Count { get; private set; } = 0;

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

    public GpuDataPtr(int itemSize, int count, object pt)
    {
      ItemSizeBytes = itemSize;
      Count = count;
      _pt = pt;
    }
    public IntPtr Lock()
    {
      _locked = true;
      pinnedArray = GCHandle.Alloc(_pt, GCHandleType.Pinned);
      return pinnedArray.AddrOfPinnedObject();
    }
    public void Unlock()
    {
      pinnedArray.Free();
      _locked = false;
    }
    ~GpuDataPtr()
    {
      if (_locked)
      {
        Gu.Log.Error("GpuDataPtr unmanaged handle wasn't freed. Must call Unlock().");
        Gu.DebugBreak();
      }
    }
  }
  public class GpuDataArray
  {
    //Raw byte data from or to the GPU, with sizing information.
    private bool _locked = false;
    private GCHandle pinnedArray;

    public byte[] Bytes { get; private set; } = null; // Managed Array

    public int ItemSizeBytes { get; private set; } = 0;
    public int Count { get; private set; } = 0;

    public GpuDataArray(int itemSize, int count, byte[] pt)
    {
      ItemSizeBytes = itemSize;
      Count = count;
      Bytes = pt;
    }
    public IntPtr Lock()
    {
      _locked = true;
      pinnedArray = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
      return pinnedArray.AddrOfPinnedObject();
    }
    public void Unlock()
    {
      pinnedArray.Free();
      _locked = false;
    }
    ~GpuDataArray()
    {
      if (_locked)
      {
        Gu.Log.Error("Gpu Data array unmanaged handle wasn't freed. Must call Unlock().");
        Gu.DebugBreak();
      }
    }
  }

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
      //if (_blendEnabled != _blendEnabledLast)
      {
        Gu.Assert(Gu.Context != null);
        Gu.Assert(Gu.Context.Renderer != null);
        Gu.Assert(Gu.Context.Renderer.CurrentStage != null);
        if (Gu.Context.Renderer.CurrentStage.OutputFramebuffer != null)
        {
          Gu.Context.Renderer.CurrentStage.OutputFramebuffer.EnableBlend(_blendEnabled);
        }
        else
        {
          if (_blendEnabled)
          {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
          }
          else
          {
            GL.Disable(EnableCap.Blend);
          }
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
          errmsg += GpuDebugInfo.DebugGetRenderState();
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
        string strId = " 0x" + msgId.ToString("X");

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
          strRenderState = (severity == DebugSeverity.DebugSeverityNotification) ? "" : GpuDebugInfo.DebugGetRenderState(true, false, false);
          strStackInfo = "";//(type ==GLenum.GL_DEBUG_TYPE_ERROR || type ==GLenum.GL_DEBUG_SEVERITY_NOTIFICATION) ? "" : DebugHelper::getStackTrace();  //error prints stack.
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
              " Render: " + Environment.NewLine + strStackInfo + Environment.NewLine;

        if (type == DebugType.DebugTypeError)
        {
          Gu.Log.Error(msg, strRenderState);
          Gu.DebugBreak();

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
    public static TextureUnit GetActiveTexture()
    {
      int tex_unit = 0;
      GL.GetInteger(GetPName.ActiveTexture, out tex_unit);
      return (TextureUnit)tex_unit;
    }
    public static unsafe T ByteArrayToStructure<T>(byte[] bytes) where T : struct
    {
      //TODO:Duplicate REsourceManager.Serialize / Deserialize is essentially the same thing.
      fixed (byte* ptr = &bytes[0])
      {
        return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
      }
    }
    // public static GpuDataArray SerializeGpuData<T>(T[] data) where T : struct
    // {
    //   //TODO:Duplicate REsourceManager.Serialize / Deserialize is essentially the same thing.
    //   var size = Marshal.SizeOf(data[0]);

    //   var bytes = new byte[size * data.Length];
    //   var ptr = Marshal.AllocHGlobal(size);
    //   for (int di = 0; di < data.Length; di++)
    //   {
    //     Marshal.StructureToPtr(data[di], ptr, false);
    //     Marshal.Copy(ptr, bytes, di * size, size);
    //   }
    //   Marshal.FreeHGlobal(ptr);
    //   GpuDataArray arr = new GpuDataArray(size, data.Length, bytes);

    //   return arr;
    // }
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
    public static bool CheckGpuErrorsRt(bool donotbreak = false, bool donotlog = false, string shadername = "", bool clearonly = false)
    {
      return GPULog.chkErrRt(donotbreak, donotlog, shadername, clearonly);
    }
    public static bool CheckGpuErrorsDbg(bool donotbreak = false, bool donotlog = false, string shadername = "", bool clearonly = false)
    {
#if DEBUG
      return GPULog.chkErrDbg(donotbreak, donotlog, shadername, clearonly);
#endif
    }
    public static Img32 GetTextureDataFromGpu(int iGLTexId, TextureTarget eTexTargetBase, ref PixelFormat outFormat, ref PixelType outType, ref PixelInternalFormat outInternalFormat, int iCubeMapSide = -1)
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
        bufsiz_bytes = w * h * 4 * 4;
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
        calculatedType = PixelType.UnsignedShort; // valid ?
        bufsiz_bytes = w * h * 2;
      }
      else if (internalFormat == PixelInternalFormat.R32ui)
      {
        calculatedFmt = PixelFormat.RedInteger;
        calculatedType = PixelType.UnsignedInt;
        bufsiz_bytes = w * h * 4;//4 for ui? 
      }
      else if (internalFormat == PixelInternalFormat.DepthComponent32f)
      {
        calculatedFmt = PixelFormat.DepthComponent;
        calculatedType = PixelType.Float;
        bufsiz_bytes = w * h * 4;//4 for ui? 
      }
      else if (internalFormat == PixelInternalFormat.DepthComponent24)
      {
        calculatedFmt = PixelFormat.DepthComponent;
        calculatedType = PixelType.Float;
        bufsiz_bytes = w * h * 4;//4 for ui? 
      }
      else if (internalFormat == PixelInternalFormat.DepthComponent16)
      {
        calculatedFmt = PixelFormat.DepthComponent;
        calculatedType = PixelType.Float; ;
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
      Img32 image = new Img32("GpuTexture", w, h, null, Img32.ImagePixelFormat.RGBA32ub);
      var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
      GL.GetTexImage(eTexTargetSide, iMipLevel, calculatedFmt, calculatedType, handle.AddrOfPinnedObject());
      handle.Free();
      Gpu.CheckGpuErrorsRt();

      GL.BindTexture(eTexTargetBase, iSavedTextureBinding);
      Gpu.CheckGpuErrorsRt();

      outFormat = calculatedFmt;
      outType = calculatedType;
      outInternalFormat = internalFormat;

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
    public static string GetObjectLabelWithId(ObjectLabelIdentifier idt, int id)
    {
      string label;
      int length;
      if (id == 0)
      {
        return " 0 (none)";
      }
      GL.GetObjectLabel(idt, id, 256, out length, out label);
      label += $" ({id})";
      return label;
    }
    //This is private now, because we have no need for it yet, but we may need it in the future to check against objects.
    private static string GetObjectLabel(ObjectLabelIdentifier idt, int id)
    {
      string label;
      int length;
      if (id == 0)
      {
        return " 0 (none)";
      }
      GL.GetObjectLabel(idt, id, 256, out length, out label);
      return label;
    }
    public static GPUBuffer CreateBuffer<T>(string name, BufferTarget t, T[] data)
    {
      int size = 0;
      int length = data.Length;
      if (data.Length == 0)
      {
        size = 0;
      }
      else
      {
        size = Marshal.SizeOf(data[0]);
      }

      var fmt = VertexFormat.GetVertexFormat<T>();
      GPUBuffer b = new GPUBuffer(name, fmt, t, size, length, (object)data);
      return b;
    }
    public static GPUBuffer CreateVertexBuffer<T>(string name, T[] verts)
    {
      return CreateBuffer(name + "-vertex", BufferTarget.ArrayBuffer, verts);
    }
    public static GPUBuffer CreateIndexBuffer<T>(string name, T[] inds)
    {
      return CreateBuffer(name + "-index", BufferTarget.ElementArrayBuffer, inds);
    }
  }//Gpu

  public class GpuDebugInfo
  {
    private static bool _bGettingRenderState = false;
    public static string DebugGetRenderState(bool bForceRun = false, bool bPrintToStdout = true, bool bSaveFramebufferTexture = false) //DebugGetGpuState
    {
      // This method is called in frames to drag down the debug arrow
      //  and we skip it unless we force it to run.
      // Do not comment
      if (!bForceRun)
      {
        return "";  // Do not comment
      }
      System.Text.StringBuilder strState = new StringBuilder();

      if (_bGettingRenderState == true)
      {
        return "Render State tried to be called recursively.";  // Prevent recursive calls.
      }
      _bGettingRenderState = true;

      // Gd::verifyRenderThread();//We must be in render thread

      var ct = Gu.Context;
      if (ct == null)
      {
        Gu.Log.Error("Context was null for DebugGetRenderState");
        return "";
      }

      strState.AppendLine($"");
      strState.AppendLine($"==============================================");
      strState.AppendLine($"=                RENDER STATE                =");
      strState.AppendLine($"==============================================");
      Gpu.CheckGpuErrorsRt();

      DebugPrintShaderLimits(strState);

      DebugGetLegacyViewAndMatrixStack(strState);
      Gpu.CheckGpuErrorsRt();
      DebugGetBufferState(strState);
      Gpu.CheckGpuErrorsRt();
      // debugGetAttribState(); // This is redundant with vertexarraystate
      //     CheckGpuErrorsDbg();
      DebugGetTextureState(strState);
      Gpu.CheckGpuErrorsRt();

      DebugGetVertexArrayState(strState);
      Gpu.CheckGpuErrorsRt();
      DebugGetFramebufferAttachmentState(strState);
      Gpu.CheckGpuErrorsRt();

      if (bPrintToStdout)
      {
        Gu.Log.Info(strState.ToString());
      }
      // if (bSaveFramebufferTexture) {
      //   string fname = FileSystem::getScreenshotFilename();
      //   saveFramebufferAsPng(std::move(fname));
      // }

      _bGettingRenderState = false;

      return strState.ToString();
    }

    #region Private

    private static void DebugPrintGLGetInteger(StringBuilder strState, GetPName pname)
    {
      //deosnt work in some cases due to dupes
      int val = 0;
      GL.GetInteger(GetPName.Blend, out val);
      strState.AppendLine(((GLenum)pname).Description() + ": " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
    }
    private static string EnabledString(int val)
    {
      return ((val > 0) ? ("Enabled") : ("Disabled"));
    }
    private static void DebugGetLegacyViewAndMatrixStack(StringBuilder strState)
    {
      int[] iScissorBox = new int[4];
      int[] iViewportBox = new int[4];
      strState.AppendLine("---------------- Legcay State ----------------");

      int val = 0;
      GL.GetInteger(GetPName.Blend, out val);
      strState.AppendLine("Blending: " + EnabledString(val));
      Gpu.CheckGpuErrorsRt();

      GL.GetInteger(GetPName.CullFace, out val);
      strState.AppendLine("Culling: " + EnabledString(val));
      Gpu.CheckGpuErrorsRt();

      GL.GetInteger(GetPName.CullFaceMode, out val);
      strState.AppendLine("CullMode: " + EnabledString(val));
      Gpu.CheckGpuErrorsRt();

      GL.GetInteger(GetPName.FrontFace, out val);
      strState.AppendLine("FrontFace: " + EnabledString(val));
      Gpu.CheckGpuErrorsRt();

      GL.GetInteger(GetPName.DepthTest, out val);
      strState.AppendLine("Depth Test: " + EnabledString(val));
      Gpu.CheckGpuErrorsRt();

      GL.GetInteger(GetPName.ScissorTest, out val);
      strState.AppendLine("Scissor Test: " + EnabledString(val));
      Gpu.CheckGpuErrorsRt();

      // View Bounds (Legacy)
      GL.GetInteger(GetPName.ScissorBox, iScissorBox);
      GL.GetInteger(GetPName.Viewport, iViewportBox);
      //GL.GetInteger(GL_SCISSOR_BOX, (int*)iScissorBox);
      //GL.GetInteger(GL_VIEWPORT, (int*)iViewportBox);
      strState.AppendLine("Scissor Box (GL 0,0=bl): " + iScissorBox[0] + "," + iScissorBox[1] + "," + iScissorBox[2] + "," + iScissorBox[3]);
      strState.AppendLine("Viewport Box (GL 0,0=bl): " + iViewportBox[0] + "," + iViewportBox[1] + "," + iViewportBox[2] + "," + iViewportBox[3]);

      GL.GetInteger(GetPName.MaxViewportDims, iViewportBox);
      strState.AppendLine("Viewport max dims: " + iViewportBox[0] + "," + iViewportBox[1]);


      // TODO: legacy matrix array state.
      Gpu.CheckGpuErrorsRt();
    }
    private static void DebugGetBufferState(StringBuilder strState)
    {
      strState.AppendLine("----------------SHADER STATE----------------");

      int iBoundBuffer;
      int iCurrentProgram;
      int iElementArrayBufferBinding;
      int iSsboBinding;  // shader storage
      int iVertexArrayBinding;

      GL.GetInteger(GetPName.ArrayBufferBinding, out iBoundBuffer);
      GL.GetInteger(GetPName.ElementArrayBufferBinding, out iElementArrayBufferBinding);
      //GL.GetInteger(GetPName.binding out iSsboBinding);
      GL.GetInteger(GetPName.VertexArrayBinding, out iVertexArrayBinding);
      GL.GetInteger(GetPName.CurrentProgram, out iCurrentProgram);
      Gpu.CheckGpuErrorsRt();

      strState.AppendLine("Bound Shader Program: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Program, iCurrentProgram));
      Gpu.CheckGpuErrorsRt();
      strState.AppendLine("Bound Vertex Array Buffer (VBO): " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Buffer, iBoundBuffer));
      Gpu.CheckGpuErrorsRt();
      strState.AppendLine("Bound Element Array Buffer (IBO): " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Buffer, iElementArrayBufferBinding));
      Gpu.CheckGpuErrorsRt();
      strState.AppendLine("Bound Shader Storage Buffer (SSBO): Not avialable in opentk?");
      // List<int> binds = new List<int>();
      // int iMaxUniformBindings;
      // GL.GetInteger(GetPName.MaxUniformBufferBindings, out iMaxUniformBindings);
      // for (int xxx = 0; xxx < iMaxUniformBindings; xxx++)
      // {
      //   int iUniformBufferBindingxx = 0;
      //   GL.GetInteger(GetIndexedPName.UniformBufferBinding, xxx, out iUniformBufferBindingxx);
      //   strState.AppendLine("Bound Uniform Buffer (UBO): " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Buffer, iUniformBufferBindingxx));
      // }
      Gpu.CheckGpuErrorsRt();
      strState.AppendLine("Bound Vertex Array Object (VAO): " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.VertexArray, iVertexArrayBinding));
      Gpu.CheckGpuErrorsRt();

      if (iCurrentProgram > 0)
      {
        DebugPrintActiveUniforms(iCurrentProgram, strState);
      }
    }
    private static void DebugPrintActiveUniforms(int iGlProgramId, StringBuilder strState)
    {
      int nUniforms;
      string uniformName;
      int name_len = -1;
      int iArraySize = -1;
      ActiveUniformType uniformType;
      int nActiveUniformBlocks;
      int nMaxUniformLocations;


      // - Get the number of uniforms
      GL.GetProgram(iGlProgramId, GetProgramParameterName.ActiveUniforms, out nUniforms);
      GL.GetProgram(iGlProgramId, GetProgramParameterName.ActiveUniformBlocks, out nActiveUniformBlocks);
      // GL.GetInteger(max uniform locations.., ref nMaxUniformLocations);

      //GL.GetInteger(GL_MAX_COMPUTE_UNIFORM_COMPONENTS, ref nMaxComponentsComp);
      Gpu.CheckGpuErrorsRt();

      strState.AppendLine("Active Uniform Blocks: " + nActiveUniformBlocks);

      strState.AppendLine("Active Uniforms (" + nUniforms + "): ");
      strState.AppendLine("  (Name, Type, Location, ArraySize)");

      // Get all uniform names and types into a list.
      for (Int32 i = 0; i < nUniforms; ++i)
      {
        // Get name an d type
        GL.GetActiveUniform(iGlProgramId, i, 256, out name_len, out iArraySize, out uniformType, out uniformName);

        // get location
        int glLocation = GL.GetUniformLocation(iGlProgramId, uniformName);

        strState.AppendLine(" " + uniformName + ", " + ((GLenum)uniformType).Description() + ", " + glLocation + ", " + iArraySize);

        // Uniform Block Data.
        Gpu.CheckGpuErrorsRt();

        int iCurrentBlockIdx;
        iCurrentBlockIdx = GL.GetUniformBlockIndex(iGlProgramId, uniformName);

        if (iCurrentBlockIdx != -1)
        {
          int iBlockBinding;
          int iBlockDataSize;
          int iBlockNameLength;
          int iBlockActiveUniforms;
          int iBlockActiveUniformIndices;

          Gpu.CheckGpuErrorsRt();
          GL.GetActiveUniformBlock(iGlProgramId, iCurrentBlockIdx, ActiveUniformBlockParameter.UniformBlockBinding, out iBlockBinding);
          Gpu.CheckGpuErrorsRt();
          GL.GetActiveUniformBlock(iGlProgramId, iCurrentBlockIdx, ActiveUniformBlockParameter.UniformBlockDataSize, out iBlockDataSize);
          Gpu.CheckGpuErrorsRt();
          GL.GetActiveUniformBlock(iGlProgramId, iCurrentBlockIdx, ActiveUniformBlockParameter.UniformBlockNameLength, out iBlockNameLength);
          Gpu.CheckGpuErrorsRt();
          GL.GetActiveUniformBlock(iGlProgramId, iCurrentBlockIdx, ActiveUniformBlockParameter.UniformBlockActiveUniforms, out iBlockActiveUniforms);
          Gpu.CheckGpuErrorsRt();
          GL.GetActiveUniformBlock(iGlProgramId, iCurrentBlockIdx, ActiveUniformBlockParameter.UniformBlockActiveUniformIndices, out iBlockActiveUniformIndices);
          Gpu.CheckGpuErrorsRt();

          strState.AppendLine("  Block Index: " + iCurrentBlockIdx);
          strState.AppendLine("  Block Binding: " + iBlockBinding);
          strState.AppendLine("  Block Data Size: " + iBlockDataSize);
          strState.AppendLine("  Block Name Length: " + iBlockNameLength);
          strState.AppendLine("  Block Active Uniforms: " + iBlockActiveUniforms);
          strState.AppendLine("  Block Active Uniform Indices: " + iBlockActiveUniformIndices);
        }

        // strState.AppendLine("  TODO: dump UBO buffer data");
        // Data
        // if (Gu::isManagerConstructed(ManagerType::ShaderMaker))
        // {
        //   // We can call this anywhere. SM is lazy initialized, so this may not be available.
        //   if (Gu::getShaderMaker()->getBound() != nullptr)
        //   {
        //     std::shared_ptr<ShaderUniform> uf = Gu::getShaderMaker()->getBound()->getUniformByName(uniformName);
        //     if (uf != nullptr)
        //     {
        //       strState.AppendLine(("  Buffer Data:"));
        //       if (uf->hasBeenSet() == false)
        //       {
        //         strState.AppendLine(("  not set."));
        //       }
        //       else
        //       {
        //         strState.AppendLine("  Text:" + (uf->debugGetUniformValueAsString(false)));
        //         strState.AppendLine("   Raw:" + (uf->debugGetUniformValueAsString(true)));
        //       }
        //     }
        //     else
        //     {
        //       strState.AppendLine("Uniform " + uniformName + " was not found.  It may be a uniform buffer.");
        //     }
        //   }
        // }
        // else
        // {
        //   strState.AppendLine(" Bound uniform Data not available. Shader manager has not been constructed yet.");
        // }
      }
    }
    private static void DebugGetAttribState(StringBuilder strState)
    {
      //// - print bound attributes
      // int iMaxAttribs;
      // int iBoundAttrib;
      // GL.GetInteger(GL_MAX_VERTEX_ATTRIBS,out iMaxAttribs);
      // std::cout<<"Attribs: max count = "<<iMaxAttribs<<std::endl;
      // for(int xx=0; xx<iMaxAttribs; ++xx)
      //{
      //     GL.GetInteger(GL_VERTEX_ATTRIB_ARRAY0_NV+xx,out iBoundAttrib);
      //     std::cout<<"attrib "<<xx<<": "<<iBoundAttrib<<std::endl;
      // }
    }
    private static void DebugGetTextureState(StringBuilder strState)
    {
      Gpu.CheckGpuErrorsRt();
      strState.AppendLine("----------------Texture State----------------");


      int iActiveTexture;
      GL.GetInteger(GetPName.ActiveTexture, out iActiveTexture);  // 0x84C0 is TEXTURE0
      strState.AppendLine("Active Texture : " + "GL_TEXTURE" + (iActiveTexture - 0x84c0));

      // Get the max id (possibly)
      Gpu.CheckGpuErrorsRt();
      int maxId = 0;
      GL.GenTextures(1, out maxId);
      GL.DeleteTexture(maxId);
      Gpu.CheckGpuErrorsRt();
      int ntexs = 0;
      for (var iTexId = 0; iTexId < maxId; ++iTexId)
      {
        if (GL.IsTexture(iTexId))
        {
          ntexs++;
        }
      }
      strState.AppendLine($"----------------All Textures ({ntexs})----------------");
      // Show all registered texture parameters
      for (var iTexId = 0; iTexId < maxId; ++iTexId)
      {
        DebugPrintTextureInfo(strState, iTexId);
      }

      strState.AppendLine("----------------Bound Textures----------------");

      Gpu.CheckGpuErrorsRt();

      // - Get bound texture units.
      int iMaxVertexTextureUnits;
      GL.GetInteger(GetPName.MaxVertexTextureImageUnits, out iMaxVertexTextureUnits);
      for (int i = 0; i < iMaxVertexTextureUnits; ++i)
      {
        int iTextureId = 0;  // Texture ID
        GL.ActiveTexture(TextureUnit.Texture0 + i);
        strState.AppendLine("  Channel " + i);
        GL.GetInteger(GetPName.TextureBinding1D, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     1D: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBinding1DArray, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     1D_ARRAY: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBinding2D, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     2D: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBinding1DArray, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     2D_ARRAY: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBinding2DMultisample, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     2D_MULTISAMPLE: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBinding2DMultisampleArray, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     2D_MULTISAMPLE_ARRAY: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBinding3D, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     3D: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBindingBuffer, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     BUFFER: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBindingCubeMap, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     CUBE_MAP: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBindingRectangle, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     RECTANGLE: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        Gpu.CheckGpuErrorsRt();
      }
    }
    private static void DebugPrintTextureInfo(StringBuilder strState, int iTexId)
    {
      if (!GL.IsTexture(iTexId))
      {
        return;
      }
      Gpu.CheckGpuErrorsRt();

      GL.ActiveTexture(TextureUnit.Texture0);

      string texName = Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTexId);
      Gpu.CheckGpuErrorsRt();

      int tex_target;
      GL.GetTextureParameter(iTexId, GetTextureParameter.TextureTarget, out tex_target);
      Gpu.CheckGpuErrorsRt();

      if (tex_target == 0)
      {
        strState.AppendLine("  " + texName + " - Texture Target was zero (error).");
      }
      else
      {
        int get_binding = (int)TexTargetToTexBindingQuery((GLenum)tex_target);
        if (get_binding == 0)
        {
          strState.AppendLine("  " + texName + " - Texture Binding information (" + tex_target + ") was invalid.");
        }
        else
        {
          strState.AppendLine($"Texture: {texName} ");
          strState.AppendLine("  Target: " + ((GLenum)tex_target).Description());
          strState.AppendLine("  Binding: " + ((GLenum)tex_target).Description());

          int iSavedTextureId = 0;
          GL.GetInteger((GetPName)get_binding, out iSavedTextureId);
          Gpu.CheckGpuErrorsRt();
          GL.BindTexture((TextureTarget)tex_target, iTexId);
          Gpu.CheckGpuErrorsRt();
          {
            DebugPrintBoundTextureAttribs(strState, texName, tex_target);
          }
          GL.BindTexture((TextureTarget)tex_target, iSavedTextureId);
          Gpu.CheckGpuErrorsRt();
        }
      }
    }
    private static GLenum TexTargetToTexBindingQuery(GLenum target)
    {
      if (target == GLenum.GL_TEXTURE_1D)
      {
        return GLenum.GL_TEXTURE_BINDING_1D;
      }
      else if (target == GLenum.GL_TEXTURE_2D)
      {
        return GLenum.GL_TEXTURE_BINDING_2D;
      }
      else if (target == GLenum.GL_TEXTURE_3D)
      {
        return GLenum.GL_TEXTURE_BINDING_3D;
      }
      else if (target == GLenum.GL_TEXTURE_RECTANGLE)
      {
        return GLenum.GL_TEXTURE_BINDING_RECTANGLE;
      }
      else if (target == GLenum.GL_TEXTURE_BUFFER)
      {
        return GLenum.GL_TEXTURE_BINDING_BUFFER;
      }
      else if (target == GLenum.GL_TEXTURE_CUBE_MAP)
      {
        return GLenum.GL_TEXTURE_BINDING_CUBE_MAP;
      }
      else if (target == GLenum.GL_TEXTURE_1D_ARRAY)
      {
        return GLenum.GL_TEXTURE_BINDING_1D_ARRAY;
      }
      else if (target == GLenum.GL_TEXTURE_2D_ARRAY)
      {
        return GLenum.GL_TEXTURE_BINDING_2D_ARRAY;
      }
      else if (target == GLenum.GL_TEXTURE_CUBE_MAP_ARRAY)
      {
        return GLenum.GL_TEXTURE_BINDING_CUBE_MAP_ARRAY;
      }
      else if (target == GLenum.GL_TEXTURE_2D_MULTISAMPLE)
      {
        return GLenum.GL_TEXTURE_BINDING_2D_MULTISAMPLE;
      }
      else if (target == GLenum.GL_TEXTURE_2D_MULTISAMPLE_ARRAY)
      {
        return GLenum.GL_TEXTURE_BINDING_2D_MULTISAMPLE_ARRAY;
      }
      return (int)0;
    }
    private static GLenum TexBindingToTexTargetQuery(GLenum binding)
    {
      if (binding == GLenum.GL_TEXTURE_BINDING_1D)
      {
        return GLenum.GL_TEXTURE_1D;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_2D)
      {
        return GLenum.GL_TEXTURE_2D;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_3D)
      {
        return GLenum.GL_TEXTURE_3D;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_RECTANGLE)
      {
        return GLenum.GL_TEXTURE_RECTANGLE;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_BUFFER)
      {
        return GLenum.GL_TEXTURE_BUFFER;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_CUBE_MAP)
      {
        return GLenum.GL_TEXTURE_CUBE_MAP;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_1D_ARRAY)
      {
        return GLenum.GL_TEXTURE_1D_ARRAY;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_2D_ARRAY)
      {
        return GLenum.GL_TEXTURE_2D_ARRAY;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_CUBE_MAP_ARRAY)
      {
        return GLenum.GL_TEXTURE_CUBE_MAP_ARRAY;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_2D_MULTISAMPLE)
      {
        return GLenum.GL_TEXTURE_2D_MULTISAMPLE;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_2D_MULTISAMPLE_ARRAY)
      {
        return GLenum.GL_TEXTURE_2D_MULTISAMPLE_ARRAY;
      }
      return 0;
    }
    private static void DebugPrintBoundTextureAttribs(StringBuilder strState, string texName, int tex_target)
    {
      int val;
      if (Gu.AllowOpenTKFaults)
      {
        GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureWidth, out val);
        strState.AppendLine("  TextureWidth: " + val);
        Gpu.CheckGpuErrorsRt();
        GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureHeight, out val);
        strState.AppendLine("  TextureHeight: " + val);
        Gpu.CheckGpuErrorsRt();
      }
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureMagFilter, out val);
      strState.AppendLine("  TextureMagFilter: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureMinFilter, out val);
      strState.AppendLine("  TextureMinFilter: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureMinLod, out val);
      strState.AppendLine("  TextureMinLod: " + val);
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureBaseLevel, out val);
      strState.AppendLine("  TextureBaseLevel: " + val);
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureMaxLevel, out val);
      strState.AppendLine("  TextureMaxLevel: " + val);
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureWrapS, out val);
      strState.AppendLine("  TextureWrapS: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureWrapT, out val);
      strState.AppendLine("  TextureWrapT: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureWrapR, out val);
      strState.AppendLine("  TextureWrapR: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureCompareMode, out val);
      strState.AppendLine("  TextureCompareMode: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureCompareFunc, out val);
      strState.AppendLine("  TextureCompareFunc: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      if (Gu.Context.GameWindow.Profile == OpenTK.Windowing.Common.ContextProfile.Compatability)
      {
        GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.DepthTextureMode, out val);
        strState.AppendLine("  DepthTextureMode: " + ((GLenum)val).Description());
        Gpu.CheckGpuErrorsRt();
        GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.GenerateMipmap, out val);
        strState.AppendLine("  GenerateMipmap: " + ((GLenum)val).Description());
        Gpu.CheckGpuErrorsRt();
      }

    }
    private static void DebugGetFramebufferAttachmentState(StringBuilder strState)
    {
      strState.AppendLine("----------------Framebuffers----------------");
      int eDrawBuffer;
      int iDrawFramebufferBinding;  // name of fb beijmg drawn to
      int iReadFramebufferBinding;  // name of fb beijmg drawn to
      int iRenderbufferBinding;
      int eReadBuffer;
      int iSamplerBinding;  //! Texture sampler (should be 2d??)
      int boundFramebuffer;

      // Reference enums
      //#define GL_FRONT 0x0404
      //#define GL_BACK 0x0405

      // Framebuffers
      GL.GetInteger(GetPName.DrawBuffer, out eDrawBuffer);  // 0x08CE0 is the COLOR ATTACHMENT 1, 0x0405 is the default BACK buffer.
      GL.GetInteger(GetPName.ReadBuffer, out eReadBuffer);  // Default: GL_BACK
      GL.GetInteger(GetPName.DrawFramebufferBinding, out iDrawFramebufferBinding);
      GL.GetInteger(GetPName.ReadFramebufferBinding, out iReadFramebufferBinding);
      GL.GetInteger(GetPName.RenderbufferBinding, out iRenderbufferBinding);
      GL.GetInteger(GetPName.SamplerBinding, out iSamplerBinding);
      GL.GetInteger(GetPName.FramebufferBinding, out boundFramebuffer);
      Gpu.CheckGpuErrorsRt();


      //strState.AppendLine(" Max Fragment Texture Image Units: " + maxFragmentTextureImageUnits);
      strState.AppendLine("Current Bound Framebuffer: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Framebuffer, boundFramebuffer));
      strState.AppendLine("Current Draw Framebuffer Binding: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Framebuffer, iDrawFramebufferBinding));
      strState.AppendLine("Current Read Framebuffer Binding: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Framebuffer, iReadFramebufferBinding));
      if (iDrawFramebufferBinding != iReadFramebufferBinding)
      {
        strState.AppendLine("   NOTE: Draw and Read framebuffers are bound different!");
      }
      strState.AppendLine("Current Draw Framebuffer Attachment: " + ((GLenum)eDrawBuffer).Description());
      strState.AppendLine("Current Read Framebuffer Attachment: " + ((GLenum)eReadBuffer).Description());
      strState.AppendLine("Current RenderBuffer Binding: " + iRenderbufferBinding);
      strState.AppendLine("Current Sampler Binding: " + iSamplerBinding);

      if (boundFramebuffer == 0)
      {
        return;
      }


      // Print details about hte bound buffer.
      int maxColorAttachments;
      GL.GetInteger(GetPName.MaxColorAttachments, out maxColorAttachments);
      strState.AppendLine("Current Attachments: (max=" + maxColorAttachments + ")");

      for (int i = 0; i < maxColorAttachments; ++i)
      {
        DebugPrintFBOAttachment(strState, (OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment0 + i), i);
      }
      DebugPrintFBOAttachment(strState, OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment, -1);
      DebugPrintFBOAttachment(strState, OpenTK.Graphics.OpenGL4.FramebufferAttachment.StencilAttachment, -1);
      //DebugPrintFBOAttachment(strState, OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthStencilAttachment);
    }
    private static void DebugPrintFBOAttachment(StringBuilder strState, OpenTK.Graphics.OpenGL4.FramebufferAttachment attachment, int icoloratt)
    {
      int attachmentName = 0;
      int attachmentType = 0;
      int mipmapLevel = 0;

      // string strAttachment = "";

      // if (attachment == OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment)
      // {
      //   strAttachment = ("GL_DEPTH_ATTACHMENT");
      // }
      // else if (attachment == OpenTK.Graphics.OpenGL4.FramebufferAttachment.StencilAttachment)
      // {
      //   strAttachment = ("GL_STENCIL_ATTACHMENT");
      // }
      // else if (attachment >= OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment0 && attachment <= OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment15)
      // {
      //   strAttachment = "GL_COLOR_ATTACHMENT" + (attachment - OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment0);
      // }

      strState.AppendLine("  Attachment: " + ((GLenum)(attachment)).Description());

      GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, attachment, FramebufferParameterName.FramebufferAttachmentObjectType, out attachmentType);
      Gpu.CheckGpuErrorsRt();
      if (attachmentType == 0)//GL_NONE is zero
      {
        strState.AppendLine("    Type: " + "GL_NONE");
      }
      else if (attachmentType == 0x8D41)//GL_RENDERBUFFER
      {
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, attachment, FramebufferParameterName.FramebufferAttachmentObjectName, out attachmentName);
        Gpu.CheckGpuErrorsRt();
        strState.AppendLine("    Type: " + "GL_RENDERBUFFER");
        strState.AppendLine("    Name: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Renderbuffer, attachmentName));
      }
      else if (attachmentType == 0x1702)//GL_TEXTURE
      {
        if (icoloratt >= 0)
        {
          int blend = 0;
          int[] rgb = new int[4];
          GL.GetInteger((GetIndexedPName)GLenum.GL_BLEND_SRC, icoloratt, out blend);
          strState.AppendLine("    BlendSrc: " + ((blend == 0) ? "GL_ZERO" : (blend == 1 ? "GL_ONE" : ((GLenum)blend).Description())));
          Gpu.CheckGpuErrorsRt();
          GL.GetInteger((GetIndexedPName)GLenum.GL_BLEND_DST, icoloratt, out blend);
          strState.AppendLine("    BlendDst: " + ((blend == 0) ? "GL_ZERO" : (blend == 1 ? "GL_ONE" : ((GLenum)blend).Description())));
          Gpu.CheckGpuErrorsRt();
          GL.GetInteger((GetIndexedPName)GLenum.GL_BLEND_SRC_RGB, icoloratt, rgb);
          strState.AppendLine("    BlendSrcRGB: " + (float)rgb[0] + "," + (float)rgb[1] + "," + (float)rgb[2] + "," + (float)rgb[3]);
          Gpu.CheckGpuErrorsRt();
          GL.GetInteger((GetIndexedPName)GLenum.GL_BLEND_DST_RGB, icoloratt, rgb);
          strState.AppendLine("    BlendDstRGB: " + (float)rgb[0] + "," + (float)rgb[1] + "," + (float)rgb[2] + "," + (float)rgb[3]);
          Gpu.CheckGpuErrorsRt();
          GL.GetInteger((GetIndexedPName)GLenum.GL_BLEND_SRC_ALPHA, icoloratt, out blend);
          strState.AppendLine("    BlendSrcAlpha: " + ((blend == 0) ? "GL_ZERO" : (blend == 1 ? "GL_ONE" : ((GLenum)blend).Description())));
          Gpu.CheckGpuErrorsRt();
          GL.GetInteger((GetIndexedPName)GLenum.GL_BLEND_DST_ALPHA, icoloratt, out blend);
          strState.AppendLine("    BlendDstAlpha: " + ((blend == 0) ? "GL_ZERO" : (blend == 1 ? "GL_ONE" : ((GLenum)blend).Description())));
          Gpu.CheckGpuErrorsRt();
        }

        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, attachment, FramebufferParameterName.FramebufferAttachmentObjectName, out attachmentName);
        Gpu.CheckGpuErrorsRt();
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, attachment, FramebufferParameterName.FramebufferAttachmentTextureLevel, out mipmapLevel);
        Gpu.CheckGpuErrorsRt();
        strState.AppendLine("    Type: " + "GL_TEXTURE");
        strState.AppendLine("    Name: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, attachmentName));
        strState.AppendLine("    Mipmap Level: " + mipmapLevel);
      }
    }
    private static void DebugGetVertexArrayState(StringBuilder strState)
    {
      strState.AppendLine(("----------------Vertex Array State----------------"));
      int nMaxAttribs;
      int iVertexArrayBinding;
      GL.GetInteger(GetPName.MaxVertexAttribs, out nMaxAttribs);
      GL.GetInteger(GetPName.VertexArrayBinding, out iVertexArrayBinding);

      strState.AppendLine("Bound Vertex Array Id (VAO): " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.VertexArray, iVertexArrayBinding));
      strState.AppendLine("Max Allowed Atribs: " + nMaxAttribs);

      int nact = 0;
      for (int iAttrib = 0; iAttrib < nMaxAttribs; ++iAttrib)
      {
        int aaaa = 0;
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.ArrayEnabled, out aaaa);
        Gpu.CheckGpuErrorsRt();
        nact++;
      }
      strState.AppendLine("Active Vertex Attribs (" + nact + "): ");

      Gpu.CheckGpuErrorsRt();

      // - Disable all arrays by default.
      for (int iAttrib = 0; iAttrib < nMaxAttribs; ++iAttrib)
      {
        // TODO:
        int iArrayBufferBinding = 0;
        int iArrayEnabled = 0;
        int iAttribArraySize = 0;
        int iAttribArrayType = 0;
        int iAttribArrayStride = 0;
        int iAttribArrayInteger = 0;

        int iAttribArrayNormalized;
        // int iAttribArrayDivisor;
        //memset(fCurAttrib, 0, sizeof(GLfloat) * 4);
        //memset(iCurAttrib, 0, sizeof(int) * 4);
        //memset(uiCurAttrib, 0, sizeof(GLuint) * 4);

        GL.GetVertexAttrib(iAttrib, (VertexAttribParameter)(GLenum.GL_VERTEX_ATTRIB_ARRAY_BUFFER_BINDING), out iArrayBufferBinding);
        Gpu.CheckGpuErrorsRt();
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.ArrayEnabled, out iArrayEnabled);
        Gpu.CheckGpuErrorsRt();
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.ArraySize, out iAttribArraySize);
        Gpu.CheckGpuErrorsRt();
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.ArrayType, out iAttribArrayType);
        Gpu.CheckGpuErrorsRt();
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.ArrayStride, out iAttribArrayStride);
        Gpu.CheckGpuErrorsRt();
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.VertexAttribArrayInteger, out iAttribArrayInteger);
        Gpu.CheckGpuErrorsRt();
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.ArrayNormalized, out iAttribArrayNormalized);
        Gpu.CheckGpuErrorsRt();
        // glGetVertexAttribiv(iAttrib, GL_VERTEX_ATTRIB_ARRAY_DIVISOR, ref iAttribArrayDivisor);
        // CheckGpuErrorsDbg();

        strState.AppendLine("  Attrib " + iAttrib + "  Enabled:" + (iArrayEnabled > 0 ? "Y" : "N"));

        if (iArrayEnabled == 0)
        {
          continue;
        }

        strState.AppendLine("    Array Buffer Binding: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Buffer, iArrayBufferBinding));
        strState.AppendLine("    Size: " + iAttribArraySize);
        strState.AppendLine("    Stride: " + iAttribArrayStride);
        strState.AppendLine("    Is Integer: " + (iAttribArrayInteger > 0 ? "Y" : "N"));
        strState.AppendLine("    Normalized: " + (iAttribArrayNormalized > 0 ? "Y" : "N"));
        strState.AppendLine("    Type: " + ((GLenum)(iAttribArrayType)).Description());


        // https://registry.khronos.org/OpenGL-Refpages/gl4/html/glGetVertexAttrib.xhtml
        // Generic vertex attribute 0 is unique in that it has no current state,
        // so an error will be generated if index is 0. The initial value for all
        // other generic vertex attributes is (0,0,0,1).
        if (iAttrib != 0)
        {
          // unsafe
          {
            int[] attri = new int[4];
            //int[] attr = new int[4];
            float[] attrf = new float[4];
            double[] attrd = new double[4];
            //We recommend using Span<T> or ReadOnlySpan<T> types to work with stack allocated memory whenever possible. MSDN
            switch (iAttribArrayType)
            {
              case (int)GLenum.GL_INT:
                GL.GetVertexAttrib(iAttrib, (VertexAttribParameter)GLenum.GL_CURRENT_VERTEX_ATTRIB, attri);
                Gpu.CheckGpuErrorsRt();
                strState.AppendLine("    Cur Value(int): " + attri[0] + "," + attri[1] + "," + attri[2] + "," + attri[3]);
                break;
              case (int)GLenum.GL_UNSIGNED_INT:
                //May be wrong. OpenTK doesn't suupport uint specifically
                GL.GetVertexAttrib(iAttrib, (VertexAttribParameter)GLenum.GL_CURRENT_VERTEX_ATTRIB, attri);
                Gpu.CheckGpuErrorsRt();
                strState.AppendLine("    Cur Value(uint): " + (uint)attri[0] + "," + (uint)attri[1] + "," + (uint)attri[2] + "," + (uint)attri[3]);
                break;
              case (int)GLenum.GL_FLOAT:
                GL.GetVertexAttrib(iAttrib, (VertexAttribParameter)GLenum.GL_CURRENT_VERTEX_ATTRIB, attrf);
                Gpu.CheckGpuErrorsRt();
                strState.AppendLine("    Cur Value(float): " + attrf[0] + "," + attrf[1] + "," + attrf[2] + "," + attrf[3]);
                break;
              case (int)GLenum.GL_DOUBLE:
                GL.GetVertexAttrib(iAttrib, (VertexAttribParameter)GLenum.GL_CURRENT_VERTEX_ATTRIB, attrd);
                Gpu.CheckGpuErrorsRt();
                strState.AppendLine("    Cur Value(double): " + attrd[0] + "," + attrd[1] + "," + attrd[2] + "," + attrd[3]);
                break;
              default:
                strState.AppendLine("    Cur Value:  NOT SUPPORTED****** TODO:::: ");
                break;
            };
          }
        }
        // This reads the attrib values such as float, int etc.
        // int iCurrentVertexAttrib;
        // glGetVertexAttribiv(iAttrib, GL_CURRENT_VERTEX_ATTRIB, ref iCurrentVertexAttrib);
      }
    }
    private static void DebugPrintShaderLimits(StringBuilder strState)
    {

      strState.AppendLine("---------------- Window Info ----------------");
      strState.AppendLine($"Cur Context: '{Gu.Context.GameWindow.Title.ToString()}'");
      strState.AppendLine($"  Title: '{Gu.Context.GameWindow.Title.ToString()}'");
      strState.AppendLine($"  Dims: {Gu.Context.GameWindow.Width}x{Gu.Context.GameWindow.Height}");
      //strState.AppendLine($"Screen Dims: {Gu.Context.GameWindow.monitor}x{Gu.Context.GameWindow.Height}");
      //strState.AppendLine($"This API: {Gu.Context.GameWindow.API.ToString()}");
      strState.AppendLine($"  GL Profile: {Gu.Context.GameWindow.Profile.ToString()}");
      strState.AppendLine($"  GL Version: {Gu.Context.GameWindow.APIVersion.ToString()}");
      strState.AppendLine($"All Windows:");
      foreach (var c in Gu.Contexts)
      {
        strState.AppendLine($"  Title: '{c.Key.Title}'  Context is Current: {c.Key.Context.IsCurrent}");
      }
      strState.AppendLine("---------------- Gpu Info ----------------");
      strState.AppendLine($"GPU: {GL.GetString​(StringName.Renderer)}");
      strState.AppendLine($"Vendor: {GL.GetString​(StringName.Vendor)}");
      strState.AppendLine($"Supported GL: {GL.GetString​(StringName.Version)}");
      strState.AppendLine($"Supported GLSL: {GL.GetString​(StringName.ShadingLanguageVersion)}");

      int iMaxVertexTextureUnits;
      int iMaxVertexGeometryUnits;
      int iMaxTextureUnits;
      int iMaxCombinedTextureUnits;

      GL.GetInteger(GetPName.MaxTextureImageUnits, out iMaxTextureUnits);
      GL.GetInteger(GetPName.MaxVertexTextureImageUnits, out iMaxVertexTextureUnits);
      GL.GetInteger(GetPName.MaxGeometryTextureImageUnits, out iMaxVertexGeometryUnits);
      GL.GetInteger(GetPName.MaxCombinedTextureImageUnits, out iMaxCombinedTextureUnits);
      Gpu.CheckGpuErrorsRt();

      strState.AppendLine("Max Texture Units: " + iMaxTextureUnits);
      strState.AppendLine("Max Vertex Texture Units: " + iMaxVertexTextureUnits);
      strState.AppendLine("Max Geometry Texture Units: " + iMaxVertexGeometryUnits);
      strState.AppendLine("Max Combined Texture Units: " + iMaxCombinedTextureUnits);

      int maxColorAttachments;
      int maxDrawBuffers;
      int maxFragmentUniformBlocks;
      int maxGeometryUniformBlocks;
      int maxVertexUniformBlocks;
      int maxVertexTextureImageUnits;
      int maxGeometryTextureImageUnits;
      int maxFragmentTextureImageUnits;
      int maxVertexUniformComponents;
      int maxGeometryUniformComponents;
      int maxFragmentUniformComponents;

      GL.GetInteger(GetPName.MaxColorAttachments, out maxColorAttachments);
      GL.GetInteger(GetPName.MaxDrawBuffers, out maxDrawBuffers);
      GL.GetInteger(GetPName.MaxFragmentUniformBlocks, out maxFragmentUniformBlocks);
      GL.GetInteger(GetPName.MaxGeometryUniformBlocks, out maxGeometryUniformBlocks);
      GL.GetInteger(GetPName.MaxVertexUniformBlocks, out maxVertexUniformBlocks);
      GL.GetInteger(GetPName.MaxVertexTextureImageUnits, out maxVertexTextureImageUnits);
      GL.GetInteger(GetPName.MaxGeometryTextureImageUnits, out maxGeometryTextureImageUnits);
      GL.GetInteger(GetPName.MaxVertexUniformComponents, out maxVertexUniformComponents);
      GL.GetInteger(GetPName.MaxGeometryUniformComponents, out maxGeometryUniformComponents);
      GL.GetInteger(GetPName.MaxFragmentUniformComponents, out maxFragmentUniformComponents);
      strState.AppendLine("Max Color Attachments: " + maxColorAttachments);
      strState.AppendLine("Max Draw Buffers: " + maxDrawBuffers);
      strState.AppendLine("Max Vertex Uniform Blocks: " + maxVertexUniformBlocks);
      strState.AppendLine("Max Vertex Uniform Components: " + maxVertexUniformComponents);
      strState.AppendLine("Max Vertex Texture Image Units: " + maxVertexTextureImageUnits);
      strState.AppendLine("Max Geometry Uniform Components: " + maxGeometryUniformComponents);
      strState.AppendLine("Max Geometry Uniform Blocks: " + maxGeometryUniformBlocks);
      strState.AppendLine("Max Geometry Texture Image Units: " + maxGeometryTextureImageUnits);
      strState.AppendLine("Max Fragment Uniform Components: " + maxFragmentUniformComponents);
      strState.AppendLine("Max Fragment Uniform Blocks: " + maxFragmentUniformBlocks);
      Gpu.CheckGpuErrorsRt();
    }

    #endregion

  }//GpuRenderSTate
}
