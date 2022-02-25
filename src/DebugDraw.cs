using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;


namespace PirateCraft
{
   //Debug Draw with points or lines.
   public class DebugDraw 
   {
      public List<v_v3c4> Lines =  new List<v_v3c4>();
      public List<v_v3c4> Points =  new List<v_v3c4>();
      public static VertexFormat VertexFormat = v_v3c4.VertexFormat;

      public bool DrawBoundBoxes { get; set; } = false;

      public DebugDraw()
      {
      }
      public void BeginFrame()
      {
         //Lines.Clear();
         //Points.Clear();
      }
      public void EndFrame()
      {
         Lines.Clear();
         Points.Clear();
      }
      public void Point(vec3 v, vec4 c)
      {
         AddVert(new v_v3c4() { _v = v, _c = c }, PrimitiveType.Points);
      }
      public void Line(vec3 v, vec3 v2, vec4 c)
      {
         AddVert(new v_v3c4() { _v = v, _c = c }, PrimitiveType.Lines);
         AddVert(new v_v3c4() { _v = v2, _c = c }, PrimitiveType.Lines);
      }
      public void Box(Box3f b, vec4 color, PrimitiveType t = PrimitiveType.Lines)
      {
         Box(b._min, b._max, color, t);
      }
      public void Box(OOBox3f b, vec4 color, PrimitiveType t = PrimitiveType.Lines)
      {
         Box(b.Verts, color, t);
      }
      public void Box(vec3 i, vec3 a, vec4 color, PrimitiveType t = PrimitiveType.Lines)
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
         Box(points, color, t);
      }
      public void Box(vec3[] points, vec4 color, PrimitiveType pt = PrimitiveType.Lines)
      {
         Gu.Assert(points.Length == 8);
         //      6     7 a
         //   2     3
         //      4      5
         // i 0     1
         if (pt == PrimitiveType.Lines)
         {
            Line(points[0], points[1], color);
            Line(points[1], points[3], color);
            Line(points[3], points[2], color);
            Line(points[2], points[0], color);
            Line(points[5], points[4], color);
            Line(points[4], points[6], color);
            Line(points[6], points[7], color);
            Line(points[7], points[5], color);
            Line(points[0], points[4], color);
            Line(points[1], points[5], color);
            Line(points[3], points[7], color);
            Line(points[2], points[6], color);
         }
         else if(pt== PrimitiveType.Points)
         {
            for(int i=0; i<8; ++i)
            {
               Point(points[i], color);
            }
         }
         else
         {
            Gu.BRThrowNotImplementedException();
         }
      }
      private void AddVert(v_v3c4 vert, PrimitiveType pt)
      {
         if (pt == PrimitiveType.Lines)
         {
            Lines.Add(vert);
         }
         else if (pt == PrimitiveType.Points)
         {
            Points.Add(vert);
         }
         else
         {
            Gu.BRThrowNotImplementedException();
         }
      }
   }
}
