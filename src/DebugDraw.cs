using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;


namespace PirateCraft
{
  //Debug Draw with points or lines.
  public class DebugDraw
  {
    public List<v_v3c4> LinePoints = new List<v_v3c4>();
    public List<uint> LineInds = new List<uint>();
    public List<v_v3c4> Points = new List<v_v3c4>();
    public List<v_v3c4> TriPoints = new List<v_v3c4>();

    public bool DrawBoundBoxes { get; set; } = false;
    public bool DrawVertexNormals { get; set; } = false;
    public bool DrawFaceNormals { get; set; } = false;

    public DebugDraw()
    {
    }
    public void Line(vec3 a, vec3 b, vec4 color)
    {
      int n = LinePoints.Count;
      LinePoints.Add(new v_v3c4() { _v = a, _c = color });
      LinePoints.Add(new v_v3c4() { _v = b, _c = color });
      Line(n + 0, n + 1);
    }
    public void EndFrame()
    {
      LinePoints.Clear();
      LineInds.Clear();
      Points.Clear();
      TriPoints.Clear();
    }
    public void Triangle(vec3 v0, vec3 v1, vec3 v2, vec4 c)
    {
      TriPoints.Add(new v_v3c4() { _v = v0, _c = c });
      TriPoints.Add(new v_v3c4() { _v = v1, _c = c });
      TriPoints.Add(new v_v3c4() { _v = v2, _c = c });
    }    
    public void Point(vec3 v, vec4 c)
    {
      Points.Add(new v_v3c4() { _v = v, _c = c });
    }
    public void Point(vec3 v)
    {
      Points.Add(new v_v3c4() { _v = v, _c = new vec4(1, 1, 1, 1) });
    }
    public void DrawAxisLine(vec3 origin, vec3 axis)
    {
      float drawaxis_length = 10000;
      vec3 linea = origin + axis * drawaxis_length;
      vec3 lineb = origin + axis * -drawaxis_length;
      Gu.Context.DebugDraw.Line(linea, lineb, new vec4(.9221f, .9413f, .7912f, 1));
    }
    // public void Sphere(int slices, int stacks, float radius, vec3 pos, vec4 color)
    // {
    //   Ellipsoid(slices, stacks, new vec3(radius, radius, radius), pos, color);
    // }
    // public void Ellipsoid(int slices, int stacks, vec3 radius, vec3 pos, vec4 color)
    // {
    //   v_v3n3x2t3u1[] verts;
    //   ushort[] inds;

    //   MeshData.GenEllipsoid(out verts, out inds, radius, slices, stacks, false, false);

    //   int n = LinePoints.Count;
    //   foreach (var vr in verts)
    //   {
    //     LinePoints.Add(new v_v3c4() { _v = pos + vr._v, _c = color });
    //   }
    //   foreach (var ind in inds)
    //   {
    //     LineInds.Add((uint)((int)n + ind));
    //   }
    // }
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
        int n = LinePoints.Count;

        foreach (var p in points)
        {
          LinePoints.Add(new v_v3c4() { _v = p, _c = color });
        }
        Line(n + 0, n + 1);
        Line(n + 1, n + 3);
        Line(n + 3, n + 2);
        Line(n + 2, n + 0);
        Line(n + 5, n + 4);
        Line(n + 4, n + 6);
        Line(n + 6, n + 7);
        Line(n + 7, n + 5);
        Line(n + 0, n + 4);
        Line(n + 1, n + 5);
        Line(n + 3, n + 7);
        Line(n + 2, n + 6);
      }
      else if (pt == PrimitiveType.Points)
      {
        for (int i = 0; i < 8; ++i)
        {
          Point(points[i], color);
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    private void Line(int a, int b)
    {
      LineInds.Add((uint)a);
      LineInds.Add((uint)b);
    }

  }
}
