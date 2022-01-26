using System;
using OpenTK.Graphics.OpenGL4;
namespace PirateCraft
{
    public class Material
    {
        public Texture Texture { get; private set; } = null;
        public Shader Shader {get; private set;} = null;
        public GpuRenderState GpuRenderState { get; set; } = new GpuRenderState();

        public Material(Texture t, Shader s)
        {
            Texture=t;
            Shader=s;
        }
        public void PreRender(double dt, Camera3D camera, mat4 model_matrix)
        {
            Gu.Context.Gpu.GpuRenderState.CullFace = GpuRenderState.CullFace;
            Gu.Context.Gpu.GpuRenderState.DepthTest = GpuRenderState.DepthTest;
            Gu.Context.Gpu.GpuRenderState.ScissorTest = GpuRenderState.ScissorTest;
            Shader.UpdateAndBind(dt, camera, model_matrix);
        }
        public void PostRender()
        {

        }
    }
}
