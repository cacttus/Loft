using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector3;
using Mat3f = OpenTK.Matrix3;
using Mat4f = OpenTK.Matrix4;
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
   public class Shader : OpenGLResource
   {
      private ShaderStage _vertexStage = null;
      private ShaderStage _fragmentStage = null;
      private ShaderStage _geomStage = null;

      private Dictionary<string, ShaderUniform> _uniforms = new Dictionary<string, ShaderUniform>();

      //Just debug stuff that will go away.
      public float GGX_X = .8f;
      public float GGX_Y = .8f;
      public int lightingModel = 2;

      private List<string> _shaderErrors = new List<string>();

      private ShaderLoadState State = ShaderLoadState.None;

      private static Shader _defaultDiffuseShader = null;
      private static Shader _defaultFlatColorShader = null;

      public string Name { get; private set; } = "<unset>";

      public static Shader DefaultFlatColorShader()
      {
         if (_defaultFlatColorShader == null)
         {
            _defaultFlatColorShader = LoadShaderGeneric("v_v3", false);
         }
         return _defaultFlatColorShader;
      }
      public static Shader DefaultDiffuse()
      {
         //Returns a basic v3 n3 x2 lambert+blinn-phong shader.
         if (_defaultDiffuseShader == null)
         {
            _defaultDiffuseShader = LoadShaderGeneric("v_v3n3x2", false);
         }
         return _defaultDiffuseShader;
      }
      private static Shader LoadShaderGeneric(string generic_name, bool gs)
      {
         Shader ret = null;
         string vert = Gu.ReadTextFile(Gu.EmbeddedDataPath + generic_name + ".vs.glsl", true);
         string geom = gs ? Gu.ReadTextFile(Gu.EmbeddedDataPath + generic_name + ".gs.glsl", true) : "";
         string frag = Gu.ReadTextFile(Gu.EmbeddedDataPath + generic_name + ".fs.glsl", true);
         ret = new Shader(generic_name, vert, frag, geom);
         return ret;
      }
      public Shader(string name, string vsSrc = "", string psSrc = "", string gsSrc = "")
      {
         Name = name;
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
      public void Bind()
      {
         Gpu.CheckGpuErrorsDbg();
         {
            GL.UseProgram(_glId);
         }
         Gpu.CheckGpuErrorsDbg();
      }
      public void Unbind()
      {
         Gpu.CheckGpuErrorsDbg();
         {
            GL.UseProgram(0);
         }
         Gpu.CheckGpuErrorsDbg();
      }
      public void UpdateAndBind(double dt, Camera3D cam, WorldObject ob)
      {
         //**Pre - render - update uniforms.
         Gpu.CheckGpuErrorsDbg();
         {
            Bind();
            BindUniforms(dt, cam, ob);
         }
         Gpu.CheckGpuErrorsDbg();
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

            string programInfoLog;
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
      private void BindUniforms(double dt, Camera3D cam, WorldObject ob)
      {
         foreach (var u in _uniforms.Values)
         {
            //bind uniforms based on name.
            switch (u.Name)
            {
               case "_ufCamera_Position":
                  GL.ProgramUniform3(_glId, u.Location, cam.Position.X, cam.Position.Y, cam.Position.Z);
                  break;
               case "_ufLightModel_GGX_X":
                  GL.Uniform1(u.Location, GGX_X);
                  break;
               case "_ufLightModel_GGX_Y":
                  GL.Uniform1(u.Location, GGX_Y);
                  break;
               case "_ufLightModel_Index":
                  GL.Uniform1(u.Location, lightingModel);
                  break;
               case "_ufTextureId_i0":
                  GL.Uniform1(u.Location, TextureUnit.Texture0 - TextureUnit.Texture0);
                  break;
               case "_ufWorldObject_Color":
                  GL.ProgramUniform4(_glId, u.Location, ob.Color.X, ob.Color.Y, ob.Color.Z, ob.Color.W);
                  break;
               case "_ufMatrix_Normal":
                  var n_mat_tk = ob.World.Inverted();
                  GL.UniformMatrix4(u.Location, false, ref n_mat_tk);
                  break;
               case "_ufMatrix_Model":
                  var m_mat_tk = ob.World;
                  GL.UniformMatrix4(u.Location, false, ref m_mat_tk);
                  break;
               case "_ufMatrix_View":
                  var v_mat_tk = cam.ViewMatrix;
                  GL.UniformMatrix4(u.Location, false, ref v_mat_tk);
                  break;
               case "_ufMatrix_Projection":
                  var p_mat_tk = cam.ProjectionMatrix;
                  GL.UniformMatrix4(u.Location, false, ref p_mat_tk);
                  break;
               default:
                  Gu.Log.Warn("Unknown uniform variable '" + u.Name + "'.");
                  break;
            }

            //Check for errors.
            Gpu.CheckGpuErrorsDbg();
         }
      }
      #endregion

   }
}
