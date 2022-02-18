using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat4f = OpenTK.Matrix4;
using System.Collections.Generic;

namespace PirateCraft
{
   /*
    *  Draw visible (culled) sets
    * */
   public class Renderer
   {
      //        //private OpenTK.GLControl _objControl;
      //        //..private BaseShader _objMainShader;
      //        //private Scene _objNodeManager;
      //        public bool ShowAxis { get; set; } = false;
      //        //public BaseCamera _objCamera;
      //        public bool ShowGrid { get; set; } = true;

      //       //public BaseCamera GetCamera() { return _objCamera; }
      //        // public GLControl GetControl() { return _objControl; }
      //        public Int64 FrameStamp { get;  private set; }
      //        public Viewport Viewport { get; private set; }
      //        public void SetBackgroundColor(Vec4f color)
      //        {
      //            GL.ClearColor(color.X, color.Y, color.Z, color.W);
      //        }
      static RenderPipelineState RenderState = RenderPipelineState.None;
      public static void BeginRender(GameWindow g, Vec4f color)
      {
         RenderState = RenderPipelineState.Begin;
         Gu.SetContext(g);
         GL.ClearColor(new OpenTK.Graphics.Color4(color.X, color.Y, color.Z, color.W));
         GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
         Gpu.CheckGpuErrorsRt();

         Renderer.SetInitialGpuRenderState();
      }
      public static void EndRender()
      {
         RenderState = RenderPipelineState.End;
         Gpu.CheckGpuErrorsRt();
         Gu.CurrentWindowContext.GameWindow.SwapBuffers();
      }
      private static void SetInitialGpuRenderState()
      {
         Gpu.CheckGpuErrorsDbg();
         GL.Enable(EnableCap.CullFace);
         GL.CullFace(CullFaceMode.Back);
         if (Gu.CoordinateSystem == CoordinateSystem.Rhs)
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
      public static void Render(Camera3D cam, List<MeshData> meshes, Material m)
      {
         //Render a single material to a group of meshes (faster)
         Gu.Assert(m != null);
         m.GpuRenderState.SetState();
         Gu.Assert(RenderState == RenderPipelineState.Begin);

         if (m.Texture != null)
         {
            //TODO material
            m.Texture.Bind();
            GL.ActiveTexture(TextureUnit.Texture0);
         }

         Gpu.CheckGpuErrorsDbg();
         m.Shader.Bind();
         foreach(MeshData mesh in meshes)
         {
            mesh.Draw();
         }
         m.Shader.Unbind();
         Gpu.CheckGpuErrorsDbg();

         if (m.Texture != null)
         {
            m.Texture.Unbind();
         }
      }
      public static void Render(Camera3D cam, WorldObject ob, Material m)
      {
         //Render single material to a single object with object data included
         Gu.Assert(m != null);
         m.GpuRenderState.SetState();
         Render(cam, ob.Mesh, m.Shader, m.Texture);
      }
      //We're using instanced rendering so vs sohuld be instanced as well.
      private static void Render(Camera3D bc, MeshData ms, Shader shader, Texture tex)// InstancedVisibleSet vs) << TODO
      {
         
         Gu.Assert(RenderState == RenderPipelineState.Begin);

         if (tex != null)
         {
            //TODO material
            tex.Bind();
            GL.ActiveTexture(TextureUnit.Texture0);
         }

         Gpu.CheckGpuErrorsDbg();
         shader.Bind();
         ms.Draw();
         shader.Unbind();
         Gpu.CheckGpuErrorsDbg();

         if (tex != null)
         {
            tex.Unbind();
         }

         ////GL.Disable(EnableCap.CullFace);
         //GL.ClearColor(color.X, color.Y, color.Z, color.W);

         //_objMainShader = new BaseShader();
         //_objMainShader.Load();
      }
      //public void Render(Renderer rm)
      //{
      //    if (_objTexture != null)
      //    {
      //        _objTexture.Bind();
      //    }
      //    Gpu.CheckGpuErrorsDbg();
      //    //bool b = GL.IsVertexArray(_intVaoId);
      //    GL.BindVertexArray(_intVaoId);
      //    Gpu.CheckGpuErrorsDbg();

      //    GL.BindBuffer(BufferTarget.ArrayBuffer, _intVboId);
      //    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _intIboId);

      //    GL.DrawElements(PrimitiveType.Triangles,
      //                    NumIndexes,
      //                    DrawElementsType.UnsignedInt,
      //                    IntPtr.Zero
      //                    );

      //    GL.BindVertexArray(0);
      //    GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
      //    GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

      //    Gpu.CheckGpuErrorsDbg();

      //    if (_objTexture != null){
      //        _objTexture.Unbind();
      //      }
      //}

      //        //private void _objOpenGl_Resize(object sender, EventArgs e)
      //        //{
      //        //    Resize();
      //        //}

      //        public void Resize()
      //        {
      //            //_objNodeManager.Resize(Viewport);
      //        }
      //        //private void CreateViewport()
      //        //{
      //        //    //Don't allow 0 width/height
      //        //    // Use 2 because of integer divison
      //        //    //int mw = _objControl.Width < 2 ? 2 : _objControl.Width;
      //        //    //int mh = _objControl.Height < 2 ? 2 : _objControl.Height;
      //        //    //Viewport = new Viewport(mw, mh);
      //        //}
      //        public void Redraw()
      //        {
      //            Gpu.CheckGpuErrorsDbg();
      //            Begin();

      //            foreach (BaseNode node in _objNodeManager.GetNodes())
      //            {
      //                if (node == null)
      //                {
      //                    return;
      //                }
      //                Gpu.CheckGpuErrorsDbg();
      //                GL.PushMatrix();
      //                {
      //                    Gpu.CheckGpuErrorsDbg();
      //                    GL.Translate(node.GetPos());
      //                    Gpu.CheckGpuErrorsDbg();
      //                    GL.Rotate(node.GetRotation(), node.GetRotationVector());
      //                    Gpu.CheckGpuErrorsDbg();
      //                    GL.Scale(node.GetScale());
      //                    Gpu.CheckGpuErrorsDbg();
      //                    node.UpdateBoundBox();
      //                    DrawNode(node);
      //                }
      //                GL.PopMatrix();
      //            }

      //            End();
      //            Gpu.CheckGpuErrorsDbg();

      //            //_objControl.Invalidate();
      //        }
      //        private Color4 _lastClearColor = Color4(1,1,1,1);
      //        public void Begin(Color4? c = null)
      //        {
      //            if (c!=null && c!=_lastClearColor) 
      //            {
      //                GL.ClearColor(c);
      //            }

      //            //Clear, set matrix
      //            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      //            Gpu.CheckGpuErrorsDbg();

      //            // Camera setup
      //            _objCamera.Setup();
      //            Gpu.CheckGpuErrorsDbg();
      //        }
      //        public void DrawNode(BaseNode node)
      //        {
      //            //Call bind  here in order to send the correct matrix.
      //            if (_objMainShader.IsLoaded())
      //            {
      //                _objMainShader.UpdateAndBind();
      //            }
      //            node.Render(this);
      //        }
      //        float angle = 0;
      //        public void End()
      //        {
      //            if (_objMainShader.IsLoaded())
      //            {
      //                _objMainShader.Unbind();
      //            }

      //            Gpu.CheckGpuErrorsDbg();
      //            {
      //                GL.PushMatrix();
      //                angle += 10.0f;
      //                //  GL.Rotate(angle, 0, 1, 0);

      //                // - Fixed function code goes here.
      //                GL.Disable(EnableCap.Texture2D);
      //                GL.UseProgram(0);
      //                if (ShowAxis)
      //                {
      //                    GL.LineWidth(3.0f);
      //                    float al = 20.0f;    // Make this smaller for non-ortho mode.
      //                    GL.Begin(PrimitiveType.Lines);
      //                    GL.Color4(1.0f, 0.0f, 0.0f, 1.0f);
      //                    GL.Vertex3(0, 0.0f, 0.0f);
      //                    GL.Vertex3(al, 0.0f, 0.0f);
      //                    GL.Color4(0.0f, 1.0f, 0.0f, 1.0f);
      //                    GL.Vertex3(0.0f, 0, 0.0f);
      //                    GL.Vertex3(0.0f, al, 0.0f);
      //                    GL.Color4(0.0f, 0.0f, 1.0f, 1.0f);
      //                    GL.Vertex3(0.0f, 0.0f, 0);
      //                    GL.Vertex3(0.0f, 0.0f, al);
      //                    GL.End();
      //                }

      //                if (ShowGrid)
      //                {
      //                    renderGrid(1, 1, 1, 10, 10, new Vec3f(0, 0, 0));
      //                }
      //                GL.PopMatrix();
      //            }
      //            Gpu.CheckGpuErrorsDbg();

      //            FrameStamp++;

      //            //Single RT error checking at the end of the loop
      //            Gpu.CheckGpuErrorsRt();
      //        }

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
      //                GL.Vertex3(center.X - (gridWidth_2),
      //                    center.Y - 0,
      //                    center.Z - (gridWidth_2) + (fSliceWidth * (float)i)
      //                    );
      //                GL.Vertex3(center.X + (gridWidth_2),
      //                    center.Y - 0,
      //                    center.Z - (gridWidth_2) + (fSliceWidth * (float)i)
      //                    );
      //            }
      //            for (int i = 0; i < nSlices + 1; ++i)
      //            {
      //                GL.Vertex3(center.X - (gridWidth_2) + (fSliceWidth * (float)i),
      //                    center.Y - 0,
      //                    center.Z - (gridWidth_2)
      //                    );
      //                GL.Vertex3(center.X - (gridWidth_2) + (fSliceWidth * (float)i),
      //                    center.Y - 0,
      //                    center.Z + (gridWidth_2)
      //                    );
      //            }
      //            GL.End();

      //            GL.PopAttrib();

      //   }

















   }
}
