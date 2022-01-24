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
        int _texLocation;
        int _camPosLocation;

        List<string> _shaderErrors = new List<string>();

        ShaderLoadState _loadState = ShaderLoadState.None;

        public bool GetIsLoaded() { return _loadState == ShaderLoadState.Success; }

        string vertexShaderSource = @"
                    #version 400
 
                    //uniform mat4 normal_matrix;            
                    uniform mat4 model_matrix;            
                    uniform mat4 view_matrix;            
                    uniform mat4 projection_matrix;
                    uniform vec3 cam_pos;

                    layout(location = 0)in vec3 _v;
                    layout(location = 1)in vec3 _n;
                    layout(location = 2)in vec2 _x;

                    out vec3 _vsNormal;
                    out vec2 _vsTcoords;
                    out vec3 _vsVertex; //should be frag pos.
                    flat out vec3 _cam_pos;

                    void main(void)
                    {
                        gl_Position = projection_matrix * view_matrix * model_matrix * vec4(_v, 1);
                        _vsVertex = (model_matrix * vec4(_v,1)).xyz;
                        _vsNormal = normalize((model_matrix * vec4(_v + _n, 1)).xyz - _vsVertex);
                        _vsTcoords = _x;
                        _cam_pos = cam_pos;
                    }
                    ";

        //precision highp float;

        string fragmentShaderSource = @"
                    #version 400
#define OREN_NAYAR_DIFFUSE 1

                    struct GpuLight {
                        vec3 _pos;
                        float _radius;
                        vec3 _color;
                        float _power; // This would be the falloff curve  ^x
                    };

                    uniform sampler2D _ufTextureId_i0;

                    in vec3 _vsNormal;
                    in vec2 _vsTcoords;
                    in vec3 _vsVertex;
                    flat in vec3 _cam_pos; 

                    out vec4 _psColorOut;
 
                    void main(void)
                    {

#define NUM_LIGHTS 2
                        GpuLight lights[NUM_LIGHTS];
                        lights[0]._pos = vec3(10,10,-10);
                        lights[0]._radius = 100.0f;
                        lights[0]._color = vec3(.95,.9691,.9488);
                        lights[0]._power = 0.67; // power within radius of light. 1 = is constant, 0.5 linear, 0 would be no light. <0.5 power falloff >0.5 is slow faloff. y=x^(1/p), p=[0,1], p!=0
                        lights[1]._pos = _cam_pos;
                        lights[1]._radius = 100.0f;
                        lights[1]._color = vec3(.9613,.9,.98);
                        lights[1]._power = 0.63;// power within radius of light. 1 = is constant, 0.5 linear, 0 would be no light. <0.5 power falloff >0.5 is slow faloff. y=x^(1/p), p=[0,1] ,p!=0

                        vec4 tex = texture(_ufTextureId_i0, vec2(_vsTcoords));

                        vec3 eye = normalize(_cam_pos - _vsVertex); // vec3(0, 10, -10);
                        //[Param]
                        float rho = 0.17f; //Albedo [0,1], 1 = 100% white, 0 = black body.
                        //[Param]
                        float E0 = 1f; // Strength [0,1]
                        //[Param]
                        float fSpecIntensity =.7; //[0,1] // I guess tecnhically speaking these two should be controlled by 'roughness'
                        //[Param]
                        float fSpecHardness =10; //[1,inf] 0=n

#ifdef OREN_NAYAR_DIFFUSE
                        //[Param]
                        float sig = .37122f; //Roughness [0,1], 1 = roguh, 0=gloss

                        float cos_theta_radiant = dot(eye, _vsNormal);
                        float theta_radiant = acos(cos_theta_radiant);
                        vec3 projected = normalize(eye - _vsNormal * dot(eye - _vsVertex,_vsNormal));
                        float phi_radiant= acos(dot(eye, projected));

                        float sig2 = sig*sig;
                        float A = 1 - 0.5*((sig2)/(sig2+0.33));
                        float B = 0.45*((sig2)/(sig2+0.09));
#endif

                        vec3 finalDiffuseColor = vec3(0,0,0);
                        vec3 finalSpecColor = vec3(0,0,0);

                        for(int i=0; i<NUM_LIGHTS; i++) {
                            vec3 lightpos_normal = normalize(lights[i]._pos - _vsVertex);
                            float cos_theta_incident = dot(lightpos_normal, _vsNormal);
                            float fFragToLightDistance = distance(lights[i]._pos, _vsVertex);

                            lights[i]._power = clamp(lights[i]._power, 0.000001f, 0.999999f);

                            float fQuadraticAttenuation = 1- pow(clamp(fFragToLightDistance/lights[i]._radius, 0, 1),(lights[i]._power)/(1-lights[i]._power)); //works well: x^(p/(1-p)) where x is pos/radius

#ifdef OREN_NAYAR_DIFFUSE
                            float theta_incident = acos(cos_theta_incident);

                            //get phi n . p + d
                            float phi_incident = acos(dot(lightpos_normal, -projected));

                            float alpha = max(theta_incident,theta_radiant);
                            float beta = min(theta_incident, theta_radiant);

                            float Lr = rho * cos_theta_incident * ( A + (B * max(0, cos(phi_incident - phi_radiant)) * sin(alpha) * tan(beta) )) * E0;
                            finalDiffuseColor += lights[i]._color * Lr * fQuadraticAttenuation;
#else
                            // Lambert
                            float Lr = rho * cos_theta_incident *  E0;
                            finalDiffuseColor += lights[i]._color * Lr * fQuadraticAttenuation; 
#endif

                            vec3 vReflect = reflect(-lightpos_normal, _vsNormal);//note the reflet angle is not in vertex space, its from vertex TO light
                            float eDotR = clamp( pow(clamp(dot(vReflect, eye), 0,1), fSpecHardness), 0,1 );
                            finalSpecColor += lights[i]._color * fSpecIntensity * fQuadraticAttenuation * eDotR;// * shadowMul;  
                        }

                        _psColorOut.xyz = finalDiffuseColor *  tex.rgb + finalSpecColor;
                        _psColorOut.w = 1.0f; //tex.a - but alpha compositing needs to be implemented.
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
        vec3 _camvpos;
        //int _normalMatrixLocation=0;
        public void UpdateAndBind(double dt, Camera3D cam, mat4 model)
        {
            //**Pre - render - update uniforms.
            Gu.CheckGpuErrorsDbg();
            {
                Bind();

                var v_mat_tk = cam.ViewMatrix.ToOpenTK();
                var p_mat_tk = cam.ProjectionMatrix.ToOpenTK();
                var m_mat_tk = model.ToOpenTK();
                var n_mat_tk = model.inverseOf().ToOpenTK();

                var a = model.inverseOf() * model;

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

                _camvpos = cam.v3pos;

                GL.ProgramUniform3(_shaderProgramHandle, _camPosLocation, _camvpos.x, _camvpos.y, _camvpos.z);
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
                {
                    GL.ShaderSource(_vertexShaderHandle, vertexShaderSource);
                }
                else
                {
                    GL.ShaderSource(_vertexShaderHandle, vsSrc);
                }
                Gu.CheckGpuErrorsDbg();
                if (psSrc == "")
                {
                    GL.ShaderSource(_fragmentShaderHandle, fragmentShaderSource);
                }
                else
                {
                    GL.ShaderSource(_fragmentShaderHandle, psSrc);
                }
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
                    _loadState = ShaderLoadState.Failed;
                }

                Debug.WriteLine(programInfoLog);
            }
            Gu.CheckGpuErrorsDbg();
        }
        private void FindUniformOrDieTryin(ref int loc, string name)
        {
            if ((loc = GL.GetUniformLocation(_shaderProgramHandle, name)) == 0)
            {
                Gu.Log.Error("Failed to find uniform location '" + name+"'");
            }

        }
        private void QueryMatrixLocations()
        {
            Gu.CheckGpuErrorsDbg();
            {
                FindUniformOrDieTryin(ref _projectionMatrixLocation , "projection_matrix");
                FindUniformOrDieTryin(ref _viewMatrixLocation ,  "view_matrix");
                FindUniformOrDieTryin(ref _modelMatrixLocation ,  "model_matrix");
                FindUniformOrDieTryin(ref _texLocation , "_ufTextureId_i0");
                FindUniformOrDieTryin(ref _camPosLocation ,  "cam_pos");
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
