using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using System.Collections.Generic;

namespace PirateCraft
{
   //Rendering pipeline
   //Sets up context, passes, FBOs, and renders to multiple cameras.
   // pipeline -> begin (FBO, pass)
   //  camera -> begin (viewport, clip, matrices)
   //   material -> begin (gpu state)
   //    shader -> begin (bind uniforms, textures)
   //     draw elements
   //  unbind so we don't mess up the state.. 
   public class Renderer
   {
      public enum RenderPipelineState
      {
         None,
         Begin,
         End
      }
      static RenderPipelineState RenderState = RenderPipelineState.None;
      public static void BeginRender(GameWindow g, vec4 color)
      {
         //Begin global rendering, to render to all cameras that want rendering.
         Gu.Assert(RenderState == RenderPipelineState.End || RenderState == RenderPipelineState.None );
         RenderState = RenderPipelineState.Begin;
         Gu.SetContext(g);
         GL.ClearColor(new OpenTK.Graphics.Color4(color.x, color.y, color.z, color.w));
         GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
         Gpu.CheckGpuErrorsRt();

         Renderer.SetInitialGpuRenderState();
      }
      public static void EndRender()
      {
         Gu.Assert(RenderState == RenderPipelineState.Begin);
         RenderState = RenderPipelineState.End;
         Gpu.CheckGpuErrorsRt();
         Gu.CurrentWindowContext.GameWindow.SwapBuffers();
      }
      private static void SetInitialGpuRenderState()
      {
         Gpu.CheckGpuErrorsDbg();
         GL.Enable(EnableCap.CullFace);
         GL.CullFace(CullFaceMode.Back);
         if (Gu.CoordinateSystem == CoordinateSystem.Lhs)
         {
            GL.FrontFace(FrontFaceDirection.Cw);
         }
         else
         {
            GL.FrontFace(FrontFaceDirection.Ccw);
         }
         GL.Enable(EnableCap.DepthTest);
         GL.Enable(EnableCap.ScissorTest);
      }
      //public static void Render(Camera3D cam, List<MeshData> meshes, Shader m)
      //{
      //   //Render a single material to a group of meshes (faster)
      //   Gu.Assert(m != null);
      //   Gu.Assert(RenderState == RenderPipelineState.Begin);
      //   foreach (MeshData mesh in meshes)
      //   {
      //      mesh.Draw();
      //   }
      //}
      //public static void Render(Camera3D cam, WorldObject ob, Material m)
      //{
      //   //Render single material to a single object with object data included
      //   Gu.Assert(m != null);
      //   Gu.Assert(RenderState == RenderPipelineState.Begin);
      //   m.GpuRenderState.SetState();
      //   Render(cam, ob.Mesh, m.Shader, m.Shader);
      //}
      ////We're using instanced rendering so vs sohuld be instanced as well.
      //private static void Render(Camera3D bc, MeshData ms, Shader shader, Texture tex)// InstancedVisibleSet vs) << TODO
      //{
         
      //   Gu.Assert(RenderState == RenderPipelineState.Begin);

      //   if (tex != null)
      //   {
      //      //TODO material
      //      tex.Bind();
      //      GL.ActiveTexture(TextureUnit.Texture0);
      //   }

      //   Gpu.CheckGpuErrorsDbg();
      //   shader.Bind();
      //   ms.Draw();
      //   shader.Unbind();
      //   Gpu.CheckGpuErrorsDbg();

      //   if (tex != null)
      //   {
      //      tex.Unbind();
      //   }

      //   ////GL.Disable(EnableCap.CullFace);
      //   //GL.ClearColor(color.x, color.y, color.z, color.W);

      //   //_objMainShader = new BaseShader();
      //   //_objMainShader.Load();
      //}

      //        void renderGrid(float r, float g, float b, int nSlices, float fSliceWidth, Vec3f center)
      //        {
      //            GL.PushAttrib(AttribMask.AllAttribBits);

      //            GL.Disable(EnableCap.CullFace);

      //            float gridWidth_2 = nSlices * fSliceWidth / 2.0f;

      //            GL.LineWidth(1.0f);
      //            GL.Begin(PrimitiveType.Lines);
      //            GL.Color3(r, g, b);
      //            //Horiz lines
      //            for (int i = 0; i < nSlices + 1; ++i)
      //            {
      //                GL.Vertex3(center.x - (gridWidth_2),
      //                    center.y - 0,
      //                    center.z - (gridWidth_2) + (fSliceWidth * (float)i)
      //                    );
      //                GL.Vertex3(center.x + (gridWidth_2),
      //                    center.y - 0,
      //                    center.z - (gridWidth_2) + (fSliceWidth * (float)i)
      //                    );
      //            }
      //            for (int i = 0; i < nSlices + 1; ++i)
      //            {
      //                GL.Vertex3(center.x - (gridWidth_2) + (fSliceWidth * (float)i),
      //                    center.y - 0,
      //                    center.z - (gridWidth_2)
      //                    );
      //                GL.Vertex3(center.x - (gridWidth_2) + (fSliceWidth * (float)i),
      //                    center.y - 0,
      //                    center.z + (gridWidth_2)
      //                    );
      //            }
      //            GL.End();

      //            GL.PopAttrib();

      //   }

















   }
}
