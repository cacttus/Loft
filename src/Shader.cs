using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Graphics.OpenGL4;

namespace PirateCraft
{
  public enum ShaderLoadState
  {
    None,
    Loading,
    Failed,
    Success
  }
  public class ShaderStage : OpenGLResource
  {
    public ShaderType ShaderType { get; private set; } = ShaderType.VertexShader;
    public ShaderStage(ShaderType tt, string src)
    {
      ShaderType = tt;
      _glId = GL.CreateShader(tt);
      Gpu.CheckGpuErrorsRt();
      GL.ShaderSource(_glId, src);
      Gpu.CheckGpuErrorsRt();
      GL.CompileShader(_glId);
      Gpu.CheckGpuErrorsRt();
    }
    
    public override void Dispose_OpenGL_RenderThread()
    {
      if (GL.IsShader(_glId))
      {
        GL.DeleteShader(_glId);
      }
    }
  }
  public class ShaderUniform
  {
    public int Location { get; private set; } = 0;
    public string Name { get; private set; } = "unset";
    public string Value { get; private set; } = "unset";
    public int SizeBytes { get; private set; } = 0;
    public ActiveUniformType Type { get; private set; } = ActiveUniformType.Int;

    public ShaderUniform(int location, int u_size, ActiveUniformType u_type, string u_name)
    {
      Location = location; ;
      Name = u_name;
      Type = u_type;
      SizeBytes = u_size;
    }
  }
  public class ShaderUniformBlock : OpenGLResource
  {
    private int _iUboId = -2;
    private int _iBlockIndex = -1;
    private int _iBindingIndex = -1;
    public int BufferSizeBytes { get; private set; } = 0;
    private bool _bHasBeenSet = false;

    public string Name { get; private set; }
    public override void Dispose_OpenGL_RenderThread()
    {
      GL.DeleteBuffer(_iUboId);
    }
    public ShaderUniformBlock(string name, int iBlockIndex, int iBindingIndex, int iBufferByteSize)
    {
      Name = name;
      BufferSizeBytes = iBufferByteSize;
      _iBindingIndex = iBindingIndex;
      _iBlockIndex = iBlockIndex;

      _iUboId = GL.GenBuffer();
      GL.BindBuffer(BufferTarget.UniformBuffer, _iUboId);
      Gpu.CheckGpuErrorsDbg();
      GL.BufferData(BufferTarget.UniformBuffer, BufferSizeBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
      Gpu.CheckGpuErrorsDbg();
      GL.BindBuffer(BufferTarget.UniformBuffer, 0);
      Gpu.CheckGpuErrorsDbg();
    }
    public void copyUniformData(IntPtr pData, int copySizeBytes)
    {
      //Copy to the shader buffer
      Gu.Assert(copySizeBytes <= BufferSizeBytes);

      //_pValue = pData;

      Gpu.CheckGpuErrorsDbg();
      GL.BindBuffer(BufferTarget.UniformBuffer, _iUboId);
      Gpu.CheckGpuErrorsDbg();
      //    void* pBuf =getContext()->glMapBuffer(GL_UNIFORM_BUFFER, GL_WRITE_ONLY);
      //if(pBuf != nullptr) {
      //    memcpy(pBuf, pData, copySizeBytes);
      //    getContext()->glMapBuffer(GL_UNIFORM_BUFFER, 0);
      //}
      //else {
      //    BroLogError("Uniform buffer could not be mapped.");
      //}
      //getContext()->glBufferData(GL_UNIFORM_BUFFER, copySizeBytes, (void*)_pValue, GL_DYNAMIC_DRAW);
      GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, copySizeBytes, pData);
      Gpu.CheckGpuErrorsDbg();

      GL.BindBuffer(BufferTarget.UniformBuffer, 0);
      Gpu.CheckGpuErrorsDbg();

      _bHasBeenSet = true;
    }
    public void bindUniformFast()
    {
      if (_bHasBeenSet == false)
      {
        Gu.Log.Warn("Shader Uniform Block '" + Name + "' value was not set ");
      }
      GL.BindBufferBase(BufferRangeTarget.UniformBuffer, _iBindingIndex, _iUboId);
      Gpu.CheckGpuErrorsDbg();
      if (_iUboId == 0)
      {
        int x = 0;
        x++;
      }

      GL.BindBuffer(BufferTarget.UniformBuffer, _iUboId);
      Gpu.CheckGpuErrorsDbg();
    }


  }
  //Shader, program on the GPU.
  public class Shader : OpenGLResource
  {
    private ShaderStage _vertexStage = null;
    private ShaderStage _fragmentStage = null;
    private ShaderStage _geomStage = null;

    private Dictionary<string, ShaderUniform> _uniforms = new Dictionary<string, ShaderUniform>();
    private Dictionary<string, ShaderUniformBlock> _uniformBlocks = new Dictionary<string, ShaderUniformBlock>();

    private TextureUnit _currUnit = TextureUnit.Texture0;
    //Technically this is a GL context thing. But it's ok for now.
    private Dictionary<TextureUnit, Texture2D> _boundTextures = new Dictionary<TextureUnit, Texture2D>();

    public class TextureInput
    {
      public string UniformName { get; }
      private TextureInput(string name) { UniformName = name; }
      public static TextureInput Albedo { get; private set; } = new TextureInput("_ufTexture2D_Albedo");
      public static TextureInput Normal { get; private set; } = new TextureInput("_ufTexture2D_Normal");
    }

    //Just debug stuff that will go away.
    public float GGX_X = .8f;
    public float GGX_Y = .8f;
    public int lightingModel = 2;
    public float nmap = 0.5f;

    private List<string> _shaderErrors = new List<string>();

    private ShaderLoadState State = ShaderLoadState.None;

    private static Shader _defaultDiffuseShader = null;
    private static Shader _defaultFlatColorShader = null;

    public string Name { get; private set; } = "<unset>";

    public static Shader DefaultFlatColorShader()
    {
      if (_defaultFlatColorShader == null)
      {
        _defaultFlatColorShader = Gu.Resources.LoadShader("v_v3", false, FileStorage.Embedded);
      }
      return _defaultFlatColorShader;
    }
    public static Shader DefaultDiffuse()
    {
      //Returns a basic v3 n3 x2 lambert+blinn-phong shader.
      if (_defaultDiffuseShader == null)
      {
        _defaultDiffuseShader = Gu.Resources.LoadShader("v_v3n3x2", false, FileStorage.Embedded);
      }
      return _defaultDiffuseShader;
    }

    public Shader(string name, string vsSrc = "", string psSrc = "", string gsSrc = "")
    {
      Name = name;
      Gu.Log.Debug("Compiling shader '" + Name + "'");
      Gpu.CheckGpuErrorsDbg();
      {
        State = ShaderLoadState.Loading;
        CreateShaders(vsSrc, psSrc, gsSrc);
        CreateProgram();
        if (State != ShaderLoadState.Failed)
        {
          GL.UseProgram(_glId);

          ParseUniforms();

          State = ShaderLoadState.Success;
        }
        else
        {
          Gu.Log.Error("Failed to load shader '" + Name + "'.\r\n" + String.Join("\r\n", _shaderErrors.ToArray()));
          Gu.Log.Info("--VERTEX SOURCE--\r\n" + vsSrc);
          Gu.Log.Info("--GEOM SOURCE--\r\n" + gsSrc);
          Gu.Log.Info("--FRAG SOURCE--\r\n" + psSrc);

          Gu.DebugBreak();
        }
      }
      Gpu.CheckGpuErrorsDbg();
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      if (GL.IsProgram(_glId))
      {
        GL.DeleteProgram(_glId);
      }
    }
    private void Bind()
    {
      Gpu.CheckGpuErrorsDbg();
      {
        GL.UseProgram(_glId);
      }
      Gpu.CheckGpuErrorsDbg();
    }
    private void Unbind()
    {
      Gpu.CheckGpuErrorsDbg();
      {
        GL.UseProgram(0);
      }
      Gpu.CheckGpuErrorsDbg();
    }
    public void BeginRender(double dt, Camera3D cam, WorldObject ob, Material m, mat4[] instanceData)
    {
      //**Pre - render - update uniforms.
      Gpu.CheckGpuErrorsDbg();
      {
        //Reset
        _currUnit = TextureUnit.Texture0;
        _boundTextures.Clear();

        Bind();
        BindUniforms(dt, cam, ob, m, instanceData);
      }
      Gpu.CheckGpuErrorsDbg();
    }
    public void EndRender()
    {
      Unbind();
      foreach (var tu in _boundTextures)
      {
        if (tu.Value != null)
        {
          tu.Value.Unbind(tu.Key);
        }
      }
      _currUnit = TextureUnit.Texture0;
      _boundTextures.Clear();
    }

    #region Private

    private void CreateShaders(string vs, string ps, string gs = "")
    {
      Gpu.CheckGpuErrorsRt();
      {
        _vertexStage = new ShaderStage(ShaderType.VertexShader, vs);
        _fragmentStage = new ShaderStage(ShaderType.FragmentShader, ps);
        if (!string.IsNullOrEmpty(gs))
        {
          _geomStage = new ShaderStage(ShaderType.GeometryShader, gs);
        }
      }
      Gpu.CheckGpuErrorsRt();
    }
    private void CreateProgram()
    {
      Gpu.CheckGpuErrorsRt();
      {
        _glId = GL.CreateProgram();

        GL.AttachShader(_glId, _vertexStage.GetGlId());
        Gpu.CheckGpuErrorsRt();
        GL.AttachShader(_glId, _fragmentStage.GetGlId());
        Gpu.CheckGpuErrorsRt();
        if (_geomStage != null)
        {
          GL.AttachShader(_glId, _geomStage.GetGlId());
          Gpu.CheckGpuErrorsRt();
        }

        GL.LinkProgram(_glId);
        Gpu.CheckGpuErrorsRt();

        string programInfoLog = "";
        GL.GetProgramInfoLog(_glId, out programInfoLog);
        _shaderErrors = programInfoLog.Split('\n').ToList();

        if (_shaderErrors.Count > 0 && programInfoLog.ToLower().Contains("error"))
        {
          State = ShaderLoadState.Failed;
        }

      }
      Gpu.CheckGpuErrorsRt();
    }
    private void ParseUniforms()
    {
      int u_count = 0;
      GL.GetProgram(_glId, GetProgramParameterName.ActiveUniforms, out u_count);
      Gpu.CheckGpuErrorsRt();

      //TODO: blocks
      for (var i = 0; i < u_count; i++)
      {
        ActiveUniformType u_type;
        int u_size = 0;
        string u_name = "DEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEAD";//idk.
        int u_name_len = 0;

        GL.GetActiveUniform(GetGlId(), i, out u_size, out u_type);
        Gpu.CheckGpuErrorsRt();
        GL.GetActiveUniformName(GetGlId(), i, u_name.Length, out u_name_len, out u_name);
        Gpu.CheckGpuErrorsRt();

        if (u_name.Contains("["))
        {
          //This is a unifrom block
          continue;
        }

        int location = GL.GetUniformLocation(GetGlId(), u_name);
        Gu.Assert(location >= 0);
        u_name = u_name.Substring(0, u_name_len);

        ShaderUniform su = new ShaderUniform(location, u_size, u_type, u_name);
        _uniforms.Add(u_name, su);
      }

      int u_block_count = 0;
      GL.GetProgram(_glId, GetProgramParameterName.ActiveUniformBlocks, out u_block_count);
      Gpu.CheckGpuErrorsRt();
      for (var i = 0; i < u_block_count; i++)
      {
        int buffer_size_bytes = 0;
        GL.GetActiveUniformBlock(GetGlId(), i, ActiveUniformBlockParameter.UniformBlockDataSize, out buffer_size_bytes);
        Gpu.CheckGpuErrorsRt();

        int binding = 0;
        GL.GetActiveUniformBlock(GetGlId(), i, ActiveUniformBlockParameter.UniformBlockBinding, out binding);
        Gpu.CheckGpuErrorsRt();

        string u_name = "DEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEAD";//idk.
        int u_name_len = 0;
        GL.GetActiveUniformBlockName(GetGlId(), i, u_name.Length, out u_name_len, out u_name);
        Gpu.CheckGpuErrorsRt();

        u_name = u_name.Substring(0, u_name_len);

        ShaderUniformBlock su = new ShaderUniformBlock(u_name, i, binding, buffer_size_bytes);// u_size, u_type, u_name);
        _uniformBlocks.Add(u_name, su);
      }
    }
    private void BindUniforms(double dt, Camera3D cam, WorldObject ob, Material m, mat4[] instanceData)
    {
      int dbg_n = 0;
      //TODO: cache uniform values and avoid updating
      foreach (var u in _uniforms.Values)
      {
        dbg_n++;
        //bind uniforms based on name.
        if (u.Name.Equals("_ufCamera_Position"))
        {
          GL.ProgramUniform3(_glId, u.Location, cam.Position.x, cam.Position.y, cam.Position.z);
        }
        else if (u.Name.Equals("_ufLightModel_GGX_X"))
        {
          GL.Uniform1(u.Location, GGX_X);
        }
        else if (u.Name.Equals("_ufLightModel_GGX_Y"))
        {
          GL.Uniform1(u.Location, GGX_Y);
        }
        else if (u.Name.Equals("_ufLightModel_Index"))
        {
          GL.Uniform1(u.Location, lightingModel);
        }
        else if (u.Name.Equals(TextureInput.Albedo.UniformName))
        {
          BindTexture(u, m, TextureInput.Albedo);
        }
        else if (u.Name.Equals(TextureInput.Normal.UniformName))
        {
          BindTexture(u, m, TextureInput.Normal);
        }
        else if (u.Name.Equals("_ufWorldObject_Color"))
        {
          GL.ProgramUniform4(_glId, u.Location, ob.Color.x, ob.Color.y, ob.Color.z, ob.Color.w);
        }
        else if (u.Name.Equals("_ufMatrix_Normal"))
        {
          var n_mat_tk = ob.World.inverseOf().ToOpenTK();
          GL.UniformMatrix4(u.Location, false, ref n_mat_tk);
        }
        else if (u.Name.Equals("_ufMatrix_Model"))
        {
          var m_mat_tk = ob.World.ToOpenTK();
          GL.UniformMatrix4(u.Location, false, ref m_mat_tk);
        }
        else if (u.Name.Equals("_ufMatrix_View"))
        {
          var v_mat_tk = cam.ViewMatrix.ToOpenTK();
          GL.UniformMatrix4(u.Location, false, ref v_mat_tk);
        }
        else if (u.Name.Equals("_ufMatrix_Projection"))
        {
          var p_mat_tk = cam.ProjectionMatrix.ToOpenTK();
          GL.UniformMatrix4(u.Location, false, ref p_mat_tk);
        }
        else if (u.Name.Equals("_ufNormalMap_Blend"))
        {
          GL.Uniform1(u.Location, nmap);
        }

        else
        {
          Gu.Log.WarnCycle("Unknown uniform variable '" + u.Name + "'.");
        }
        Gpu.CheckGpuErrorsDbg();

      }
      foreach (var u in _uniformBlocks.Values)
      {
        if (u.Name.Equals("_ufInstanceData_Block"))
        {
          int m4size = Marshal.SizeOf(default(mat4));
          int num_bytes_to_copy = m4size * instanceData.Length;
          if (num_bytes_to_copy > u.BufferSizeBytes)
          {
            num_bytes_to_copy = u.BufferSizeBytes;
            Gu.Log.WarnCycle("Exceeded max index count of " + u.BufferSizeBytes / m4size + " matrices. Tried to copy " + instanceData.Length + " instance matrices.");
          }
          var handle = GCHandle.Alloc(instanceData, GCHandleType.Pinned);
          u.copyUniformData(handle.AddrOfPinnedObject(), num_bytes_to_copy);
          handle.Free();

          u.bindUniformFast();
        }
        else
        {
          Gu.Log.WarnCycle("Unknown uniform block '" + u.Name + "'.");
        }
      }
      //Check for errors.
      Gpu.CheckGpuErrorsDbg();
    }
    private void BindTexture(ShaderUniform su, Material m, TextureInput tu)
    {
      GL.Uniform1(su.Location, (int)(_currUnit - TextureUnit.Texture0));
      var tex = m.GetTextureOrDefault(tu);

      if (tex != null)
      {
        tex.Bind(_currUnit);
        _boundTextures.Add(_currUnit, tex);
      }
      else
      {
        Gu.Log.WarnCycle("Texture unit " + su.Name + " was not found in material and had no default.");
      }

      _currUnit++;
    }
    private static string ParseIncludeLine(string line)
    {
      int part = 0;
      string filename = "";
      foreach (char c in line)
      {
        if (c == '"')
        {
          if (part == 1)
          {
            break;
          }
          part = 1;
        }
        else if (part == 1)
        {
          filename += c;
        }
      }
      return filename;
    }
    public static string ProcessFile(FileLoc loc, List<string> file_lines = null, int callnumber = 0)
    {
      //Returns the entire processed string on the first function invocation. 
      //Do not set file_lines if you want the return value
      bool firstcall = false;
      if (file_lines == null)
      {
        file_lines = new List<string>();
        firstcall = true;
      }
      file_lines.Add("//" + new StringBuilder().Insert(0, "->", callnumber).ToString() + "BEGIN: " + loc.RawPath + " (" + loc.QualifiedPath + ")\n");

      string file_text = ResourceManager.ReadTextFile(loc);
      string[] lines = file_text.Split("\n");
      foreach (string line in lines)
      {
        //Replace all \r
        string line_proc = line.Replace("\r\n", "\n");
        if (!line_proc.EndsWith("\n"))
        {
          line_proc += "\n";//Avoid the last line ending with \0
        }

        if (line_proc.StartsWith("#include "))//note the space
        {
          var inc = ParseIncludeLine(line_proc);

          string? dir = System.IO.Path.GetDirectoryName(loc.RawPath);
          if (dir != null)
          {
            string fs = "";
            if (!String.IsNullOrEmpty(dir))
            {
              fs = System.IO.Path.Combine(dir, inc);
            }
            else
            {
              fs = inc;
            }

            ProcessFile(new FileLoc(fs, loc.FileStorage), file_lines, callnumber + 1);
          }
          else
          {
            Gu.BRThrowException("Directory name" + loc.RawPath + " was null");
          }
        }
        else
        {
          file_lines.Add(line_proc);
        }
      }
      file_lines.Add("//" + new StringBuilder().Insert(0, "->", callnumber).ToString() + "END: " + loc.RawPath + " (" + loc.QualifiedPath + ")\n");


      string ret = "";
      if (firstcall)
      {
        foreach (string line in file_lines)
        {
          ret += line;
        }
      }
      return ret;
    }
  }
  #endregion

  #region ShaderCompiler 

  public class ShaderCompiler
  {
    //      typedef std::vector<ShaderIncludeRef> IncludeVec;
    //      string_t c_strShaderFileVersion = "0.01";
    //      std::shared_ptr<GLContext> _pContext = nullptr;
    //      char** ySrcPtr;               // - The pointer to a the source code.
    //      ShaderStatus::e _loadStatus;  //for temp errors
    //      string_t _error;              //for temp errors
    //      string_t _fileDir;

    public ShaderCompiler()
    {
    }
    //      /**
    //      *  @fn fileToArray()
    //      *  @brief I believe this turns a file into an array of lines.
    //*/
    //void ShaderCompiler::loadSource(std::shared_ptr<ShaderSubProgram> pSubProg)
    //{
    //   AssertOrThrow2(pSubProg != NULL);

    //   _loadStatus = ShaderStatus::Uninitialized;
    //   pSubProg->setStatus(ShaderStatus::Uninitialized);
    //   pSubProg->getSourceLines().clear();

    //   time_t greatestModifyTime = 0;  //TIME_T_MIN;

    //   // - First try to load the srouce
    //   try
    //   {
    //      BRLogDebug("Loading source for Shader " + pSubProg->getSourceLocation());
    //      loadSource_r(pSubProg, pSubProg->getSourceLocation(), pSubProg->getSourceLines(), greatestModifyTime, 0);
    //   }
    //   catch (const Exception&e) {
    //      //pSubProg->debugPrintShaderSource();
    //      _loadStatus = ShaderStatus::CompileError;
    //      _error = e.what();
    //   }

    //   // - If shader is not uninitialized there was an error during loading.
    //   if (_loadStatus != ShaderStatus::Uninitialized)
    //   {
    //      return;
    //   }

    //   // - Cache the modification time of the whole shader include hierarchy
    //   pSubProg->setSourceLastGreatestModificationTime(greatestModifyTime);

    //   pSubProg->setStatus(ShaderStatus::e::Loaded);
    //   }
    //   void ShaderCompiler::loadSource_r(std::shared_ptr < ShaderSubProgram > pSubProg, const string_t&location, std::vector<string_t> & lines, time_t & greatestModifyTime, int_fast32_t iIncludeLevel) {
    //      time_t modTime;

    //      if (pSubProg->getStatus() != ShaderStatus::e::Uninitialized && !ShaderMaker::isGoodStatus(pSubProg->getStatus()))
    //      {
    //         pSubProg->getGeneralErrors().push_back("Subprogram was not in good state.");
    //         return;
    //      }

    //      if (!Gu::getPackage()->fileExists((string_t)location))
    //      {
    //         pSubProg->setStatus(ShaderStatus::e::CompileError);
    //         Gu::debugBreak();
    //         BRThrowException("Could not find shader file or #include file, " + location);
    //      }

    //      // Store the greater modify time for shader cache.
    //      modTime = Gu::getPackage()->getLastModifyTime((string_t)location);
    //      greatestModifyTime = MathUtils::brMax(modTime, greatestModifyTime);

    //      // Load all source bytes
    //      std::shared_ptr<BinaryFile> bf = std::make_shared<BinaryFile>(c_strShaderFileVersion);
    //      loadSourceData(location, bf);

    //      if (_loadStatus != ShaderStatus::Uninitialized)
    //      {
    //         return;
    //      }

    //      // Helps Identify files.
    //      string_t nameHdr = Stz "// ----------- BEGIN " + FileSystem::getFileNameFromPath(location);
    //      addSourceLineAt(0, lines, nameHdr);

    //      // Parse Lines
    //      parseSourceIntoLines(bf, lines);

    //      string_t nameHdr2 = Stz "// ----------- END " + FileSystem::getFileNameFromPath(location);
    //      addSourceLineAt(lines.size(), lines, nameHdr2);

    //      //Indent
    //      //This is probably not good to do (would mess with # directives).
    //      // string_t indent = StringUtil::repeat("  ", iIncludeLevel);
    //      // for (size_t iLine = 0; iLine < lines.size(); ++iLine) {
    //      //   lines[iLine] = indent + lines[iLine];
    //      // }

    //      // Recursively do includes
    //      searchIncludes(pSubProg, lines, greatestModifyTime, iIncludeLevel);
    //   }
    //         void ShaderCompiler::addSourceLineAt(size_t pos, std::vector<string_t> & vec, string_t line) {
    //            string_t linemod = line;
    //            linemod += '\n';
    //            linemod += '\0';

    //            vec.insert(vec.begin() + pos, linemod);
    //         }
    //         /**
    //         *  @fn searchIncludes
    //         *  @brief Includes files.
    //*/
    //         void ShaderCompiler::searchIncludes(std::shared_ptr < ShaderSubProgram > subProg, std::vector<string_t> & lines, time_t & greatestModifyTime, int_fast32_t iIncludeLevel) {
    //            IncludeVec _includes;  //map of include offsets in the data to their source locations.
    //            string_t locStr;
    //            std::vector<string_t> includeLines;
    //            IncludeVec::iterator ite2;
    //            size_t includeOff;

    //            _includes = getIncludes(lines);
    //            IncludeVec::iterator ite = _includes.begin();

    //            uint_fast32_t added_incl_off = 0;
    //            // - Recursively parse all includes
    //            for (; ite != _includes.end(); ++ite) {
    //               includeOff = ite->lineNo;
    //               locStr = *(ite->str);
    //               locStr = FileSystem::combinePath(_fileDir, locStr);
    //               includeLines.clear();

    //               loadSource_r(subProg, locStr, includeLines, greatestModifyTime, iIncludeLevel + 1);

    //               lines.insert(lines.begin() + includeOff, includeLines.begin(), includeLines.end());

    //               ite2 = ite;
    //               ite2++;
    //               for (; ite2 != _includes.end(); ite2++) {
    //                  ite2->lineNo += includeLines.size();
    //               }

    //               delete ite->str;  //delete the allocated string
    //            }
    //         }
    //         /**
    //         *    @fn getIncludes
    //         *    @brief Compiles all includes in the source lines into a map of include to its line number
    //*/
    //         ShaderCompiler::IncludeVec ShaderCompiler::getIncludes(std::vector<string_t> & lines) {
    //            IncludeVec _includes;  //map of include offsets in the data to their source locations.
    //            string_t locStr;

    //            for (size_t i = 0; i < lines.size(); ++i) {
    //               //We're trimming now because we're indenting the files. NOTE this may be invalid 9/2020
    //               locStr = StringUtil::trim(lines[i]);
    //               locStr = locStr.substr(0, 8);
    //               locStr = StringUtil::trim(locStr);
    //               //this check is to make sure there is no space or comments before the include.
    //               if (locStr.compare("#include") != 0) {
    //                  continue;
    //               }

    //               // - Expand the include
    //               // - Expand the include
    //               // - Expand the include
    //               locStr = StringUtil::trim(lines[i]);
    //               lines.erase(lines.begin() + i);
    //               i--;

    //               // - Split our include data
    //               std::vector<string_t> vs = StringUtil::split(locStr, ' ');
    //               vs[0] = StringUtil::trim(vs[0]);

    //               // error checking
    //               if (vs.size() != 2) {
    //                  _loadStatus = ShaderStatus::e::CompileError;
    //                  _error = string_t("Compile Error -->\"") + vs[0] + string_t("\"");

    //                  //free data
    //                  IncludeVec::iterator ite = _includes.begin();
    //                  for (; ite != _includes.end(); ite++)
    //                     delete ite->str;
    //                  BRThrowException("Compile Error -->\"Not enough arguments for include directive. \"");
    //               }

    //               if (vs[0].compare("#include") != 0) {
    //                  _loadStatus = ShaderStatus::e::CompileError;
    //                  _error = string_t("Compile Error -->\"") + vs[0] + string_t("\"");
    //                  //free data
    //                  IncludeVec::iterator ite = _includes.begin();
    //                  for (; ite != _includes.end(); ite++)
    //                     delete ite->str;
    //                  BRThrowException("Compile Error -->\"Not enough arguments for include directive. \"");
    //               }

    //               vs[1] = StringUtil::trim(vs[1]);
    //               vs[1] = StringUtil::stripDoubleQuotes(vs[1]);

    //               // - Insert the include by its offset in our base data so we can go back and paste it in.
    //               ShaderIncludeRef srf;
    //               srf.str = new string_t(vs[1]);
    //               srf.lineNo = i + 1;
    //               _includes.push_back(srf);
    //            }

    //            return _includes;
    //         }
    //         void ShaderCompiler::loadSourceData(const string_t&location, std::shared_ptr<BinaryFile> __out_ sourceData) {
    //            if (!Gu::getPackage()->fileExists(location)) {
    //               sourceData = NULL;
    //               _loadStatus = ShaderStatus::e::FileNotFound;
    //               BRLogError("Shader Source File not found : " + location);
    //               BRLogError(" CWD: " + FileSystem::getCurrentDirectory());
    //               return;
    //            }

    //            Gu::getPackage()->getFile(location, sourceData, true);
    //         }
    //         /**
    //          * @param data [in] The binary data.
    //          * @param out_lines [inout] The source file lines.
    //          */
    //         void ShaderCompiler::parseSourceIntoLines(std::shared_ptr < BinaryFile > data, std::vector<string_t> & out_lines) {
    //            // - Parse file into lines
    //            string_t strTemp;
    //            char* c = data->getData().ptr(), *d;
    //            int len;
    //            int temp_filesize = 0;
    //            int filesize = (int)data->getData().count();

    //            while (temp_filesize < (int)filesize) {
    //               d = c;
    //               len = 0;
    //               strTemp.clear();
    //               while (((temp_filesize + len) < filesize) && ((int)(*d)) && ((int)(*d) != ('\n')) && ((int)(*d) != ('\r'))) {
    //                  len++;
    //                  d++;
    //               }

    //               d = c;
    //               len = 0;  // - Reuse of len.  It is not the length now but an index.
    //               while (((temp_filesize + len) < filesize) && ((int)(*d)) && ((int)(*d) != ('\n')) && ((int)(*d) != ('\r'))) {
    //                  strTemp += (*d);
    //                  d++;
    //               }

    //               // - We want newlines. Also this removes the \r
    //               if (((*d) == '\n') || ((*d) == '\r') || ((*d) == '\0')) {
    //                  strTemp += '\n';
    //               }

    //               if (strTemp.length()) {
    //                  strTemp += '\0';
    //                  out_lines.push_back(strTemp);
    //                  c += strTemp.length() - 1;
    //                  temp_filesize += (int)strTemp.length() - 1;
    //               }

    //               // - Remove any file format garbage at the end (windows)
    //               len = 0;
    //               while (((temp_filesize + len) < filesize) && (((int)(*c) == ('\r')))) {
    //                  len++;
    //                  c++;
    //               }

    //               // increment the Newline. !important
    //               if (((temp_filesize + len) < filesize) && (((int)(*c) == ('\n')))) {
    //                  len++;
    //                  c++;
    //               }
    //               temp_filesize += len;
    //            }
    //         }
    //         /**
    //         *  @fn compile
    //         *  @brief Compile a shader.
    //         *  @remarks Compiles a shader.
    //*/
    //         void ShaderCompiler::compile(std::shared_ptr < ShaderSubProgram > pSubProg) {
    //            BRLogInfo("Compiling shader " + pSubProg->getSourceLocation());

    //            //DOWNCAST:
    //            // GLstd::shared_ptr<ShaderSubProgram> shader = dynamic_cast<GLstd::shared_ptr<ShaderSubProgram>>(pSubProg);
    //            GLint b;

    //            if (pSubProg->getStatus() != ShaderStatus::e::Loaded) {
    //               BRThrowException("Shader was in an invalid state when trying to compile.");
    //            }

    //            GLchar** arg = new char*[pSubProg->getSourceLines().size()];
    //            for (size_t i = 0; i < pSubProg->getSourceLines().size(); ++i) {
    //               if (pSubProg->getSourceLines()[i].size()) {
    //                  arg[i] = new char[pSubProg->getSourceLines()[i].size()];
    //                  std::memcpy(arg[i], pSubProg->getSourceLines()[i].c_str(), pSubProg->getSourceLines()[i].size());
    //                  //Windows..Error
    //                  //memcpy_s(arg[i], pSubProg->getSourceLines()[i].size(), pSubProg->getSourceLines()[i].c_str(), pSubProg->getSourceLines()[i].size());
    //               }
    //            }

    //            _pContext->glShaderSource(pSubProg->getGlId(), (GLsizei)pSubProg->getSourceLines().size(), (const GLchar**)arg, NULL);
    //            _pContext->glCompileShader(pSubProg->getGlId());
    //            _pContext->glGetShaderiv(pSubProg->getGlId(), GL_COMPILE_STATUS, &b);

    //            // - Gets the Gpu's error list.  This may also include warnings and stuff.
    //            pSubProg->getCompileErrors() = getErrorList(pSubProg);

    //            //  if (EngineSetup::getSystemConfig()->getPrintShaderSourceOnError() == TRUE)
    //            {
    //               if (pSubProg->getCompileErrors().size() > 0) {
    //                  string_t str = pSubProg->getHumanReadableErrorString();
    //                  if (StringUtil::lowercase(str).find("error") != string_t::npos) {
    //                     pSubProg->debugPrintShaderSource();
    //                     BRLogErrorNoStack(str);
    //                     Gu::debugBreak();
    //                  }
    //                  else {
    //                     BRLogWarn(str);
    //                  }
    //               }
    //            }

    //            //OOPS you didn't delete arg[]
    //            //6/10/21 - This needs to be tested. This is a memory leak.
    //            // Gu::debugBreak();
    //            for (size_t i = 0; i < pSubProg->getSourceLines().size(); ++i) {
    //               if (pSubProg->getSourceLines()[i].size()) {
    //                  delete[] arg[i];
    //               }
    //            }
    //            delete[] arg;

    //            if (!b) {
    //               pSubProg->setStatus(ShaderStatus::CompileError);
    //            }
    //            else {
    //               pSubProg->setStatus(ShaderStatus::Compiled);
    //            }
    //         }
    //         /**
    //         *    @fn getErrorList()
    //         *    @brief Returns a list of strings that are the errors of the compiled shader source.
    //*/
    //         std::vector<string_t> ShaderCompiler::getErrorList(const std::shared_ptr<ShaderSubProgram> shader) const {
    //            int buf_size = 16384;
    //            char* log_out = (char*)GameMemoryManager::allocBlock(buf_size);
    //            GLsizei length_out;

    //            _pContext->glGetShaderInfoLog(shader->getGlId(), buf_size, &length_out, log_out);

    //            std::vector<string_t> ret;
    //            string_t tempStr;
    //            char* c = log_out;

    //            while ((*c)) {
    //               while (((*c) != '\n') && ((*c))) {
    //                  tempStr += (*c);
    //                  c++;
    //               }
    //               ret.push_back(tempStr);
    //               tempStr.clear();
    //               c++;
    //            }
    //            GameMemoryManager::freeBlock(log_out);

    //            return ret;
    //         }

  }
  #endregion

  #region ShaderCache

  public class GLProgramBinary
  {

    //  public GLProgramBinary(ShaderCache* cc, size_t binLength) : _pShaderCache(cc),
    //                                                                   _binaryLength(binLength),
    //                                                                   _binaryData(NULL),
    //                                                                   _compileTime(0)
    //   {
    //      _binaryData = new char[binLength];
    //   }
    //   //GLProgramBinary::~GLProgramBinary()
    //   //{
    //   //   if (_binaryData)
    //   //   {
    //   //      delete[] _binaryData;
    //   //   }
    //   //   _binaryData = NULL;
    //   //}

    //   GLenum _glFormat;
    //size_t _binaryLength;
    //char* _binaryData;
    //ShaderCache* _pShaderCache;
    //time_t _compileTime;    // Time the binary was compiled.
  };

  /**
  *  @class ShaderCache
  *  @brief Caches shader binaries.
*/
  public class ShaderCache //: public GLFramework
  {
    //   bool _bCacheIsSupported = false;
    //   std::vector<GLProgramBinary*> _vecBinaries;
    //   string_t _strCacheDirectory;

    //      ShaderCache::ShaderCache(std::shared_ptr<GLContext> ct, string_t cacheDir) : GLFramework(ct)
    //      {
    //         _strCacheDirectory = cacheDir;
    //         GLint n;
    //         glGetIntegerv(GL_NUM_PROGRAM_BINARY_FORMATS, &n);

    //         if (n <= 0)
    //         {
    //            BRLogWarn("[ShaderCache] Gpu does not support any program binary formats.");
    //            _bCacheIsSupported = false;
    //         }
    //      }
    //      ShaderCache::~ShaderCache()
    //      {
    //         for (size_t i = 0; i < _vecBinaries.size(); ++i)
    //            delete _vecBinaries[i];
    //         _vecBinaries.resize(0);
    //      }
    //      string_t ShaderCache::getBinaryNameFromProgramName(const string_t& progName) {
    //  string_t fb = progName + ".sb";
    //  return FileSystem::combinePath(_strCacheDirectory, fb);  //::appendCacheDirectory(fb);
    //}
    //   GLProgramBinary* ShaderCache::getBinaryFromGpu(std::shared_ptr<ShaderBase> prog)
    //   {
    //      GLint binBufSz = 0;
    //      GLint outLen = 0;

    //      getContext()->glGetProgramiv(prog->getGlId(), GL_PROGRAM_BINARY_LENGTH, &binBufSz);
    //      getContext()->chkErrRt();

    //      if (binBufSz == 0 || binBufSz > MemSize::e::MEMSZ_GIG2)
    //      {
    //         BRThrowException("Shader program binary was 0 or exceeded " + MemSize::e::MEMSZ_GIG2 + " bytes; actual: " + binBufSz);
    //      }

    //      GLProgramBinary* b = new GLProgramBinary(this, binBufSz);

    //      getContext()->glGetProgramBinary(prog->getGlId(), binBufSz, &outLen, &(b->_glFormat), (void*)b->_binaryData);
    //      getContext()->chkErrRt();

    //      if (binBufSz != outLen)
    //      {
    //         delete b;
    //         BRThrowException("GPU reported incorrect program size and returned a program with a different size.");
    //      }

    //      //Critical: set compile time.
    //      b->_compileTime = prog->getCompileTime();

    //      _vecBinaries.push_back(b);

    //      return b;
    //   }
    //   /**
    //   *  @fn getBinaryFromDisk
    //   *  @brief Pass in the program name, not the name of the binary.
    //   * 
    //   * 
    //   * Shader Binary Cache File Format
    //   * 
    //   * Extension: .sb
    //   * 
    //   *     compile time (int64)
    //   *     shader format (int32)
    //   *     binary size (int32)
    //   *     binary (char*)
    //   * 
    //*/
    //   GLProgramBinary* ShaderCache::getBinaryFromDisk(string_t& programName)
    //   {
    //      DiskFile df;
    //      GLProgramBinary* pbin = nullptr;
    //      GLenum glenum;
    //      size_t binSz;
    //      string_t binaryName = getBinaryNameFromProgramName(programName);

    //      time_t compTime;

    //      if (!FileSystem::fileExists(binaryName))
    //      {
    //         BRLogDebug(string_t("Program binary not found: ") + binaryName);

    //         return nullptr;
    //      }

    //      BRLogDebug(string_t("Loading program binary ") + binaryName);
    //      try
    //      {
    //         df.openForRead(DiskLoc(binaryName));
    //         df.read((char*)&(compTime), sizeof(time_t));
    //         df.read((char*)&(glenum), sizeof(glenum));
    //         df.read((char*)&(binSz), sizeof(binSz));

    //         // if we're too big - freak out
    //         if ((binSz < 0) || (binSz > MemSize::e::MEMSZ_GIG2))
    //         {
    //            BRLogError("Invalid shader binary file size '" + binSz + "', recompiling binary.");
    //            pbin = nullptr;
    //         }
    //         else
    //         {
    //            pbin = new GLProgramBinary(this, binSz);
    //            pbin->_glFormat = glenum;
    //            pbin->_compileTime = compTime;

    //            df.read((char*)pbin->_binaryData, pbin->_binaryLength);
    //            df.close();

    //            _vecBinaries.push_back(pbin);
    //         }
    //      }
    //      catch (const Exception&ex) {
    //         //fail silently

    //         BRLogError("Failed to load program binary " + binaryName + ex.what());
    //         if (pbin)
    //         {
    //            delete pbin;
    //         }
    //         pbin = nullptr;
    //      }

    //      return pbin;
    //      }
    //      void ShaderCache::saveBinaryToDisk(const string_t&programName, GLProgramBinary* bin) {
    //         DiskFile df;
    //         string_t binaryName = getBinaryNameFromProgramName(programName);
    //         string_t binPath = FileSystem::getDirectoryNameFromPath(binaryName);

    //         BRLogInfo(" Shader program Bin path = " + binPath);

    //         try
    //         {
    //            BRLogDebug(string_t("[ShaderCache] Caching program binary ") + binaryName);
    //            FileSystem::createDirectoryRecursive(binPath);
    //            df.openForWrite(DiskLoc(binaryName), FileWriteMode::Truncate);
    //            df.write((char*)&(bin->_compileTime), sizeof(bin->_compileTime));
    //            df.write((char*)&(bin->_glFormat), sizeof(bin->_glFormat));
    //            df.write((char*)&(bin->_binaryLength), sizeof(bin->_binaryLength));
    //            df.write((char*)bin->_binaryData, bin->_binaryLength);
    //            df.close();
    //         }
    //         catch (const Exception&ex) {
    //            BRLogError("Failed to save program binary " + binaryName + ex.what());
    //         }
    //         }
    //         void ShaderCache::deleteBinaryFromDisk(const string_t&programName) {
    //            string_t binaryName = getBinaryNameFromProgramName(programName);

    //            if (!FileSystem::fileExists(binaryName))
    //            {
    //               BRLogError(string_t("Failed to delete file ") + binaryName);
    //            }

    //            FileSystem::deleteFile(binaryName);
    //         }
    //         void ShaderCache::freeLoadedBinary(GLProgramBinary * bin) {
    //            _vecBinaries.erase(std::remove(_vecBinaries.begin(), _vecBinaries.end(), bin), _vecBinaries.end());
    //            delete bin;
    //         }
    //         void ShaderCache::saveCompiledBinaryToDisk(std::shared_ptr < ShaderBase > pProgram) {
    //            GLProgramBinary* bin = getBinaryFromGpu(pProgram);
    //            saveBinaryToDisk(pProgram->getProgramName(), bin);
    //         }
    //         /**
    //         *  @fn tryLoadCachedBinary
    //         *  @brief Try to load a cached GLSL binary to the GPU.
    //         *  @return false if the load failed or file was not found.
    //         *  @return true if the program loaded successfully
    //*/
    //         std::shared_ptr<ShaderBase> ShaderCache::tryLoadCachedBinary(std::string programName, std::vector < string_t > shaderFiles) {
    //            bool bSuccess = false;
    //            GLProgramBinary* binary;
    //            std::shared_ptr<ShaderBase> ret = nullptr;

    //            binary = getBinaryFromDisk(programName);

    //            if (binary != NULL)
    //            {
    //               time_t maxTime = 0;
    //               for (string_t file : shaderFiles)
    //               {
    //                  FileInfo inf = FileSystem::getFileInfo(file);
    //                  if (!inf._exists)
    //                  {
    //                     BRLogError("Shader source file '" + file + "' does not exist.");
    //                     Gu::debugBreak();
    //                  }
    //                  else
    //                  {
    //                     maxTime = MathUtils::brMax(inf._modified, maxTime);
    //                  }
    //               }

    //               if (binary->_compileTime >= maxTime)
    //               {
    //                  //pProgram has already asked GL for an ID.
    //                  try
    //                  {
    //                     ret = loadBinaryToGpu(programName, binary);
    //                     if (ret == nullptr)
    //                     {
    //                        BRLogInfo("Program binary for '" + programName + "' out of date.  Deleting from disk");
    //                        deleteBinaryFromDisk(programName);
    //                     }
    //                  }
    //                  catch (const Exception&e) {
    //                     BRLogWarn("[ShaderCache] Loading program binary returned warnings/errors:\r\n");
    //                     BRLogWarn(e.what());
    //                     deleteBinaryFromDisk(programName);
    //                  }
    //                  }

    //                  freeLoadedBinary(binary);
    //               }

    //               return ret;
    //            }
    //            /**
    //            *  @fn loadBinaryToGpu
    //            *  @brief Attaches the binary to the already created program object and loads it to the GPU. Prog must already have been created.
    //            *  @return false if the program returned errors.
    //*/
    //            std::shared_ptr<ShaderBase> ShaderCache::loadBinaryToGpu(std::string programName, GLProgramBinary * bin) {
    //               getContext()->chkErrRt();

    //               std::shared_ptr<ShaderBase> pProgram = std::make_shared<ShaderBase>(getContext(), programName);
    //               pProgram->init();
    //               getContext()->chkErrRt();

    //               GLboolean b1 = getContext()->glIsProgram(pProgram->getGlId());
    //               if (b1 == false)
    //               {
    //                  BRLogWarn("[ShaderCache] Program was not valid before loading to GPU");
    //                  return nullptr;
    //               }
    //               getContext()->chkErrRt();

    //               BRLogDebug("[ShaderCache] Loading Cached Program Binary to GPU");
    //               getContext()->glProgramBinary(pProgram->getGlId(), bin->_glFormat, (void*)bin->_binaryData, (GLsizei)bin->_binaryLength);
    //               if (getContext()->chkErrRt(true, true))
    //               {
    //                  //If we have en error here, we failed to load the binary.
    //                  BRLogWarn("[ShaderCache] Failed to load binary to GPU - we might be on a different platform.");
    //                  return nullptr;
    //               }

    //               //Print Log
    //               std::vector<string_t> inf;
    //               pProgram->getProgramErrorLog(inf);
    //               for (size_t i = 0; i < inf.size(); ++i)
    //               {
    //                  BRLogWarn("   " + inf[i]);
    //               }

    //               //validate program.
    //               GLint iValid;
    //               getContext()->glValidateProgram(pProgram->getGlId());
    //               getContext()->chkErrRt();

    //               getContext()->glGetProgramiv(pProgram->getGlId(), GL_VALIDATE_STATUS, (GLint*)&iValid);
    //               getContext()->chkErrRt();

    //               if (iValid == GL_FALSE)
    //               {
    //                  // Program load faiiled
    //                  BRLogWarn("[ShaderCache] glValidateProgram says program binary load failed.  Check the above logs for errors.");
    //                  return nullptr;
    //               }

    //               GLboolean b2 = getContext()->glIsProgram(pProgram->getGlId());
    //               getContext()->chkErrRt();

    //               if (b2 == false)
    //               {
    //                  BRThrowException("[ShaderCache] glIsProgram says program was not valid after loading to GPU");
    //               }

    //               pProgram->bind();
    //               // - If the program failed to load it will raise an error after failing to bind.
    //               GLenum e = glGetError();
    //               if (e != GL_NO_ERROR)
    //               {
    //                  BRLogWarn("[ShaderCache], GL error " + StringUtil::toHex(e, true) + " , program was not valid after loading to GPU.");
    //                  return nullptr;
    //               }

    //               //Save Name.
    //               getContext()->setObjectLabel(GL_PROGRAM, pProgram->getGlId(), pProgram->getProgramName());

    //               pProgram->unbind();
    //               getContext()->chkErrRt();

    //               return pProgram;
    //            }



  };

  #endregion



}
