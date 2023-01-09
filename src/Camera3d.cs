using OpenTK.Graphics.OpenGL4;
using System.Runtime.Serialization;

namespace Loft
{
  public enum RenderViewMode
  {
    //in the future we can support different editors 
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
    public float WidthNear { get { return _widthNear; } }
    public float WidthFar { get { return _widthFar; } }
    public float HeightNear { get { return _heightNear; } }
    public float HeightFar { get { return _heightFar; } }

    public vec3[] Points { get { return _points; } }

    #endregion
    #region Private:Members

    public const int fpt_nbl = 0;
    public const int fpt_fbl = 1;
    public const int fpt_fbr = 2;
    public const int fpt_nbr = 3;
    public const int fpt_ntl = 4;
    public const int fpt_ftl = 5;
    public const int fpt_ftr = 6;
    public const int fpt_ntr = 7;
    public const int fp_near = 0;
    public const int fp_far = 1;
    public const int fp_left = 2;
    public const int fp_right = 3;
    public const int fp_top = 4;
    public const int fp_bottom = 5;

    private Camera3D _camera;
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
      _camera = cam;
    }
    public void Update()
    {
      //width n/f will remain the same regardless of projection, using FOV and near plane to get the orthographic width.
      float tanfov2 = MathUtils.tanf(_camera.FOV / 2.0f);
      float ar = (float)_camera.Viewport.Width / (float)_camera.Viewport.Height;
      _widthNear = tanfov2 * _camera.Near * 2.0f;
      _heightNear = _widthNear / ar;

      if (_camera.ProjectionMode == ProjectionMode.Orthographic)
      {
        _widthFar = _widthNear;
        _heightFar = _heightNear;
      }
      else if (_camera.ProjectionMode == ProjectionMode.Perspective)
      {
        _widthFar = tanfov2 * _camera.Far * 2.0f;
        _heightFar = _widthFar / ar;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      NearCenter = _camera.Position_World + _camera.BasisZ_World * _camera.Near;
      FarCenter = _camera.Position_World + _camera.BasisZ_World * _camera.Far;
      NearTopLeft = NearCenter - _camera.BasisX_World * _widthNear * 0.5f + _camera.BasisY_World * _heightNear * 0.5f;
      FarTopLeft = FarCenter - _camera.BasisX_World * _widthFar * 0.5f + _camera.BasisY_World * _heightFar * 0.5f;

      ConstructPointsAndPlanes(FarCenter, NearCenter, _camera.BasisY_World, _camera.BasisX_World, _widthNear * 0.5f, _widthFar * 0.5f, _heightNear * 0.5f, _heightFar * 0.5f);
    }
    public OOBox3f? BeamcastWorld(vec2 p0, vec2 p1, float begin = 1.01f, float end = -1)
    {
      //beam cast to the end of the view frustum, does not go beyond frustum.
      //p0,p1 -> mouse coords top left window
      //begin/end - begin and end of frustum depth, 
      //point order: LeftBotNear,RBN,LTN,RTN,LBF,RBF,LTF,RTF,
      OOBox3f? ret = null;

      float p0x = (float)p0.x / (float)_camera.Viewport.Width;
      float p1x = (float)p1.x / (float)_camera.Viewport.Width;
      float p0y = (float)(_camera.Viewport.Height - p0.y) / (float)_camera.Viewport.Height;
      float p1y = (float)(_camera.Viewport.Height - p1.y) / (float)_camera.Viewport.Height;

      end = end < 0 ? _camera.Far : end;
      vec2 farwh = ViewportWidthHeightForDepth(end);
      vec2 nearwh = ViewportWidthHeightForDepth(_camera.Near);

      var nbl = _camera.Position_World + _camera.BasisZ_World * begin - _camera.BasisX_World * nearwh.x * 0.5f - _camera.BasisY_World * nearwh.y * 0.5f;
      var fbl = _camera.Position_World + _camera.BasisZ_World * end - _camera.BasisX_World * farwh.x * 0.5f - _camera.BasisY_World * farwh.y * 0.5f;
      var nx = _camera.BasisX_World * _widthNear;
      var ny = _camera.BasisY_World * _heightNear;
      var fx = _camera.BasisX_World * farwh.x;
      var fy = _camera.BasisY_World * farwh.y;

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

      return ret;
    }
    private vec2 ViewportWidthHeightForDepth(float depthz)
    {
      //get viewport w/h for a given z depth
      vec2 r = vec2.Zero;

      if (_camera.ProjectionMode == ProjectionMode.Orthographic)
      {
        r = new vec2(_widthNear, _heightNear);//TODO: - 
      }
      else if (_camera.ProjectionMode == ProjectionMode.Perspective)
      {
        float tanfov2 = MathUtils.tanf(_camera.FOV / 2.0f);
        float ar = (float)_camera.Viewport.Width / (float)_camera.Viewport.Height;
        r.x = tanfov2 * depthz * 2.0f;
        r.y = r.x / ar;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      return r;
    }
    public Line3f? RaycastWorld(vec2 point_on_screen_topleftorigin, TransformSpace space = TransformSpace.World, float maxDistance = -1)
    {
      //Raycastscreentoworld screen to world
      //returns a line from the camera lens to the end of the view frustum
      Line3f? pt_ret = null;
      Line3f pt = new Line3f();
      float left_pct = (float)point_on_screen_topleftorigin.x / (float)_camera.Viewport.Width;
      float top_pct = (float)point_on_screen_topleftorigin.y / (float)_camera.Viewport.Height;

      pt.p0 = NearTopLeft + _camera.BasisX_World * _widthNear * left_pct - _camera.BasisY_World * _heightNear * top_pct;//***2 -- the times ttwo is a huge error FIX
      pt.p1 = FarTopLeft + _camera.BasisX_World * _widthFar * left_pct - _camera.BasisY_World * _heightFar * top_pct;

      //**additional depth is wrong, it pushes the ray into the scren, must add to normalize ray length
      //pt.p0 += _camera.BasisZ * additionalZDepthNear;

      if (maxDistance > 0)
      {
        pt.p1 = pt.p0 + (pt.p1 - pt.p0).normalize() * (maxDistance);
      }

      pt_ret = pt;
      return pt_ret;
    }
    public vec2 WorldToScreen_Window(vec3 v)
    {
      //Project point in world onto screen in window coords
      // ** also converts y to top left origin instead of bot left
      var x = _camera.ViewMatrix * _camera.ProjMatrix * new vec4(v, 1);
      x.x /= x.w;
      x.y /= x.w;
      vec2 scyup = (x.xy + 1.0f) / 2.0f;
      scyup.y = 1.0f - scyup.y;
      scyup *= new vec2(_camera.Viewport.Width, _camera.Viewport.Height);
      return scyup;
    }
    public vec3 WorldToScreen_3D(vec3 v)
    {
      Gu.BRThrowNotImplementedException();
      //Project point in world onto screen in 3D
      //Note point may not be within the frustum.
      // float d = _camera.BasisZ_World.dot(_points[fpt_ntl]);
      // float dist = _camera.BasisZ_World.dot(v) - d;
      // vec3 r = v - _camera.BasisZ_World * dist;
      //FOr some reason matrix mul is correct and not in shader. wonder why.
      //var x = _camera.ViewMatrix * _camera.ProjMatrix * new vec4(v, 1);
      //return r;
      return new vec3(0, 0, 0);
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

  public class RenderView : MutableState
  {
    //RenderView
    //Window viewport and overlay 
    //a view may not have a camera in the case of UI/etc and camera may not have view,
    //however we need the same data, perspective mats, etc, to render
    public const string c_EditGUI_Root = "c_EditGUI_Root";//enable/disable edit gui.
    public const string c_StatusBar = "c_StatusBar";//enable/disable edit gui.
    private static int s_idGen = 0;

    public int Id { get { return _id; } }
    public string Name { get { return _name; } }
    public bool Enabled { get { return _enabled; } set { _enabled = value; if (_camera != null) { _camera.Visible = _enabled; } } }
    public Camera3D Camera
    {
      get { return _camera; }
      set
      {
        _camera = value;
        SyncCamera();
      }
    }


    public Viewport Viewport { get { return _viewport; } set { _viewport = value; } }
    public IGui2d? Gui { get { return _gui; } set { _gui = value; } }
    public IUiWindow? WorldInfo { get { return _worldDebugInfo; } set { _worldDebugInfo = value; } }
    public IUiWindow? GpuInfo { get { return _gpuDebugInfo; } set { _gpuDebugInfo = value; } }
    public IUiWindow? ControlsInfo { get { return _controlsInfo; } set { _controlsInfo = value; } }
    public IUiWindow? ProfInfo { get { return _profInfo; } set { _profInfo = value; } }
    public ViewportOverlay Overlay { get { return _overlay; } }
    public IUiToast? Toast { get { return _toast; } set { _toast = value; } }
    public ViewInputMode ViewInputMode { get { return _viewInputMode; } set { _viewInputMode = value; } }
    public RenderViewMode ViewMode { get { return _viewMode; } }

    //Testing this stuff
    private GPUBuffer _camBuf = null;
    public GPUBuffer GpuCamera
    {
      get
      {
        return _camBuf;
      }
    }

    [DataMember] private Camera3D _camera;//camera that takes up entire viewport.
    [DataMember] private int _id = 0;
    [DataMember] private string _name = "";
    [DataMember] private bool _enabled = true;
    [DataMember] private Viewport _viewport = new Viewport(0, 0, 1, 1);
    [DataMember] private GpuCamera _gpuCamera = new GpuCamera();
    [DataMember] private vec2 _uv0 = vec2.Zero;
    [DataMember] private vec2 _uv1 = vec2.Zero;
    [DataMember] private RenderViewMode _viewMode = RenderViewMode.UIAndWorld;
    [DataMember] private ViewInputMode _viewInputMode = ViewInputMode.Edit;
    [DataMember] private ViewportOverlay _overlay;
    [DataMember] private IGui2d? _gui = null;
    [DataMember] private IUiWindow? _worldDebugInfo = null;
    [DataMember] private IUiWindow? _gpuDebugInfo = null;
    [DataMember] private IUiWindow? _controlsInfo = null;
    [DataMember] private IUiWindow? _profInfo = null;
    [DataMember] private IUiToast? _toast = null;
    private mat4? _customProj = null;

    public RenderView(string name, RenderViewMode mode, vec2 uv0, vec2 uv1, int sw, int sh)
    {
      _name = name;
      _viewMode = mode;
      _id = s_idGen++;
      _overlay = new ViewportOverlay(this);

      SetSize(uv0, uv1, sw, sh);
    }
    public void CreateDefaultCamera()
    {
      _camera = new Camera3D(this.Name + "-def-cam", 0.01f, 4000);
      _camera.Position_Local = new vec3(-16, 16, -16);
      _camera.FOV = MathUtils.ToRadians(81.7f);

      _camera.PhysicsData = new PhysicsData()
      {
        HasGravity = false,
        Collides = false,
      };

      _camera.Mesh = null;
      _camera.Material = null;
      _camera.AddComponent(new FPSInputComponent(this));
      Box3f b = Box3f.Zero;
      _camera.Update(0, ref b);
      Gu.World.AddObject(_camera, false);
      SyncCamera();

      // var l = new WorldObject(this.Name + "-def-light");
      // l.HasLight = true;
      // l.LightRadius = 50;
      // l.LightPower = 0.75f;
      // l.LightColor = new vec3(1, 1, 1);
      // l.Position_Local = new vec3(0, 0, 0);
      // _camera.AddChild(l);
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
    public void BeginRender2D(mat4? customProj)
    {
      _customProj = customProj;
      CompileGpuData();
      //** we do not do this nw
      //ProjectionMatrix = mat4.getOrtho((float)Viewport.X, (float)Viewport.Width, (float)Viewport.Y, (float)Viewport.Height, -1.0f, 1.0f);
      SetModified();
    }
    public void EndRender2D()
    {
      _customProj = null;
      SetModified();
    }

    public bool BeginPipelineStage(PipelineStage ps)
    {
      UpdateDimensions(ps.Size.width, ps.Size.height);
      GL.Viewport(Viewport.X,
                  ps.Size.y - Viewport.Y - Viewport.Height, //OpenGL Y = Bottom left!!!
                  Viewport.Width,
                  Viewport.Height);

      //if we have an active camera set the scissor to the camera, else set to the viewport. e.g. make "black bars"
      var clip = GetClipViewport();
      GL.Scissor(clip.X,
                 ps.Size.y - clip.Y - clip.Height, //OpenGL Y = Bottom left!!!
                 clip.Width,
                 clip.Height);

      CompileGpuData();

      return true;
    }
    public void EndPipelineStage(PipelineStage ps)
    {
    }
    private Viewport GetClipViewport()
    {
      if (_camera != null)
      {
        return _camera.Viewport;
      }
      else
      {
        return Viewport;
      }
    }
    public void OnResize(int sw, int sh)
    {
      UpdateDimensions(sw, sh);
      Gui?.OnResize();//Gui is translated to the current FBO size in the shader.
    }
    private void UpdateDimensions(int cur_output_fbo_w, int cur_output_fbo_h)
    {
      //** CALLED EVERY PIPE STAGE **

      var b = ComputeScaledView(_uv0, _uv1, cur_output_fbo_w, cur_output_fbo_h);

      if (b.w <= 0 || b.h <= 0)
      {
        Gu.Log.Info("Resize View " + Name + ": " + b.x + "," + b.y + " " + b.w + "," + b.h);
        Gu.Log.Error("Resize View " + Name + " w/h was zero, setting to 1");
        Gu.DebugBreak();
        if (b.w <= 0) { b.w = 1; }
        if (b.h <= 0) { b.h = 1; }
      }

      _viewport.X = b.x;
      _viewport.Y = b.y;
      _viewport.Width = b.w;
      _viewport.Height = b.h;

      SyncCamera();
    }
    private void SyncCamera()
    {
      //cam must always be in sync
      if (_camera != null && _enabled)
      {
        _camera.ComputeView(this);
      }
    }
    public static Box2i ComputeScaledView(vec2 uv0, vec2 uv1, int width, int height)
    {
      //render views are defined as taking up a uv % of the window, this computes the w/h from the uv %
      Box2i b = new Box2i();
      b.x = (int)(Math.Round(uv0.x * (float)width));
      b.y = (int)(Math.Round(uv0.y * (float)height));
      b.w = (int)(Math.Round((uv1.x - uv0.x) * (float)width));
      b.h = (int)(Math.Round((uv1.y - uv0.y) * (float)height));
      return b;
    }
    private void CompileGpuData()
    {
      // if (Modified || Gu.EngineConfig.Debug_AlwaysCompileAndReloadGpuUniformData)
      // {
      if (ViewMode != RenderViewMode.UIOnly)
      {
        _gpuCamera._vViewPos = _camera.Position_World;
        _gpuCamera._vViewDir = _camera.Heading;
        _gpuCamera._m4View = _camera.ViewMatrix;
        _gpuCamera._widthNear = _camera.Frustum.WidthNear;
        _gpuCamera._heightNear = _camera.Frustum.HeightNear;
        _gpuCamera._heightFar = _camera.Frustum.HeightFar;
        _gpuCamera._widthFar = _camera.Frustum.WidthFar;
        _gpuCamera._fZNear = _camera.Near;
        _gpuCamera._fZFar = _camera.Far;
      }
      else
      {
        _gpuCamera._vViewPos = vec3.Zero;
        _gpuCamera._vViewDir = vec3.Zero;
        _gpuCamera._m4View = mat4.Identity;
        _gpuCamera._m4Projection = mat4.Identity;
      }

      if (ViewMode != RenderViewMode.UIOnly)
      {
        if (_customProj != null)
        {
          _gpuCamera._m4Projection = _customProj.Value;
        }
        else
        {
          _gpuCamera._m4Projection = _camera.ProjMatrix;
        }
      }
      else
      {
        _gpuCamera._m4Projection = mat4.Identity;
      }

      _gpuCamera._fWindowWidth = Gu.Context.GameWindow.Width;
      _gpuCamera._fWindowHeight = Gu.Context.GameWindow.Height;
      _gpuCamera._fRenderWidth = (float)Gu.Context.Renderer.CurrentStageFBOSize.x;
      _gpuCamera._fRenderHeight = (float)Gu.Context.Renderer.CurrentStageFBOSize.y;

      //TODO: this may be different from camera viewport / invalid
      //   var vp =GetClipViewport();
      _gpuCamera._vWindowViewport.x = Viewport.X;
      _gpuCamera._vWindowViewport.y = Viewport.Y;
      _gpuCamera._vWindowViewport.z = Viewport.Width;
      _gpuCamera._vWindowViewport.w = Viewport.Height;

      if (_camBuf == null)
      {
        _camBuf = Gpu.CreateUniformBuffer("renderview_cambuf", _gpuCamera);
      }
      else
      {
        _camBuf.CopyToGPU(_gpuCamera);
      }



      //}
    }
  }//cls
  [DataContract]
  public class Camera3D : WorldObject
  {
    //TODO: data block
    //Camera is loosely tied to the render viewport
    // RenderViewport: Area of the window to render to. Could be whole window, or part of a window.
    // Camera defines a sub-area of the viewport (e.g. blender->view camera)
    // The camera viewport needs separate e.g. Our window is 16:9 but we only want to render to a 4:3 game.. then.. black bars.    
    public Frustum Frustum { get { return _frustum; } }
    public float FOV
    {
      get { return _fov; }
      set
      {
        _fov = value;
        float e = (float)0.000001f;
        if (_fov > (float)Math.PI - e)
        {
          Gu.DebugBreak();
          _fov = (float)Math.PI - e;

        }
        if (_fov < e)
        {
          Gu.DebugBreak();
          _fov = e;
        }
      }
    }
    public float Near { get { return _near; } set { _near = value; } }
    public float Far { get { return _far; } set { _far = value; } }
    public mat4 ViewMatrix { get { return _view; } }
    public mat4 ProjMatrix { get { return _persp; } }
    public ProjectionMode ProjectionMode { get { return _projectionMode; } set { _projectionMode = value; } }
    public Viewport Viewport { get { return _computedViewport; } }
    public ivec2? RenderWH { get { return _renderWH; } set { _renderWH = value; } }

    [DataMember] private ivec2? _renderWH = null;//new ivec2(300, 999); //rendering dims / pixels. - default to renderview size
    [DataMember] private Viewport _computedViewport;
    [DataMember] private mat4 _persp = mat4.Identity;
    [DataMember] private mat4 _view = mat4.Identity;
    [DataMember] private float _fov = (float)Math.PI * 0.6f;
    [DataMember] private float _near = 1.0f;
    [DataMember] private float _far = 1000.0f;
    [DataMember] private ProjectionMode _projectionMode = ProjectionMode.Perspective;
    [DataMember] private Frustum _frustum;

    public Camera3D(string name, float near, float far) : base(name)
    {
      _near = near;
      _far = far;
      _computedViewport = new Viewport(0, 0, 1, 1);
      _frustum = new Frustum(this);
    }
    public void LookAt(vec3 pos, vec3 at)
    {
      //Test
      Position_Local = pos;
      vec3 n = (at - pos).normalize();
      float ang = (float)Math.Acos(BasisZ_World.dot(n));
      vec3 c = n.cross(BasisZ_World);
      Rotation_Local *= quat.fromAxisAngle(c, ang, true);
    }
    public void ComputeView(RenderView rv)
    {
      //sets the camera viewport to be centered within the viewing region
      //perhaps in the future we will account for random viewports (for whatever reason)
      //we can only *shrink* the view's viewport, no growing or going out of bounds.

      if (_renderWH == null)
      {
        _computedViewport.X = rv.Viewport.X;
        _computedViewport.Y = rv.Viewport.Y;
        _computedViewport.Width = rv.Viewport.Width;
        _computedViewport.Height = rv.Viewport.Height;
      }
      else
      {
        ivec2 wh = _renderWH.Value;

        _computedViewport.X = rv.Viewport.X + (rv.Viewport.Width - wh.width) / 2;
        _computedViewport.Y = rv.Viewport.Y + (rv.Viewport.Height - wh.height) / 2;
        _computedViewport.Width = wh.width;
        _computedViewport.Height = wh.height;

        int minw = 2; //min camera viewport size w/h
        int minh = 2;

        int maxx = (rv.Viewport.X + rv.Viewport.Width - minw) / 2;
        int maxy = (rv.Viewport.Y + rv.Viewport.Height - minh) / 2;

        //if specified rwh goes above/below view's viewport then clamp it.
        _computedViewport.X = Math.Min(Math.Max(rv.Viewport.X, _computedViewport.X), maxx);
        _computedViewport.Y = Math.Min(Math.Max(rv.Viewport.Y, _computedViewport.Y), maxy);
        _computedViewport.Width = (maxx - _computedViewport.X) * 2;
        _computedViewport.Height = (maxy - _computedViewport.Y) * 2;
      }
    }
    public override void Update(double dt, ref Box3f parentBoundBox)
    {
      base.Update(dt, ref parentBoundBox);

      var p = this.WorldMatrix.ExtractTranslation();
      _view = mat4.lookAt(p, new vec3(p + BasisZ_World), new vec3(0, 1, 0));
      if (_projectionMode == ProjectionMode.Orthographic)
      {
        float wn2 = _frustum.WidthNear / 2;
        float hn2 = _frustum.HeightNear / 2;
        float aspect = _computedViewport.Width / _computedViewport.Height;
        //we need to scale the z axis somehow z/w .. etc
        // p.z = 2*(p.z/p.w) -1; - this makes orthographic clipping possible.
        _persp = (mat4.ortho(wn2, -wn2, hn2, -hn2, _near, _far)).transpose();//GL's matrix is column major so the transpose is the correct, somehow raster2d is working..
                                                                             //_persp = (mat4.ortho(wn2, -wn2, wn2, -wn2, -(_far-_near)/2, (_far-_near)/2)).transpose();//GL's matrix is column major so the transpose is the correct, somehow raster2d is working..
                                                                             //_persp = (mat4.ortho(80, -80, 80, -80, -(_far-_near)/2, (_far-_near)/2)).transpose();//GL's matrix is column major so the transpose is the correct, somehow raster2d is working..
                                                                             //_persp = (mat4.ortho(_computedViewport.Width/2, -_computedViewport.Width/2, _computedViewport.Height/2, -_computedViewport.Height/2, 0, 1)).transpose();//GL's matrix is column major so the transpose is the correct, somehow raster2d is working..
                                                                             //_persp = (mat4.ortho(50, -50, 50*aspect, -50*aspect, 0, 1)).transpose();//GL's matrix is column major so the transpose is the correct, somehow raster2d is working..
                                                                             // var w = _computedViewport.Width/2;
                                                                             //  var h = _computedViewport.Height/2;
                                                                             //  _persp = (mat4.ortho(w, -w, h, -h, -1300, 1300)).transpose();//GL's matrix is column major so the transpose is the correct, somehow raster2d is working..
      }
      else if (_projectionMode == ProjectionMode.Perspective)
      {
        _persp = mat4.projection(_fov, _computedViewport.Width, _computedViewport.Height, _near, _far);
      }
      else { Gu.BRThrowNotImplementedException(); }

      _frustum.Update();
    }


  }
}
