using System;
using OpenTK.Graphics.OpenGL4;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat3f = OpenTK.Matrix3;
using Mat4f = OpenTK.Matrix4;
namespace PirateCraft
{
  public class Material
  {
    public Texture Texture { get; private set; } = null;
    public Shader Shader { get; private set; } = null;
    public GpuRenderState GpuRenderState { get; set; } = new GpuRenderState(); //The rendering state of the material: clipping, depth, alpha, culling, etc

    private static Material _default = null; //Default color material / shader.

    public static Material Default(Vec4f color)
    {
      //TODO: - the input shader should also be default.
      if (_default == null)
      {
        _default = new Material(Texture.Default(), Shader.DefaultDiffuse());
      }
      return _default;
    }
    public Material(Texture t, Shader s)
    {
      Texture = t;
      Shader = s;
    }
    public void BeginRender(double dt, Camera3D camera, Mat4f model_matrix)
    {
      Gu.Window.Gpu.GpuRenderState.CullFace = GpuRenderState.CullFace;
      Gu.Window.Gpu.GpuRenderState.DepthTest = GpuRenderState.DepthTest;
      Gu.Window.Gpu.GpuRenderState.ScissorTest = GpuRenderState.ScissorTest;
      Shader.UpdateAndBind(dt, camera, model_matrix);
    }
    public void EndRender()
    {
      Shader.Unbind();
    }
  }
}
