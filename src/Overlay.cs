using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.Serialization;

namespace Loft
{
  public class OffColor
  {
    public static vec4 White { get; } = new vec4(0.9914f, 0.9923f, 0.9894f, 1);
    public static vec4 Charcoal { get; } = new vec4(0.1914f, 0.1923f, 0.1894f, 1);
    public static vec4 Red { get; } = new vec4(0.8914f, 0.0129f, 0.0129f, 1);
    public static vec4 Green { get; } = new vec4(0.0129f, 0.8923f, 0.8894f, 1);
    public static vec4 Blue { get; } = new vec4(0.0129f, 0.0129f, 0.8923f, 1);
    public static vec4 Yellow { get; } = new vec4(0.9234f, 0.9914f, 0.0034f, 1);
    public static vec4 Pink { get; } = new vec4(0.0129f, 0.0129f, 0.8894f, 1);
    public static vec4 Cyan { get; } = new vec4(0.0129f, 0.8923f, 0.8894f, 1);
    public static vec4 Magenta { get; } = new vec4(0.0129f, 0.8923f, 0.8894f, 1);
    public static vec4 VeryDarkGray { get; } = new vec4(0.0914f, 0.0914f, 0.0914f, 1);
    public static vec4 DarkGray { get; } = new vec4(0.1914f, 0.1914f, 0.1914f, 1);
    public static vec4 MediumGray { get; } = new vec4(0.4914f, 0.4914f, 0.4914f, 1);
    public static vec4 LightGray { get; } = new vec4(0.7214f, 0.7214f, 0.7214f, 1);
    public static vec4 VeryLightGray { get; } = new vec4(0.9614f, 0.9614f, 0.9614f, 1);
    public static vec4 LightBlue { get; } = new vec4(0.4021f, 0.6134f, 0.9859f, 1);
    public static vec4 LightGreen { get; } = new vec4(0.4221f, 0.9934f, 0.4259f, 1);
    public static vec4 LightYellow { get; } = new vec4(0.9234f, 0.9914f, 0.5034f, 1);
  }
  public enum MeshRenderMode
  {
    Points = 0,
    Lines = 1,
    Solid = 2,
    Material = 3,
  }
  public enum ObjectRenderMode
  {
    Solid,
    Wire,
    Textured,   //blender:material preview
    Rendered
  }
  [DataContract]
  public class ViewportOverlay
  {
    //Manages Overlay, Debug Draw, and other 3D / flat color drawing

    #region Public: Members

    public ObjectRenderMode ObjectRenderMode { get { return _objectRenderMode; } set { _objectRenderMode = value; } }
    public bool DrawWireframeOverlay { get { return _drawWireframeOverlay; } set { _drawWireframeOverlay = value; } }
    public bool DrawObjectBasis { get { return _drawObjectBasis; } set { _drawObjectBasis = value; } }
    public bool DrawBoundBoxesAndGizmos { get { return _drawBoundBoxes; } set { _drawBoundBoxes = value; } }
    public bool DrawVertexAndFaceNormalsAndTangents { get { return _drawVertexAndFaceNormalsAndTangents; } set { _drawVertexAndFaceNormalsAndTangents = value; } }
    public bool ShowSelectionOrigin { get { return _showSelectionOrigin; } set { _showSelectionOrigin = value; } }

    #endregion
    #region Private: Members

    [DataMember] private ObjectRenderMode _objectRenderMode = ObjectRenderMode.Rendered;
    [DataMember] private bool _drawWireframeOverlay = false; //overlay, or base render.
    [DataMember] private bool _drawObjectBasis = false;
    [DataMember] private bool _drawBoundBoxes = false;
    [DataMember] private bool _drawVertexAndFaceNormalsAndTangents = false;
    [DataMember] private bool _showSelectionOrigin = true;

    public int DbgTotalIndsThisFrame { get; private set; } = 0;

    private const int c_pointOB = 0;
    private const int c_lineOB = 1;
    private const int c_triOB = 2;
    private RenderView _view;
    private GrowList<v_debug_draw> _verts = new GrowList<v_debug_draw>();
    private GrowList<int>[] _inds = new GrowList<int>[3] { new GrowList<int>(), new GrowList<int>(), new GrowList<int>() };
    private Drawable[] _debugOb = new Drawable[3];
    private GPUBuffer _mesh_verts;

    #endregion

    public ViewportOverlay(RenderView rv)
    {
      CreateMeshes();
      _view = rv;
    }

    public void BuildDebugDrawMeshes(VisibleStuff s)
    {
      DbgTotalIndsThisFrame = _inds[c_pointOB].Count + _inds[c_lineOB].Count + _inds[c_triOB].Count;

      if (_verts.Count > 0 && DbgTotalIndsThisFrame > 0)
      {
        _mesh_verts.ExpandCopy(_verts);

        for (int iprim = 0; iprim < 3; iprim++)
        {
          if (_inds[iprim].Count > 0)
          {
            if (_view.Camera.Frustum.HasBox(_debugOb[iprim].BoundBox))
            {
              _debugOb[iprim].MeshView.ExpandCopyIndexes(_inds[iprim]);
              s.AddObject(_view, _debugOb[iprim]);
            }
          }
        }

      }

    }
    public void EndFrame()
    {
      for (int i = 0; i < 3; ++i)
      {
        _debugOb[i].BoundBox.genResetLimits();
        _inds[i].Reset();
      }
      _verts.Reset();
    }
    public void DrawFrustum(Frustum f, float length, MeshRenderMode m = MeshRenderMode.Lines)
    {
      length = Math.Clamp(length, 0.0001f, 99999);
      //     6         7                                       
      //          f       
      //     4         5                              
      //2     3 
      //   a    
      //0     1      
      vec2 ptsize = new vec2(5, 5);
      vec2 linsize = new vec2(3, 3);

      int n = _verts.Count;

      var fbl = f.Points[Frustum.fpt_nbl] + (f.Points[Frustum.fpt_fbl] - f.Points[Frustum.fpt_nbl]).normalize() * length;
      var fbr = f.Points[Frustum.fpt_nbr] + (f.Points[Frustum.fpt_fbr] - f.Points[Frustum.fpt_nbr]).normalize() * length;
      var ftl = f.Points[Frustum.fpt_ntl] + (f.Points[Frustum.fpt_ftl] - f.Points[Frustum.fpt_ntl]).normalize() * length;
      var ftr = f.Points[Frustum.fpt_ntr] + (f.Points[Frustum.fpt_ftr] - f.Points[Frustum.fpt_ntr]).normalize() * length;

      AddVert(new v_debug_draw() { _v = f.Points[Frustum.fpt_nbl], _c = OffColor.White, _size = linsize, _outl = vec3.Zero });
      AddVert(new v_debug_draw() { _v = f.Points[Frustum.fpt_nbr], _c = OffColor.White, _size = linsize, _outl = vec3.Zero });
      AddVert(new v_debug_draw() { _v = f.Points[Frustum.fpt_ntl], _c = OffColor.White, _size = linsize, _outl = vec3.Zero });
      AddVert(new v_debug_draw() { _v = f.Points[Frustum.fpt_ntr], _c = OffColor.White, _size = linsize, _outl = vec3.Zero });
      AddVert(new v_debug_draw() { _v = fbl, _c = OffColor.White, _size = linsize, _outl = vec3.Zero });
      AddVert(new v_debug_draw() { _v = fbr, _c = OffColor.White, _size = linsize, _outl = vec3.Zero });
      AddVert(new v_debug_draw() { _v = ftl, _c = OffColor.White, _size = linsize, _outl = vec3.Zero });
      AddVert(new v_debug_draw() { _v = ftr, _c = OffColor.White, _size = linsize, _outl = vec3.Zero });

      Box(new int[] { n + 0, n + 1, n + 2, n + 3, n + 4, n + 5, n + 6, n + 7 }, m);

      Point(n + 0);
      Point(n + 1);
      Point(n + 2);
      Point(n + 3);
      Point(n + 4);
      Point(n + 5);
      Point(n + 6);
      Point(n + 7);
    }
    public void Box(Box3f b, vec4 color, float width = 1, MeshRenderMode rm = MeshRenderMode.Lines)
    {
      Box(b._min, b._max, color, width, rm);
    }
    public void Box(OOBox3f b, vec4 color, float width = 1, MeshRenderMode rm = MeshRenderMode.Lines)
    {
      Box(b.Verts, color, width, rm);
    }
    public void Box(vec3 i, vec3 a, vec4 color, float width = 1, MeshRenderMode rm = MeshRenderMode.Lines)
    {
      vec3[] points = new vec3[]
      {
        new vec3(i.x, i.y, i.z),
        new vec3(a.x, i.y, i.z),
        new vec3(i.x, a.y, i.z),
        new vec3(a.x, a.y, i.z),
        new vec3(i.x, i.y, a.z),
        new vec3(a.x, i.y, a.z),
        new vec3(i.x, a.y, a.z),
        new vec3(a.x, a.y, a.z)
      };
      Box(points, color, width, rm);
    }
    public void Box(vec3[] points, vec4 color, float width = 1, MeshRenderMode rm = MeshRenderMode.Lines)
    {
      Gu.Assert(points.Length == 8);
      int n = _verts.Count;
      for (int i = 0; i < 8; ++i)
      {
        AddVert(new v_debug_draw() { _v = points[i], _c = color, _size = new vec2(width, width), _outl = vec3.Zero });
      }
      Box(new int[] { n + 0, n + 1, n + 2, n + 3, n + 4, n + 5, n + 6, n + 7 }, rm);
    }
    private void Box(int[] inds, MeshRenderMode rm)
    {
      //      6     7 a
      //   2     3
      //      4      5
      // i 0     1      
      Gu.Assert(inds.Length == 8);

      if (rm == MeshRenderMode.Lines)
      {
        Line(inds[0], inds[1]);
        Line(inds[1], inds[3]);
        Line(inds[3], inds[2]);
        Line(inds[2], inds[0]);
        Line(inds[5], inds[4]);
        Line(inds[4], inds[6]);
        Line(inds[6], inds[7]);
        Line(inds[7], inds[5]);
        Line(inds[0], inds[4]);
        Line(inds[1], inds[5]);
        Line(inds[3], inds[7]);
        Line(inds[2], inds[6]);
      }
      else if (rm == MeshRenderMode.Points)
      {
        Point(inds[0]);
        Point(inds[1]);
        Point(inds[2]);
        Point(inds[3]);
        Point(inds[4]);
        Point(inds[5]);
        Point(inds[6]);
        Point(inds[7]);
      }
      else if (rm == MeshRenderMode.Solid)
      {
        Tri(inds[0], inds[1], inds[3]);//a
        Tri(inds[0], inds[3], inds[2]);

        Tri(inds[5], inds[4], inds[6]);//f
        Tri(inds[5], inds[6], inds[7]);

        Tri(inds[1], inds[5], inds[7]);//r
        Tri(inds[1], inds[7], inds[3]);

        Tri(inds[4], inds[0], inds[2]);//l
        Tri(inds[4], inds[2], inds[6]);

        Tri(inds[5], inds[4], inds[0]);//b
        Tri(inds[5], inds[0], inds[1]);

        Tri(inds[2], inds[3], inds[7]);//t --room 237!
        Tri(inds[2], inds[7], inds[6]);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    public void Line(vec3 a, vec3 b, vec4 color, float width = 3)
    {
      AddVert(new v_debug_draw() { _v = a, _c = color, _size = new vec2(width, width), _outl = vec3.Zero });
      AddVert(new v_debug_draw() { _v = b, _c = color, _size = new vec2(width, width), _outl = vec3.Zero });
      Line(_verts.Count - 2, _verts.Count - 1);
    }
    public void Triangle(vec3 v0, vec3 v1, vec3 v2, vec4 c)
    {
      AddVert(new v_debug_draw() { _v = v0, _c = c, _size = vec2.Zero, _outl = vec3.Zero });
      AddVert(new v_debug_draw() { _v = v1, _c = c, _size = vec2.Zero, _outl = vec3.Zero });
      AddVert(new v_debug_draw() { _v = v2, _c = c, _size = vec2.Zero, _outl = vec3.Zero });
      Tri(_verts.Count - 3, _verts.Count - 2, _verts.Count - 1);
    }
    public void Point(vec3 v, float size = 3)
    {
      Point(v, OffColor.White, size);
    }
    public void Point(vec3 v, vec4 c, float size = 3, vec3? outline = null)
    {
      //add a single point.
      AddVert(new v_debug_draw() { _v = v, _c = c, _size = new vec2(size, size), _outl = ((outline == null) ? (OffColor.White.toVec3()) : outline.Value) });
      Point(_verts.Count - 1);
    }
    private void Point(int idx)
    {
      _inds[c_pointOB].Add(idx);
      _debugOb[c_pointOB].BoundBox.genExpandByPoint(_verts[idx]._v);
    }
    private void Line(int a, int b)
    {
      _inds[c_lineOB].Add(a);
      _inds[c_lineOB].Add(b);
      _debugOb[c_lineOB].BoundBox.genExpandByPoint(_verts[a]._v);
      _debugOb[c_lineOB].BoundBox.genExpandByPoint(_verts[b]._v);
    }
    private void Tri(int a, int b, int c)
    {
      _inds[c_triOB].Add(a);
      _inds[c_triOB].Add(b);
      _inds[c_triOB].Add(c);
      _debugOb[c_triOB].BoundBox.genExpandByPoint(_verts[a]._v);
      _debugOb[c_triOB].BoundBox.genExpandByPoint(_verts[b]._v);
      _debugOb[c_triOB].BoundBox.genExpandByPoint(_verts[c]._v);
    }
    private void AddVert(v_debug_draw v)
    {
      _verts.Add(v);
    }
    private void CreateMeshes()
    {
      _mesh_verts = Gpu.CreateVertexBuffer("debug_vts", _verts.ToArray());

      CreateDebugMesh("points", Rs.Material.DebugDrawMaterial_Points, PrimitiveType.Points, c_pointOB);
      CreateDebugMesh("lines", Rs.Material.DebugDrawMaterial_Lines, PrimitiveType.Lines, c_lineOB);
      CreateDebugMesh("tris", Rs.Material.DebugDrawMaterial_Tris, PrimitiveType.Triangles, c_triOB);
    }
    private void CreateDebugMesh(string name_pfx, string materialname, PrimitiveType pt, int index)
    {
      var pind = Gpu.CreateIndexBuffer($"debug_{name_pfx}", _inds[index].ToArray());
      _debugOb[index] = new WorldObject($"debug_{name_pfx}ob");
      _debugOb[index].Material = Gu.Lib.GetMaterial(materialname);
      _debugOb[index].Mesh = new MeshData($"debug_{name_pfx}", pt, _mesh_verts, pind, null, false);
    }


  }//cls


}//ns
