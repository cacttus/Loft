using System;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Text;
namespace PirateCraft
{
  public enum InputState
  {
    SelectObject,//Global state
    Camera_Move,//TODO:
    Camera_Pan, //TODO:
    Selected_Move
  }
  public enum WorldActionState
  {
    StillDoing,
    Done,
    Cancel,
  }

  public abstract class WorldAction
  {
    public WorldAction() { }
    //False if the action has no history
    public abstract bool IsHistoryAction { get; }
    //Do the interactive thing, view can be null (window not focused)
    public abstract WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m);
    //Undo changes immediately
    public abstract void Undo(WorldEditor editor);
    //Redo changes immediately
    public abstract void Redo(WorldEditor editor);
    public virtual WorldActionState HandleEvent(WorldEditor editor, WorldEditEvent coide) { return WorldActionState.StillDoing; }
    public virtual void CollectVisibleObjects(RenderView? rv) { }//override to render
  }
  public class GlobalAction : WorldAction
  {
    //any action - for anything
    public GlobalAction() { }
    public override bool IsHistoryAction { get; }
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m) { return WorldActionState.Cancel; }
    public override void Undo(WorldEditor editor) { }
    public override void Redo(WorldEditor editor) { }
  }
  public class GUIAction : WorldAction
  {
    //for when gui is picked only
    public GUIAction() { }
    public override bool IsHistoryAction { get; }
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m) { return WorldActionState.Cancel; }
    public override void Undo(WorldEditor editor) { }
    public override void Redo(WorldEditor editor) { }
  }
  public abstract class WorldObjectAction : WorldAction
  {
    protected List<WorldObject> _objects;
    public WorldObjectAction(List<WorldObject> obs)
    {
      Gu.Assert(obs != null);
      _objects = new List<WorldObject>(obs);
    }
    public override bool IsHistoryAction { get { return true; } }
  }
  public class ObjectDeleteAction : WorldObjectAction
  {
    public ObjectDeleteAction(List<WorldObject> obs) : base(obs) { }
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m)
    {
      Redo(editor);
      return WorldActionState.Done;
    }
    public override void Undo(WorldEditor editor)
    {
      if (_objects != null)
      {
        foreach (var ob in _objects)
        {
          Gu.World.AddObject(ob);
        }
      }
    }
    public override void Redo(WorldEditor editor)
    {
      if (_objects != null)
      {
        foreach (var ob in _objects)
        {
          Gu.World.RemoveObject(ob);
        }
      }
    }
  }
  public class ObjectSelectAction : WorldObjectAction
  {
    private bool _somethingChanged = false;
    public enum SelectActionType { Select, Deselect }
    private SelectActionType _type = SelectActionType.Deselect;
    public ObjectSelectAction(WorldObject ob_selected, SelectActionType t) : this(new List<WorldObject>() { ob_selected }, t)
    {
    }
    public ObjectSelectAction(List<WorldObject> obs_selected, SelectActionType t) : base(obs_selected)
    {
      _type = t;
    }
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m)
    {
      Redo(editor);
      if (!_somethingChanged)
      {
        return WorldActionState.Cancel; //cancel if nothing changed to avoid bad history
      }
      return WorldActionState.Done;
    }
    public override void Undo(WorldEditor editor)
    {
      DoOrRedo(editor, _type == SelectActionType.Select ? SelectActionType.Deselect : SelectActionType.Select);
    }
    public override void Redo(WorldEditor editor)
    {
      DoOrRedo(editor, _type);
    }
    private void DoOrRedo(WorldEditor editor, SelectActionType type)
    {
      if (_objects != null)
      {
        foreach (var ob in _objects)
        {
          if (type == SelectActionType.Select)
          {
            if (!editor.SelectedObjects.Contains(ob))
            {
              editor.SelectedObjects.Add(ob);
              ob.IsSelected = true;
              _somethingChanged = true;
            }
          }
          else if (type == SelectActionType.Deselect)
          {
            if (editor.SelectedObjects.Contains(ob))
            {
              editor.SelectedObjects.Remove(ob);
              ob.IsSelected = false;
              _somethingChanged = true;
            }
          }
          else
          {
            Gu.BRThrowNotImplementedException();
          }
        }
        editor.UpdateSelectionOrigin();
      }
    }
  }
  public class WorldActionGroup : WorldAction
  {
    private List<WorldAction>? _actions = null;
    private List<WorldAction>? _doing = null;
    public WorldActionGroup(List<WorldAction> actions)
    {
      Gu.Assert(actions != null);
      _actions = actions;
      _doing = null;
    }
    public override bool IsHistoryAction { get { return true; } }//hm..
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m)
    {
      Gu.Assert(_actions != null);
      if (_doing == null)
      {
        _doing = new List<WorldAction>(_actions);
      }
      bool did_cancel = false;
      _doing.IterateSafe<WorldAction>((x) =>
      {
        var dd = x.Do(editor, renderview, k, m);
        if (dd == WorldActionState.Done)
        {
          _doing.Remove(x);
        }
        else if (dd == WorldActionState.Cancel)
        {
          did_cancel = true;
          return LambdaBool.Break;
        }
        return LambdaBool.Continue;
      });
      if (did_cancel)
      {
        return WorldActionState.Cancel;
      }
      return _doing.Count == 0 ? WorldActionState.Done : WorldActionState.StillDoing;
    }
    public override void Undo(WorldEditor editor)
    {
      Gu.Assert(_actions != null);
      foreach (var act in _actions)
      {
        act.Undo(editor);
      }
    }
    public override void Redo(WorldEditor editor)
    {
      Gu.Assert(_actions != null);
      foreach (var act in _actions)
      {
        act.Redo(editor);
      }
    }
  }
  public enum XFormOrigin
  {
    Average, Individual,
  }
  public enum XFormSpace
  {
    Global, Local, Free
  }
  public class MoveRotateScaleAction : WorldObjectAction
  {
    private class ObjectXFormSpace
    {
      //Stores temporary to transform object in the current space
      /*
      fixingthis..
      KEEP FIRST PROJECT POS**
      sub projection pos p2-p1 to calculate delta
      the further mouse is away from object, the greater the delta meaning
      |p2-p1| / |p1-origin|

      move free -> project ray onto view plane
      move 2 axes -> project ray onto plane with axis as normal
      move 1 axis -> project ray onto axis line

      scale free (all dims)-> project ray onto view plane -> use delta distance from ray to center as scale change amount
      scale 2 axes -> same, project onto plane, use distance as delta.
      scale 1 axis-> project onto ray line, use delta as change

      rotate -> mouse move in circular motion always no matter what axis selected
      p1 = cos, sin
      p2 = cos, sin
      wrap when p1>p2 or p1<p2
      free -> rotate about view plane
      2 axes, .. same as others .. use the axis
      */
      public mat4 Space;
      public float ConstraintX;
      public float ConstraintY;
      public float ConstraintZ;
      public vec3 Axis;
      public vec3 Origin;
      public bool IsPlane = false;
      public ObjectXFormSpace(mat4 space, float cx, float cy, float cz, vec3 axis, vec3 origin, bool isplane)
      {
        Space = space;
        ConstraintX = cx;
        ConstraintY = cy;
        ConstraintZ = cz;
        Origin = origin;
        Axis = axis;
        IsPlane = isplane;
      }
    }
    public enum MoveRotateScale
    {
      Move, Rotate, Scale
    }

    private XFormSpace _xFormSpace = XFormSpace.Free;//global/local transform blender: z->z /x->x etc
    private MoveRotateScale _type = MoveRotateScale.Move;
    private List<PRS> _prevPRS = null;
    private List<PRS> _nextPRS = null;
    private List<ObjectXFormSpace> _obj_xform = null;
    private vec2 _xform_MouseStart;
    private vec2 _mouse_wrapcount;
    private Line3f? _xform_mouseStartRay = null;
    private WorldEditEvent? _current_XForm = null;

    public MoveRotateScaleAction(List<WorldObject> objs, MoveRotateScale type) : base(objs)
    {
      _type = type;

      _prevPRS = new List<PRS>();
      for (int i = 0; i < _objects.Count; ++i)
      {
        var w = _objects[i];
        _prevPRS.Add(w.GetPRS_Local());
      }

      _xform_MouseStart = Gu.Context.PCMouse.Pos;
      _mouse_wrapcount = new vec2(0, 0);
      _xform_mouseStartRay = Gu.TryCastRayFromScreen(_xform_MouseStart);

      ComputeXFormSpace(Gu.World.Editor);
    }
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m)
    {
      return Update(editor, renderview, k, m);
    }
    public override void Undo(WorldEditor editor)
    {
      Gu.Assert(_prevPRS != null && _prevPRS.Count == _objects.Count);
      for (int i = 0; i < _objects.Count; ++i)
      {
        var w = _objects[i];
        w.SetPRS_Local(_prevPRS[i]);
      }
      editor.UpdateSelectionOrigin();
    }
    public override void Redo(WorldEditor editor)
    {
      Gu.Assert(_nextPRS != null && _nextPRS.Count == _objects.Count);
      for (int i = 0; i < _objects.Count; ++i)
      {
        var w = _objects[i];
        w.SetPRS_Local(_nextPRS[i]);
      }
      editor.UpdateSelectionOrigin();
    }
    public WorldActionState Update(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m)
    {
      if (renderview == null)
      {
        return WorldActionState.Cancel;
      }
      return UpdateMove(editor, renderview, k, m);
    }
    private void ToggleXFormSpace(WorldEditor e)
    {
      if (_xFormSpace == XFormSpace.Global)
      {
        _xFormSpace = XFormSpace.Local;
      }
      else if (_xFormSpace == XFormSpace.Local)
      {
        _xFormSpace = XFormSpace.Free;
      }
      else if (_xFormSpace == XFormSpace.Free)
      {
        _xFormSpace = XFormSpace.Global;
      }
    }
    // private vec3 ComputeLocalObjectAxis(int obi, vec3 global_axis)
    // {
    //   vec3 locla = (_prevPRS[obi].invert() * global_axis.toVec4(1)).toVec3().normalize();
    //   return locla;
    // }
    private vec3 _global_axis = vec3.Zero;
    private void ComputeXFormSpace(WorldEditor editor)
    {
      Gu.Assert(Gu.World.Editor.SelectionOrigin != null);
      // vec3 average_axis = new vec3(0, 0, 0);
      // for (int obi = 0; obi < _objects.Count; obi++)
      // {
      //   average_axis += ComputeLocalObjectAxis(obi, _xform_global_axis);
      // }
      // average_axis = (average_axis / (float)_objects.Count).normalize();
      if (_xFormSpace == XFormSpace.Free)
      {
        _global_axis = new vec3(0, 1, 0);
        if (Gu.TryGetSelectedView(out var rv))
        {
          _global_axis = -rv.Camera.BasisZ_World; //"view" translation
        }
      }

      _obj_xform = new List<ObjectXFormSpace>();
      for (int obi = 0; obi < _objects.Count; obi++)
      {
        var xf = ComputeObjectXFormSpace(obi, _xFormSpace, editor.XFormOrigin, editor.SelectionOrigin.Value);
        _obj_xform.Add(xf);
      }
    }
    private ObjectXFormSpace ComputeObjectXFormSpace(int obi, XFormSpace space, XFormOrigin origin, vec3 average_origin)
    {
      ObjectXFormSpace ret = null;

      var ob_origin = vec3.Zero;
      bool plane = false;
      vec3 basisX = new vec3(1, 0, 0), basisY = new vec3(0, 1, 0), basisZ = new vec3(0, 0, 1);
      float cX = 1, cY = 1, cZ = 1;
      mat4 mspace = new mat4();
      vec3 axis = new vec3(0, 1, 0);

      if (space == XFormSpace.Free)
      {
        //"view" translation or "free"
        // mspace = new mat4(new vec4(c.BasisX_World, 0), new vec4(-c.BasisZ_World, 0), new vec4(c.BasisY_World, 0), new vec4(0, 0, 0, 1));
        cX = cZ = 1;
        cY = 0;
        axis = _global_axis;
        ob_origin = _prevPRS[obi].Position.Value;
        plane = true;
      }
      else
      {
        //local/global

        if (_current_XForm == WorldEditEvent.MRS_TransformAxisX) { cX = 1; cY = 0; cZ = 0; axis = new vec3(1, 0, 0); plane = false; }
        else if (_current_XForm == WorldEditEvent.MRS_TransformAxisY) { cX = 0; cY = 1; cZ = 0; axis = new vec3(0, 1, 0); plane = false; }
        else if (_current_XForm == WorldEditEvent.MRS_TransformAxisZ) { cX = 0; cY = 0; cZ = 1; axis = new vec3(0, 0, 1); plane = false; }

        else if (_current_XForm == WorldEditEvent.MRS_TransformPlaneX) { cX = 0; cY = 1; cZ = 1; axis = new vec3(1, 0, 0); plane = true; }
        else if (_current_XForm == WorldEditEvent.MRS_TransformPlaneY) { cX = 1; cY = 0; cZ = 1; axis = new vec3(0, 1, 0); plane = true; }
        else if (_current_XForm == WorldEditEvent.MRS_TransformPlaneZ) { cX = 1; cY = 1; cZ = 0; axis = new vec3(0, 0, 1); plane = true; }

        if (space == XFormSpace.Global)
        {
          // mspace = mat4.Identity;
        }
        else if (space == XFormSpace.Local)
        {
          axis = (_prevPRS[obi].toMat4().inverseOf() * new vec4(axis, 1)).toVec3().normalize();
          // mspace = _prevPRS[obi].toQuat().toMat4();
        }
      }

      if (origin == XFormOrigin.Average)
      {
        ob_origin = average_origin;
      }
      else if (origin == XFormOrigin.Individual)
      {
        ob_origin = _prevPRS[obi].Position.Value;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      ret = new ObjectXFormSpace(mspace, cX, cY, cZ, axis, ob_origin, plane);
      return ret;
    }
    public override WorldActionState HandleEvent(WorldEditor editor, WorldEditEvent code)
    {
      if (code == WorldEditEvent.MRS_TransformToggleOrigin)
      {
        if (editor.XFormOrigin == XFormOrigin.Average)
        {
          editor.XFormOrigin = XFormOrigin.Individual;
        }
        else
        {
          editor.XFormOrigin = XFormOrigin.Average;
        }
        ComputeXFormSpace(editor);
      }
      else if (code == WorldEditEvent.MRS_TransformAxisX || code == WorldEditEvent.MRS_TransformAxisY || code == WorldEditEvent.MRS_TransformAxisZ ||
                code == WorldEditEvent.MRS_TransformPlaneX || code == WorldEditEvent.MRS_TransformPlaneY || code == WorldEditEvent.MRS_TransformPlaneZ)
      {
        //move -> global, local free
        //Rotate: Global->Local->free (also it is saved.)

        if (_current_XForm == code || _current_XForm == null)//x->x
        {
          ToggleXFormSpace(editor);
        }
        _current_XForm = code;

        ComputeXFormSpace(editor);
      }
      else if (code == WorldEditEvent.Cancel)
      {
        return WorldActionState.Cancel;
      }
      else if (code == WorldEditEvent.Confirm)
      {
        return WorldActionState.Done;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      return WorldActionState.StillDoing;
    }
    private Line3f? GetWrappedScreenRay(RenderView? renderview)
    {
      if (Gu.Context.PCKeyboard.Press(Keys.B))
      {
        Gu.Trap();
      }
      //update - wrap mouse - wrap and count wraps, then project into world.
      var vwrap = Gu.Context.PCMouse.WarpMouse(renderview, WarpMode.Wrap, 0.001f);
      if (vwrap != null)
      {
        _mouse_wrapcount += vwrap.Value;
      }

      var mouse_wrapped = Gu.Context.PCMouse.GetWrappedPosition(renderview, _mouse_wrapcount);
      return Gu.TryCastRayFromScreen(mouse_wrapped);
    }
    private WorldActionState UpdateMove(WorldEditor editor, RenderView? rv, PCKeyboard k, PCMouse m)
    {
      var screen_ray = GetWrappedScreenRay(rv);
      if (screen_ray != null)
      {
        Gu.Assert(_prevPRS != null);
        Gu.Assert(_objects.Count == _prevPRS.Count);

        _nextPRS = new List<PRS>();
        for (var obi = 0; obi < _prevPRS.Count; obi++)
        {
          var space = _obj_xform[obi];

          //TODO: this should  not happen every frame, create a mesh

          PRS p = _prevPRS[obi].Clone();
          if (this._type == MoveRotateScale.Move)
          {
            p.Position = DoMove(_prevPRS[obi].Position.Value, space.Axis, space.Origin, space.IsPlane, screen_ray, rv);
          }
          if (this._type == MoveRotateScale.Rotate)
          {
            // mn = space.Space * quat.fromAxisAngle(axis, delta, false).toMat4() * _lastMat[obi];
          }
          if (this._type == MoveRotateScale.Scale)
          {
            //to scale local we need mat4 no?
            // Either Premultiply or postmultiply
            //local -> multiply the object's local matrix by the scale
            //global -> multiply the object's global matrix by the scale
            //[delta, 0 0, 0]
            //[0 d 0, 0]
            //[0 0 d, 0]
            // scale *= Basisxyz * delta * constraint (0,1)
            //pos += basisxyz * delta * constraint
            //to do it we have a matrix in global coords, or local, like curently with the basis
            //  the issue though is we need to figure out how to constrain one or more axes.. I guess that's easy - for scale set 1 for constrained axis..easy
            //  we have to change PRS to use a matrix.
            //  ok, then we can set the components by decomposing the matrix.
            //  how to put one matrix in another space.. idk either A * B^-1..

            //var ls = _lastMat[obi].Scale.Value;
            //p.Scale = new vec3(ls.x * delta * space.ConstraintX, ls.y * delta * space.ConstraintY, ls.z * delta * space.ConstraintZ);
            // var mmm = CSharpScript.Call("PRSScript.cs", null);
            // if (mmm != null)
            // {
            //   mn = (mat4)mmm;
            // }
            // else
            // {
            //   mn = mat4.getScale(delta * space.ConstraintX + 1, delta * space.ConstraintY + 1, delta * space.ConstraintZ + 1) * _lastMat[obi];
            // }

            // if (space.Free)
            // {
            //   p.Scale = _lastPRS[obi].Scale * delta;
            // }
            // else if (space.Plane)
            // {
            //       if (this._current_XForm == WorldEditEvent.TransformPlaneX) { p.Scale = new vec3(ls.x, ls.y * delta, ls.z * delta); }
            //       else if (this._current_XForm == WorldEditEvent.TransformPlaneY) { p.Scale = new vec3(ls.x * delta, ls.y, ls.z * delta); }
            //       else if (this._current_XForm == WorldEditEvent.TransformPlaneZ) { p.Scale = new vec3(ls.x * delta, ls.y * delta, ls.z); }
            //       else { Gu.BRThrowNotImplementedException(); }
            // }
            // else
            // {
            //   var ls = _lastPRS[obi].Scale.Value;
            //   if (this._current_XForm == WorldEditEvent.TransformAxisX) { p.Scale = new vec3(ls.x * delta, ls.y, ls.z); }
            //   else if (this._current_XForm == WorldEditEvent.TransformAxisY) { p.Scale = new vec3(ls.x, ls.y * delta, ls.z); }
            //   else if (this._current_XForm == WorldEditEvent.TransformAxisZ) { p.Scale = new vec3(ls.x, ls.y, ls.z * delta); }
            //   else { Gu.BRThrowNotImplementedException(); }
            // }

          }
          _nextPRS.Add(p);
        }

        Redo(editor);
      }

      return WorldActionState.StillDoing;

    }//update
    public override void CollectVisibleObjects(RenderView? rv)
    {
      if (_mouse_ray_hit_pt != null)
      {
        rv.Overlay.Point(_mouse_ray_hit_pt.Value, OffColor.Magenta * 0.6f, 20);
      }
      if (_mouse_ray_hit_pt2 != null)
      {
        rv.Overlay.Point(_mouse_ray_hit_pt2.Value, OffColor.Pink * 0.6f, 20);
      }

      vec4 c = new vec4(0.892, 0.890, 0.883, 0.8);

      for (var obi = 0; obi < _prevPRS.Count; obi++)
      {
        if (_current_XForm != null)
        {
          if (_current_XForm.Value == WorldEditEvent.MRS_TransformPlaneX ||
             _current_XForm.Value == WorldEditEvent.MRS_TransformAxisX)
          {
            c.g = c.b = 0.0129f;
          }
          else if (_current_XForm.Value == WorldEditEvent.MRS_TransformPlaneY ||
             _current_XForm.Value == WorldEditEvent.MRS_TransformAxisY)
          {
            c.r = c.b = 0.0129f;
          }
          else if (_current_XForm.Value == WorldEditEvent.MRS_TransformPlaneZ ||
             _current_XForm.Value == WorldEditEvent.MRS_TransformAxisZ)
          {
            c.r = c.g = 0.0129f;
          }
        }

        var space = _obj_xform[obi];
        rv.Overlay.Line(space.Origin + space.Axis * 100, space.Origin - space.Axis * 100, c, 7);


        //**blah debug
        if (asdfasdf != null)
        {
          rv.Overlay.Point(asdfasdf.Value, OffColor.LightGray * 0.6f, 20);
          rv.Overlay.Point(asdfasdf3.Value, OffColor.Yellow * 0.6f, 20);
          rv.Overlay.Line(asdfsadf.Value.p0, asdfsadf.Value.p1, OffColor.LightYellow, 20);
          rv.Overlay.Line(sdf3sefd.Value.p0, sdf3sefd.Value.p1, OffColor.LightGreen, 20);
        }

      }
      base.CollectVisibleObjects(rv);
    }
    vec3? _mouse_ray_hit_pt = null;
    vec3? _mouse_ray_hit_pt2 = null;
    vec3? asdfasdf = null;
    vec3? asdfasdf3 = null;
    Line3f? asdfsadf = null;
    Line3f? sdf3sefd = null;

    private vec3 DoMove(vec3 op, vec3 axis, vec3 origin, bool isplane, Line3f? cur_screen_ray, RenderView rv)
    {
      Gu.Assert(_xform_mouseStartRay != null);
      Gu.Assert(cur_screen_ray != null);
      vec3 newp = origin;
      if (isplane)
      {
        //cast plane
        Plane3f pf = new Plane3f(axis, origin);
        vec3 pt_cur = vec3.Zero;
        //vec3 pt_start = vec3.Zero;
        bool didhit = true;
        //didhit = didhit && pf.IntersectLine(_xform_mouseStartRay.Value.p0, _xform_mouseStartRay.Value.p1, out pt_start);
        didhit = didhit && pf.IntersectLine(cur_screen_ray.Value.p0, cur_screen_ray.Value.p1, out pt_cur);
        if (didhit)
        {
          _mouse_ray_hit_pt = pt_cur;

          newp = pt_cur;
        }
      }
      else
      {
        //cast to the axis/line
        //|(p0 + tv0)-(p1 + tv1)| = d(t)
        //  p0-p1=p, v0-v1=v
        //  => t^2(v.v) + 2t(p.v) + p.p = d(t)^2
        //  A = v.v, B = 2p.v, C = p.p
        //  => At^2 + Bt + C = d(t)^2
        //  => dd/dt = 2At + B = 2d(t)
        //  => dd/dt = At + B/2 = d(t)=0
        //  => t = -B/2A
        //  => t = -(p.v)/(v.v)
        var s0_p0 = _xform_mouseStartRay.Value.p0;
        var s0_v0 = (_xform_mouseStartRay.Value.p1 - _xform_mouseStartRay.Value.p0).normalize();//prevent  huge numbers

        var s1_p0 = cur_screen_ray.Value.p0;
        var s1_v0 = (cur_screen_ray.Value.p1 - cur_screen_ray.Value.p0).normalize();//prevent  huge numbers

        var a_p = origin;
        var a_v = axis;
        vec3 tv, tp;
        float t0, t1;

        tv = (a_v - s0_v0);
        tp = (a_p - s0_p0);
        t0 = -tp.dot(tv) / tv.dot(tv);

        tv = (a_v - s1_v0);
        tp = (a_p - s1_p0);
        t1 = -tp.dot(tv) / tv.dot(tv);

        vec3 p0 = origin + axis * t0;
        vec3 p1 = origin + axis * t1;

        _mouse_ray_hit_pt = p0;
        _mouse_ray_hit_pt2 = p1;

        asdfasdf = s0_p0 + s0_v0 * t0;
        asdfasdf3 = s1_p0 + s1_v0 * t1;

        asdfsadf = new Line3f(s0_p0, s0_p0 + s0_v0 * 1000);
        sdf3sefd = new Line3f(s1_p0, s1_p0 + s1_v0 * 1000);


        newp = p1;
      }
      return newp;
    }

  }//cls
  public class QuitApplicationAction : WorldAction
  {
    public QuitApplicationAction() { }
    public override bool IsHistoryAction { get { return false; } }
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m)
    {
      Gu.Exit(false);
      return WorldActionState.Done;
    }
    public override void Undo(WorldEditor editor)
    {
    }
    public override void Redo(WorldEditor editor)
    {
    }
  }
  public class PrintMessageAction : WorldAction
  {
    string _msg = Library.UnsetName;
    public PrintMessageAction(string message) { _msg = message; }
    public override bool IsHistoryAction { get { return false; } }
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m)
    {
      Gu.Log.Info(_msg);
      return WorldActionState.Done;
    }
    public override void Undo(WorldEditor editor)
    {
    }
    public override void Redo(WorldEditor editor)
    {
    }
  }
  public class SelectRegionAction : WorldAction
  {
    //selectregion selectrange
    public vec2 _selectStart;
    private bool _somethingChanged = false;
    private List<WorldObject> _prev = new List<WorldObject>();
    private List<WorldObject> _cur = new List<WorldObject>();
    private bool _wasClick = false;//whether the user clicked (Raycast) or used region (beam)
    WorldObject? _prevActive = null;
    WorldObject? _curActive = null;

    public SelectRegionAction()
    {
      _selectStart = Gu.Context.PCMouse.Pos;
    }
    public override bool IsHistoryAction { get { return true; } }
    public override WorldActionState Do(WorldEditor editor, RenderView? rv, PCKeyboard k, PCMouse m)
    {
      CastBeam(_selectStart, Gu.Context.PCMouse.Pos);

      return WorldActionState.StillDoing;
    }

    public override void Undo(WorldEditor editor)
    {
      DoOrRedo(editor, true);
    }
    public override void Redo(WorldEditor editor)
    {
      DoOrRedo(editor, false);
    }
    private bool CheckIfSomethingChanged(WorldEditor editor)
    {
      if (editor.SelectedObjects.Count == _cur.Count)
      {
        foreach (var ob in _cur)
        {
          if (!editor.SelectedObjects.Contains(ob))
          {
            _somethingChanged = true;
            break;
          }
        }

        if (_curActive != _prevActive)
        {
          _somethingChanged = true;
        }
      }
      else
      {
        _somethingChanged = true;
      }
      return _somethingChanged;
    }
    private void DoOrRedo(WorldEditor editor, bool undo)
    {
      editor.ActiveObject = undo ? _prevActive : _curActive;

      // do select
      editor.SelectedObjects.IterateSafe((x) =>
      {
        x.IsSelected = false;
        return LambdaBool.Continue;
      });
      editor.SelectedObjects = new List<WorldObject>(undo ? _prev : _cur);
      editor.SelectedObjects.IterateSafe((x) =>
      {
        x.IsSelected = true;
        return LambdaBool.Continue;
      });
      editor.UpdateSelectionOrigin();
    }
    public override WorldActionState HandleEvent(WorldEditor editor, WorldEditEvent coide)
    {
      if (coide == WorldEditEvent.Edit_SelectRegionEnd)
      {
        FinalizeSelection(editor);
        if (!_somethingChanged)
        {
          return WorldActionState.Cancel;
        }
        return WorldActionState.Done;
      }
      else if (coide == WorldEditEvent.Cancel)
      {
        return WorldActionState.Cancel;
      }

      return WorldActionState.StillDoing;
    }
    OOBox3f? _castedBeam = null;
    public override void CollectVisibleObjects(RenderView? rv)
    {
      if (_castedBeam != null)
      {
        DrawBeam(_castedBeam, rv);
      }
    }
    private OOBox3f? CastBeam(vec2 p1, vec2 p2)
    {
      _castedBeam = null;
      if (Gu.TryGetSelectedViewCamera(out var cam))
      {
        _castedBeam = cam.Frustum.BeamcastWorld(_selectStart, p2, 1.01f, 200);//push in so you can see the thing
      }
      return _castedBeam;
    }
    private void DrawBeam(OOBox3f beam, RenderView rv)
    {
      Gu.Assert(rv != null);
      beam.GetTrianglesAndPlanes(out var tris, out var planes);
      vec4 c = new vec4(0.794, 0.814f, 0.893f, 0.245f); // color of selection area

      if (rv.Overlay.DrawBoundBoxes)
      {
        foreach (var tr in tris)
        {
          rv.Overlay.Triangle(tr._v[0], tr._v[1], tr._v[2], c);
        }
        //min/max, far
        rv.Overlay.Point(beam.Center, new vec4(0, 0.8f, 0.3928f, 1));
      }
      else
      {
        //selection area
        rv.Overlay.Triangle(tris[8]._v[0], tris[8]._v[1], tris[8]._v[2], c);
        rv.Overlay.Triangle(tris[9]._v[0], tris[9]._v[1], tris[9]._v[2], c);
      }
    }
    private List<WorldObject> GetPickedObjectsRegion(vec2 p1, vec2 p2)
    {
      List<WorldObject> objs = new List<WorldObject>();
      var bea = CastBeam(p1, p2);
      if (bea != null)
      {
        bea.GetTrianglesAndPlanes(out var tris, out var planes);
        var obs = WorldEditor.RemoveInvalidObjectsFromSelection(Gu.World.GetAllVisibleRootObjects());
        foreach (var ob in obs)
        {
          if (ob.BoundBoxMesh != null)
          {
            if (ConvexHull.HasBox(ob.BoundBoxMesh, planes))
            {
              objs.Add(ob);
            }
          }
        }
      }
      return objs;
    }
    private void FinalizeSelection(WorldEditor editor)
    {
      //Update selection
      _cur = new List<WorldObject>();
      _prev = new List<WorldObject>(editor.SelectedObjects);

      var picked = GetPickedObjects();

      if (Gu.Context.PCKeyboard.ModIsDown(KeyMod.Shift))
      {
        _cur = new List<WorldObject>(editor.SelectedObjects);
        _cur.AddRange(picked.Where(x => x.IsSelected == false));
      }
      else
      {
        _cur = picked;
      }

      //Set active object
      _curActive = null;
      _prevActive = editor.ActiveObject;
      if (_cur.Count == 0)
      {
        _curActive = null;
      }
      else if (!_cur.Contains(_prevActive))
      {
        _curActive = picked[0];
      }
      else
      {
        _curActive = _prevActive;
      }

      //avoid adding history if nothing changed.
      if (!CheckIfSomethingChanged(editor))
      {
        return;
      }

      Redo(editor);
    }
    private List<WorldObject> GetPickedObjects()
    {
      //pick a beam region, or ray
      var p2 = Gu.Context.PCMouse.Pos;
      if (Math.Abs(p2.x - _selectStart.x) > 3 && Math.Abs(p2.y - _selectStart.y) > 3)
      {
        _wasClick = false;
        //beam
        var objs = GetPickedObjectsRegion(_selectStart, p2);
        return objs;
      }
      else
      {
        _wasClick = true;
        //ray
        List<WorldObject> objs = new List<WorldObject>();
        var ob = Gu.Context.Renderer.Picker.PickedObjectFrame as WorldObject;
        if (ob != null)
        {
          objs.Add(ob);
        }
        return objs;
      }
    }
  }
  public class CloneObjectAction : WorldAction
  {
    public CloneObjectAction() { }
    public override bool IsHistoryAction { get; }
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m) { return WorldActionState.Done; }
    public override void Undo(WorldEditor editor) { }
    public override void Redo(WorldEditor editor) { }
    public override WorldActionState HandleEvent(WorldEditor editor, WorldEditEvent coide) { return WorldActionState.StillDoing; }
  }
  public enum UserGesture
  {
    MouseMove, MouseWheel
  }
  [DataContract]
  public class KeyStroke
  {
    //keyboard / mouse 
    [DataMember] public Keys? Key = null;
    [DataMember] public MouseButton? MouseButton = null;
    [DataMember] public UserGesture? Gesture = null;
    [DataMember] public KeyMod Mod = KeyMod.None;
    [DataMember] public ButtonState? State;
    public KeyStroke(KeyMod mod, Keys key, ButtonState state = ButtonState.Press)
    {
      Key = key;
      Mod = mod;
      State = state;
    }
    public KeyStroke(KeyMod mod, MouseButton b, ButtonState state = ButtonState.Press)
    {
      MouseButton = b;
      Mod = mod;
      State = state;
    }
    public KeyStroke(KeyMod mod, UserGesture act)
    {
      Gesture = act;
      Mod = mod;
    }
    public bool Trigger(PCKeyboard k, PCMouse m)
    {
      Gu.Assert(Key != null || MouseButton != null, "no key/mouse was specifie.d");

      bool triggered = k.ModIsDown(Mod);
      if (Key != null)
      {
        triggered = triggered && (State.Value == ButtonState.Any || k.HasState(Key.Value, State.Value));
      }
      if (MouseButton != null)
      {
        triggered = triggered && (State.Value == ButtonState.Any || m.HasState(MouseButton.Value, State.Value));
      }
      if (Gesture != null)
      {
        if (Gesture == UserGesture.MouseMove)
        {
          if (m.PosDelta.x == 0 && m.PosDelta.y == 0)
          {
            triggered = false;
          }
        }
        else if (Gesture == UserGesture.MouseWheel)
        {
          if (m.ScrollDelta.x == 0 && m.ScrollDelta.y == 0)
          {
            triggered = false;
          }
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
      }
      return triggered;
    }
    public class EqualityComparer : IEqualityComparer<KeyStroke>
    {
      public bool Equals(KeyStroke? a, KeyStroke? b)
      {
        if (a == null && b == null)
        {
          return true;
        }
        if (a == null || b == null)
        {
          return false;
        }
        return
        a.Key == b.Key &&
        a.Mod == b.Mod &&
        a.State == b.State;
      }
      public int GetHashCode(KeyStroke a)
      {
        return a.GetHashCode();
      }
    }
    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();

      if (Mod != KeyMod.Any && Mod != KeyMod.None)
      {
        sb.Append(Mod.ToString());
        sb.Append("+");
      }
      if (Key != null)
      {
        sb.Append(Key.Value.ToString());
      }
      else if (MouseButton != null)
      {
        if (MouseButton.Value == OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left)
        {
          sb.Append("MouseLeft");
        }
        else if (MouseButton.Value == OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right)
        {
          sb.Append("MouseRight");
        }
        else if (MouseButton.Value == OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle)
        {
          sb.Append("MouseMiddle");
        }
        else
        {
          sb.Append(MouseButton.Value.ToString());
        }

      }
      // sb.Append($"({State.ToString()})");

      return sb.ToString();
    }
  }
  [DataContract]
  public class ActionCondition
  {
    public string Name { get { return _name; } }
    public Func<WorldEditor, object?, bool> Func { get { return _func; } }

    [DataMember] private string _name = Library.UnsetName;
    [DataMember] private Func<WorldEditor, object?, bool> _func;

    public Type? ContextAction = null;

    public ActionCondition(string name, Func<WorldEditor, object?, bool> cond)
    {
      Gu.Assert(cond != null);
      _name = name;
      _func = cond;
    }
    public ActionCondition(Type context_action)
    {
      ContextAction = context_action;

      Gu.Assert(context_action == typeof(WorldAction) ||
                context_action.BaseType == typeof(WorldAction) ||
                context_action.BaseType == typeof(WorldObjectAction));

      _name = context_action.Name;
      _func = (e, o) =>
      {
        if (e.Current != null)
        {
          return e.Current.GetType() == context_action;
        }
        return false;
      };
    }
  }
  [DataContract]
  public class KeyCombo
  {
    #region Public: Members

    public static KeyStroke NullStroke = new KeyStroke(KeyMod.None, Keys.Unknown);

    public KeyStroke First { get { return _first; } set { _first = value; } }
    public KeyStroke Second { get { return _second; } set { _second = value; } }
    public ActionCondition Condition { get { return _condition; } set { _condition = value; } }
    public WorldEditEvent Event { get { return _event; } set { _event = value; } }

    #endregion
    #region Private: Members

    [DataMember] private KeyStroke _first;
    [DataMember] private KeyStroke _second = NullStroke;
    [DataMember] private ActionCondition _condition;
    [DataMember] private WorldEditEvent _event;

    #endregion
    #region Public: Methods

    public KeyCombo(ActionCondition condition, WorldEditEvent action, Keys key)
      : this(condition, action, KeyMod.None, key)
    {
    }
    public KeyCombo(ActionCondition condition, WorldEditEvent action, Keys key1, Keys key2)
      : this(condition, action, KeyMod.None, key1, KeyMod.None, key2)
    {
    }
    public KeyCombo(ActionCondition condition, WorldEditEvent action, KeyMod mod, Keys key)
      : this(condition, action, new KeyStroke(mod, key))
    {
    }
    public KeyCombo(ActionCondition condition, WorldEditEvent action, KeyMod mod, Keys key, ButtonState keystate)
      : this(condition, action, new KeyStroke(mod, key, keystate))
    {
    }
    public KeyCombo(ActionCondition condition, WorldEditEvent action, KeyMod mod1, Keys key1, KeyMod mod2, Keys key2)
      : this(condition, action, new KeyStroke(mod1, key1), new KeyStroke(mod2, key2))
    {
    }
    public KeyCombo(ActionCondition condition, WorldEditEvent action, MouseButton key)
      : this(condition, action, KeyMod.None, key)
    {
    }
    public KeyCombo(ActionCondition condition, WorldEditEvent action, KeyMod mod, MouseButton key)
      : this(condition, action, new KeyStroke(mod, key), NullStroke)
    {
    }
    public KeyCombo(ActionCondition condition, WorldEditEvent action, MouseButton key, ButtonState state)
      : this(condition, action, KeyMod.None, key, state)
    {
    }
    public KeyCombo(ActionCondition condition, WorldEditEvent action, KeyMod mod, MouseButton key, ButtonState state)
      : this(condition, action, new KeyStroke(mod, key, state), NullStroke)
    {
    }
    public KeyCombo(ActionCondition condition, WorldEditEvent action, KeyMod mod1, Keys key1, KeyMod mod2, MouseButton key2)
      : this(condition, action, new KeyStroke(mod1, key1), new KeyStroke(mod2, key2))
    {
    }
    public KeyCombo(ActionCondition condition, WorldEditEvent action, KeyStroke stroke)
      : this(condition, action, stroke, NullStroke)
    {
    }
    public KeyCombo(ActionCondition condition, WorldEditEvent action, KeyMod mod, UserGesture gesture)
      : this(condition, action, new KeyStroke(mod, gesture), NullStroke)
    {

    }
    public KeyCombo(ActionCondition condition, WorldEditEvent action, KeyStroke first, KeyStroke second)
    {
      Gu.Assert(first != null);
      Gu.Assert(second != null);
      Gu.Assert(condition != null);
      Event = action;
      First = first;
      Second = second;
      Condition = condition;
    }
    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();//
      if (Condition != null)
      {
        sb.Append($"[{Condition.Name}] ");
      }
      sb.Append($"{this.Event.ToString()} = ");
      if (First != null && First != NullStroke)
      {
        sb.Append(First.ToString());
      }
      else
      {
        sb.Append("Error - no first keystroke");
      }
      if (Second != null && Second != NullStroke)
      {
        sb.Append($", {Second.ToString()}");
      }
      return sb.ToString();
    }

    #endregion
  }
  [DataContract]
  public enum WorldEditEvent
  {
    Quit, Cancel, Confirm,

    Edit_Undo, Edit_Redo, Edit_Create, Edit_Delete, Edit_CloneSelected,
    Edit_SelectRegion, Edit_SelectRegionEnd, Edit_SelectAll, Edit_DeselectAll,
    Edit_MoveObjects, Edit_ScaleObjects, Edit_RotateObjects,
    Edit_ToggleView1, Edit_ToggleView2, Edit_ToggleView3, Edit_ToggleView4,
    Edit_ToggleGameMode,

    MRS_TransformAxisX, MRS_TransformAxisY, MRS_TransformAxisZ,
    MRS_TransformPlaneX, MRS_TransformPlaneY, MRS_TransformPlaneZ, MRS_TransformToggleOrigin,

    Debug_MoveCameraToOrigin,
    Debug_UI_Toggle_ShowOverlay, Debug_UI_Toggle_DisableClip,
    Debug_ShowInfoWindow, Debug_ToggleShowConsole,
    Debug_ToggleDebugInfo, Debug_ToggleVSync, Debug_DrawBoundBoxes, Debug_DrawNormalsTangents, Debug_SaveFBOs,
    Debug_DrawObjectBasis, Debug_ShowWireFrame_Legacy, Debug_Toggle_Wireframe_Overlay,
    Debug_Toggle_RenderMode,
    Debug_UI_Toggle_DisableBorders, Debug_UI_Toggle_DisableMargins, Debug_UI_Toggle_DisablePadding,

    Object_Hide_Selected, Object_Unhide_All, Object_Isolate_Selected,

    Window_ToggleFullscreen,

    Camera_Toggle_Perspective,


    TestScript,

    Ui_MouseLeft, Ui_MouseRight, Ui_MouseMove
  }
  [DataContract]
  public class KeyMap
  {
    #region Private: Members

    private Dictionary<KeyStroke, List<KeyCombo>>? _first = null;
    private Dictionary<ActionCondition, Dictionary<KeyStroke, Dictionary<KeyStroke, List<KeyCombo>>>> _dict;
    private List<KeyCombo> _combos;

    #endregion
    #region Public: Methods

    public KeyMap()
    {
      var Global = new ActionCondition("Global", (e, o) => (e.Current == null) && true);
      var IsEditMode = new ActionCondition("IsEditMode", (e, o) => (e.Current == null) && Gu.World.GameMode == GameMode.Edit);
      var IsGUI = new ActionCondition("IsGUI", (e, o) => (e.Current == null) && o is UiElement);
      var IsWorld = new ActionCondition("IsWorld", (e, o) => (e.Current == null) && !(o is UiElement));
      var MoveRotScale = new ActionCondition(typeof(MoveRotateScaleAction));
      var SelectRegion = new ActionCondition(typeof(SelectRegionAction));

      //TODO: read from file.
      _combos = new List<KeyCombo>(){

        //Debug
        new KeyCombo(Global, WorldEditEvent.Edit_ToggleGameMode, KeyMod.None, Keys.F1, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Debug_ToggleVSync, KeyMod.None, Keys.F2, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Debug_Toggle_RenderMode, KeyMod.None, Keys.F3, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Debug_Toggle_RenderMode, KeyMod.None, Keys.Z, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Debug_Toggle_Wireframe_Overlay, KeyMod.Shift, Keys.Z, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Debug_DrawBoundBoxes, KeyMod.None, Keys.F4, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Debug_DrawNormalsTangents, KeyMod.None, Keys.F5, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Debug_SaveFBOs, KeyMod.None, Keys.F6, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Debug_ToggleDebugInfo, KeyMod.None, Keys.F7, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Debug_UI_Toggle_ShowOverlay, KeyMod.None, Keys.F9, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Window_ToggleFullscreen, KeyMod.None, Keys.F11, ButtonState.Press),

        new KeyCombo(Global, WorldEditEvent.Debug_DrawObjectBasis, KeyMod.Shift, Keys.F4, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Debug_UI_Toggle_DisableClip, KeyMod.Shift, Keys.F10, ButtonState.Press),

        new KeyCombo(Global, WorldEditEvent.Debug_UI_Toggle_DisableBorders, KeyMod.Ctrl, Keys.F9, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Debug_UI_Toggle_DisableMargins, KeyMod.Ctrl, Keys.F10, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Debug_UI_Toggle_DisablePadding, KeyMod.Ctrl, Keys.F11, ButtonState.Press),
        new KeyCombo(Global, WorldEditEvent.Camera_Toggle_Perspective, KeyMod.None, Keys.D5, ButtonState.Press),

        new KeyCombo(IsEditMode, WorldEditEvent.Edit_ToggleView1, KeyMod.None, Keys.D1, ButtonState.Press),
        new KeyCombo(IsEditMode, WorldEditEvent.Edit_ToggleView2, KeyMod.None, Keys.D2, ButtonState.Press),
        new KeyCombo(IsEditMode, WorldEditEvent.Edit_ToggleView3, KeyMod.None, Keys.D3, ButtonState.Press),
        new KeyCombo(IsEditMode, WorldEditEvent.Edit_ToggleView4, KeyMod.None, Keys.D4, ButtonState.Press),

        new KeyCombo(IsEditMode, WorldEditEvent.Object_Hide_Selected, KeyMod.None, Keys.H, ButtonState.Press),
        new KeyCombo(IsEditMode, WorldEditEvent.Object_Isolate_Selected, KeyMod.Shift, Keys.H, ButtonState.Press),
        new KeyCombo(IsEditMode, WorldEditEvent.Object_Isolate_Selected, KeyMod.None, Keys.Slash, ButtonState.Press),
        new KeyCombo(IsEditMode, WorldEditEvent.Object_Unhide_All, KeyMod.Alt, Keys.H, ButtonState.Press),

        //World 
        new KeyCombo(IsWorld, WorldEditEvent.Quit, KeyMod.Ctrl, Keys.Q),
        new KeyCombo(IsWorld, WorldEditEvent.Edit_SelectRegion, KeyMod.Any, MouseButton.Left, ButtonState.Press),
        new KeyCombo(SelectRegion, WorldEditEvent.Edit_SelectRegionEnd, KeyMod.Any, MouseButton.Left, ButtonState.Release),
        new KeyCombo(SelectRegion, WorldEditEvent.Cancel, Keys.Escape),
        new KeyCombo(IsWorld, WorldEditEvent.Edit_SelectAll, KeyMod.Ctrl, Keys.A),
        new KeyCombo(IsWorld, WorldEditEvent.Edit_DeselectAll, KeyMod.Alt, Keys.A),
        new KeyCombo(IsWorld, WorldEditEvent.Edit_MoveObjects, Keys.G),
        new KeyCombo(IsWorld, WorldEditEvent.Edit_ScaleObjects, KeyMod.CtrlShift, Keys.S),
        new KeyCombo(IsWorld, WorldEditEvent.Edit_RotateObjects, Keys.R),
        new KeyCombo(IsWorld, WorldEditEvent.Edit_Delete, Keys.Delete),
        new KeyCombo(IsWorld, WorldEditEvent.Edit_Delete, Keys.X),
        new KeyCombo(IsWorld, WorldEditEvent.Edit_Undo, KeyMod.Ctrl, Keys.Z),
        new KeyCombo(IsWorld, WorldEditEvent.Edit_Redo, KeyMod.CtrlShift, Keys.Z),
        new KeyCombo(IsWorld, WorldEditEvent.Edit_Redo, KeyMod.Ctrl, Keys.Y),
        new KeyCombo(IsWorld, WorldEditEvent.Edit_CloneSelected, KeyMod.Shift, Keys.D),
        new KeyCombo(IsWorld, WorldEditEvent.Debug_MoveCameraToOrigin, Keys.O),
        new KeyCombo(IsWorld, WorldEditEvent.TestScript, KeyMod.CtrlShift, Keys.K),


        //GUI
        new KeyCombo(IsGUI, WorldEditEvent.Ui_MouseLeft, KeyMod.Any,  MouseButton.Left, ButtonState.Any),
        new KeyCombo(IsGUI, WorldEditEvent.Ui_MouseRight, KeyMod.Any, MouseButton.Right, ButtonState.Any),
        new KeyCombo(IsGUI, WorldEditEvent.Ui_MouseMove, KeyMod.Any, UserGesture.MouseMove),

        //MRS
        new KeyCombo(MoveRotScale, WorldEditEvent.MRS_TransformAxisX, Keys.X),
        new KeyCombo(MoveRotScale, WorldEditEvent.MRS_TransformAxisY, Keys.Y),
        new KeyCombo(MoveRotScale, WorldEditEvent.MRS_TransformAxisZ, Keys.Z),
        new KeyCombo(MoveRotScale, WorldEditEvent.MRS_TransformPlaneX, KeyMod.Shift, Keys.X),
        new KeyCombo(MoveRotScale, WorldEditEvent.MRS_TransformPlaneY, KeyMod.Shift, Keys.Y),
        new KeyCombo(MoveRotScale, WorldEditEvent.MRS_TransformPlaneZ, KeyMod.Shift, Keys.Z),
        new KeyCombo(MoveRotScale, WorldEditEvent.Cancel, Keys.Escape),
        new KeyCombo(MoveRotScale, WorldEditEvent.Cancel, MouseButton.Right),
        new KeyCombo(MoveRotScale, WorldEditEvent.Cancel, Keys.Enter),
        new KeyCombo(MoveRotScale, WorldEditEvent.Confirm, MouseButton.Left),
        new KeyCombo(MoveRotScale, WorldEditEvent.MRS_TransformToggleOrigin, Keys.O),
      };

      BuildLookupTable();
    }
    public void Update(WorldEditor e, PCKeyboard k, PCMouse m)
    {
      var picked_ob = Gu.Context.Renderer.Picker.PickedObjectFrame;

      if (_first == null)
      {
        foreach (var context in _dict)
        {
          if (context.Key.Func(e, picked_ob))
          {
            foreach (var first in context.Value)
            {
              if (first.Key.Trigger(k, m))
              {
                _first = null;

                if (first.Value.TryGetValue(KeyCombo.NullStroke, out var combos))
                {
                  //null second, so do these actions
                  foreach (var c in combos)
                  {
                    if (c.First.Key == Keys.Z)
                    {
                      Gu.Trap();
                    }

                    e.DoEvent(c.Event);
                  }
                }
                else
                {
                  _first = first.Value;
                }
                break;
              }
            }
          }
        }
      }
      else
      {
        foreach (var second in _first.Keys)
        {
          if (second.Trigger(k, m))
          {
            var combos = _first[second];
            foreach (var c in combos)
            {
              e.DoEvent(c.Event);
            }
          }
        }
        List<Keys> keys = new List<Keys>();
        if (k.AnyNonModKeyWasPressed(keys))
        {
          //whatever user presses now
          _first = null;
        }
      }
    }
    public void ToString(StringBuilder sb, string tab = "")
    {
      //Q&D dump of control information
      Gu.Assert(sb != null);
      Gu.Assert(_combos != null);
      foreach (var c in _combos)
      {
        sb.AppendLine($"{tab}{c.ToString()}");
      }
    }

    #endregion
    #region Private: Methods

    private void BuildLookupTable()
    {
      _dict = new Dictionary<ActionCondition, Dictionary<KeyStroke, Dictionary<KeyStroke, List<KeyCombo>>>>();
      foreach (var k in _combos)
      {
        Gu.Assert(k.First != null);
        Dictionary<KeyStroke, Dictionary<KeyStroke, List<KeyCombo>>>? first = null;
        if (!_dict.TryGetValue(k.Condition, out first))
        {
          first = new Dictionary<KeyStroke, Dictionary<KeyStroke, List<KeyCombo>>>(new KeyStroke.EqualityComparer());

          _dict.Add(k.Condition, first);
        }

        CheckDuplicateStroke(k, k.First, first.Keys.ToList());

        Dictionary<KeyStroke, List<KeyCombo>>? second = null;
        if (!first.TryGetValue(k.First, out second))
        {
          second = new Dictionary<KeyStroke, List<KeyCombo>>(new KeyStroke.EqualityComparer());
          first.Add(k.First, second);
        }
        CheckDuplicateStroke(k, k.Second, second.Keys.ToList());

        List<KeyCombo>? combolist = null;//null should be ok? maybe we'll see
        if (!second.TryGetValue(k.Second, out combolist))
        {
          combolist = new List<KeyCombo>();
          second.Add(k.Second, combolist);
        }
        combolist.Add(k);
      }
    }
    private void CheckDuplicateStroke(KeyCombo nkc, KeyStroke nks, List<KeyStroke> existing)
    {
      Gu.Assert(nkc != null);
      Gu.Assert(nks != null);
      Gu.Assert(existing != null);
      foreach (var eks in existing)
      {
        //Two equivalent keystrokes can't have 'any' modifier. Must differ by type
        if (nks.Key == eks.Key && nks.MouseButton == eks.MouseButton && nks.State == eks.State)
        {
          if (
            (eks.Mod == KeyMod.Any) ||
            (nks.Mod == KeyMod.Any) ||
            (eks.Mod == nks.Mod)
            )
          {
            Gu.Log.Error($"Duplicate keystrokes found, with equivalent, or 'any' modifiers, action='{nkc.Event.ToString()}'");
            Gu.DebugBreak();
          }
        }

      }
    }

    #endregion

  }//keymap
  [DataContract]
  public class WorldEditor
  {
    // @class WorldEditor
    // @description The world editor relies on a keymap that uses sends Events.
    //  Events are sent from the keymap to the editor. 
    //  They are then consumed, or routed to the active WorldAction.
    //  Class updates its state with the routed events until the action is complete.

    #region Public: Members

    public XFormOrigin XFormOrigin { get { return _xFormOrigin; } set { _xFormOrigin = value; } }
    public int EditView { get { return _editView; } set { _editView = value; } }
    public List<WorldActionGroup> History { get { return _history; } set { _history = value; } }
    public bool Edited { get { return _worldEdited; } set { _worldEdited = value; } }
    public List<WorldObject> SelectedObjects { get { return _selectedObjects; } set { _selectedObjects = value; } }
    public WorldObject? ActiveObject
    {
      get { return _activeObject; }
      set
      {
        if (value == _activeObject)
        {
          return;
        }
        if (value != null)
        {
          value.IsActive = true;
        }
        if (_activeObject != null)
        {
          _activeObject.IsActive = false;
        }
        _activeObject = value;
      }
    }
    public InputState InputState { get { return _inputState; } set { _inputState = value; } }
    public vec3? SelectionOrigin { get { return _selectionOrigin; } set { _selectionOrigin = value; } }
    public WorldAction? Current { get { return _current; } }
    public KeyMap KeyMap { get { return _keyMap; } }

    #endregion
    #region Private: Members

    private List<WorldObject> _selectedObjects = new List<WorldObject>();
    private WorldObject? _activeObject = null;
    private List<WorldActionGroup> _history = new List<WorldActionGroup>();
    private int _historyIndex = -1;
    private bool _worldEdited = false;
    private InputState _inputState = InputState.SelectObject;
    private vec3? _selectionOrigin = null;
    private WorldAction? _current = null;
    private KeyMap _keyMap = new KeyMap();
    [DataMember] private int _editView = 1;//how many viewports are showing
    [DataMember] private XFormOrigin _xFormOrigin = XFormOrigin.Average;

    #endregion
    #region Public: Methods

    public WorldEditor()
    {
    }
    public void Update(RenderView? rv)
    {
      var m = Gu.Context.PCMouse;
      var k = Gu.Context.PCKeyboard;

      //if window lost focus, then keyup / buttonup everything that is down..
      _keyMap.Update(this, k, m);

      if (_current != null)
      {
        DoCurrentAction(rv, k, m);
      }
    }
    public void CollectVisibleObjects(RenderView rv)
    {
      Gu.Assert(rv != null);

      //Collect editor specific stuff, then collect current action stuff.
      if (rv.Overlay.ShowSelectionOrigin && SelectionOrigin != null)
      {
        rv.Overlay.Point(SelectionOrigin.Value, new vec4(.91201f, .89910f, .9479f, 1), 10);
      }

      if (_current != null)
      {
        _current.CollectVisibleObjects(rv);
      }
    }
    public void DoAction(WorldActionGroup act)
    {
      DoAction((WorldAction)act);
    }
    public void DoAction(WorldAction act)
    {
      Gu.Assert(act != null);
      _current = act;

      //cut history based on index
      FixHistoryIndex();
      if (_historyIndex + 1 < _history.Count)
      {
        _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
      }
    }
    public void DoEvent(WorldEditEvent code, Type? context_action = null)
    {
      var k = Gu.Context.PCKeyboard;
      var m = Gu.Context.PCMouse;
      WorldAction? act = null;
      if (_current != null)
      {
        var state = _current.HandleEvent(this, code);
        HandleCurrentActionState(state);
      }
      else
      {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        if (code == WorldEditEvent.Quit)
        {
          act = new QuitApplicationAction();
        }
        else if (code == WorldEditEvent.Edit_SelectRegion)
        {
          DoAction(new SelectRegionAction());
        }
        else if (code == WorldEditEvent.Edit_SelectAll)
        {
          SelectAll(false);
        }
        else if (code == WorldEditEvent.Edit_DeselectAll)
        {
          SelectAll(true);
        }
        else if (code == WorldEditEvent.Edit_Delete)
        {
          DoAction(new ObjectDeleteAction(SelectedObjects));
        }
        else if (code == WorldEditEvent.Edit_Undo)
        {
          UndoAction();
        }
        else if (code == WorldEditEvent.Edit_Redo)
        {
          RedoAction();
        }
        else if (code == WorldEditEvent.Edit_MoveObjects)
        {
          if (SelectedObjects.Count > 0)
          {
            DoAction(new MoveRotateScaleAction(SelectedObjects, MoveRotateScaleAction.MoveRotateScale.Move));
          }
        }
        else if (code == WorldEditEvent.Edit_RotateObjects)
        {
          if (SelectedObjects.Count > 0)
          {
            DoAction(new MoveRotateScaleAction(SelectedObjects, MoveRotateScaleAction.MoveRotateScale.Rotate));
          }
        }
        else if (code == WorldEditEvent.Edit_ScaleObjects)
        {
          if (SelectedObjects.Count > 0)
          {
            DoAction(new MoveRotateScaleAction(SelectedObjects, MoveRotateScaleAction.MoveRotateScale.Scale));
          }
        }
        else if (code == WorldEditEvent.Debug_Toggle_Wireframe_Overlay)
        {
          if (Gu.TryGetSelectedView(out var rv))
          {
            rv.Overlay.DrawWireframeOverlay = !rv.Overlay.DrawWireframeOverlay;
          }
        }
        else if (code == WorldEditEvent.Debug_Toggle_RenderMode)
        {
          if (Gu.TryGetSelectedView(out var rv))
          {
            if (rv.Overlay.ObjectRenderMode == ObjectRenderMode.Wire)
            {
              rv.Overlay.ObjectRenderMode = ObjectRenderMode.Flat;
            }
            else if (rv.Overlay.ObjectRenderMode == ObjectRenderMode.Flat)
            {
              rv.Overlay.ObjectRenderMode = ObjectRenderMode.Material;
            }
            else if (rv.Overlay.ObjectRenderMode == ObjectRenderMode.Material)
            {
              rv.Overlay.ObjectRenderMode = ObjectRenderMode.Wire;
            }
            else
            {
              Gu.BRThrowNotImplementedException();
            }
          }

        }
        else if (code == WorldEditEvent.Edit_CloneSelected)
        {
          if (SelectedObjects.Count > 0)
          {
            DoAction(

              new MoveRotateScaleAction(SelectedObjects, MoveRotateScaleAction.MoveRotateScale.Scale)
              );
          }
        }
        else if (code == WorldEditEvent.Debug_MoveCameraToOrigin)
        {
          if (Gu.TryGetSelectedViewCamera(out var cm))
          {
            cm.RootParent.Position_Local = new vec3(0, 0, 0);
            cm.RootParent.Velocity = vec3.Zero;
          }
        }
        else if (code == WorldEditEvent.TestScript)
        {
          TestScript();
        }
        else if (code == WorldEditEvent.Ui_MouseLeft)
        {
          // if(Gu.TryGetSelectedViewGui(out var g){
          //   g.DoMouseEvents();
          // }
        }
        else if (code == WorldEditEvent.Debug_UI_Toggle_ShowOverlay)
        {
          if (Gu.TryGetSelectedViewGui(out var g))
          {
            g.DebugDraw.ShowOverlay = !g.DebugDraw.ShowOverlay;
          }
        }
        else if (code == WorldEditEvent.Debug_UI_Toggle_DisableMargins)
        {
          if (Gu.TryGetSelectedViewGui(out var g))
          {
            g.DebugDraw.DisableMargins = !g.DebugDraw.DisableMargins;
          }
        }
        else if (code == WorldEditEvent.Debug_UI_Toggle_DisablePadding)
        {
          if (Gu.TryGetSelectedViewGui(out var g))
          {
            g.DebugDraw.DisablePadding = !g.DebugDraw.DisablePadding;
          }
        }
        else if (code == WorldEditEvent.Debug_UI_Toggle_DisableBorders)
        {
          if (Gu.TryGetSelectedViewGui(out var g))
          {
            g.DebugDraw.DisableBorders = !g.DebugDraw.DisableBorders;
          }
        }
        else if (code == WorldEditEvent.Debug_UI_Toggle_DisableClip)
        {
          if (Gu.TryGetSelectedViewGui(out var g))
          {
            g.DebugDraw.DisableClip = !g.DebugDraw.DisableClip;
          }
        }

        else if (code == WorldEditEvent.Debug_ShowInfoWindow)
        {
          string c_infoWindowName = "ObjectInfo";
          if (Gu.TryGetWindowByName(c_infoWindowName, out var win))
          {
            win.IsVisible = !win.IsVisible;
          }
          else
          {
            var iw = new InfoWindow(c_infoWindowName, Gu.Translator.Translate(Phrase.Information), new ivec2(200, 200), new ivec2(500, 400));
            iw.IsVisible = true;
          }
        }
        else if (code == WorldEditEvent.Debug_ToggleShowConsole)
        {
          OperatingSystem.ToggleShowConsole();
        }
        else if (code == WorldEditEvent.Debug_ToggleDebugInfo)
        {
          if (Gu.TryGetSelectedView(out var rv))
          {
            if (rv.Gui != null)
            {
              if (rv.ControlsInfo.Visible)
              {
                rv.ControlsInfo.Visible = false;
                rv.WorldDebugInfo.Visible = true;
              }
              else if (rv.WorldDebugInfo.Visible)
              {
                rv.WorldDebugInfo.Visible = false;
                rv.GpuDebugInfo.Visible = true;
              }
              else if (rv.GpuDebugInfo.Visible)
              {
                rv.GpuDebugInfo.Visible = false;
              }
              else
              {
                rv.ControlsInfo.Visible = true;
              }
            }
          }
        }
        else if (code == WorldEditEvent.Debug_ToggleVSync)
        {
          if (Gu.TryGetMainwWindow(out var mw))
          {
            if (mw.VSync == OpenTK.Windowing.Common.VSyncMode.Off)
            {
              mw.VSync = OpenTK.Windowing.Common.VSyncMode.On;
            }
            else
            {
              mw.VSync = OpenTK.Windowing.Common.VSyncMode.Off;
            }
          }
        }
        else if (code == WorldEditEvent.Debug_DrawBoundBoxes)
        {
          if (Gu.TryGetSelectedView(out var rv))
          {
            rv.Overlay.DrawBoundBoxes = !rv.Overlay.DrawBoundBoxes;
          }
        }
        else if (code == WorldEditEvent.Debug_DrawObjectBasis)
        {
          if (Gu.TryGetSelectedView(out var rv))
          {
            rv.Overlay.DrawObjectBasis = !rv.Overlay.DrawObjectBasis;
          }
        }
        else if (code == WorldEditEvent.Debug_DrawNormalsTangents)
        {
          if (Gu.TryGetSelectedView(out var rv))
          {
            rv.Overlay.DrawVertexAndFaceNormalsAndTangents = !rv.Overlay.DrawVertexAndFaceNormalsAndTangents;
          }
        }
        else if (code == WorldEditEvent.Debug_SaveFBOs)
        {
          Gu.SaveFBOs();
        }
        else if (code == WorldEditEvent.Window_ToggleFullscreen)
        {
          if (Gu.TryGetFocusedWindow(out var w))
          {
            if (w.WindowState == OpenTK.Windowing.Common.WindowState.Fullscreen)
            {
              w.WindowState = OpenTK.Windowing.Common.WindowState.Normal;
            }
            else
            {
              w.WindowState = OpenTK.Windowing.Common.WindowState.Fullscreen;
            }
          }
        }
        else if (code == WorldEditEvent.Edit_ToggleView1)
        {
          if (Gu.TryGetMainwWindow(out var w))
          {
            Gu.World.Editor.EditView = 1;
            w.SetGameMode(Gu.World.GameMode);
          }
        }
        else if (code == WorldEditEvent.Edit_ToggleView2)
        {
          if (Gu.TryGetMainwWindow(out var w))
          {
            Gu.World.Editor.EditView = 2;
            w.SetGameMode(Gu.World.GameMode);
          }
        }
        else if (code == WorldEditEvent.Edit_ToggleView3)
        {
          if (Gu.TryGetMainwWindow(out var w))
          {
            Gu.World.Editor.EditView = 3;
            w.SetGameMode(Gu.World.GameMode);
          }
        }
        else if (code == WorldEditEvent.Edit_ToggleView4)
        {
          if (Gu.TryGetMainwWindow(out var w))
          {
            Gu.World.Editor.EditView = 4;
            w.SetGameMode(Gu.World.GameMode);
          }
        }
        else if (code == WorldEditEvent.Edit_ToggleGameMode)
        {
          if (Gu.TryGetMainwWindow(out var w))
          {
            w.ToggleGameMode();
          }
        }
        else if (code == WorldEditEvent.Object_Hide_Selected)
        {
          foreach (var ob in this.SelectedObjects)
          {
            ob.Visible = false;
          }
        }
        else if (code == WorldEditEvent.Object_Isolate_Selected)
        {
          if (this.SelectedObjects.Count > 0)
          {
            Gu.World.IterateRootObjectsSafe((ob) =>
            {
              if (!this.SelectedObjects.Contains(ob))
              {
                ob.Visible = false;
              }
              return LambdaBool.Continue;
            });
          }
        }
        else if (code == WorldEditEvent.Object_Unhide_All)
        {
          Gu.World.IterateRootObjectsSafe((ob) =>
          {
            ob.Visible = true;
            return LambdaBool.Continue;
          });
        }
        else if (code == WorldEditEvent.Camera_Toggle_Perspective)
        {
          if (Gu.TryGetSelectedView(out var rv))
          {
            if (rv.Camera.ProjectionMode == ProjectionMode.Orthographic)
            {
              rv.Camera.ProjectionMode = ProjectionMode.Perspective;
            }
            else if (rv.Camera.ProjectionMode == ProjectionMode.Perspective)
            {
              rv.Camera.ProjectionMode = ProjectionMode.Orthographic;
            }
            else
            {
              Gu.BRThrowNotImplementedException();
            }
          }
        }





        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        else
        {
          Gu.BRThrowNotImplementedException();
        }

        if (act != null)
        {
          DoAction(act);
        }
      }
    }
    public static List<WorldObject> RemoveInvalidObjectsFromSelection(List<WorldObject> s)
    {
      Camera3D? cam = null;
      Gu.TryGetSelectedViewCamera(out cam);
      s = s.Where((x) => { return (x.Selectable == true) && (x != cam); }).ToList();
      return s;
    }
    public void UndoAction()
    {
      if (_current != null)
      {
        Gu.Log.Warn($"Could not undo an action. Action '{_current.GetType().ToString()}' is in progress.");
        return;
      }
      FixHistoryIndex();

      if (_historyIndex >= 0)
      {
        _history[_historyIndex].Undo(this);
        _historyIndex -= 1;//-1 is ok for index
      }
    }
    public void RedoAction()
    {
      if (_current != null)
      {
        Gu.Log.Warn($"Could not redo an action. Action '{_current.GetType().ToString()}' is in progress.");
        return;
      }
      FixHistoryIndex();

      if (_historyIndex + 1 < _history.Count)
      {
        _historyIndex += 1;
        _history[_historyIndex].Redo(this);
      }
    }
    public void UpdateSelectionOrigin()
    {
      if (SelectedObjects.Count == 0)
      {
        _selectionOrigin = null;
      }
      else
      {
        _selectionOrigin = new vec3(0, 0, 0);
        foreach (var ob in SelectedObjects)
        {
          _selectionOrigin += ob.Position_World;
        }
        _selectionOrigin = _selectionOrigin / (float)SelectedObjects.Count;
      }
    }

    #endregion
    #region Private: Methods

    private void TestScript()
    {
      // CSharpScript s = new CSharpScript(new FileLoc("startup.cs", FileStorage.Embedded));
      // s.Compile();
      // s.Run();
    }
    private void SelectAll(bool deselect)
    {
      var all_obs = RemoveInvalidObjectsFromSelection(Gu.World.GetAllVisibleRootObjects());

      List<WorldObject> toSelect = new List<WorldObject>();
      foreach (var ob in all_obs)
      {
        if (ob.IsSelected == deselect)
        {
          toSelect.Add(ob);
        }
      }
      if (deselect)
      {
        DoAction(new ObjectSelectAction(toSelect, ObjectSelectAction.SelectActionType.Deselect));
      }
      else
      {
        if (toSelect.Count > 0)
        {
          DoAction(new ObjectSelectAction(toSelect, ObjectSelectAction.SelectActionType.Select));
        }
        else
        {
          //all objs are already selected, now deselect them
          DoAction(new ObjectSelectAction(all_obs, ObjectSelectAction.SelectActionType.Deselect));
        }
      }
    }
    private void FixHistoryIndex()
    {
      if (_historyIndex < -1)
      {
        _historyIndex = -1;
      }
      else if (_historyIndex >= _history.Count)
      {
        _historyIndex = _history.Count - 1;
      }
    }
    private void DoCurrentAction(RenderView? renderView, PCKeyboard k, PCMouse m)
    {
      Gu.Assert(_current != null);

      var state = _current.Do(this, renderView, k, m);

      HandleCurrentActionState(state);
    }
    private void HandleCurrentActionState(WorldActionState state)
    {
      if (state == WorldActionState.StillDoing)
      {
        //keep doing
      }
      else if (state == WorldActionState.Done)
      {
        //Commit 
        Gu.Assert(_current != null);
        if (_current.IsHistoryAction)
        {
          _history.Add(new WorldActionGroup(new List<WorldAction>() { _current }));

          if (_history.Count > Gu.EngineConfig.MaxUndoHistoryItems)
          {
            _history.RemoveRange(Gu.EngineConfig.MaxUndoHistoryItems, _history.Count);
          }
          _historyIndex = _history.Count - 1;
        }
        _current = null;
      }
      else if (state == WorldActionState.Cancel)
      {
        //discard action, don't add to history
        Gu.Assert(_current != null);
        _current.Undo(this);
        _current = null;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    private bool GetPickedObject(out WorldObject? ob)
    {
      ob = null;
      var ob_picked = Gu.Context.Renderer.Picker.PickedObjectFrame;
      if (ob_picked != null && ob_picked is WorldObject)
      {
        ob = ob_picked as WorldObject;
        return true;
      }
      return false;
    }

    #endregion

  }//wordleditor





}