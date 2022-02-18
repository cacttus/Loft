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

      private static Material _defaultDiffuse = null; //Default color material / shader.
      private static Material _defaultFlatColor = null; //Default color material / shader.
      public static Material DefaultFlatColor()
      {
         //TODO: - the input shader should also be default.
         if (_defaultFlatColor == null)
         {
            _defaultFlatColor = new Material(Texture.Default(), Shader.DefaultFlatColorShader());
         }
         return _defaultFlatColor;
      }
      public static Material DefaultDiffuse()
      {
         //TODO: - the input shader should also be default.
         if (_defaultDiffuse == null)
         {
            _defaultDiffuse = new Material(Texture.Default(), Shader.DefaultDiffuse());
         }
         return _defaultDiffuse;
      }
      public Material(Texture t, Shader s)
      {
         Texture = t;
         Shader = s;
      }
      public void BeginRender(double dt, Camera3D camera, WorldObject ob)
      {
         Gu.CurrentWindowContext.Gpu.GpuRenderState.CullFace = GpuRenderState.CullFace;
         Gu.CurrentWindowContext.Gpu.GpuRenderState.DepthTest = GpuRenderState.DepthTest;
         Gu.CurrentWindowContext.Gpu.GpuRenderState.ScissorTest = GpuRenderState.ScissorTest;
         Shader.UpdateAndBind(dt, camera, ob);
      }
      public void EndRender()
      {
         Shader.Unbind();
      }
   }
}
