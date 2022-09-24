using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace PirateCraft
{
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
    public void Update()
    {
      if (_camera != null && _camera.TryGetTarget(out Camera3D cam))
      {
        if (cam.View != null && cam.View.TryGetTarget(out var view))
        {
          //Frustum
          float tanfov2 = MathUtils.tanf(cam.FOV / 2.0f);
          float ar = (float)view.Viewport.Width / (float)view.Viewport.Height;

          //tan(fov2) = w2/near
          //tan(fov2) * near = w2
          //w/h = w2/h2
          //(w/h)*h2 = w2
          //w2/(w/h) = h2
          _widthNear = tanfov2 * cam.Near * 2;
          _heightNear = _widthNear / ar;
          _widthFar = tanfov2 * cam.Far * 2;
          _heightFar = _widthFar / ar;

          NearCenter = cam.Position_World + cam.BasisZ * cam.Near;
          FarCenter = cam.Position_World + cam.BasisZ * cam.Far;
          //X is right in RHS
          NearTopLeft = NearCenter - cam.BasisX * _widthNear * 0.5f + cam.BasisY * _heightNear * 0.5f;
          FarTopLeft = FarCenter - cam.BasisX * _widthFar * 0.5f + cam.BasisY * _heightFar * 0.5f;

          ConstructPointsAndPlanes(FarCenter, NearCenter, cam.BasisY, cam.BasisX, _widthNear, _widthFar, _heightNear, _heightFar);
        }
      }
    }
    public Line3f? ScreenToWorld(vec2 point_on_screen_topleftorigin, TransformSpace space = TransformSpace.World, float additionalZDepthNear = 0, float maxDistance = -1)
    {
      //Returns null if the camera or view are not set.
      Line3f? pt_ret = null;

      if (_camera != null && _camera.TryGetTarget(out var cam))
      {
        if (cam.View != null && cam.View.TryGetTarget(out var view))
        {
          Line3f pt = new Line3f();
          float left_pct = (float)point_on_screen_topleftorigin.x / (float)view.Viewport.Width;
          float top_pct = (float)point_on_screen_topleftorigin.y / (float)view.Viewport.Height;

          if (space == TransformSpace.Local)
          {
            //Transform in local coordinates.
            vec3 localX = new vec3(1, 0, 0);
            vec3 localY = new vec3(0, 1, 0);
            vec3 localZ = new vec3(0, 0, 1);
            vec3 near_center_local = localZ * cam.Near;
            vec3 far_center_local = localZ * cam.Far;
            vec3 ntl = near_center_local - localX * _widthNear + localY * _heightNear;
            vec3 ftl = far_center_local - localX * _widthFar + localY * _heightFar;
            pt.p0 = ntl + localX * _widthNear * left_pct + localY * _heightNear * top_pct;
            pt.p1 = ftl + localX * _widthFar * left_pct + localY * _heightFar * top_pct;
            pt.p0 += localZ * additionalZDepthNear;
          }
          else
          {
            pt.p0 = NearTopLeft + cam.BasisX * _widthNear * left_pct - cam.BasisY * _heightNear * top_pct;
            pt.p1 = FarTopLeft + cam.BasisX * _widthFar * left_pct - cam.BasisY * _heightFar * top_pct;
            pt.p0 += cam.BasisZ * additionalZDepthNear;

            if (maxDistance > 0)
            {
              pt.p1 = pt.p0 + (pt.p1 - pt.p0).normalize() * (maxDistance - additionalZDepthNear);
            }
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

        float t = _planes[fp_near].IntersectLine(v, campos);

        vec3 ret = campos + (v - campos) * t;
        return ret;
      }
      return null;
    }
    public bool HasBox(in Box3f pCube)
    {
      vec3 min, max;
      float d1, d2;
      if (!pCube.Validate())
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

        d1 = _planes[i].dist(max);
        d2 = _planes[i].dist(min);

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
        if (_planes[pi].dist(p) < 0)
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
      _planes[fp_near] = new Plane3f(_points[fpt_ntl], _points[fpt_ntr], _points[fpt_nbl], _points[fpt_nbr]);
      _planes[fp_far] = new Plane3f(_points[fpt_ftr], _points[fpt_ftl], _points[fpt_fbr], _points[fpt_fbl]);

      _planes[fp_left] = new Plane3f(_points[fpt_ftl], _points[fpt_ntl], _points[fpt_fbl], _points[fpt_nbl]);
      _planes[fp_right] = new Plane3f(_points[fpt_ntr], _points[fpt_ftr], _points[fpt_nbr], _points[fpt_fbr]);

      _planes[fp_top] = new Plane3f(_points[fpt_ntr], _points[fpt_ntl], _points[fpt_ftr], _points[fpt_ftl]);
      _planes[fp_bottom] = new Plane3f(_points[fpt_fbr], _points[fpt_fbl], _points[fpt_nbr], _points[fpt_nbl]);
    }
    #endregion
  }

  public class Viewport
  {
    //Viewport is in TOP LEFT coordinates.
    // OpenGL = bottom left, we convert this in the renderer
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
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

  //temp location for this class
  //so, this is separate from picker because picker is generally a rendering layer thing, that comes at the end
  //of rendering.. this comes within the update, and specifically the world- update- input- control logic
  // however it is dependent on the current RenderView.
  public enum InputState
  {
    Select,//Global state
    Camera_Move,//TODO:
    Camera_Pan, //TODO:
    Selected_Move
  }
  public class KeyMap
  {
    //Pretty much, ready for this guy
    //KeyCombo Ctrl+L
    //KeyStroke Action KeyCombo(Ctrl+L) | KeyCombo(Ctrl+W)
  }
  public class ObjectSelector
  {
    public InputState InputState { get; private set; } = InputState.Select;
    public List<WorldObject> SelectedObjects { get; set; } = new List<WorldObject>();

    private List<PRS> _selectedObjectsPreviousState = new List<PRS>();
    private vec3 _selectionOrigin = new vec3(0, 0, 0);
    private Plane3f _selectionPlane = new Plane3f();
    private bool _xform_XAxis = true;
    private bool _xform_YAxis = true;
    private bool _xform_ZAxis = true;
    private vec2 _xform_MouseStart;
    private vec2 _xform_WrapCount;
    private Line3f? _xform_startRay = null;

    public void Update(RenderView v)
    {
      var m = Gu.Context.PCMouse;
      var k = Gu.Context.PCKeyboard;

      if (InputState == InputState.Selected_Move)
      {
        //Grab the delta point 
        if (k.Press(Keys.X))
        {
          if (k.PressOrDown(Keys.LeftShift) || k.PressOrDown(Keys.RightShift))
          {
            _xform_XAxis = false;
            _xform_YAxis = true;
            _xform_ZAxis = true;
            _selectionPlane = new Plane3f(new vec3(1, 0, 0), _selectionOrigin);
          }
          else
          {
            _xform_XAxis = true;
            _xform_YAxis = false;
            _xform_ZAxis = false;
            _selectionPlane = new Plane3f(new vec3(0, 1, 0), _selectionOrigin);
          }
        }
        if (k.Press(Keys.Y))
        {
          if (k.PressOrDown(Keys.LeftShift) || k.PressOrDown(Keys.RightShift))
          {
            _xform_XAxis = true;
            _xform_YAxis = false;
            _xform_ZAxis = true;
            _selectionPlane = new Plane3f(new vec3(0, 1, 0), _selectionOrigin);
          }
          else
          {
            _xform_XAxis = false;
            _xform_YAxis = true;
            _xform_ZAxis = false;
            _selectionPlane = new Plane3f(new vec3(1, 0, 0), _selectionOrigin);
          }
        }
        if (k.Press(Keys.Z))
        {
          if (k.PressOrDown(Keys.LeftShift) || k.PressOrDown(Keys.RightShift))
          {
            _xform_XAxis = true;
            _xform_YAxis = true;
            _xform_ZAxis = false;
            _selectionPlane = new Plane3f(new vec3(0, 0, 1), _selectionOrigin);
          }
          else
          {
            _xform_XAxis = false;
            _xform_YAxis = false;
            _xform_ZAxis = true;
            _selectionPlane = new Plane3f(new vec3(1, 0, 0), _selectionOrigin);
          }
        }

        if (k.Press(Keys.Escape) || m.Press(MouseButton.Right))
        {
          //Revert changes
          Gu.Assert(SelectedObjects.Count == _selectedObjectsPreviousState.Count);
          for (var obi = 0; obi < _selectedObjectsPreviousState.Count; obi++)
          {
            SelectedObjects[obi].SetPRS_Local(_selectedObjectsPreviousState[obi]);
          }
          _selectedObjectsPreviousState.Clear();

          InputState = InputState.Select;
        }
        else if (k.Press(Keys.Enter) || m.Press(MouseButton.Left))
        {
          //Keep changes.
          InputState = InputState.Select;
        }
        else
        {
          //wrap mouse
          var vwrap = Gu.Mouse.WarpMouse(v, WarpMode.Wrap, 0.001f);
          if (vwrap != null)
          {
            _xform_WrapCount += vwrap.Value;
          }
          var mouse_wrapped = Gu.Mouse.GetWrappedPosition(v, _xform_WrapCount);
          var xform_curRay = Gu.CastRayFromScreen(mouse_wrapped);

          //Update
          if (xform_curRay != null)
          {
            vec3 delta_pos = new vec3(0, 0, 0);
            float tcur = _selectionPlane.IntersectLine(xform_curRay.Value.p0, xform_curRay.Value.p1);
            //  float tstart = _selectionPlane.IntersectLine(_xform_startRay.Value.p0, _xform_startRay.Value.p1);
            //  if (tcur >= 0 && tcur <= 1 /*&& tstart >= 0 && tstart <= 1*/)
            // {
            //dont check ray for [0,1] because we may be on any side of the plane.
            //from the projected mouse points, start, and cur, get an "axis ray" that defines the xform axis.
            //multiply this vector by the mouse movement distance adding wraps
            var mouse_hit_plane_c = xform_curRay.Value.p0 + (xform_curRay.Value.p1 - xform_curRay.Value.p0) * tcur;
            // var mouse_hit_plane_s = (_xform_startRay.Value.p1 - _xform_startRay.Value.p0) * tstart;
            // var axis = mouse_hit_plane_c - mouse_hit_plane_s;

            delta_pos = mouse_hit_plane_c - _selectionOrigin;
            // }
            Gu.CustomDebugBreak();

            //Constrain
            delta_pos = new vec3(
              delta_pos.x * (_xform_XAxis ? 1.0f : 0.0f),
              delta_pos.y * (_xform_YAxis ? 1.0f : 0.0f),
              delta_pos.z * (_xform_ZAxis ? 1.0f : 0.0f)
            );

            Gu.Assert(SelectedObjects.Count == _selectedObjectsPreviousState.Count);
            for (var obi = 0; obi < _selectedObjectsPreviousState.Count; obi++)
            {
              var newpos = _selectedObjectsPreviousState[obi].Position + delta_pos;
              SelectedObjects[obi].Position_Local = newpos;
            }
          }


        }
      }
      else if (InputState == InputState.Select)
      {
        if (m.Press(MouseButton.Left))
        {
          bool did_act = false;
          var ob = Gu.Context.Renderer.Picker.PickedObjectFrame;
          if (ob is WorldObject)
          {
            var wob = ob as WorldObject;
            if (wob.Selectable)
            {
              if (k.PressOrDown(Keys.LeftShift) || k.PressOrDown(Keys.RightShift))
              {
                if (!SelectedObjects.Contains(wob))
                {
                  SelectedObjects.Add(wob);
                  did_act = true;
                }
                else
                {
                  SelectedObjects.Remove(wob);
                  did_act = true;
                }
              }
              else
              {
                if (SelectedObjects.Contains(wob))
                {
                  SelectedObjects.Remove(wob);
                  did_act = true;
                }
                else
                {
                  SelectedObjects.Clear();
                  SelectedObjects.Add(wob);
                  did_act = true;
                }
              }
            }
          }

          //Deselect all if user clicked something that is not the UI 
          // but isn't selectable or outside selection
          if (did_act == false)
          {
            if (
              ((ob is WorldObject) && ((ob as WorldObject).Selectable == false)) ||
              (ob == null)
              )
            {
              SelectedObjects.Clear();
            }
          }

        }
        else if (k.Press(Keys.G))
        {
          if (SelectedObjects.Count > 0)
          {
            InputState = InputState.Selected_Move;
            _selectedObjectsPreviousState.Clear();
            _selectionOrigin = new vec3();
            foreach (var ob in SelectedObjects)
            {
              _selectionOrigin += ob.Position_World;
              _selectedObjectsPreviousState.Add(ob.GetPRS_Local());
            }
            _selectionOrigin = _selectionOrigin / (float)SelectedObjects.Count;
            _selectionPlane = new Plane3f(new vec3(0, 1, 0), _selectionOrigin);

            _xform_XAxis = _xform_YAxis = _xform_ZAxis = true;
            _xform_MouseStart = m.Pos;
            _xform_WrapCount = new vec2(0, 0);
            _xform_startRay = Gu.CastRayFromScreen(_xform_MouseStart);

          }
        }
      }

    }
  }

  public class RenderView : MutableState
  {
    //RenderView: The part of the window in which to render.
    //RenderView needs to use a percentage of the screen not exact coords, since resizing the screen we don't know how big to make the view.
    public WeakReference<Camera3D> Camera { get; set; } = null;
    public Viewport Viewport { get; set; } = null;
    public mat4 ProjectionMatrix { get; private set; } = mat4.Identity;
    public GpuCamera GpuCamera { get { return _gpuCamera; } private set { _gpuCamera = value; } }
    public int Id { get; private set; } = 0;
    private static int s_idGen = 0;
    public Gui2d ActiveGui { get; set; } = null;
    public Gui2d EditGui { get; set; } = null;
    public Gui2d GameGui { get; set; } = null;
    public UiElement DebugInfo { get; set; } = null;
    public PolygonMode PolygonMode = PolygonMode.Fill;
    public ViewInputMode ViewInputMode = ViewInputMode.Edit;
    //public bool Visible { get; set; } = true;
    public ObjectSelector ObjectSelector { get; private set; } = new ObjectSelector();
    public Line3f? MouseRay { get; private set; } = null;

    private mat4 _projLast = mat4.Identity;
    private GpuCamera _gpuCamera = new GpuCamera();
    private vec2 _uv0 = vec2.Zero;
    private vec2 _uv1 = vec2.Zero;
    public string Name { get; private set; } = "";

    public RenderView(string name, vec2 uv0, vec2 uv1, int sw, int sh)
    {
      Name = name;
      //note: xy is bottom left in opengl
      Id = s_idGen++;

      _uv0 = uv0;
      _uv1 = uv1;
      Gu.Assert(_uv0.x < _uv1.x);
      Gu.Assert(_uv0.y < _uv1.y);
      OnResize(sw, sh);
      SetModified();
    }
    public void Update_PostView()
    {
      ActiveGui?.Update(Gu.Context.Delta);
      ObjectSelector?.Update(this);

      //Update Mouse Ray
      if (Gu.Context.GameWindow.ActiveViewCamera != null)
      {
        if (Gu.Context.GameWindow.ActiveViewCamera.Frustum != null)
        {
          MouseRay = Gu.Context.GameWindow.ActiveViewCamera.Frustum.ScreenToWorld(Gu.Mouse.Pos);
        }
      }
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
      if (Camera != null && Camera.TryGetTarget(out var c))
      {
        c.View = new WeakReference<RenderView>(this);
      }
      SetModified();
    }
    public bool BeginRender3D()
    {
      //Return false if the camera for this view isn't set.
      if (Camera != null && Camera.TryGetTarget(out var c))
      {
        //Viewport.SetupViewport();
        _projLast = ProjectionMatrix;
        ProjectionMatrix = mat4.projection(c.FOV, Viewport.Width, Viewport.Height, c.Near, c.Far);

        GL.PolygonMode(MaterialFace.Front, this.PolygonMode);

        SetCurrent();
        return true;
      }
      return false;
    }
    public void EndRender3D()
    {
      ProjectionMatrix = _projLast;
      SetModified();
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
    public void OnResize(int sw, int sh)
    {
      int x = (int)(Math.Round(_uv0.x * (float)sw));
      int y = (int)(Math.Round(_uv0.y * (float)sh));
      int w = (int)(Math.Round((_uv1.x - _uv0.x) * (float)sw));
      int h = (int)(Math.Round((_uv1.y - _uv0.y) * (float)sh));

      Gu.Log.Info("Resize View " + Name + ": " + x + "," + y + " " + w + "," + h);

      if (w <= 0 || h <= 0)
      {
        Gu.Log.Error("Resize View " + Name + " w/h was zero, setting to 1");
        if (w <= 0) w = 1;
        if (h <= 0) h = 1;
      }

      Viewport = new Viewport(x, y, w, h);
      ActiveGui?.OnResize();
    }
    public void CompileGpuData()
    {
      if (Modified || Gu.EngineConfig.Debug_AlwaysCompileAndReloadGpuUniformData)
      {
        Gu.Assert(Camera != null);
        if (Camera != null && Camera.TryGetTarget(out var c))
        {
          _gpuCamera._vViewPos = c.Position_World;
          _gpuCamera._vViewDir = c.Heading;
          _gpuCamera._m4View = c.ViewMatrix;
        }
        _gpuCamera._m4Projection = ProjectionMatrix;//Could be orthographic, or perspective depending
        _gpuCamera._fWindowWidth = Gu.Context.GameWindow.Width;
        _gpuCamera._fWindowHeight = Gu.Context.GameWindow.Height;
        _gpuCamera._vWindowViewport.x = Viewport.X;
        _gpuCamera._vWindowViewport.y = Viewport.Y;
        _gpuCamera._vWindowViewport.z = Viewport.Width;
        _gpuCamera._vWindowViewport.w = Viewport.Height;
      }
    }
  }

  //Camera is loosely tied to the render viewport
  // RenderViewport: Area of the window to render to. Could be whole window, or part of a window.
  // Camera defines a sub-area of the viewport (e.g. blender->view camera)
  // The camera viewport needs separate e.g. Our window is 16:9 but we only want to render to a 4:3 game.. then.. black bars.
  public class Camera3D : WorldObject
  {
    public Frustum Frustum { get; private set; } = null;
    public float FOV { get { return _fov; } set { _fov = value; } }
    public float Near { get { return _near; } set { _near = value; } }
    public float Far { get { return _far; } set { _far = value; } }
    public mat4 ViewMatrix { get { return _viewMatrix; } private set { _viewMatrix = value; } }
    public WeakReference<RenderView> View { get; set; } = null;//This may be null if the camera is not being viewed.

    private mat4 _viewMatrix = mat4.Identity;
    private float _fov = MathUtils.ToRadians(70.0f);
    private float _near = 1;
    private float _far = 1000;

    public Camera3D(string name, RenderView rv, float near = 1, float far = 1000) : base(name + "-cam")
    {
      _near = near;
      _far = far;
      View = new WeakReference<RenderView>(rv);
      Frustum = new Frustum(this);
    }
    public override void Update(World world, double dt, ref Box3f parentBoundBox)
    {
      base.Update(world, dt, ref parentBoundBox);
      var p = this.WorldMatrix.ExtractTranslation();
      ViewMatrix = mat4.getLookAt(p, new vec3(p + BasisZ), new vec3(0, 1, 0));
      Frustum.Update();
    }


  }
}
