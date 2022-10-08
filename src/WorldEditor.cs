using System;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using OpenTK.Windowing.GraphicsLibraryFramework;

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
  }
  public class GlobalAction : WorldAction
  {
    public GlobalAction() { }
    public override bool IsHistoryAction { get { return false; } }
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m) { return WorldActionState.Done; }
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
              ob.Selected = true;
              _somethingChanged = true;
            }
          }
          else if (type == SelectActionType.Deselect)
          {
            if (editor.SelectedObjects.Contains(ob))
            {
              editor.SelectedObjects.Remove(ob);
              ob.Selected = false;
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
      _doing.IterateSafe<WorldAction>((x) =>
      {
        if (x.Do(editor, renderview, k, m) == WorldActionState.Done)
        {
          _doing.Remove(x);
        }
        return LambdaBool.Continue;
      });
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
  public class MoveRotateScaleAction : WorldObjectAction
  {
    private class ObjectXFormSpace
    {
      public vec3 Axis;
      public vec3 Origin;
      public ObjectXFormSpace(vec3 axis, vec3 origin)
      {
        Axis = axis;
        Origin = origin;
      }
    }
    public enum XFormOrigin
    {
      Average, Individual,
    }
    public enum XFormSpace
    {
      Global, Local
    }
    public enum MoveRotateScale
    {
      Move, Rotate, Scale
    }
    private MoveRotateScale _type = MoveRotateScale.Move;
    private List<PRS> _lastPRS = null;
    private List<PRS> _newPRS = null;
    private List<ObjectXFormSpace> _obj_xform = null;
    private vec3 _savedSelectionOrigin = new vec3(0, 0, 0);
    private bool _xform_Plane = false;//move along plane, or axis
    private vec3 _xform_global_axis = new vec3(0, 1, 0);
    private WorldEditEvent _current_XForm = WorldEditEvent.TransformPlaneView;
    private XFormSpace _xform_Space = XFormSpace.Global;//global/local transform blender: z->z /x->x etc
    private XFormOrigin _xform_Origin = XFormOrigin.Average;
    private vec2 _xform_MouseStart;
    private vec2 _mouse_wrapcount;
    private Line3f? _xform_startRay = null;

    public MoveRotateScaleAction(List<WorldObject> objs, MoveRotateScale type) : base(objs)
    {
      _type = type;
      Gu.Assert(Gu.World.Editor.SelectionOrigin != null);
      _savedSelectionOrigin = Gu.World.Editor.SelectionOrigin.Value;

      _lastPRS = new List<PRS>();
      for (int i = 0; i < _objects.Count; ++i)
      {
        var w = _objects[i];
        _lastPRS.Add(w.GetPRS_Local());
      }

      SetXFormView();

      _xform_MouseStart = Gu.Context.PCMouse.Pos;
      _mouse_wrapcount = new vec2(0, 0);
      _xform_startRay = Gu.CastRayFromScreen(_xform_MouseStart);

      ComputeXFormSpace(_xform_Space, _xform_Origin);
    }
    private void SetXFormView()
    {
      if (Gu.TryGetSelectedView(out var rv))
      {
        if (rv.Camera.TryGetTarget(out var c))
        {
          _xform_global_axis = -c.BasisZ; //"view" translation
          _xform_Plane = true;
          _current_XForm = WorldEditEvent.TransformPlaneView;
        }
      }
    }
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m)
    {
      return Update(editor, renderview, k, m);
    }
    public override void Undo(WorldEditor editor)
    {
      Gu.Assert(_lastPRS != null && _lastPRS.Count == _objects.Count);
      for (int i = 0; i < _objects.Count; ++i)
      {
        var w = _objects[i];
        w.SetPRS_Local(_lastPRS[i]);
      }
      editor.UpdateSelectionOrigin();
    }
    public override void Redo(WorldEditor editor)
    {
      Gu.Assert(_newPRS != null && _newPRS.Count == _objects.Count);
      for (int i = 0; i < _objects.Count; ++i)
      {
        var w = _objects[i];
        w.SetPRS_Local(_newPRS[i]);
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
    private ObjectXFormSpace GetObjectXForm(int index)
    {
      return _obj_xform[index];
    }
    private void ToggleGlobalLocalXFormAxis()
    {
      if (_xform_Space == XFormSpace.Global)
      {
        _xform_Space = XFormSpace.Local;
      }
      else
      {
        _xform_Space = XFormSpace.Global;
      }
      ComputeXFormSpace(_xform_Space, _xform_Origin);
    }
    private vec3 ComputeLocalObjectAxis(int obi, vec3 global_axis)
    {
      vec3 locla = (_lastPRS[obi].toMat4().invert() * global_axis.toVec4(1)).toVec3().normalize();
      return locla;
    }
    private void ComputeXFormSpace(XFormSpace space, XFormOrigin origin)
    {
      vec3 average_origin = _savedSelectionOrigin;
      vec3 average_axis = new vec3(0, 0, 0);
      for (int obi = 0; obi < _objects.Count; obi++)
      {
        average_axis += ComputeLocalObjectAxis(obi, _xform_global_axis);
      }
      average_axis = (average_axis / (float)_objects.Count).normalize();

      _obj_xform = new List<ObjectXFormSpace>();
      for (int obi = 0; obi < _objects.Count; obi++)
      {
        _obj_xform.Add(ComputeObjectXFormSpace(obi, space, origin, _xform_global_axis, average_origin, average_axis));
      }
    }
    private ObjectXFormSpace ComputeObjectXFormSpace(int obi, XFormSpace space, XFormOrigin origin, vec3 global_axis, vec3 average_origin, vec3 average_axis)
    {
      ObjectXFormSpace ret = null;

      var ob_axis = vec3.Zero;
      var ob_origin = vec3.Zero;

      if (origin == XFormOrigin.Average)
      {
        ob_origin = average_origin;
      }
      else if (origin == XFormOrigin.Individual)
      {
        ob_origin = _lastPRS[obi].Position.Value;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      if (space == XFormSpace.Local)
      {
        ob_axis = ComputeLocalObjectAxis(obi, global_axis);
      }
      else if (space == XFormSpace.Global)
      {
        ob_axis = global_axis;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      ret = new ObjectXFormSpace(ob_axis, ob_origin);
      return ret;
    }
    public override WorldActionState HandleEvent(WorldEditor editor, WorldEditEvent code)
    {
      if (code == WorldEditEvent.TransformPlaneView)
      {
        SetXFormView();
      }
      else if (code == WorldEditEvent.TransformAxisX || code == WorldEditEvent.TransformAxisY || code == WorldEditEvent.TransformAxisZ ||
                code == WorldEditEvent.TransformPlaneX || code == WorldEditEvent.TransformPlaneY || code == WorldEditEvent.TransformPlaneZ)
      {
        if (code == WorldEditEvent.TransformAxisX) { _xform_global_axis = new vec3(1, 0, 0); _xform_Plane = false; }
        if (code == WorldEditEvent.TransformAxisY) { _xform_global_axis = new vec3(0, 1, 0); _xform_Plane = false; }
        if (code == WorldEditEvent.TransformAxisZ) { _xform_global_axis = new vec3(0, 0, 1); _xform_Plane = false; }
        if (code == WorldEditEvent.TransformPlaneX) { _xform_global_axis = new vec3(0, 1, 0); _xform_Plane = true; }
        if (code == WorldEditEvent.TransformPlaneY) { _xform_global_axis = new vec3(0, 0, 1); _xform_Plane = true; }
        if (code == WorldEditEvent.TransformPlaneZ) { _xform_global_axis = new vec3(1, 0, 0); _xform_Plane = true; }

        if (_current_XForm == code)//x->x
        {
          ToggleGlobalLocalXFormAxis();
        }
        _current_XForm = code;
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
      //update - wrap mouse - wrap and count wraps, then project into world.
      var vwrap = Gu.Mouse.WarpMouse(renderview, WarpMode.Wrap, 0.001f);
      if (vwrap != null)
      {
        _mouse_wrapcount += vwrap.Value;
      }
      var mouse_wrapped = Gu.Mouse.GetWrappedPosition(renderview, _mouse_wrapcount);
      return Gu.CastRayFromScreen(mouse_wrapped);
    }
    private WorldActionState UpdateMove(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m)
    {
      var screen_ray = GetWrappedScreenRay(renderview);
      if (screen_ray != null)
      {
        float amt_R_S = 0;
        if (Gu.TryGetSelectedView(out var selv))
        {
          var dmos = (Gu.Mouse.Pos - _xform_MouseStart);
          var dl = dmos.length();
          if (dl != 0)
          {
            amt_R_S = ((dmos * _mouse_wrapcount).length() / dl) * 3;
          }
          else
          {
            amt_R_S = 0;
          }
          Gu.AssertDebug(Gu.SaneFloat(amt_R_S));
        }

        Gu.Assert(_lastPRS != null);
        Gu.Assert(_objects.Count == _lastPRS.Count);
        _newPRS = new List<PRS>();
        for (var obi = 0; obi < _lastPRS.Count; obi++)
        {
          vec3 axis = GetObjectXForm(obi).Axis;
          vec3 origin = GetObjectXForm(obi).Origin;

          //TODO: this should  not happen every frame, create a mesh
          Gu.Context.DebugDraw.DrawAxisLine(origin, axis);

          PRS p = new PRS();
          if (this._type == MoveRotateScale.Move)
          {
            p.Position = Translate(axis, origin, screen_ray);
          }
          if (this._type == MoveRotateScale.Rotate)
          {
            p.Rotation = _lastPRS[obi].Rotation * quat.fromAxisAngle(axis, amt_R_S, false);
            Gu.AssertDebug(p.Rotation.Value.IsSane());
          }
          if (this._type == MoveRotateScale.Scale)
          {
            p.Scale = _lastPRS[obi].Scale + amt_R_S * axis;
          }
          _newPRS.Add(p);
        }

        Redo(editor);
      }

      return WorldActionState.StillDoing;

    }//update
    private vec3 Translate(vec3 axis, vec3 origin, Line3f? screen_ray)
    {
      vec3 output_pos = new vec3(0, 0, 0);
      if (_xform_Plane)
      {
        //different delta if we're on a plane.
        Plane3f sp = new Plane3f(axis, origin);
        float tcur = sp.IntersectLine(screen_ray.Value.p0, screen_ray.Value.p1);
        vec3 mouse_hit_plane_c = screen_ray.Value.p0 + (screen_ray.Value.p1 - screen_ray.Value.p0) * tcur;
        output_pos = origin + mouse_hit_plane_c - origin;
      }
      else
      {
        /*
          ray-ray
          pa = p0 + tv1
          pb = p1 + tv2
          |(p0 + tv0)-(p1 + tv1)| = d(t)
          => |p + tv| = d(t)
          => (t^2(v.v) + 2t(p.v) + p.p)^(1/2) = d(t)
          => (t^2(v.v) + 2t(p.v) + p.p)^(1/2) = d/dt = 
          
          (1/2) (t^2(v.v) + 2t(p.v) + p.p)^(-1/2) (2t(v.v) + 2(p.v))

        */
        var p0 = screen_ray.Value.p0;
        var v0 = (screen_ray.Value.p1 - screen_ray.Value.p0).normalize();//prevent  huge numbers
        var p1 = origin;
        var v1 = axis;
        var tv = (v1 - v0);
        var tp = (p1 - p0);
        var t = tp.dot(tv) / tv.dot(tv);

        vec3 c0 = (p0 - p1 + t * (v0 - v1));
        vec3 c1 = (p0 - p1 + t * (v0 - v1));
        /*

                  p0-p1=p, v0-v1=v
                  => t^2(v.v) + 2t(p.v) + p.p = d(t)^2
                  A = v.v, B = 2p.v, C = p.p
                  => At^2 + Bt + C = d(t)^2
                  => dd/dt = 2At + B = 2d(t)
                  => dd/dt = At + B/2 = d(t)=0
                  => t = -B/2A
                  => t = -(p.v)/(v.v)
        */
        if (Gu.Context.PCKeyboard.Press(Keys.B))
        {
          Gu.Trap();
        }
        //This returns an exact position. kind of neat, but not axis
        //delta_pos = (p0 + v1*t) - origin;
        output_pos = origin + axis * (float)t;
        //Gu.Context.DebugDraw.Point(origin, new vec4(1, 0, 0, 1));//TODO: this should  not happen every frame, create a mesh and keep it
        //Gu.Context.DebugDraw.Point(output_pos, new vec4(0, 1, 1, 1));//TODO: this should  not happen every frame, create a mesh and keep it
        Gu.Context.DebugDraw.Point(c0, new vec4(1, 0, 0, 1));//TODO: this should  not happen every frame, create a mesh and keep it
        Gu.Context.DebugDraw.Point(c1, new vec4(1, 0, 1, 1));//TODO: this should  not happen every frame, create a mesh and keep it

        //delta_pos = Line3f.pointOnRay_t(axis, xform_curRay.Value.p0) - origin;
      }
      return output_pos;

    }//calcorigin

  }//cls
  public class QuitApplicationAction : WorldAction
  {
    public QuitApplicationAction() { }
    public override bool IsHistoryAction { get { return false; } }
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m)
    {
      Gu.Exit();
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
  public class SelectRangeAction : WorldAction//selectregion
  {
    public vec2 _selectStart;
    private bool _somethingChanged = false;
    private List<WorldObject> _prev = new List<WorldObject>();
    private List<WorldObject> _cur = new List<WorldObject>();
    //   private List<WorldObject> _deselect = new List<WorldObject>();

    public SelectRangeAction()
    {
      _selectStart = Gu.Mouse.Pos;
    }
    public override bool IsHistoryAction { get { return true; } }
    public override WorldActionState Do(WorldEditor editor, RenderView? renderview, PCKeyboard k, PCMouse m)
    {
      CastBeam(_selectStart, Gu.Mouse.Pos, true);

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
        if (!_somethingChanged)
        {
          return false;
        }
      }
      return true;
    }
    private void DoOrRedo(WorldEditor editor, bool undo)
    {
      //avoid adding history if nothing changed.
      if (!CheckIfSomethingChanged(editor))
      {
        return;
      }

      // do select
      editor.SelectedObjects.IterateSafe((x) =>
      {
        x.Selected = false;
        return LambdaBool.Continue;
      });
      editor.SelectedObjects = new List<WorldObject>(undo ? _prev : _cur);
      editor.SelectedObjects.IterateSafe((x) =>
      {
        x.Selected = true;
        return LambdaBool.Continue;
      });
      editor.UpdateSelectionOrigin();
    }
    public override WorldActionState HandleEvent(WorldEditor editor, WorldEditEvent coide)
    {
      if (coide == WorldEditEvent.SelectRangeEnd)
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

    private OOBox3f? CastBeam(vec2 p1, vec2 p2, bool draw)
    {
      OOBox3f? ret = null;
      if (Gu.TryGetSelectedViewCamera(out var cam))
      {
        ret = cam.Frustum.BeamcastWorld(_selectStart, p2, 1.01f, 200);//push in so you can see the thing
        if (ret != null)
        {
          if (draw)
          {
            DrawSelection(ret.Value);
          }
        }
      }
      return ret;
    }
    private void DrawSelection(OOBox3f beam)
    {
      beam.GetTrianglesAndPlanes(out var tris, out var planes);
      vec4 c = new vec4(0.794, 0.814f, 0.893f, 0.245f); // color of selection area

      if (Gu.Context.DebugDraw.DrawBoundBoxes)
      {
        //box
        foreach (var tr in tris)
        {
          Gu.Context.DebugDraw.Triangle(tr._v[0], tr._v[1], tr._v[2], c);
        }
        //min/max, far
        Gu.Context.DebugDraw.Point(beam.Center, new vec4(0, 0.8f, 0.3928f, 1));
      }
      else
      {
        //selection area
        Gu.Context.DebugDraw.Triangle(tris[8]._v[0], tris[8]._v[1], tris[8]._v[2], c);
        Gu.Context.DebugDraw.Triangle(tris[9]._v[0], tris[9]._v[1], tris[9]._v[2], c);
      }
    }
    private List<WorldObject> GetPickedObjectsRegion(vec2 p1, vec2 p2)
    {
      List<WorldObject> objs = new List<WorldObject>();
      var bea = CastBeam(p1, p2, false);
      if (bea != null)
      {
        var beam = bea.Value;
        beam.GetTrianglesAndPlanes(out var tris, out var planes);
        var obs = WorldEditor.RemoveInvalidObjectsFromSelection(Gu.World.GetAllRootObjects());
        foreach (var ob in obs)
        {
          if (ConvexHull.HasBox(ob.BoundBox, planes))
          {
            objs.Add(ob);
          }
        }
      }
      return objs;
    }
    private void FinalizeSelection(WorldEditor editor)
    {
      _cur = new List<WorldObject>();
      _prev = new List<WorldObject>(editor.SelectedObjects);

      var picked = GetPickedObjects();
      if (Gu.Keyboard.ModIsDown(KeyMod.Shift))
      {
        _cur = new List<WorldObject>(editor.SelectedObjects);
        _cur.AddRange(picked.Where(x => x.Selected == false));
      }
      else
      {
        _cur = picked;
      }

      Redo(editor);
    }
    private List<WorldObject> GetPickedObjects()
    {
      //pick a beam region, or ray
      var p2 = Gu.Mouse.Pos;
      if (Math.Abs(p2.x - _selectStart.x) > 3 && Math.Abs(p2.y - _selectStart.y) > 3)
      {
        //beam
        var objs = GetPickedObjectsRegion(_selectStart, p2);
        return objs;
      }
      else
      {
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
  /*
  MoveRotateScaleAction(new ObjectSelectAction(new CloneObjectAction()), Move)
cloneobjectaction().Then(new ObjectSelectAction()).Then()
  */

  [DataContract]
  public class KeyStroke
  {
    [DataMember] public Keys? Key = null;
    [DataMember] public MouseButton? MouseButton = null;
    [DataMember] public KeyMod Mod = KeyMod.None;
    [DataMember] public ButtonState State;
    public KeyStroke(KeyMod mod, Keys key, ButtonState state = ButtonState.Press)
    {
      Key = key; Mod = mod; State = state;
    }
    public KeyStroke(KeyMod mod, MouseButton b, ButtonState state = ButtonState.Press)
    {
      MouseButton = b; Mod = mod; State = state;
    }
    public bool Trigger(PCKeyboard k, PCMouse m)
    {
      bool mod = k.ModIsDown(Mod);

      if (Key != null)
      {
        if (mod && k.State(Key.Value, State))
        {
          return true;
        }
      }
      else if (MouseButton != null)
      {
        if (mod && m.State(MouseButton.Value, State))
        {
          return true;
        }
      }
      else
      {
        Gu.BRThrowException("no key/mouse was specifie.d");
      }

      return false;
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
  }
  [DataContract]
  public class KeyCombo
  {
    public static KeyStroke NullStroke = new KeyStroke(KeyMod.None, Keys.LastKey);
    [DataMember] public KeyStroke First;
    [DataMember] public KeyStroke Second = NullStroke;
    [DataMember] public Type Context;

    public WorldEditEvent ActionType;

    public KeyCombo(Type context, WorldEditEvent action, Keys key)
      : this(context, action, KeyMod.None, key)
    {
    }
    public KeyCombo(Type context, WorldEditEvent action, Keys key1, Keys key2)
      : this(context, action, KeyMod.None, key1, KeyMod.None, key2)
    {
    }
    public KeyCombo(Type context, WorldEditEvent action, KeyMod mod, Keys key)
      : this(context, action, new KeyStroke(mod, key))
    {
    }
    public KeyCombo(Type context, WorldEditEvent action, KeyMod mod1, Keys key1, KeyMod mod2, Keys key2)
      : this(context, action, new KeyStroke(mod1, key1), new KeyStroke(mod2, key2))
    {
    }
    public KeyCombo(Type context, WorldEditEvent action, MouseButton key)
      : this(context, action, KeyMod.None, key)
    {
    }
    public KeyCombo(Type context, WorldEditEvent action, KeyMod mod, MouseButton key)
      : this(context, action, new KeyStroke(mod, key), NullStroke)
    {
    }
    public KeyCombo(Type context, WorldEditEvent action, MouseButton key, ButtonState state)
      : this(context, action, KeyMod.None, key, state)
    {
    }
    public KeyCombo(Type context, WorldEditEvent action, KeyMod mod, MouseButton key, ButtonState state)
      : this(context, action, new KeyStroke(mod, key, state), NullStroke)
    {
    }
    public KeyCombo(Type context, WorldEditEvent action, KeyMod mod1, Keys key1, KeyMod mod2, MouseButton key2)
      : this(context, action, new KeyStroke(mod1, key1), new KeyStroke(mod2, key2))
    {
    }
    public KeyCombo(Type context, WorldEditEvent action, KeyStroke stroke)
      : this(context, action, stroke, NullStroke)
    {
    }
    public KeyCombo(Type context, WorldEditEvent action, KeyStroke first, KeyStroke second)
    {
      Gu.Assert(first != null);
      Gu.Assert(second != null);
      Gu.Assert(context != null);
      Gu.Assert(context.BaseType == typeof(WorldAction) || context.BaseType == typeof(WorldObjectAction));
      ActionType = action;
      First = first;
      Second = second;
      Context = context;
    }

  }
  [DataContract]
  public enum WorldEditEvent
  {
    Quit, Cancel, Confirm, Undo, Redo, Create, Delete, CloneSelected,
    SelectRange, SelectRangeEnd, SelectAll, DeselectAll,
    TranslateObjects, ScaleObjects, RotateObjects,
    TransformAxisX, TransformAxisY, TransformAxisZ,
    TransformPlaneX, TransformPlaneY, TransformPlaneZ, TransformPlaneView,
    Overlay_ShowWireFrame
  }
  [DataContract]
  public class KeyMap
  {
    private Dictionary<KeyStroke, List<KeyCombo>>? _first = null;
    private Dictionary<Type, Dictionary<KeyStroke, Dictionary<KeyStroke, List<KeyCombo>>>> _dict;
    public KeyMap()
    {
      var Q = new KeyStroke(KeyMod.None, Keys.Q);
      var Escape = new KeyStroke(KeyMod.None, Keys.Escape);
      var CS_Escape = new KeyStroke(KeyMod.CtrlShift, Keys.Escape);

      List<KeyCombo> combos = new List<KeyCombo>(){
        //type->event->keys
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.Quit, KeyMod.Ctrl, Keys.Q),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.SelectRange, MouseButton.Left,  ButtonState.Press),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.SelectRange, KeyMod.Any, MouseButton.Left,  ButtonState.Press),
        new KeyCombo(typeof(SelectRangeAction), WorldEditEvent.SelectRangeEnd, KeyMod.Any, MouseButton.Left, ButtonState.Release),
        new KeyCombo(typeof(SelectRangeAction), WorldEditEvent.Cancel, Keys.Escape),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.SelectAll,KeyMod.Ctrl, Keys.A),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.DeselectAll,KeyMod.Alt, Keys.A),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.TranslateObjects, Keys.G),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.ScaleObjects, KeyMod.Shift, Keys.S),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.RotateObjects, Keys.R),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.Delete, Keys.Delete),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.Delete, Keys.X),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.Undo, KeyMod.Ctrl, Keys.Z),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.Redo, KeyMod.CtrlShift, Keys.Z),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.Redo, KeyMod.Ctrl, Keys.Y),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.Overlay_ShowWireFrame, Keys.Z),
        new KeyCombo(typeof(GlobalAction), WorldEditEvent.CloneSelected, KeyMod.Shift, Keys.D),

        new KeyCombo(typeof(MoveRotateScaleAction), WorldEditEvent.TransformAxisX, Keys.X),
        new KeyCombo(typeof(MoveRotateScaleAction), WorldEditEvent.TransformAxisY, Keys.Y),
        new KeyCombo(typeof(MoveRotateScaleAction), WorldEditEvent.TransformAxisZ, Keys.Z),
        new KeyCombo(typeof(MoveRotateScaleAction), WorldEditEvent.TransformPlaneView, Keys.V),
        new KeyCombo(typeof(MoveRotateScaleAction), WorldEditEvent.TransformPlaneX, KeyMod.Shift, Keys.X),
        new KeyCombo(typeof(MoveRotateScaleAction), WorldEditEvent.TransformPlaneY, KeyMod.Shift, Keys.Y),
        new KeyCombo(typeof(MoveRotateScaleAction), WorldEditEvent.TransformPlaneZ, KeyMod.Shift, Keys.Z),
        new KeyCombo(typeof(MoveRotateScaleAction), WorldEditEvent.Cancel, Keys.Escape),
        new KeyCombo(typeof(MoveRotateScaleAction), WorldEditEvent.Cancel, MouseButton.Right),
        new KeyCombo(typeof(MoveRotateScaleAction), WorldEditEvent.Cancel, Keys.Enter),
        new KeyCombo(typeof(MoveRotateScaleAction), WorldEditEvent.Confirm, MouseButton.Left),
      };
      _dict = new Dictionary<Type, Dictionary<KeyStroke, Dictionary<KeyStroke, List<KeyCombo>>>>();

      foreach (var k in combos)
      {
        Gu.Assert(k.First != null);
        Dictionary<KeyStroke, Dictionary<KeyStroke, List<KeyCombo>>>? first = null;
        if (!_dict.TryGetValue(k.Context, out first))
        {
          first = new Dictionary<KeyStroke, Dictionary<KeyStroke, List<KeyCombo>>>(new KeyStroke.EqualityComparer());
          _dict.Add(k.Context, first);
        }

        Dictionary<KeyStroke, List<KeyCombo>>? second = null;
        if (!first.TryGetValue(k.First, out second))
        {
          second = new Dictionary<KeyStroke, List<KeyCombo>>(new KeyStroke.EqualityComparer());
          first.Add(k.First, second);
        }

        List<KeyCombo>? combolist = null;//null should be ok? maybe we'll see
        if (!second.TryGetValue(k.Second, out combolist))
        {
          combolist = new List<KeyCombo>();
          second.Add(k.Second, combolist);
        }
        combolist.Add(k);
      }
    }
    public void Update(WorldEditor e, PCKeyboard k, PCMouse m)
    {
      if (_first == null)
      {
        foreach (var context in _dict)
        {
          if ((e.Current == null && context.Key == typeof(GlobalAction)) || (e.Current != null && context.Key == e.Current.GetType()))
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

                    e.DoEvent(c.ActionType);
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
              e.DoEvent(c.ActionType);
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
  }//keymap
  [DataContract]
  public class WorldEditor
  {
    [NonSerialized] private List<WorldObject> _selectedObjects = new List<WorldObject>();
    [NonSerialized] private List<WorldActionGroup> _history = new List<WorldActionGroup>();
    [NonSerialized] private int _historyIndex = -1;
    [NonSerialized] private bool _worldEdited = false;
    [NonSerialized] public InputState _inputState = InputState.SelectObject;
    [NonSerialized] private vec3? _selectionOrigin = null;
    [NonSerialized] private WorldAction? _current = null;
    [NonSerialized] private KeyMap KeyMap = new KeyMap();
    [DataMember] private int _editView = 1;//how many viewports are showing


    public int EditView { get { return _editView; } set { _editView = value; } }
    public List<WorldActionGroup> History { get { return _history; } set { _history = value; } }
    public bool Edited { get { return _worldEdited; } set { _worldEdited = value; } }
    public List<WorldObject> SelectedObjects { get { return _selectedObjects; } set { _selectedObjects = value; } }
    public InputState InputState { get { return _inputState; } set { _inputState = value; } }
    public vec3? SelectionOrigin { get { return _selectionOrigin; } set { _selectionOrigin = value; } }
    public WorldAction? Current { get { return _current; } }

    public WorldEditor()
    {
    }
    public void Update(RenderView? renderview)
    {
      var m = Gu.Context.PCMouse;
      var k = Gu.Context.PCKeyboard;

      //if window lost focus, then keyup / buttonup everything that is down..
      KeyMap.Update(this, k, m);

      if (_current != null)
      {
        DoCurrentAction(renderview, k, m);
      }

      //Draw select origin
      if (SelectionOrigin != null)
      {
        Gu.Context.DebugDraw.Points.Add(new v_v3c4() { _v = SelectionOrigin.Value, _c = new vec4(.91201f, .89910f, .9479f, 1) });
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
    public void DoEvent(WorldEditEvent code)
    {
      var k = Gu.Keyboard;
      var m = Gu.Mouse;
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
        else if (code == WorldEditEvent.SelectRange)
        {
          DoAction(new SelectRangeAction());
        }
        else if (code == WorldEditEvent.SelectAll)
        {
          SelectAll(false);
        }
        else if (code == WorldEditEvent.DeselectAll)
        {
          SelectAll(true);
        }
        else if (code == WorldEditEvent.Delete)
        {
          DoAction(
            new WorldActionGroup(new List<WorldAction>()
            {
              new ObjectSelectAction(SelectedObjects, ObjectSelectAction.SelectActionType.Deselect),
              new ObjectDeleteAction(SelectedObjects),
            }));
        }
        else if (code == WorldEditEvent.Undo)
        {
          UndoAction();
        }
        else if (code == WorldEditEvent.Redo)
        {
          RedoAction();
        }
        else if (code == WorldEditEvent.TranslateObjects)
        {
          if (SelectedObjects.Count > 0)
          {
            DoAction(new MoveRotateScaleAction(SelectedObjects, MoveRotateScaleAction.MoveRotateScale.Move));
          }
        }
        else if (code == WorldEditEvent.RotateObjects)
        {
          if (SelectedObjects.Count > 0)
          {
            DoAction(new MoveRotateScaleAction(SelectedObjects, MoveRotateScaleAction.MoveRotateScale.Rotate));
          }
        }
        else if (code == WorldEditEvent.ScaleObjects)
        {
          if (SelectedObjects.Count > 0)
          {
            DoAction(new MoveRotateScaleAction(SelectedObjects, MoveRotateScaleAction.MoveRotateScale.Scale));
          }
        }
        else if (code == WorldEditEvent.Overlay_ShowWireFrame)
        {
          if (Gu.TryGetSelectedViewOverlay(out var v))
          {
            v.ToggleWireFrame();
          }
        }
        else if (code == WorldEditEvent.CloneSelected)
        {
          if (SelectedObjects.Count > 0)
          {
            DoAction(

              new MoveRotateScaleAction(SelectedObjects, MoveRotateScaleAction.MoveRotateScale.Scale)
              );
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
    private void SelectAll(bool deselect)
    {
      var all_obs = RemoveInvalidObjectsFromSelection(Gu.World.GetAllRootObjects());

      List<WorldObject> toSelect = new List<WorldObject>();
      foreach (var ob in all_obs)
      {
        if (ob.Selected == deselect)
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
  }





}