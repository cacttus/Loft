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

        List<string> _shaderErrors = new List<string>();

        ShaderLoadState _loadState = ShaderLoadState.None;

        public bool GetIsLoaded() { return _loadState == ShaderLoadState.Success; }

        string vertexShaderSource = @"
                    #version 400
 
                    uniform mat4 model_matrix;            
                    uniform mat4 view_matrix;            
                    uniform mat4 projection_matrix;

                    layout(location = 0)in vec3 _v;
                    layout(location = 1)in vec3 _n;
                    layout(location = 2)in vec2 _x;

                    out vec3 _vsNormal;
                    out vec2 _vsTcoords;
                    out vec3 _vsVertex;
                    void main(void)
                    {
                        //not a proper transformation if modelview_matrix involves non-uniform scaling
                        mat4 mv = view_matrix * model_matrix;

                        _vsNormal = normalize((inverse(mv) * vec4( _n, 0 )).xyz);
                        _vsTcoords = _x;
                        gl_Position = projection_matrix * mv * vec4(_v, 1);
                        _vsVertex = gl_Position.xyz;
                    }
                    ";

        //precision highp float;

        string fragmentShaderSource = @"
                    #version 400

                    uniform sampler2D _ufTextureId_i0;

                    in vec3 _vsNormal;
                    in vec2 _vsTcoords;
                    in vec3 _vsVertex;

                    out vec4 _psColorOut;
 
                    void main(void)
                    {
                        vec3 light = vec3(0, 10, -10);
                        vec3 surface = normalize(light - _vsVertex);
                        vec4 albedo = texture(_ufTextureId_i0, vec2(_vsTcoords));

                        vec3 lightedTexel = albedo.xyz  * dot(_vsNormal, surface);

                        _psColorOut.xyz = lightedTexel;
                        _psColorOut.w = 1.0f;
                    }
                    ";

        public void Load(string vsSrc = "", string psSrc = "")
        {
            Gu.CheckGpuErrorsDbg();
            {
                _loadState = ShaderLoadState.Loading;
                CreateShaders(vsSrc, psSrc);
                CreateProgram();
                if (_loadState != ShaderLoadState.Failed)
                {
                    GL.UseProgram(_shaderProgramHandle);
                    QueryMatrixLocations();

                    _loadState = ShaderLoadState.Success;
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

        public void UpdateAndBind(double dt, Camera3D cam, mat4 model)
        {
            //**Pre - render - update uniforms.
            Gu.CheckGpuErrorsDbg();
            {
                Bind();

                var v_mat_tk = cam.ViewMatrix.ToOpenTK();
                var p_mat_tk = cam.ProjectionMatrix.ToOpenTK();
                var m_mat_tk = model.ToOpenTK();

                GL.UniformMatrix4(_viewMatrixLocation, false,ref v_mat_tk);
                Gu.CheckGpuErrorsDbg();
                GL.UniformMatrix4(_projectionMatrixLocation, false,ref p_mat_tk);
                Gu.CheckGpuErrorsDbg();
                GL.UniformMatrix4(_modelMatrixLocation, false, ref m_mat_tk);
                Gu.CheckGpuErrorsDbg();

                int texLocation = GL.GetUniformLocation(_shaderProgramHandle, "_ufTextureId_i0");
                GL.Uniform1(texLocation, TextureUnit.Texture0 - TextureUnit.Texture0);
                Gu.CheckGpuErrorsDbg();
            }
            Gu.CheckGpuErrorsDbg();
        }
        #region Private
        private void CreateShaders(string vsSrc = "", string psSrc = "")
        {
            Gu.CheckGpuErrorsDbg();
            {
                _vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
                Gu.CheckGpuErrorsDbg();
                _fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);
                Gu.CheckGpuErrorsDbg();

                if (vsSrc == "")
                    GL.ShaderSource(_vertexShaderHandle, vertexShaderSource);
                else
                    GL.ShaderSource(_vertexShaderHandle, vsSrc);
                Gu.CheckGpuErrorsDbg();
                if (psSrc == "")
                    GL.ShaderSource(_fragmentShaderHandle, fragmentShaderSource);
                else
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
                    _loadState = ShaderLoadState.Failed;
                    
                Debug.WriteLine(programInfoLog);
            }
            Gu.CheckGpuErrorsDbg();
        }
        private void QueryMatrixLocations()
        {
            Gu.CheckGpuErrorsDbg();
            {
                _projectionMatrixLocation = GL.GetUniformLocation(_shaderProgramHandle, "projection_matrix");
                _viewMatrixLocation = GL.GetUniformLocation(_shaderProgramHandle, "view_matrix");
                _modelMatrixLocation = GL.GetUniformLocation(_shaderProgramHandle, "model_matrix");
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
