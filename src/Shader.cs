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
    public class Shader
    {
        int _vertexShaderHandle;
        int _fragmentShaderHandle;
        int _shaderProgramHandle;
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
        public Shader(string vsSrc = "", string psSrc = "")
        {
            Gu.CheckGpuErrorsDbg();
            {
                State = ShaderLoadState.Loading;
                CreateShaders(vsSrc, psSrc);
                CreateProgram();
                if (State != ShaderLoadState.Failed)
                {
                    GL.UseProgram(_shaderProgramHandle);
                    QueryMatrixLocations();

                    State = ShaderLoadState.Success;
                }
                else
                {
                    Gu.Log.Error("Failed to load shader.\r\n" + String.Join("\r\n", _shaderErrors.ToArray()));
                }
            }
            Gu.CheckGpuErrorsDbg();
        }
        public void Bind()
        {
            Gu.CheckGpuErrorsDbg();
            {
                GL.UseProgram(_shaderProgramHandle);
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

                GL.ProgramUniform3(_shaderProgramHandle, _camPosLocation, _camvpos.X, _camvpos.Y, _camvpos.Z);
                Gu.CheckGpuErrorsDbg();
            }
            Gu.CheckGpuErrorsDbg();
        }
        #region Private
        private void CreateShaders(string vsSrc, string psSrc)
        {
            Gu.CheckGpuErrorsDbg();
            {
                _vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
                Gu.CheckGpuErrorsDbg();
                _fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);
                Gu.CheckGpuErrorsDbg();

                GL.ShaderSource(_vertexShaderHandle, vsSrc);
                Gu.CheckGpuErrorsDbg();

                GL.ShaderSource(_fragmentShaderHandle, psSrc);
                Gu.CheckGpuErrorsDbg();

                GL.CompileShader(_vertexShaderHandle);
                Gu.CheckGpuErrorsDbg();
                GL.CompileShader(_fragmentShaderHandle);
                Gu.CheckGpuErrorsDbg();
            }
            Gu.CheckGpuErrorsDbg();
        }
        private void CreateProgram()
        {
            Gu.CheckGpuErrorsDbg();
            {
                _shaderProgramHandle = GL.CreateProgram();

                GL.AttachShader(_shaderProgramHandle, _vertexShaderHandle);
                Gu.CheckGpuErrorsDbg();
                GL.AttachShader(_shaderProgramHandle, _fragmentShaderHandle);
                Gu.CheckGpuErrorsDbg();

                GL.LinkProgram(_shaderProgramHandle);
                Gu.CheckGpuErrorsDbg();

                string programInfoLog;
                GL.GetProgramInfoLog(_shaderProgramHandle, out programInfoLog);
                _shaderErrors = programInfoLog.Split('\n').ToList();

                if (_shaderErrors.Count > 0 && programInfoLog.ToLower().Contains("error"))
                {
                    State = ShaderLoadState.Failed;
                }

                Debug.WriteLine(programInfoLog);
            }
            Gu.CheckGpuErrorsDbg();
        }
        private void FindUniformOrDieTryin(ref int loc, string name)
        {
            if ((loc = GL.GetUniformLocation(_shaderProgramHandle, name)) == 0)
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
                // _normalMatrixLocation = GL.GetUniformLocation(_shaderProgramHandle, "normal_matrix");
            }
            Gu.CheckGpuErrorsDbg();
        }
        #endregion

        //private void LoadVertexPositions()
        //{
        //    GL.GenBuffers(1, out positionVboHandle);
        //    GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
        //    GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
        //        new IntPtr(positionVboData.Length * Vector3.SizeInBytes),
        //        positionVboData, BufferUsageHint.StaticDraw);

        //    GL.EnableVertexAttribArray(0);
        //    GL.BindAttribLocation(shaderProgramHandle, 0, "vertex_position");
        //    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
        //}

        //private void LoadVertexNormals()
        //{
        //    GL.GenBuffers(1, out normalVboHandle);
        //    GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
        //    GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
        //        new IntPtr(positionVboData.Length * Vector3.SizeInBytes),
        //        positionVboData, BufferUsageHint.StaticDraw);

        //    GL.EnableVertexAttribArray(1);
        //    GL.BindAttribLocation(shaderProgramHandle, 1, "vertex_normal");
        //    GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
        //}

        //private void LoadIndexer()
        //{
        //    GL.GenBuffers(1, out indicesVboHandle);
        //    GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVboHandle);
        //    GL.BufferData<uint>(BufferTarget.ElementArrayBuffer,
        //        new IntPtr(indicesVboData.Length * Vector3.SizeInBytes),
        //        indicesVboData, BufferUsageHint.StaticDraw);
        //}

        //protected override void OnUpdateFrame(FrameEventArgs e)
        //{
        //    SetModelviewMatrix(Matrix4.RotateY((float)e.Time) * modelviewMatrix);

        //    if (Keyboard[OpenTK.Input.Key.Escape])
        //        Exit();
        //}

        //protected override void OnRenderFrame(FrameEventArgs e)
        //{
        //    GL.Viewport(0, 0, Width, Height);
        //    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        //    GL.DrawElements(BeginMode.Triangles, indicesVboData.Length,
        //        DrawElementsType.UnsignedInt, IntPtr.Zero);

        //    GL.Flush();
        //    SwapBuffers();
        //}

        //protected override void OnResize(EventArgs e)
        //{
        //    float widthToHeight = ClientSize.Width / (float)ClientSize.Height;
        //    SetProjectionMatrix(Matrix4.Perspective(1.3f, widthToHeight, 1, 20));
        //}

    }
}
