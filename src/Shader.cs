using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using System.Diagnostics;

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
      public override void Dispose()
      {
         if (GL.IsShader(_glId))
         {
            GL.DeleteShader(_glId);
         }
         base.Dispose();
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
   //Shader, program on the GPU.
   public class Shader : OpenGLResource
   {
      private ShaderStage _vertexStage = null;
      private ShaderStage _fragmentStage = null;
      private ShaderStage _geomStage = null;

      private Dictionary<string, ShaderUniform> _uniforms = new Dictionary<string, ShaderUniform>();

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
            _defaultFlatColorShader = LoadShader("v_v3", false);
         }
         return _defaultFlatColorShader;
      }
      public static Shader DefaultDiffuse()
      {
         //Returns a basic v3 n3 x2 lambert+blinn-phong shader.
         if (_defaultDiffuseShader == null)
         {
            _defaultDiffuseShader = LoadShader("v_v3n3x2", false);
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
               Gu.DebugBreak();
            }
         }
         Gpu.CheckGpuErrorsDbg();
      }
      public override void Dispose()
      {
         if (GL.IsProgram(_glId))
         {
            GL.DeleteProgram(_glId);
         }
         base.Dispose();
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
      public static Shader LoadShader(string generic_name, bool gs)
      {
         string vert = Gu.ReadTextFile(new FileLoc(generic_name + ".vs.glsl", FileStorage.Embedded));
         string geom = gs ? Gu.ReadTextFile(new FileLoc(generic_name + ".gs.glsl", FileStorage.Embedded)) : "";
         string frag = Gu.ReadTextFile(new FileLoc(generic_name + ".fs.glsl", FileStorage.Embedded));
         Shader ret = new Shader(generic_name, vert, frag, geom);
         return ret;
      }

      public void BeginRender(double dt, Camera3D cam, WorldObject ob, Material m)
      {
         //**Pre - render - update uniforms.
         Gpu.CheckGpuErrorsDbg();
         {
            //Reset
            _currUnit = TextureUnit.Texture0;
            _boundTextures.Clear();

            Bind();
            BindUniforms(dt, cam, ob, m);
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

            u_name = u_name.Substring(0, u_name_len);

            ShaderUniform su = new ShaderUniform(i, u_size, u_type, u_name);
            _uniforms.Add(u_name, su);
         }
      }
      private void BindUniforms(double dt, Camera3D cam, WorldObject ob, Material m)
      {
         foreach (var u in _uniforms.Values)
         {
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
               GL.Uniform1(u.Location,  nmap);
            }
            
            else
            {
               Gu.Log.WarnCycle("Unknown uniform variable '" + u.Name + "'.");
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
   }
   #endregion

}
