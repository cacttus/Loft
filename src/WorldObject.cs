using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace PirateCraft
{
  public enum WorldObjectCloneMode
  {
    Instance, // reference to the data
    Copy      // copy everything
  }

  public enum WorldObjectState
  {
    Created,
    Active,
    Removed, //pending removal from the scene, or removed
  }

  public enum NodeType
  {
    Object,
    Camera,
    Light,
    Joint,
    Armature,
  }

  [DataContract]
  public class PRS
  {
    //Optional p/r/s
    private quat? _rotation = null;
    private vec3? _scale = null;
    private vec3? _position = null;
    public vec3? Position { get { return _position; } set { _position = value; } }
    public quat? Rotation { get { return _rotation; } set { _rotation = value; } }//xyz,angle
    public vec3? Scale { get { return _scale; } set { _scale = value; } }
    public mat4 toMat4()
    {
      mat4 mScl = _scale == null ? mat4.Identity : mat4.getScale(_scale.Value);
      mat4 mRot = _rotation == null ? mat4.Identity : mat4.getRotation(_rotation.Value);
      mat4 mPos = _position == null ? mat4.Identity : mat4.getTranslation(_position.Value);
      mat4 m = (mScl) * (mRot) * (mPos);
      return m;
    }
    public PRS Clone()
    {
      return new PRS() { _rotation = this._rotation, _scale = this._scale, _position = this._position };
    }
  }
  [DataContract]
  public class Drawable : DataBlock
  {
    //@class Drawable
    //@desc  Lightweight version of WorldObject (matrix+mesh+material)
    //       for rendering transformed geometry without the extra data.
    #region Public: Members

    public mat4 WorldMatrix { get { return _world; } }
    public MeshData? Mesh { get { return _meshView == null ? null : _meshView.MeshData; } set { SetMeshView(value); } }
    public MeshView? MeshView { get { return _meshView; } set { _meshView = value; } }
    public Material? Material { get { return _material; } set { _material = value; } }
    public bool Visible { get { return _visible; } set { _visible = value; } }
    public bool Selectable { get { return _selectable; } set { _selectable = value; } }
    public bool IsSelected { get { return _selected; } set { _selected = value; } }//May not be necessary adds extra data, we can use flags or just use WorldEditor.SelectedObjects
    public bool IsPicked { get { return _picked; } set { _picked = value; } }//object is under the mouse cursor
    public bool IsActive { get { return _active; } set { _active = value; } }//object is active selected object.
    public uint PickId { get { return _pickId; } set { Gu.BRThrowException("PickId cannot be set on WorldObject."); } }
    public Box3f BoundBox { get { return _boundBox; } } //Entire AABB, force fields, light area, with all meshes and children inside

    public bool Pickable
    {
      get
      {
        return _pickable;
      }
      set
      {
        if ((_pickable == false && value == true) || _pickId == 0)
        {
          GenPickID();
        }
        else if (value == false)
        {
          _pickId = 0;
        }
        _pickable = value;
      }
    }

    #endregion
    #region Protected: Members

    [DataMember] protected bool _visible = true;
    [DataMember] protected bool _selectable = true;//TODO: these can be flags
    [DataMember] protected bool _selected = false;
    [DataMember] protected bool _pickable = true;
    [DataMember] protected bool _picked = false;
    [DataMember] protected bool _active = false;
    [DataMember] protected mat4 _world = mat4.Identity;
    [DataMember] protected Material? _material = null;
    [DataMember] protected MeshView? _meshView = null;
    [DataMember] protected Box3f _boundBox = Box3f.One;//contains all visible sub-objects
    private uint _pickId = 0;//generated

    #endregion
    #region Public: Methods

    protected Drawable() { }//clone/copy
    public Drawable(string name) : base(name)
    {
      //For now, everything gets a pick color for debug reasons.
      GenPickID();
    }
    public Drawable(string name, MeshView? mesh, Material? mat, mat4 mworld) : base(name)
    {
      _meshView = mesh;
      _material = mat;
      _world = mworld;
    }
    public Drawable Clone()
    {
      return (Drawable)this.MemberwiseClone();
    }
    public virtual void Pick()
    {
      if (!Pickable)
      {
        return;
      }
      if (Gu.Context.Renderer.Picker.PickedObjectFrame != null)
      {
        //Picking is pixel perfect, so the first picked object is the exact object.
        //However objects may have children, and components which can also be picked, and may not be in the global list.
        //Obviously, a list of pickid->obj would be the fastest.
        return;
      }
      if (_pickId != Picker.c_iInvalidPickId)
      {
        var pixid = Gu.Context.Renderer.Picker.SelectedPixelId;
        if (pixid != 0)
        {
          if (pixid == this._pickId)
          {
            Gu.Context.Renderer.Picker.PickedObjectFrame = this;
          }
        }
      }
    }
    public void SetMeshView(MeshData? mesh, int? start = null, int? count = null)
    {
      if (mesh == null)
      {
        if (_meshView != null)
        {
          _meshView.MeshData = null;
        }
      }
      else if (_meshView == null)
      {
        _meshView = new MeshView(mesh, start, count);
      }
      else
      {
        _meshView.MeshData = mesh;
        _meshView.SetLimits(start, count);
      }
    }
    private void GenPickID()
    {
      if (_pickable && Gu.Context != null && Gu.Context.Renderer != null && Gu.Context.Renderer.Picker != null)
      {
        //NOTE: the pick IDs are from the context.. and they should be basd on eadch context.
        // This is INVALID
        _pickId = Gu.Context.Renderer.Picker.GenPickId();
      }
    }

    #endregion

  }//cls
  [DataContract]
  public class WorldObject : Drawable
  {
    // main object that stores matrix for pos/rot/scale, and components for mesh, sound, script .. GameObject ..
    #region Public: Members

    public WorldObjectState State { get { return _state; } set { _state = value; } }
    public bool TransformChanged { get { return _transformChanged; } private set { _transformChanged = value; } }

    public OOBox3f? BoundBoxMeshTransform { get { return _boundBoxMeshOO; } } //Transformed local mesh bound box
    public Box3f? BoundBoxMesh { get { return _boundBoxMeshAA; } } //Local Mesh only AABB.

    public vec3 Position_Local { get { return _position; } set { _position = value; SetTransformChanged(); } }
    public quat Rotation_Local { get { return _rotation; } set { _rotation = value; SetTransformChanged(); } }//xyz,angle
    public vec3 Scale_Local { get { return _scale; } set { _scale = value; SetTransformChanged(); } }
    public vec3 Position_World { get { return _positionWorld; } private set { _positionWorld = value; } }
    public mat4 LocalMatrix { get { return _local; } set { _local = value; DecomposeLocalMatrix(); } }

    public vec3 BasisX_World { get { return _basisX_World; } }
    public vec3 BasisY_World { get { return _basisY_World; } }
    public vec3 BasisZ_World { get { return _basisZ_World; } }
    public vec3 Heading { get { return _basisZ_World; } }

    //Mesh Object data
    public Action<WorldObject>? OnUpdate { get; set; } = null;//Script system should be for this
    public IObjectScript? Script { get { return _script; } set { _script = value; } }
    public List<Modifier> Modifiers { get { return _modifiers; } set { _modifiers = value; } }
    public List<Component> Components { get { return _components; } set { _components = value; } }

    public PhysicsData? PhysicsData { get { return _physicsData; } set { _physicsData = value; } }
    public AnimationData? AnimationData { get { return _animationData; } set { _animationData = value; } }

    public bool HasPhysics { get { return _physicsData != null; } }

    public static WorldObject Default
    {
      get
      {
        return new WorldObject();
      }
    }
    public WorldObject RootParent
    {
      //Return root object parent that is NOT the scene root. E.g., the root of the given object
      get
      {
        var thep = this;
        for (int ip = 0; Gu.WhileTrueGuard(ip, Gu.c_intMaxWhileTrueLoop); ip++)
        {
          if (thep.Parent != null)
          {
            if (thep.Parent != Gu.World.SceneRoot)
            {
              thep = thep.Parent;
            }
            else
            {
              break;
            }
          }
          else
          {
            break;
          }
        }
        return thep;
      }
    }
    public WorldObject? Parent
    {
      get
      {
        if (_parent != null)
        {
          return _parent;
        }
        return null;
      }
      private set
      {
        _parent = value;
        SetTransformChanged();
      }
    }
    public WeakReference<RenderView>? ExcludeFromRenderView { get { return _excludeFromRenderView; } set { _excludeFromRenderView = value; } }//DO NOT render in THIS context. Used for an FPS seeing other characters.

    private vec3? _lookAtConstraint = null;

    public void LookAtConstraint(vec3? p)
    {
      //set to null to disable.
      _lookAtConstraint = p;
    }

    #endregion
    #region Private: Members

    //Values
    [DataMember] private quat _rotation = new quat(0, 0, 0, 1); //Axis-Angle xyz,ang
    [DataMember] private vec3 _scale = new vec3(1, 1, 1);
    [DataMember] private vec3 _position = new vec3(0, 0, 0);
    [DataMember] private mat4 _local = mat4.Identity;
    [DataMember] private vec3 _basisX_World = new vec3(1, 0, 0);
    [DataMember] private vec3 _basisY_World = new vec3(0, 1, 0);
    [DataMember] private vec3 _basisZ_World = new vec3(0, 0, 1);
    [DataMember] private vec3 _positionWorld = vec3.Zero;
    [DataMember] private vec4 _color = new vec4(1, 1, 1, 1);
    [DataMember] private OOBox3f? _boundBoxMeshOO = null;//Transformed mesh
    [DataMember] protected Box3f? _boundBoxMeshAA = null;//Bound box of this object with all base meshes
    [DataMember] private int _treeDepth = 0; //used to check for DAG cycles

    //Refs
    [DataMember] private WorldObject? _parent = null;
    [DataMember] private List<WorldObject>? _children = null;
    [DataMember] private List<Component>? _components = null;
    [DataMember] private List<Modifier>? _modifiers = null;
    [DataMember] private IObjectScript? _script = null;
    [DataMember] private PhysicsData? _physicsData = null;
    [DataMember] protected AnimationData? _animationData = null;

    //Temps/generated
    private WorldObjectState _state = WorldObjectState.Created;
    private bool _transformChanged = false;
    private WeakReference<RenderView>? _excludeFromRenderView = null; //DO NOT render in THIS context. Used for an FPS seeing other characters.
    protected AnimationClip? _currentClip = null;//eventually this data will be on a more generic action sequencer
    protected KeyframeData? _currentKeys = null;

    //Junk
    private static bool skip_transform_validation_check = false;

    #endregion
    #region Public: Methods

    protected WorldObject() { }//Clone ctor
    public WorldObject(string name) : base(name)
    {
      //by default all world objects persist if attached to the scene, this makes all their data references persist as well
      //For optimization, nothing shoudl be here. WorldObject is new'd a lot each frame      
      _color = Random.NextVec4(new vec4(0.2f, 0.2f, 0.2f, 1), new vec4(1, 1, 1, 1));
    }
    public virtual void Update(double dt, ref Box3f parentBoundBox)
    {
      if (!Visible)
      {
        return;
      }

      OnUpdate?.Invoke(this);
      DispatchModifiers();
      UpdateComponents(dt);
      ApplyConstraints();
      CompileAndAnimate(dt);
      ApplyParentMatrix();

      _basisX_World = (_world * new vec4(1, 0, 0, 0)).xyz.normalized();
      _basisY_World = (_world * new vec4(0, 1, 0, 0)).xyz.normalized();
      _basisZ_World = (_world * new vec4(0, 0, 1, 0)).xyz.normalized();
      DecomposeWorldMatrix();

      _boundBox.genResetLimits();
      IterateChildrenSafe((child) =>
      {
        Gu.Assert(child != null);
        child.Update(dt, ref _boundBox);
        return LambdaBool.Continue;
      });
      CalcBoundBox(ref parentBoundBox);

      SanitizeTransform();
    }
    public void SanitizeTransform()
    {
      if (!skip_transform_validation_check)
      {
        if (!_position.IsSane())
        {
          Gu.DebugBreak();
          _position = vec3.Zero;
        }
        if (!_rotation.IsSane())
        {
          Gu.DebugBreak();
          _rotation = quat.Identity;
        }
        if (!_scale.IsSane())
        {
          Gu.DebugBreak();
          _scale = vec3.One;
        }

        if (!_local.IsSane())
        {
          Gu.DebugBreak();
          _local = mat4.Identity;
        }
        if (!_world.IsSane())
        {
          Gu.DebugBreak();
          _world = mat4.Identity;
        }

        if (!_boundBox._max.IsSane() || !_boundBox._min.IsSane())
        {
          Gu.DebugBreak();
          _boundBox._max = new vec3(1, 1, 1);
          _boundBox._min = new vec3(0, 0, 0);
        }
      }
    }
    public override void Pick()
    {
      if (!Pickable)
      {
        return;
      }
      base.Pick();

      IterateComponentsSafe((cmp) =>
      {
        if (Gu.Context.Renderer.Picker.PickedObjectFrame != null)
        {
          return LambdaBool.Break;
        }
        cmp.OnPick();
        if (Gu.Context.Renderer.Picker.PickedObjectFrame != null)
        {
          //The component (gui) picked something that it owns. Set the worldobject to this.
          Gu.Context.Renderer.Picker.PickedObjectFrame = this;
          return LambdaBool.Break;
        }
        return LambdaBool.Continue;
      });
      IterateChildrenSafe((child) =>
      {
        if (Gu.Context.Renderer.Picker.PickedObjectFrame == null)
        {
          child.Pick();
        }
        else
        {
          return LambdaBool.Break;
        }
        return LambdaBool.Continue;
      });

    }
    public WorldObject Clone()
    {
      /*
      update nov 2022
      all components get copied for now
      mesh+mat get referenced and must be set manually
      */
      WorldObject other = (WorldObject)this.MemberwiseClone();

      if (other._components != null)
      {
        this._components = new List<Component>();
        other.IterateComponentsSafe((c) =>
        {
          _components.Add((Component)c.Clone());
          return LambdaBool.Continue;
        });
      }

      IterateChildrenSafe((ch) =>
      {
        var wo = (WorldObject)ch.Clone();
        this.AddChild(wo);
        return LambdaBool.Continue;
      });

      return other;
    }
    public void Remove()
    {
      _state = WorldObjectState.Removed;//Picked up and destroyed by the world.
    }
    public void SetTransformChanged()
    {
      if (GetType() == typeof(Camera3D))
      {
        int n = 0;
        n++;
      }
      SanitizeTransform();

      TransformChanged = true;
    }
    public void AddComponent(Component c)
    {
      Gu.Assert(c != null);
      _components = _components.ConstructIfNeeded();
      _components.Add(c);
    }
    public bool RemoveComponent(Component c)
    {
      Gu.Assert(c != null);
      if (_components == null)
      {
        return false;
      }

      return _components.Remove(c);
    }
    public WorldObject AddChild(WorldObject child)
    {
      //yes technically we should check for cycles we dont do that yet
      Gu.Assert(child != Gu.World.SceneRoot);
      Gu.Assert(child != this);
      //cant add libreary deps to anything. Clone if need.
      Gu.Assert(child.Persistence != DataPersistence.LibraryDependency);

      if (child.Parent != null)
      {
        child.Parent.RemoveChild(child);
      }
      _children = _children.ConstructIfNeeded();
      _children.Add(child);
      child.Parent = this;

      child.UpdateTreeDepth();


      //child.PromoteResource(ResourcePromotion.SceneAdd);

      return this;
    }
    public void RemoveChild(WorldObject child)
    {
      if (_children == null)
      {
        return;
      }
      //Note this will keep the object in memory, to delete an object call World.DestroyObject
      if (!_children.Remove(child))
      {
        Gu.Log.Error("Child '" + child.Name + "' not found in '" + Name + "'");
        Gu.DebugBreak();
      }

      //child.PromoteResource(ResourcePromotion.SceneRemove);

      child._parent = null;
      child.UpdateTreeDepth();
      if (_children.Count == 0)
      {
        _children = null;
      }
    }
    public void CalcBoundBox(ref Box3f parent)
    {
      if (Mesh != null)
      {
        if (_boundBoxMeshAA == null)
        {
          _boundBoxMeshAA = new Box3f();
        }
        _boundBoxMeshAA.genResetLimits();

        _boundBoxMeshOO = new OOBox3f(Mesh.BoundBox_Extent._min, Mesh.BoundBox_Extent._max);
        for (int vi = 0; vi < OOBox3f.VertexCount; ++vi)
        {
          vec4 v = _world * _boundBoxMeshOO.Verts[vi].toVec4(1);
          _boundBoxMeshOO.Verts[vi] = v.xyz;
          _boundBoxMeshAA.genExpandByPoint(_boundBoxMeshOO.Verts[vi]);
          _boundBox.genExpandByPoint(_boundBoxMeshOO.Verts[vi]);
        }

        VolumizeBoundBox(_boundBoxMeshAA);

        if (!_boundBoxMeshAA.Validate(false, false))
        {
          Gu.Log.ErrorCycle($"'{this.Name}' BoundBox was invalid.");
          Gu.DebugBreak();
        }
      }
      else
      {
        _boundBoxMeshAA = null;
        _boundBoxMeshOO = null;
        _boundBox.genExpandByPoint(Position_World);
      }

      SubclassModifyBoundBox();

      //bound box can be just a point - but not invalid.
      VolumizeBoundBox(_boundBox);
      if (!_boundBox.Validate(false, false))
      {
        Gu.Log.ErrorCycle($"'{this.Name}' BoundBox was invalid.");
        Gu.DebugBreak();
      }

      parent.genExpandByPoint(_boundBox._min);
      parent.genExpandByPoint(_boundBox._max);
    }
    protected virtual void SubclassModifyBoundBox() { }

    // public void Animate(AnimationTransition par)
    // {
    //   Gu.BRThrowNotImplementedException();
    //   // Gu.Assert(par != null);
    //   // if (TryGetComponent<AnimationComponent>(par._name, out var cmp))
    //   // {
    //   //   cmp.Play(par);
    //   // }
    //   // else
    //   // {
    //   //   Gu.Log.Warn($"Could not find/play object animation '{par.ToString()}'");
    //   //   Gu.DebugBreak();
    //   // }
    // }
    // public bool TryGetComponent<T>(string? name, out T comp) where T : class
    // {
    //   //Gets the first component of the given template type
    //   bool res = false;
    //   comp = null;
    //   T found = null;
    //   IterateComponentsSafe((c) =>
    //   {
    //     if (c is T)
    //     {
    //       if ((name == null) || (name != null && c.Name == name))
    //       {
    //         found = c as T;
    //         res = true;
    //         return LambdaBool.Break;
    //       }
    //     }
    //     return LambdaBool.Continue;
    //   });
    //   comp = found;
    //   return res;
    // }
    public void IterateComponentsSafe(Func<Component, LambdaBool> act)
    {
      _components?.IterateSafe(act);
    }
    // public void IterateConstraintsSafe(Func<Constraint, LambdaBool> act)
    // {
    //   _constraints?.IterateSafe(act);
    // }
    public void IterateChildrenSafe(Func<WorldObject, LambdaBool> act, bool iterateDeleted = false)
    {
      _children?.IterateSafe((ob) =>
      {
        if (iterateDeleted || (!iterateDeleted && ob.State != WorldObjectState.Removed))
        {
          act(ob);
        }
        return LambdaBool.Continue;
      });
    }
    public void UnlinkFromParent()
    {
      TryGetParent()?.RemoveChild(this);
    }
    public void UnlinkHierarchy()
    {
      //Unlink object for destroy
      IterateChildrenSafe((c) =>
      {
        c.UnlinkHierarchy();
        return LambdaBool.Continue;
      });
      _children = null;
      UnlinkFromParent();
    }
    public WorldObject TryGetParent()
    {
      if (_parent != null)
      {
        return _parent;
      }
      return null;
    }
    public PRS PRS_Local
    {
      get
      {
        return new PRS()
        {
          Position = this.Position_Local,
          Rotation = this.Rotation_Local,
          Scale = this.Scale_Local,
        };
      }
      set
      {
        var p = value;
        if (p.Position != null)
        {
          this.Position_Local = p.Position.Value;
        }
        if (p.Rotation != null)
        {
          this.Rotation_Local = p.Rotation.Value;
        }
        if (p.Scale != null)
        {
          this.Scale_Local = p.Scale.Value;
        }
      }
    }
    #endregion
    #region Private: Methods

    private void DispatchModifiers()
    {
      //TODO: send all modifiers to GPU for computation
      // we are doing this async not on the vert shader
      //also can apply smoothing and springs.
      if (_modifiers != null)
      {
        foreach (var mod in _modifiers)
        {
          mod.Dispatch(this.Mesh);
        }
      }
    }
    private void UpdateComponents(double dt)
    {
      if (_components != null)
      {
        IterateComponentsSafe((cmp) =>
        {
          if (cmp.ComponentState == ComponentState.Added)
          {
            cmp.OnCreate(this);
            cmp.ComponentState = ComponentState.Initialized;
          }
          if (cmp.Enabled)
          {
            cmp.OnUpdate(dt, this);
          }
          return LambdaBool.Continue;
        });
      }
    }
    private void UpdateTreeDepth()
    {
      if (Parent == null)
      {
        _treeDepth = 0;
      }
      else
      {
        _treeDepth = Parent._treeDepth + 1;
        IterateChildrenSafe((cc) =>
        {
          cc.UpdateTreeDepth();
          return LambdaBool.Continue;
        });
      }
    }
    private void ApplyConstraints()
    {
      // foreach (var c in Constraints)
      // {
      //   c.Apply(this);
      // }
    }
    private void VolumizeBoundBox(Box3f b)
    {
      float epsilon = 0.01f;//float.Epsilon
      if (b._max.y - b._min.y == 0)
      {
        b._max.y += epsilon;
        b._min.y -= epsilon;
      }
      if (b._max.x - b._min.x == 0)
      {
        b._max.x += epsilon;
        b._min.x -= epsilon;
      }
      if (b._max.z - b._min.z == 0)
      {
        b._max.z += epsilon;
        b._min.z -= epsilon;
      }
    }
    public virtual void Play(AnimationClip ac)
    {
      Gu.Assert(ac != null);
      if (_animationData != null)
      {
        if (_animationData.Animations.TryGetValue(ac.Name, out this._currentKeys))
        {
          _currentClip = ac;
        }
      }

      IterateChildrenSafe((wo) =>
      {
        wo.Play(ac);
        return LambdaBool.Continue;
      });
    }
    private void CompileAndAnimate(double dt)
    {
      if (TransformChanged == true)
      {
        mat4 mScl = mat4.getScale(Scale_Local);
        mat4 mRot = mat4.getRotation(Rotation_Local);
        mat4 mPos = mat4.getTranslation(Position_Local);

        _local = (mScl) * (mRot) * (mPos);
      }

      if (_currentClip != null && _currentKeys != null)
      {
        _currentClip.Update(dt, _currentKeys.MaxTime);
        _local = _local *_currentKeys.Animate(_currentClip.Time) ;
        if (_currentClip.State == ActionState.Stop)
        {
          _currentClip = null;
        }
      }
      //old xform..
      //_local = (mScl * mSclA) * (mRot * mRotA) * (mPos * mPosA);

    }
    public void ApplyParentMatrix()
    {
      //TODO: Parent types
      //if isBoneNode
      if (Parent != null)
      {
        _world = _local * Parent.WorldMatrix ;
      }
      else
      {
        _world = _local;
      }
    }    
    private void DecomposeLocalMatrix()
    {
      mat4 tmprot;
      vec4 pw;
      vec4 sw;
      LocalMatrix.decompose(out pw, out tmprot, out sw);
      _position = pw.xyz;
      _scale = sw.xyz;
      _rotation = tmprot.toQuat();
      SetTransformChanged();
    }
    private void DecomposeWorldMatrix()
    {
      //We should just compute these if we need them.
      // positionworld get{if(!decomposed) decompose.. }
      _positionWorld = WorldMatrix.ExtractTranslation();
      // mat4 tmprot;
      // vec4 pw;
      // vec4 sw;
      // WorldMatrix.decompose(out pw, out tmprot, out sw);
      // _positionWorld = pw.xyz;
      //_scaleWorld = sw.xyz;
      //_rotationWorld = tmprot.toQuat();
    }

    #endregion

  }//cls




  public class Light : WorldObject
  {
    public bool Enabled { get { return _enabled; } set { _enabled = value; } }
    public float LightRadius { get { return _lightRadius; } set { _lightRadius = value; } }
    public vec3 LightColor { get { return _lightColor; } set { _lightColor = value; } }
    public float LightPower { get { return _lightPower; } set { _lightPower = value; } }
    public LightType LightType { get { return _lightType; } set { _lightType = value; } }

    [DataMember] private float _lightRadius = 100;//Distance in the case of directional light
    [DataMember] private vec3 _lightColor = vec3.One;
    [DataMember] private float _lightPower = 10;
    [DataMember] private LightType _lightType = LightType.Point;
    [DataMember] private bool _enabled = true;

    protected Light() { }
    public Light(string name) : base(name) { }

    public override void Update(double dt, ref Box3f parentBoundBox)
    {
      base.Update(dt, ref parentBoundBox);
    }
    protected override void SubclassModifyBoundBox()
    {
      _boundBox.genExpandByPoint(Position_Local - LightRadius);
      _boundBox.genExpandByPoint(Position_Local + LightRadius);
    }

  }

}//ns 
