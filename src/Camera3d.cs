using OpenTK.Graphics.OpenGL4;

namespace PirateCraft
{
  public class Frustum
  {

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

    WeakReference<Camera3D> _camera;
    float _widthNear = 1;
    float _heightNear = 1;
    float _widthFar = 1;
    float _heightFar = 1;
    Plane3f[] _planes = new Plane3f[6];
    vec3[] _points = new vec3[8];
    vec3 _nearCenter = new vec3(0, 0, 0);
    vec3 _farCenter = new vec3(0, 0, 0);
    vec3 _nearTopLeft = new vec3(0, 0, 0);
    vec3 _farTopLeft = new vec3(0, 0, 0);
    Box3f _boundBox = new Box3f(new vec3(0, 0, 0), new vec3(1, 1, 1));

    public vec3 NearCenter { get { return _nearCenter; } private set { _nearCenter = value; } }
    public vec3 FarCenter { get { return _farCenter; } private set { _farCenter = value; } }
    public vec3 NearTopLeft { get { return _nearTopLeft; } private set { _nearTopLeft = value; } }
    public vec3 FarTopLeft { get { return _farTopLeft; } private set { _farTopLeft = value; } }
    public Box3f BoundBox { get { return _boundBox; } private set { _boundBox = value; } }

    public Frustum(Camera3D cam)
    {
      _camera = new WeakReference<Camera3D>(cam);
    }
    public void Update()
    {
      if (_camera.TryGetTarget(out Camera3D cam))
      {
        //Frustum
        float tanfov2 = MathUtils.tanf(cam.FOV / 2.0f);
        float ar = (float)cam.Viewport_Width / (float)cam.Viewport_Height;

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
    public Line3f ScreenToWorld(vec2 point_on_screen_topleftorigin, TransformSpace space = TransformSpace.World, float additionalZDepthNear = 0, float maxDistance = -1)
    {
      //Note it is good to Set maxDistance to what you need instead of using the whole frustum. A long ray can affect physics accuracy.
      Line3f pt = new Line3f();

      if (_camera.TryGetTarget(out Camera3D cam))
      {
        float left_pct = (float)point_on_screen_topleftorigin.x / (float)cam.Viewport_Width;
        float top_pct = (float)point_on_screen_topleftorigin.y / (float)cam.Viewport_Height;

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
      }
      return pt;
    }
    public vec3? WorldToScreen(vec3 v)
    {
      //Project point in world onto screen
      //Note point may not be within the frustum.
      if (_camera.TryGetTarget(out var cam))
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
      bool ret = false;  // Inside the frustum
      vec3 min, max;
      float d1, d2;
      if (pCube._max < pCube._min)
      {
        Gu.Assert(pCube._max >= pCube._min);
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
  }

class RenderViewport {
  
}

  public class Camera3D : WorldObject
  {
    float _fov = MathUtils.ToRadians(70.0f);
    float _near = 1;
    float _far = 1000;

    public Frustum Frustum { get; private set; } = null;
    mat4 _projectionMatrix = mat4.identity();
    mat4 _viewMatrix = mat4.identity();
    //ProjectionMode ProjectionMode = ProjectionMode.Perspective;

    public float FOV { get { return _fov; } set { _fov = value; } }
    public float Near { get { return _near; } set { _near = value; } }
    public float Far { get { return _far; } set { _far = value; } }

    public mat4 ProjectionMatrix { get { return _projectionMatrix; } private set { _projectionMatrix = value; } }
    public mat4 ViewMatrix { get { return _viewMatrix; } private set { _viewMatrix = value; } }

    public int _view_x = 0, _view_y = 0, _view_w = 800, _view_h = 600;
    public int Viewport_X { get { return _view_x; } set { _view_x = value; } }
    public int Viewport_Y { get { return _view_y; } set { _view_y = value; } }
    public int Viewport_Width { get { return _view_w; } set { _view_w = value; } }
    public int Viewport_Height { get { return _view_h; } set { _view_h = value; } }

    private bool _bRasterMode = false;
    private mat4 _savedProjection = mat4.identity();

    public Camera3D(string name, int w, int h, float near = 1, float far = 1000) : base(name)
    {
      //Do not select camera (at least not active camera) since we wont be able to hit anything else.
      //SelectEnabled = false;
      _view_w = w;
      _view_h = h;
      Frustum = new Frustum(this);
    }
    public override void Update(World world, double dt, ref Box3f parentBoundBox)
    {
      base.Update(world, dt, ref parentBoundBox);
      ProjectionMatrix = mat4.projection(FOV, Viewport_Width, Viewport_Height, Near, Far);
      var p = this.WorldMatrix.extractTranslation();
      ViewMatrix = mat4.getLookAt(p, new vec3(p + BasisZ), new vec3(0, 1, 0));
      Frustum.Update();
    }
    public void BeginRender()
    {
      GL.Viewport(0, 0, Viewport_Width, Viewport_Height);
      GL.Scissor(0, 0, Viewport_Width, Viewport_Height);
    }
    public void EndRender()
    {
    }
    public void beginRaster()
    {
      //The second viewport call isn't necessary, however, this is "just in case'
      GL.Viewport(0, 0, Viewport_Width, Viewport_Height);
      GL.Scissor(0, 0, Viewport_Width, Viewport_Height);
      //Enter orthorgraphic projection mode for drawing images directly to the screen.
      _bRasterMode = true;
      _savedProjection = ProjectionMatrix;
      //Note: in the past the width/height of viewport has been off by -1 (math issue)
      ProjectionMatrix = mat4.getOrtho(Viewport_X, (float)Viewport_Width, Viewport_Y, (float)Viewport_Height, -1.0f, 1.0f);
    }
    public void endRaster()
    {
      ProjectionMatrix = _savedProjection;
    }
    //public override void Resize(Viewport vp) { }
    //public override void Update(double dt) { base.Update(dt); }
    //public override void Render(Renderer rm) { }
    //public override void Free() { }
    //public override void UpdateBoundBox()
    //{
    //    //BoundBoxComputed._vmax = BoundBoxComputed._vmin = Pos;
    //    //BoundBoxComputed._vmax.x += _mainVolume._radius;
    //    //BoundBoxComputed._vmax.y += _mainVolume._radius;
    //    //BoundBoxComputed._vmax.z += _mainVolume._radius;
    //    //BoundBoxComputed._vmin.x += -_mainVolume._radius;
    //    //BoundBoxComputed._vmin.y += -_mainVolume._radius;
    //    //BoundBoxComputed._vmin.z += -_mainVolume._radius;
    //}


  }
}
