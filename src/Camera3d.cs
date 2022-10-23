using OpenTK.Graphics.OpenGL4;
using System.Runtime.Serialization;

namespace PirateCraft
{
  public enum RenderViewMode
  {
    UIOnly,
    WorldOnly,
    UIAndWorld,
  }
  public enum ViewInputMode
  {
    Play,
    Edit
  }

  public class Frustum
  {
    #region Public:Members

    public vec3 NearCenter { get { return _nearCenter; } private set { _nearCenter = value; } }
    public vec3 FarCenter { get { return _farCenter; } private set { _farCenter = value; } }
    public vec3 NearTopLeft { get { return _nearTopLeft; } private set { _nearTopLeft = value; } }
    public vec3 FarTopLeft { get { return _farTopLeft; } private set { _farTopLeft = value; } }
    public Box3f BoundBox { get { return _boundBox; } private set { _boundBox = value; } }

    #endregion
    #region Private:Members

    private const int fpt_nbl = 0;
    private const int fpt_fbl = 1;
    private const int fpt_fbr = 2;
    private const int fpt_nbr = 3;
    private const int fpt_ntl = 4;
    private const int fpt_ftl = 5;
    private const int fpt_ftr = 6;
    private const int fpt_ntr = 7;
    private const int fp_near = 0;
    private const int fp_far = 1;
    private const int fp_left = 2;
    private const int fp_right = 3;
    private const int fp_top = 4;
    private const int fp_bottom = 5;

    private WeakReference<Camera3D> _camera;
    private float _widthNear = 1;
    private float _heightNear = 1;
    private float _widthFar = 1;
    private float _heightFar = 1;
    private Plane3f[] _planes = new Plane3f[6];
    private vec3[] _points = new vec3[8];
    private vec3 _nearCenter = new vec3(0, 0, 0);
    private vec3 _farCenter = new vec3(0, 0, 0);
    private vec3 _nearTopLeft = new vec3(0, 0, 0);
    private vec3 _farTopLeft = new vec3(0, 0, 0);
    private Box3f _boundBox = new Box3f(new vec3(0, 0, 0), new vec3(1, 1, 1));

    #endregion
    #region Public:Methods

    public Frustum(Camera3D cam)
    {
      _camera = new WeakReference<Camera3D>(cam);
    }
    public vec2 WidthHeightForDepth(float depthz)
    {
      vec2 r = vec2.Zero;
      if (_camera != null && _camera.TryGetTarget(out Camera3D cam))
      {
        if (cam.RenderView != null && cam.RenderView.TryGetTarget(out var view))
        {
          float tanfov2 = MathUtils.tanf(cam.FOV / 2.0f);
          float ar = (float)view.Viewport.Width / (float)view.Viewport.Height;
          r.x = tanfov2 * depthz * 2.0f;
          r.y = r.x / ar;
        }
      }
      return r;
    }
    public void Update()
    {
      if (_camera != null && _camera.TryGetTarget(out Camera3D cam))
      {
        if (cam.RenderView != null && cam.RenderView.TryGetTarget(out var view))
        {
          //Frustum
          float tanfov2 = MathUtils.tanf(cam.FOV / 2.0f);
          float ar = (float)view.Viewport.Width / (float)view.Viewport.Height;
          _widthNear = tanfov2 * cam.Near * 2.0f;
          _heightNear = _widthNear / ar;
          _widthFar = tanfov2 * cam.Far * 2.0f;
          _heightFar = _widthFar / ar;

          NearCenter = cam.Position_World + cam.BasisZ_World * cam.Near;
          FarCenter = cam.Position_World + cam.BasisZ_World * cam.Far;
          NearTopLeft = NearCenter - cam.BasisX_World * _widthNear * 0.5f + cam.BasisY_World * _heightNear * 0.5f;
          FarTopLeft = FarCenter - cam.BasisX_World * _widthFar * 0.5f + cam.BasisY_World * _heightFar * 0.5f;

          ConstructPointsAndPlanes(FarCenter, NearCenter, cam.BasisY_World, cam.BasisX_World, _widthNear * 0.5f, _widthFar * 0.5f, _heightNear * 0.5f, _heightFar * 0.5f);
        }
      }
    }
    public OOBox3f? BeamcastWorld(vec2 p0, vec2 p1, float begin = 1.01f, float end = -1)
    {
      //beam cast to the end of the view frustum, does not go beyond frustum.
      //p0,p1 -> mouse coords top left window
      //begin/end - begin and end of frustum depth, 
      //point order: LeftBotNear,RBN,LTN,RTN,LBF,RBF,LTF,RTF,
      OOBox3f? ret = null;
      if (_camera != null && _camera.TryGetTarget(out var cam))
      {
        if (cam.RenderView != null && cam.RenderView.TryGetTarget(out var view))
        {
          float p0x = (float)p0.x / (float)view.Viewport.Width;
          float p1x = (float)p1.x / (float)view.Viewport.Width;
          float p0y = (float)(view.Viewport.Height - p0.y) / (float)view.Viewport.Height;
          float p1y = (float)(view.Viewport.Height - p1.y) / (float)view.Viewport.Height;

          end = end < 0 ? cam.Far : end;
          vec2 farwh = WidthHeightForDepth(end);
          vec2 nearwh = WidthHeightForDepth(cam.Near);

          var nbl = cam.Position_World + cam.BasisZ_World * begin - cam.BasisX_World * nearwh.x * 0.5f - cam.BasisY_World * nearwh.y * 0.5f;
          var fbl = cam.Position_World + cam.BasisZ_World * end - cam.BasisX_World * farwh.x * 0.5f - cam.BasisY_World * farwh.y * 0.5f;
          var nx = cam.BasisX_World * _widthNear;
          var ny = cam.BasisY_World * _heightNear;
          var fx = cam.BasisX_World * farwh.x;
          var fy = cam.BasisY_World * farwh.y;

          //everyting origin to bot left
          if (p0x > p1x)
          {
            var tmp = p1x;
            p1x = p0x;
            p0x = tmp;
          }
          if (p0y > p1y)
          {
            var tmp = p1y;
            p1y = p0y;
            p0y = tmp;
          }

          var b = new OOBox3f();
          b.Verts = new vec3[]
          {
            nbl + nx * p0x + ny * p0y, // nbl
            nbl + nx * p1x + ny * p0y, // nbr
            nbl + nx * p0x + ny * p1y, // ntl
            nbl + nx * p1x + ny * p1y, // ntr
            
            fbl + fx * p0x + fy * p0y, // fbl
            fbl + fx * p1x + fy * p0y, // fbr
            fbl + fx * p0x + fy * p1y, // ftl
            fbl + fx * p1x + fy * p1y, // ftr
          };
          ret = b;

        }
      }
      return ret;
    }
    public Line3f? RaycastWorld(vec2 point_on_screen_topleftorigin, TransformSpace space = TransformSpace.World, float maxDistance = -1)
    {
      //Raycastscreentoworld screen to world
      //returns a line from the camera lens to the end of the view frustum
      Line3f? pt_ret = null;

      if (_camera != null && _camera.TryGetTarget(out var cam))
      {
        if (cam.RenderView != null && cam.RenderView.TryGetTarget(out var view))
        {
          Line3f pt = new Line3f();
          float left_pct = (float)point_on_screen_topleftorigin.x / (float)view.Viewport.Width;
          float top_pct = (float)point_on_screen_topleftorigin.y / (float)view.Viewport.Height;

          pt.p0 = NearTopLeft + cam.BasisX_World * _widthNear * left_pct - cam.BasisY_World * _heightNear * top_pct;//***2 -- the times ttwo is a huge error FIX
          pt.p1 = FarTopLeft + cam.BasisX_World * _widthFar * left_pct - cam.BasisY_World * _heightFar * top_pct;

          //**additional depth is wrong, it pushes the ray into the scren, must add to normalize ray length
          //pt.p0 += cam.BasisZ * additionalZDepthNear;

          if (maxDistance > 0)
          {
            pt.p1 = pt.p0 + (pt.p1 - pt.p0).normalize() * (maxDistance);
          }

          pt_ret = pt;
        }
      }
      return pt_ret;
    }
    public vec3? WorldToScreen(vec3 v)
    {
      //Project point in world onto screen
      //Note point may not be within the frustum.
      if (_camera != null && _camera.TryGetTarget(out var cam))
      {
        vec3 campos = cam.Position_World;

        float t = 0;
        if (_planes[fp_near].IntersectLine(v, campos, out t))
        {
          vec3 ret = campos + (v - campos) * t;
          return ret;
        }
      }
      return null;
    }
    public bool HasBox(in Box3f pCube)
    {
      vec3 min, max;
      float d1, d2;
      if (!pCube.Validate(false, false))
      {
        Gu.Log.ErrorCycle("Box was invalid");
        Gu.DebugBreak();
        return false;
      }

      for (int i = 0; i < 6; ++i)
      {
        min = pCube._min;
        max = pCube._max;

        //  - Calculate the negative and positive vertex
        if (_planes[i].n.x < 0)
        {
          min.x = pCube._max.x;
          max.x = pCube._min.x;
        }

        if (_planes[i].n.y < 0)
        {
          min.y = pCube._max.y;
          max.y = pCube._min.y;
        }

        if (_planes[i].n.z < 0)
        {
          min.z = pCube._max.z;
          max.z = pCube._min.z;
        }

        d1 = _planes[i].Distance(max);
        d2 = _planes[i].Distance(min);

        if (d1 < 0.0f && d2 < 0.0f)
        {
          return false;
        }
        //if(d2< 0.0f)
        //ret = true; // Currently we intersect the frustum.  Keep checking the rest of the planes to see if we're outside.
      }
      return true;
    }
    public bool HasPoint(vec3 p)
    {
      for (int pi = 0; pi < 6; pi++)
      {
        //this can be faster if we just take the sign bit of the float..
        if (_planes[pi].Distance(p) < 0)
        {
          return false;
        }
      }
      return true;
    }

    #endregion
    #region Private:Methods

    private void ConstructPointsAndPlanes(vec3 farCenter, vec3 nearCenter,
                                        vec3 upVec, vec3 rightVec,
                                        float w_near_2, float w_far_2,
                                        float h_near_2, float h_far_2)
    {
      _points[fpt_nbl] = (nearCenter - (upVec * h_near_2) - (rightVec * w_near_2));
      _points[fpt_fbl] = (farCenter - (upVec * h_far_2) - (rightVec * w_far_2));

      _points[fpt_nbr] = (nearCenter - (upVec * h_near_2) + (rightVec * w_near_2));
      _points[fpt_fbr] = (farCenter - (upVec * h_far_2) + (rightVec * w_far_2));

      _points[fpt_ntl] = (nearCenter + (upVec * h_near_2) - (rightVec * w_near_2));
      _points[fpt_ftl] = (farCenter + (upVec * h_far_2) - (rightVec * w_far_2));

      _points[fpt_ntr] = (nearCenter + (upVec * h_near_2) + (rightVec * w_near_2));
      _points[fpt_ftr] = (farCenter + (upVec * h_far_2) + (rightVec * w_far_2));

      // - Construct AA bound box
      _boundBox._min = vec3.VEC3_MAX();
      _boundBox._max = vec3.VEC3_MIN();

      for (int i = 0; i < 8; ++i)
      {
        _boundBox._min = vec3.minv(BoundBox._min, _points[i]);
        _boundBox._max = vec3.maxv(BoundBox._max, _points[i]);
      }
      //TODO: Optimize:
      //        1) we don't use the fourth value of the QuadPlane4 at all
      //        2) QuadPLane4 calculates a TBN basis.  We don't need that.
      //  1   2
      //
      //  3   4
      //
      // - Construct so that the normals are facing into the frustum  - Checked all is good
      _planes[fp_near] = new Plane3f(_points[fpt_ntl], _points[fpt_ntr], _points[fpt_nbl]);
      _planes[fp_far] = new Plane3f(_points[fpt_ftr], _points[fpt_ftl], _points[fpt_fbr]);

      _planes[fp_left] = new Plane3f(_points[fpt_ftl], _points[fpt_ntl], _points[fpt_fbl]);
      _planes[fp_right] = new Plane3f(_points[fpt_ntr], _points[fpt_ftr], _points[fpt_nbr]);

      _planes[fp_top] = new Plane3f(_points[fpt_ntr], _points[fpt_ntl], _points[fpt_ftr]);
      _planes[fp_bottom] = new Plane3f(_points[fpt_fbr], _points[fpt_fbl], _points[fpt_nbr]);
    }
    #endregion
  }
  [DataContract]
  public class Viewport
  {
    //Viewport is in TOP LEFT coordinates.
    // OpenGL = bottom left, we convert this in the renderer
    //*Unscaled dimensiosn are the exact window dimensions (default framebuffer)
    //*Current dimensions are the dimensions scaled to the current framebuffer size
    public int X { get { return _x; } set { _x = value; } }
    public int Y { get { return _y; } set { _y = value; } }
    public int Width { get { return _width; } set { _width = value; } }
    public int Height { get { return _height; } set { _height = value; } }

    [DataMember] private int _x = 0;
    [DataMember] private int _y = 0;
    [DataMember] private int _width = 1;
    [DataMember] private int _height = 1;

    public Viewport() { }
    public Viewport(int x, int y, int w, int h)
    {
      X = x;
      Y = y;
      Width = w;
      Height = h;
    }
    public bool Contains_Point_Window_Relative_BR_Exclusive(vec2 win_rel)
    {
      //note: our viewports are in TOp left coords, opengl we convert to bottom left
      //Exclude BR to prevent overlaps.
      return (win_rel.x >= X) && (win_rel.y >= Y) && (win_rel.x < (X + Width)) && (win_rel.y < (Y + Height));
    }
  }

  public class ViewportOverlay
  {
    private int _polygonMode = 0;
    public bool ShowOverlay = true;//todo:all the overlay booleans, showfaces, shownormals..
    private RenderView _view;
    public ViewportOverlay(RenderView rv)
    {
      _view = rv;
    }
    public void ToggleWireFrame()
    {
      //This is old and not needed.
      if (_polygonMode == 0)
      {
        _view.PolygonMode = PolygonMode.Line;
        _polygonMode = 1;
      }
      else if (_polygonMode == 1)
      {
        _view.PolygonMode = PolygonMode.Fill;
        _polygonMode = 0;
      }
    }
  }

  public class RenderView : MutableState
  {
    public const string c_EditGUI_Root = "c_EditGUI_Root";//enable/disable edit gui.
    public const string c_StatusBar = "c_StatusBar";//enable/disable edit gui.

    //RenderView: The part of the window in which to render.
    //RenderView needs to use a percentage of the screen not exact coords, since resizing the screen we don't know how big to make the view.
    public WeakReference<Camera3D> Camera { get; set; } = null;//i think the idea here is to allow objects to destroy when they are removed from scene
    public Viewport Viewport { get; set; } = null;
    public mat4 ProjectionMatrix { get; private set; } = mat4.Identity;
    public GpuCamera GpuCamera { get { return _gpuCamera; } private set { _gpuCamera = value; } }
    public int Id { get; private set; } = 0;
    private static int s_idGen = 0;
    public Gui2d Gui { get; set; } = null;
    public UiElement WorldDebugInfo { get; set; } = null;
    public UiElement GpuDebugInfo { get; set; } = null;
    public UiElement ControlsInfo { get; set; } = null;
    public PolygonMode PolygonMode = PolygonMode.Fill;
    public ViewInputMode ViewInputMode = ViewInputMode.Edit;
    public ViewportOverlay Overlay { get; private set; } = null;
    public string Name { get; private set; } = "";
    public bool Enabled { get; set; } = true;
    public UiToast Toast { get; set; } = null;

    public RenderViewMode ViewMode { get { return _viewMode; } }
    private RenderViewMode _viewMode = RenderViewMode.UIAndWorld;

    private mat4 _projLast = mat4.Identity;
    private GpuCamera _gpuCamera = new GpuCamera();
    private vec2 _uv0 = vec2.Zero;
    private vec2 _uv1 = vec2.Zero;
    private float? _renderFOV = null;
    private float? _renderNear = null;
    private float? _renderFar = null;//temps for rendering


    public RenderView(string name, RenderViewMode mode, vec2 uv0, vec2 uv1, int sw, int sh)
    {
      Name = name;
      _viewMode = mode;
      //note: xy is bottom left in opengl
      Id = s_idGen++;
      Overlay = new ViewportOverlay(this);
      SetSize(uv0, uv1, sw, sh);
    }
    public void SetSize(vec2 uv0, vec2 uv1, int sw, int sh)
    {
      _uv0 = uv0;
      _uv1 = uv1;
      Gu.Assert(_uv0.x < _uv1.x);
      Gu.Assert(_uv0.y < _uv1.y);
      OnResize(sw, sh);
      SetModified();
    }
    public void SetCurrent()
    {
      //***TODO: change
      //***TODO: change
      //***TODO: change
      //***TODO: change
      //***TODO: change
      //***TODO: change
      //***TODO: change
      //This will change.. camera viewport will be separate
      //Cameras may be shared by different renderviews so we need to update it if we are on the current view
      //The reason for chagne is that cameras will have sub-views within a window's renderview area (black bars)
      //But of course, I mean, we could just change the dimensions of the renderview itself..right?

      if (_viewMode == RenderViewMode.WorldOnly || _viewMode == RenderViewMode.UIAndWorld)
      {
        if (Camera != null && Camera.TryGetTarget(out var c))
        {
          c.RenderView = new WeakReference<RenderView>(this);
          SetModified();
        }
      }
    }
    public bool BeginRender3D()
    {
      var ret = false;
      _renderFOV = null;
      _renderNear = null;
      _renderFar = null;
      if (_viewMode == RenderViewMode.UIOnly)
      {
        ret = true;
      }
      else if (_viewMode == RenderViewMode.WorldOnly || _viewMode == RenderViewMode.UIAndWorld)
      {
        //Return false if the camera for this view isn't set.
        if (Camera != null && Camera.TryGetTarget(out var c))
        {
          _renderFOV = c.FOV;
          _renderNear = c.Near;
          _renderFar = c.Far;
          //Viewport.SetupViewport();

          GL.PolygonMode(MaterialFace.Front, this.PolygonMode);

          SetCurrent();
          ret = true;
        }
        else
        {
          Gu.Log.ErrorCycle("No camera was set for the view.", 300);
          _renderFOV = (float)Math.PI / 6;
          _renderNear = 1;
          _renderFar = 1000;
          ret = true;//continue so we can draw GUI
        }
      }
      return ret;
    }
    public void EndRender3D()
    {
      if (_viewMode == RenderViewMode.WorldOnly || _viewMode == RenderViewMode.UIAndWorld)
      {
        SetModified();
      }
    }
    public void BeginRender2D(mat4? customProj)
    {
      //Viewport.SetupViewport();
      //Enter orthorgraphic projection mode for drawing images directly to the screen.
      //Note: in the past the width/height of viewport has been off by -1 (math issue)
      _projLast = ProjectionMatrix;
      if (customProj != null)
      {
        ProjectionMatrix = customProj.Value;
      }
      else
      {
        ProjectionMatrix = mat4.getOrtho((float)Viewport.X, (float)Viewport.Width, (float)Viewport.Y, (float)Viewport.Height, -1.0f, 1.0f);
      }
      SetModified();
    }
    public void EndRender2D()
    {
      ProjectionMatrix = _projLast;
      SetModified();
    }
    public bool BeginPipelineStage(PipelineStage ps)
    {
      UpdateDimensions(ps.Size.width, ps.Size.height);
      int vx = Viewport.X;
      //OpenGL Y = Bottom left!!!
      int vy = ps.Size.y - Viewport.Y - Viewport.Height;
      int vw = Viewport.Width;
      int vh = Viewport.Height;
      GL.Viewport(vx, vy, vw, vh);
      GL.Scissor(vx, vy, vw, vh);

      _projLast = ProjectionMatrix;

      if (ViewMode != RenderViewMode.UIOnly)
      {
        if (_renderFar == null || _renderNear == null || _renderFOV == null)
        {
          Gu.Log.Error($"{Name} - camera props null");
          Gu.DebugBreak();
          return false;
        }
        ProjectionMatrix = mat4.projection(_renderFOV.Value, Viewport.Width, Viewport.Height, _renderNear.Value, _renderFar.Value);
      }
      else
      {
        ProjectionMatrix = mat4.Identity;//we are rendering ui - this wont get used.
      }
      return true;
    }
    public void EndPipelineStage(PipelineStage ps)
    {
      ProjectionMatrix = _projLast;
    }

    public void OnResize(int sw, int sh)
    {
      UpdateDimensions(sw, sh);
      Gui?.OnResize();//Gui is translated to the current FBO size in the shader.
    }
    public void UpdateDimensions(int cur_output_fbo_w, int cur_output_fbo_h)
    {
      //** CALLED EVERY PIPE STAGE **
      //gui should be tied to the FBO that it renders to.
      //hmm yes
      var b = ComputeScaledView(_uv0, _uv1, cur_output_fbo_w, cur_output_fbo_h);

      if (b.w <= 0 || b.h <= 0)
      {
        Gu.Log.Info("Resize View " + Name + ": " + b.x + "," + b.y + " " + b.w + "," + b.h);
        Gu.Log.Error("Resize View " + Name + " w/h was zero, setting to 1");
        Gu.DebugBreak();
        if (b.w <= 0) { b.w = 1; }
        if (b.h <= 0) { b.h = 1; }
      }

      Viewport = new Viewport(b.x, b.y, b.w, b.h);
    }

    // int x = (int)(Math.Round(_uv0.x * (float)sw));
    // int y = (int)(Math.Round(_uv0.y * (float)sh));
    // int w = (int)(Math.Round((_uv1.x - _uv0.x) * (float)sw));
    // int h = (int)(Math.Round((_uv1.y - _uv0.y) * (float)sh));

    // Gu.Log.Info("Resize View " + Name + ": " + x + "," + y + " " + w + "," + h);

    // if (w <= 0 || h <= 0)
    // {
    //   Gu.Log.Error("Resize View " + Name + " w/h was zero, setting to 1");
    //   if (w <= 0) w = 1;
    //   if (h <= 0) h = 1;
    // }

    // Viewport = new Viewport(x, y, w, h);
    // ActiveGui?.OnResize();
    public static Box2i ComputeScaledView(vec2 uv0, vec2 uv1, int width, int height)
    {
      Box2i b = new Box2i();
      b.x = (int)(Math.Round(uv0.x * (float)width));
      b.y = (int)(Math.Round(uv0.y * (float)height));
      b.w = (int)(Math.Round((uv1.x - uv0.x) * (float)width));
      b.h = (int)(Math.Round((uv1.y - uv0.y) * (float)height));
      return b;
    }

    public void CompileGpuData()
    {
      if (Modified || Gu.EngineConfig.Debug_AlwaysCompileAndReloadGpuUniformData)
      {
        if (this.ViewMode != RenderViewMode.UIOnly)
        {
          if (Camera != null && Camera.TryGetTarget(out var c))
          {
            _gpuCamera._vViewPos = c.Position_World;
            _gpuCamera._vViewDir = c.Heading;
            _gpuCamera._m4View = c.ViewMatrix;
          }
          else
          {
            _gpuCamera._vViewPos = new vec3(0, 0, 0);
            _gpuCamera._vViewDir = new vec3(-1, 0, 0);
            _gpuCamera._m4View = mat4.Identity;
            Gu.Log.ErrorCycle($"{Name} - Camera was null", 300);
          }
        }
        else
        {
          _gpuCamera._vViewPos = vec3.Zero;
          _gpuCamera._vViewDir = vec3.Zero;
          _gpuCamera._m4View = mat4.Identity;
        }
        _gpuCamera._m4Projection = ProjectionMatrix;//Could be orthographic, or perspective depending
        _gpuCamera._fWindowWidth = Gu.Context.GameWindow.Width;
        _gpuCamera._fWindowHeight = Gu.Context.GameWindow.Height;
        _gpuCamera._fRenderWidth = (float)Gu.Context.Renderer.CurrentStageFBOSize.x;
        _gpuCamera._fRenderHeight = (float)Gu.Context.Renderer.CurrentStageFBOSize.y;
        _gpuCamera._vWindowViewport.x = Viewport.X;
        _gpuCamera._vWindowViewport.y = Viewport.Y;
        _gpuCamera._vWindowViewport.z = Viewport.Width;
        _gpuCamera._vWindowViewport.w = Viewport.Height;
      }
    }
  }
  public class Camera3D : WorldObject
  {
    //Camera is loosely tied to the render viewport
    // RenderViewport: Area of the window to render to. Could be whole window, or part of a window.
    // Camera defines a sub-area of the viewport (e.g. blender->view camera)
    // The camera viewport needs separate e.g. Our window is 16:9 but we only want to render to a 4:3 game.. then.. black bars.    
    public Frustum Frustum { get; private set; } = null;
    public float FOV { get { return _fov; } set { _fov = value; } }
    public float Near { get { return _near; } set { _near = value; } }
    public float Far { get { return _far; } set { _far = value; } }
    public mat4 ViewMatrix { get { return _viewMatrix; } private set { _viewMatrix = value; } }
    public WeakReference<RenderView> RenderView { get; set; } = null;//This may be null if the camera is not being viewed.

    private mat4 _viewMatrix = mat4.Identity;
    private float _fov = MathUtils.ToRadians(70.0f);
    private float _near = 1;
    private float _far = 1000;

    public Camera3D(string name, RenderView rv, float near = 1, float far = 1000) : base(name)
    {
      _near = near;
      _far = far;
      RenderView = new WeakReference<RenderView>(rv);
      Frustum = new Frustum(this);
    }
    public override void Update(World world, double dt, ref Box3f parentBoundBox)
    {
      base.Update(world, dt, ref parentBoundBox);
      var p = this.WorldMatrix.ExtractTranslation();
      ViewMatrix = mat4.getLookAt(p, new vec3(p + BasisZ_World), new vec3(0, 1, 0));
      Frustum.Update();
    }


  }
}
