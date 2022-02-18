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
         Gu.CheckGpuErrorsRt();
         GL.ShaderSource(_glId, src);
         Gu.CheckGpuErrorsRt();
         GL.CompileShader(_glId);
         Gu.CheckGpuErrorsRt();
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
   public class Shader : OpenGLResource
   {
      ShaderStage _vertexStage = null;
      ShaderStage _fragmentStage = null;
      ShaderStage _geomStage = null;

      int _viewMatrixLocation;
      int _modelMatrixLocation;
      int _projectionMatrixLocation;
      int _texLocation;
      int _camPosLocation;
      int _lightingModelLocation;
      int _GGX_XLocation;
      int _GGX_YLocation;
      public float GGX_X = .8f;
      public float GGX_Y = .8f;
      public int lightingModel = 2;
      List<string> _shaderErrors = new List<string>();

      ShaderLoadState State = ShaderLoadState.None;

      private static Shader _defaultDiffuseShader = null;
      public static Shader DefaultDiffuse()
      {
         //Returns a basic v3 n3 x2 lambert+blinn-phong shader.
         if (_defaultDiffuseShader == null)
         {
            string frag = Gu.ReadTextFile(Gu.EmbeddedDataPath + "BasicShader_frag.glsl", true);
            string vert = Gu.ReadTextFile(Gu.EmbeddedDataPath + "BasicShader_vert.glsl", true);
            _defaultDiffuseShader = new Shader(vert, frag);
         }
         return _defaultDiffuseShader;
      }
      public Shader(string vsSrc = "", string psSrc = "", string gsSrc="")
      {
         Gu.CheckGpuErrorsDbg();
         {
            State = ShaderLoadState.Loading;
            CreateShaders(vsSrc, psSrc, gsSrc);
            CreateProgram();
            if (State != ShaderLoadState.Failed)
            {
               GL.UseProgram(_glId);
               QueryMatrixLocations();

               State = ShaderLoadState.Success;
            }
            else
            {
               Gu.Log.Error("Failed to load shader.\r\n" + String.Join("\r\n", _shaderErrors.ToArray()));
               Gu.DebugBreak();
            }
         }
         Gu.CheckGpuErrorsDbg();
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
         Gu.CheckGpuErrorsDbg();
         {
            GL.UseProgram(_glId);
         }
         Gu.CheckGpuErrorsDbg();
      }
      public void Unbind()
      {
         Gu.CheckGpuErrorsDbg();
         {
            GL.UseProgram(0);
         }
         Gu.CheckGpuErrorsDbg();
      }
      Vec3f _camvpos;
      //int _normalMatrixLocation=0;
      public void UpdateAndBind(double dt, Camera3D cam, Mat4f model)
      {
         //**Pre - render - update uniforms.
         Gu.CheckGpuErrorsDbg();
         {
            Bind();

            var v_mat_tk = cam.ViewMatrix;
            var p_mat_tk = cam.ProjectionMatrix;
            var m_mat_tk = model;
            var n_mat_tk = model.Inverted();

            //var a = model.inverseOf() * model;

            GL.UniformMatrix4(_viewMatrixLocation, false, ref v_mat_tk);
            Gu.CheckGpuErrorsDbg();
            GL.UniformMatrix4(_projectionMatrixLocation, false, ref p_mat_tk);
            Gu.CheckGpuErrorsDbg();
            GL.UniformMatrix4(_modelMatrixLocation, false, ref m_mat_tk);
            Gu.CheckGpuErrorsDbg();
            //GL.UniformMatrix4(_normalMatrixLocation, false, ref n_mat_tk);
            //Gu.CheckGpuErrorsDbg();

            GL.Uniform1(_texLocation, TextureUnit.Texture0 - TextureUnit.Texture0);
            Gu.CheckGpuErrorsRt();
            GL.Uniform1(_lightingModelLocation, lightingModel);
            Gu.CheckGpuErrorsRt();
            GL.Uniform1(_GGX_XLocation, GGX_X);
            GL.Uniform1(_GGX_YLocation, GGX_Y);
            Gu.CheckGpuErrorsRt();

            _camvpos = cam.Position;

            GL.ProgramUniform3(_glId, _camPosLocation, _camvpos.X, _camvpos.Y, _camvpos.Z);
            Gu.CheckGpuErrorsDbg();
         }
         Gu.CheckGpuErrorsDbg();
      }
      #region Private
      private void CreateShaders(string vs, string ps, string gs="")
      {
         Gu.CheckGpuErrorsRt();
         {
            _vertexStage = new ShaderStage(ShaderType.VertexShader, vs);
            _fragmentStage = new ShaderStage(ShaderType.FragmentShader, ps);
            if (!string.IsNullOrEmpty(gs))
            {
               _geomStage = new ShaderStage(ShaderType.GeometryShader, gs);
            }
         }
         Gu.CheckGpuErrorsRt();
      }
      private void CreateProgram()
      {
         Gu.CheckGpuErrorsRt();
         {
            _glId = GL.CreateProgram();

            GL.AttachShader(_glId, _vertexStage.GetGlId());
            Gu.CheckGpuErrorsRt();
            GL.AttachShader(_glId, _fragmentStage.GetGlId());
            Gu.CheckGpuErrorsRt();
            if (_geomStage != null)
            {
               GL.AttachShader(_glId, _geomStage.GetGlId());
               Gu.CheckGpuErrorsRt();
            }

            GL.LinkProgram(_glId);
            Gu.CheckGpuErrorsRt();

            string programInfoLog;
            GL.GetProgramInfoLog(_glId, out programInfoLog);
            _shaderErrors = programInfoLog.Split('\n').ToList();

            if (_shaderErrors.Count > 0 && programInfoLog.ToLower().Contains("error"))
            {
               State = ShaderLoadState.Failed;
            }

         }
         Gu.CheckGpuErrorsRt();
      }
      private void FindUniformOrDieTryin(ref int loc, string name)
      {
         if ((loc = GL.GetUniformLocation(_glId, name)) == 0)
         {
            Gu.Log.Error("Failed to find uniform location '" + name + "'");
         }

      }
      private void QueryMatrixLocations()
      {
         Gu.CheckGpuErrorsDbg();
         {
            FindUniformOrDieTryin(ref _projectionMatrixLocation, "projection_matrix");
            FindUniformOrDieTryin(ref _viewMatrixLocation, "view_matrix");
            FindUniformOrDieTryin(ref _modelMatrixLocation, "model_matrix");
            FindUniformOrDieTryin(ref _texLocation, "_ufTextureId_i0");
            FindUniformOrDieTryin(ref _camPosLocation, "cam_pos");
            FindUniformOrDieTryin(ref _lightingModelLocation, "lightingModel");
            FindUniformOrDieTryin(ref _GGX_XLocation, "GGX_X");
            FindUniformOrDieTryin(ref _GGX_YLocation, "GGX_Y");

            Gu.CheckGpuErrorsRt();
         }
         Gu.CheckGpuErrorsDbg();
      }
      
      #endregion

   }
}
