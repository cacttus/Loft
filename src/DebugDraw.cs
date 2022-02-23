using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;


namespace PirateCraft
{
   public class DebugDraw 
   {
      public List<v_v3c4> Verts =  new List<v_v3c4>();
      public static VertexFormat VertexFormat = v_v3c4.VertexFormat;
      public DebugDraw()
      {
      }
      public void BeginFrame()
      {
         Gu.Assert(Gu.Context.Renderer.RenderState == Renderer.RenderPipelineState.Begin);
         Verts.Clear();
      }
      public void point(vec3 v, vec4 c)
      {
         Verts.Add(new v_v3c4() { _v = v, _c = c });
      }
      public void line(vec3 v, vec3 v2, vec4 c)
      {
         point(v, c);
         point(v2, c);
      }
      public void EndFrame()
      {
         Gu.Assert(Gu.Context.Renderer.RenderState == Renderer.RenderPipelineState.End);

         Verts.Clear();
      }
      public void Box(Box3f b, vec4 color)
      {
         Box(b._min, b._max, color);
      }
      public void Box(OOBox3f b, vec4 color)
      {
         Box(b.Verts, color);
      }
      public void Box(vec3 i, vec3 a, vec4 color)
      {
         //      6     7 a
         //   2     3
         //      4      5
         // i 0     1
         vec3[] points = new vec3[8];
         points[0] = new vec3(i.x, i.y, i.z);
         points[1] = new vec3(a.x, i.y, i.z);
         points[2] = new vec3(i.x, a.y, i.z);
         points[3] = new vec3(a.x, a.y, i.z);
         points[4] = new vec3(i.x, i.y, a.z);
         points[5] = new vec3(a.x, i.y, a.z);
         points[6] = new vec3(i.x, a.y, a.z);
         points[7] = new vec3(a.x, a.y, a.z);
         Box(points, color);
      }
      public void Box(vec3[] points, vec4 color)
      {
         Gu.Assert(points.Length == 8);
         //      6     7 a
         //   2     3
         //      4      5
         // i 0     1
         line(points[0], points[1], color);
         line(points[1], points[3], color);
         line(points[3], points[2], color);
         line(points[2], points[0], color);
         line(points[5], points[4], color);
         line(points[4], points[6], color);
         line(points[6], points[7], color);
         line(points[7], points[5], color);
         line(points[0], points[4], color);
         line(points[1], points[5], color);
         line(points[3], points[7], color);
         line(points[2], points[6], color);
      }
   }
}
