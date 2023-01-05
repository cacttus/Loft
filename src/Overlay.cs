using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.Serialization;

//debug draw & draw utilities

namespace Loft
{
  public class OffColor
  {
    public static vec4 Charcoal { get; } = new vec4(0.1914f, 0.1923f, 0.1894f, 1);
    public static vec4 VeryDarkGray { get; } = new vec4(0.0914f, 0.0914f, 0.0914f, 1);
    public static vec4 MediumDarkGray { get; } = new vec4(0.3014f, 0.3014f, 0.3014f, 1);
    public static vec4 MediumGray { get; } = new vec4(0.4914f, 0.4914f, 0.4914f, 1);
    public static vec4 VeryLightGray { get; } = new vec4(0.9614f, 0.9614f, 0.9614f, 1);
    public static vec4 ControlColor { get; } = vec4.rgba_ub(230, 230, 230, 255);
    public static vec4 ControlFontColor { get; } = vec4.rgba_ub(42, 42, 42, 255);

    //https://yorktown.cbe.wwu.edu/sandvig/shared/NetColors.aspx
    public static vec4 AliceBlue = vec4.rgb_ub(0xF0F8FF);
    public static vec4 AntiqueWhite = vec4.rgb_ub(0xFAEBD7);
    public static vec4 Aqua = vec4.rgb_ub(0x00FFFF);
    public static vec4 Aquamarine = vec4.rgb_ub(0x7FFFD4);
    public static vec4 Azure = vec4.rgb_ub(0xF0FFFF);
    public static vec4 Beige = vec4.rgb_ub(0xF5F5DC);
    public static vec4 Bisque = vec4.rgb_ub(0xFFE4C4);
    public static vec4 Black = vec4.rgb_ub(0x000000);
    public static vec4 BlanchedAlmond = vec4.rgb_ub(0xFFEBCD);
    public static vec4 Blue = vec4.rgb_ub(0x0000FF);
    public static vec4 BlueViolet = vec4.rgb_ub(0x8A2BE2);
    public static vec4 Brown = vec4.rgb_ub(0xA52A2A);
    public static vec4 BurlyWood = vec4.rgb_ub(0xDEB887);
    public static vec4 CadetBlue = vec4.rgb_ub(0x5F9EA0);
    public static vec4 Chartreuse = vec4.rgb_ub(0x7FFF00);
    public static vec4 Chocolate = vec4.rgb_ub(0xD2691E);
    public static vec4 Coral = vec4.rgb_ub(0xFF7F50);
    public static vec4 CornflowerBlue = vec4.rgb_ub(0x6495ED);
    public static vec4 Cornsilk = vec4.rgb_ub(0xFFF8DC);
    public static vec4 Crimson = vec4.rgb_ub(0xDC143C);
    public static vec4 Cyan = vec4.rgb_ub(0x00FFFF);
    public static vec4 DarkBlue = vec4.rgb_ub(0x00008B);
    public static vec4 DarkCyan = vec4.rgb_ub(0x008B8B);
    public static vec4 DarkGoldenrod = vec4.rgb_ub(0xB886BB);
    public static vec4 DarkGray = vec4.rgb_ub(0xA9A9A9);
    public static vec4 DarkGreen = vec4.rgb_ub(0x006400);
    public static vec4 DarkKhaki = vec4.rgb_ub(0xBDB76B);
    public static vec4 DarkMagenta = vec4.rgb_ub(0x8B008B);
    public static vec4 DarkOliveGreen = vec4.rgb_ub(0x556B2F);
    public static vec4 DarkOrange = vec4.rgb_ub(0xFF8C00);
    public static vec4 DarkOrchid = vec4.rgb_ub(0x9932CC);
    public static vec4 DarkRed = vec4.rgb_ub(0x8B0000);
    public static vec4 DarkSalmon = vec4.rgb_ub(0xE9967A);
    public static vec4 DarkSeaGreen = vec4.rgb_ub(0x8FBC8B);
    public static vec4 DarkSlateBlue = vec4.rgb_ub(0x483D8B);
    public static vec4 DarkSlateGray = vec4.rgb_ub(0x2F4F4F);
    public static vec4 DarkTurquoise = vec4.rgb_ub(0x00CED1);
    public static vec4 DarkViolet = vec4.rgb_ub(0x9400D3);
    public static vec4 DeepPink = vec4.rgb_ub(0xFF1493);
    public static vec4 DeepSkyBlue = vec4.rgb_ub(0x00BFFF);
    public static vec4 DimGray = vec4.rgb_ub(0x696969);
    public static vec4 DodgerBlue = vec4.rgb_ub(0x1E90FF);
    public static vec4 Firebrick = vec4.rgb_ub(0xB22222);
    public static vec4 FloralWhite = vec4.rgb_ub(0xFFFAF0);
    public static vec4 ForestGreen = vec4.rgb_ub(0x228B22);
    public static vec4 Fuchsia = vec4.rgb_ub(0xFF00FF);
    public static vec4 Gainsboro = vec4.rgb_ub(0xDCDCDC);
    public static vec4 GhostWhite = vec4.rgb_ub(0xF8F8FF);
    public static vec4 Gold = vec4.rgb_ub(0xFFD700);
    public static vec4 Goldenrod = vec4.rgb_ub(0xDAA520);
    public static vec4 Gray = vec4.rgb_ub(0x808080);
    public static vec4 Green = vec4.rgb_ub(0x008000);
    public static vec4 GreenYellow = vec4.rgb_ub(0xADFF2F);
    public static vec4 Honeydew = vec4.rgb_ub(0xF0FFF0);
    public static vec4 HotPink = vec4.rgb_ub(0xFF69B4);
    public static vec4 IndianRed = vec4.rgb_ub(0xCD5C5C);
    public static vec4 Indigo = vec4.rgb_ub(0x4B0082);
    public static vec4 Ivory = vec4.rgb_ub(0xFFFFF0);
    public static vec4 Khaki = vec4.rgb_ub(0xF0E68C);
    public static vec4 Lavender = vec4.rgb_ub(0xE6E6FA);
    public static vec4 LavenderBlush = vec4.rgb_ub(0xFFF0F5);
    public static vec4 LawnGreen = vec4.rgb_ub(0x7CFC00);
    public static vec4 LemonChiffon = vec4.rgb_ub(0xFFFACD);
    public static vec4 LightBlue = vec4.rgb_ub(0xADD8E6);
    public static vec4 LightCoral = vec4.rgb_ub(0xF08080);
    public static vec4 LightCyan = vec4.rgb_ub(0xE0FFFF);
    public static vec4 LightGoldenrodYellow = vec4.rgb_ub(0xFAFAD2);
    public static vec4 LightGray = vec4.rgb_ub(0xD3D3D3);
    public static vec4 LightGreen = vec4.rgb_ub(0x90EE90);
    public static vec4 LightPink = vec4.rgb_ub(0xFFB6C1);
    public static vec4 LightSalmon = vec4.rgb_ub(0xFFA07A);
    public static vec4 LightSeaGreen = vec4.rgb_ub(0x20B2AA);
    public static vec4 LightSkyBlue = vec4.rgb_ub(0x87CEFA);
    public static vec4 LightSlateGray = vec4.rgb_ub(0x778899);
    public static vec4 LightSteelBlue = vec4.rgb_ub(0xB0C4DE);
    public static vec4 LightYellow = vec4.rgb_ub(0xFFFFE0);
    public static vec4 Lime = vec4.rgb_ub(0x00FF00);
    public static vec4 LimeGreen = vec4.rgb_ub(0x32CD32);
    public static vec4 Linen = vec4.rgb_ub(0xFAF0E6);
    public static vec4 Magenta = vec4.rgb_ub(0xFF00FF);
    public static vec4 Maroon = vec4.rgb_ub(0x800000);
    public static vec4 MediumAquamarine = vec4.rgb_ub(0x66CDAA);
    public static vec4 MediumBlue = vec4.rgb_ub(0x0000CD);
    public static vec4 MediumOrchid = vec4.rgb_ub(0xBA55D3);
    public static vec4 MediumPurple = vec4.rgb_ub(0x9370DB);
    public static vec4 MediumSeaGreen = vec4.rgb_ub(0x3CB371);
    public static vec4 MediumSlateBlue = vec4.rgb_ub(0x7B68EE);
    public static vec4 MediumSpringGreen = vec4.rgb_ub(0x00FA9A);
    public static vec4 MediumTurquoise = vec4.rgb_ub(0x48D1CC);
    public static vec4 MediumVioletRed = vec4.rgb_ub(0xC71585);
    public static vec4 MidnightBlue = vec4.rgb_ub(0x191970);
    public static vec4 MintCream = vec4.rgb_ub(0xF5FFFA);
    public static vec4 MistyRose = vec4.rgb_ub(0xFFE4E1);
    public static vec4 Moccasin = vec4.rgb_ub(0xFFE4B5);
    public static vec4 NavajoWhite = vec4.rgb_ub(0xFFDEAD);
    public static vec4 Navy = vec4.rgb_ub(0x000080);
    public static vec4 OldLace = vec4.rgb_ub(0xFDF5E6);
    public static vec4 Olive = vec4.rgb_ub(0x808000);
    public static vec4 OliveDrab = vec4.rgb_ub(0x6B8E23);
    public static vec4 Orange = vec4.rgb_ub(0xFFA500);
    public static vec4 OrangeRed = vec4.rgb_ub(0xFF4500);
    public static vec4 Orchid = vec4.rgb_ub(0xDA70D6);
    public static vec4 PaleGoldenrod = vec4.rgb_ub(0xEEE8AA);
    public static vec4 PaleGreen = vec4.rgb_ub(0x98FB98);
    public static vec4 PaleTurquoise = vec4.rgb_ub(0xAFEEEE);
    public static vec4 PaleVioletRed = vec4.rgb_ub(0xDB7093);
    public static vec4 PapayaWhip = vec4.rgb_ub(0xFFEFD5);
    public static vec4 PeachPuff = vec4.rgb_ub(0xFFDAB9);
    public static vec4 Peru = vec4.rgb_ub(0xCD853F);
    public static vec4 Pink = vec4.rgb_ub(0xFFC0CB);
    public static vec4 Plum = vec4.rgb_ub(0xDDA0DD);
    public static vec4 PowderBlue = vec4.rgb_ub(0xB0E0E6);
    public static vec4 Purple = vec4.rgb_ub(0x800080);
    public static vec4 Red = vec4.rgb_ub(0xFF0000);
    public static vec4 RosyBrown = vec4.rgb_ub(0xBC8F8F);
    public static vec4 RoyalBlue = vec4.rgb_ub(0x4169E1);
    public static vec4 SaddleBrown = vec4.rgb_ub(0x8B4513);
    public static vec4 Salmon = vec4.rgb_ub(0xFA8072);
    public static vec4 SandyBrown = vec4.rgb_ub(0xF4A460);
    public static vec4 SeaGreen = vec4.rgb_ub(0x2E8B57);
    public static vec4 SeaShell = vec4.rgb_ub(0xFFF5EE);
    public static vec4 Sienna = vec4.rgb_ub(0xA0522D);
    public static vec4 Silver = vec4.rgb_ub(0xC0C0C0);
    public static vec4 SkyBlue = vec4.rgb_ub(0x87CEEB);
    public static vec4 SlateBlue = vec4.rgb_ub(0x6A5ACD);
    public static vec4 SlateGray = vec4.rgb_ub(0x708090);
    public static vec4 Snow = vec4.rgb_ub(0xFFFAFA);
    public static vec4 SpringGreen = vec4.rgb_ub(0x00FF7F);
    public static vec4 SteelBlue = vec4.rgb_ub(0x4682B4);
    public static vec4 Tan = vec4.rgb_ub(0xD2B48C);
    public static vec4 Teal = vec4.rgb_ub(0x008080);
    public static vec4 Thistle = vec4.rgb_ub(0xD8BFD8);
    public static vec4 Tomato = vec4.rgb_ub(0xFF6347);
    public static vec4 Turquoise = vec4.rgb_ub(0x40E0D0);
    public static vec4 Violet = vec4.rgb_ub(0xEE82EE);
    public static vec4 Wheat = vec4.rgb_ub(0xF5DEB3);
    public static vec4 White = vec4.rgb_ub(0xFFFFFF);
    public static vec4 WhiteSmoke = vec4.rgb_ub(0xF5F5F5);
    public static vec4 Yellow = vec4.rgb_ub(0xFFFF00);
    public static vec4 YellowGreen = vec4.rgb_ub(0x9ACD32);
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
