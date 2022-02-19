using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace PirateCraft
{

   public class Camera3D : WorldObject
   {
      float _fov = MathUtils.ToRadians(70.0f);
      float _near = 1;
      float _far = 1000;
      private float _widthNear = 1;
      private float _heightNear = 1;
      private float _widthFar = 1;
      private float _heightFar = 1;

      vec3 _nearCenter = new vec3(0, 0, 0);
      vec3 _farCenter = new vec3(0, 0, 0);
      vec3 _nearTopLeft = new vec3(0, 0, 0);
      vec3 _farTopLeft = new vec3(0, 0, 0);
      mat4 _projectionMatrix = mat4.identity();
      mat4 _viewMatrix = mat4.identity();
      //ProjectionMode ProjectionMode = ProjectionMode.Perspective;

      public float FOV { get { return _fov; } set { _fov = value; } }
      public float Near { get { return _near; } private set { _near = value; } }
      public float Far { get { return _far; } private set { _far = value; } }
      public vec3 NearCenter { get { return _nearCenter; } private set { _nearCenter = value; } }
      public vec3 FarCenter { get { return _farCenter; } private set { _farCenter = value; } }
      public vec3 NearTopLeft { get { return _nearTopLeft; } private set { _nearTopLeft = value; } }
      public vec3 FarTopLeft { get { return _farTopLeft; } private set { _farTopLeft = value; } }
      public mat4 ProjectionMatrix { get { return _projectionMatrix; } private set { _projectionMatrix = value; } }
      public mat4 ViewMatrix { get { return _viewMatrix; } private set { _viewMatrix = value; } }

      public int _view_x = 0, _view_y = 0, _view_w = 800, _view_h = 600;
      public int Viewport_X { get { return _view_x; } set { _view_x = value; } }
      public int Viewport_Y { get { return _view_y; } set { _view_y = value; } }
      public int Viewport_Width { get { return _view_w; } set { _view_w = value; } }
      public int Viewport_Height { get { return _view_h; } set { _view_h = value; } }

      public Camera3D(string name, int w, int h, float near = 1, float far = 1000)
      {
         //Do not select camera (at least not active camera) since we wont be able to hit anything else.
         //SelectEnabled = false;
         Name = name;
         _view_w = w;
         _view_h = h;
      }
      public override void Update(double dt, Box3f? parentBoundBox = null)
      {
         base.Update(dt, parentBoundBox);

         //Not really necessary to keep calling this unless we change window parameters
      //   ProjectionMatrix = mat4.CreatePerspectiveFieldOfView(FOV, Viewport_Width / Viewport_Height, Near, Far);
      //  ViewMatrix = mat4.LookAt(Position, Position + BasisZ.Normalized(), new vec3(0, 1, 0));
         ProjectionMatrix = mat4.projection(FOV, Viewport_Width, Viewport_Height, Near, Far);
         ViewMatrix = mat4.getLookAt(new vec3(Position), new vec3(Position+BasisZ.normalized()), new vec3(0, 1, 0));

         //Frustum
         float tanfov2 = MathUtils.tanf(FOV / 2.0f);
         float ar = ((float)Viewport_Width / (float)Viewport_Height);

         //tan(fov2) = w2/near
         //tan(fov2) * near = w2
         //w/h = w2/h2
         //(w/h)*h2 = w2
         //w2/(w/h) = h2
         _widthNear = tanfov2 * Near * 2;
         _heightNear = _widthNear / ar;
         _widthFar = tanfov2 * Far * 2;
         _heightFar = _widthFar / ar;

         NearCenter = Position + BasisZ * Near;
         FarCenter = Position + BasisZ * Far;
         NearTopLeft = NearCenter - BasisX * _widthNear + BasisY * _heightNear;
         FarTopLeft = FarCenter - BasisX * _widthFar + BasisY * _heightFar;

         //    }
         //    _updating = false;
         //}
         //_dirty = false;
      }
      public void BeginRender()
      {
         GL.Viewport(0, 0, Viewport_Width, Viewport_Height);
         GL.Scissor(0, 0, Viewport_Width, Viewport_Height);
      }
      public void EndRender()
      {
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

      public Line3f ProjectPoint(vec2 point_on_screen, TransformSpace space = TransformSpace.World, float additionalZDepthNear = 0)
      {
         //Note: we were using PickRay before because that's used to pick OOBs. We don't need that right now but we will in the future.
         Line3f pt = new Line3f();

         float left_pct = point_on_screen.x / (float)Viewport_Width;
         float top_pct = (point_on_screen.y) / (float)Viewport_Height;

         if (space == TransformSpace.Local)
         {
            //Transform in local coordinates.
            vec3 localX = new vec3(1, 0, 0);
            vec3 localY = new vec3(0, 1, 0);
            vec3 localZ = new vec3(0, 0, 1);
            vec3 near_center_local = localZ * Near;
            vec3 far_center_local = localZ * Far;
            vec3 ntl = near_center_local - localX * _widthNear + localY * _heightNear;
            vec3 ftl = far_center_local - localX * _widthFar + localY * _heightFar;
            pt.p0 = ntl + localX * _widthNear * left_pct + localY * _heightNear * top_pct;
            pt.p1 = ftl + localX * _widthFar * left_pct + localY * _heightFar * top_pct;
            pt.p0 += localZ * additionalZDepthNear;
         }
         else
         {
            pt.p0 = NearTopLeft + BasisX * _widthNear * left_pct + BasisY * _heightNear * top_pct;
            pt.p1 = FarTopLeft + BasisX * _widthFar * left_pct + BasisY * _heightFar * top_pct;
            pt.p0 += BasisZ * additionalZDepthNear;
         }

         return pt;
      }

   }
}
