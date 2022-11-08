using System.Runtime.Serialization;

namespace PirateCraft
{
  public enum WorldObjectCloneMode
  {
    Instance, //reference to the data
    Copy //copy everything
  }
  public class Model : WorldObject
  {
    //i cant think of a reason for this, but it seems to make sense as
    //we would need to root all the WorldObject's in a loaded model anyway - so we need
    //to create a WorldObject node anyway - but what is the purpose of a subclass?
    //a WO is a model simply by the fact that it has a GLTFFile DataSource
    //if we just return a WO scene it would be ok, but it isn't a "model" per se
    //so what to do? i don't know. - 
    //this clss may go away eventually

    protected Model() { }
    public Model(string name) : base(name) { }

    //todo:
    //a base class that handles GLTF laoding - basically a data source for GLTF
    //has its own scnee.
    // WorldObject _modelRoot - 

  }

  public enum WorldObjectState
  {
    Created,
    Active,
    Removed, //pending removal from the scene, or removed
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
    public override void GetSubResources(List<DataBlock?>? deps)
    {
      Gu.Assert(deps != null);
      base.GetSubResources(deps);
      deps.Add(_meshView);
      deps.Add(_material);
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
    public bool CanDestroy { get { return _canDestroy; } set { _canDestroy = value; } }

    public OOBox3f? BoundBoxMeshTransform { get { return _boundBoxMeshOO; } } //Transformed local mesh bound box
    public Box3f? BoundBoxMesh { get { return _boundBoxMeshAA; } } //Local Mesh only AABB.

    public vec3 Position_Local { get { return _position; } set { _position = value; SetTransformChanged(); } }
    public quat Rotation_Local { get { return _rotation; } set { _rotation = value; SetTransformChanged(); } }//xyz,angle
    public vec3 Scale_Local { get { return _scale; } set { _scale = value; SetTransformChanged(); } }

    //technically you can set the world PRS just multiply by world^-1 (possibly parent, not sure)
    //then set the local PRS

    public vec3 Position_World { get { return _positionWorld; } private set { _positionWorld = value; } }
    public quat Rotation_World { get { return _rotationWorld; } private set { _rotationWorld = value; } }
    public vec3 Scale_World { get { return _scaleWorld; } private set { _scaleWorld = value; } }

    public vec3 AnimatedPosition { get { return _animatedPosition; } set { _animatedPosition = value; SetTransformChanged(); } }
    public quat AnimatedRotation { get { return _animatedRotation; } set { _animatedRotation = value; SetTransformChanged(); } }
    public vec3 AnimatedScale { get { return _animatedScale; } set { _animatedScale = value; SetTransformChanged(); } }

    public mat4 BindMatrix { get { return _bind; } } // Skinned Bind matrix
    public mat4 InverseBindMatrix { get { return _inverse_bind; } } // Skinned Inverse Bind
    public mat4 LocalMatrix { get { return _local; } set { _local = value; DecomposeLocalMatrix(); } }

    public vec3 BasisX_World { get { return _basisX_World; } }
    public vec3 BasisY_World { get { return _basisY_World; } }
    public vec3 BasisZ_World { get { return _basisZ_World; } }
    public vec3 ForwardNormalVector { get { return _basisZ_World; } }
    public vec3 Heading { get { return _basisZ_World; } }

    //Script system should be for this
    public Action<WorldObject>? OnUpdate { get; set; } = null;
    //public Action<WorldObject>? OnAddedToScene { get; set; } = null;
    //public Action<WorldObject>? OnDestroyed { get; set; } = null;

    public IObjectScript? Script { get { return _script; } set { _script = value; } }

    public bool HasPhysics { get { return _hasPhysics; } set { _hasPhysics = value; } }
    public vec3 Velocity { get { return _velocity; } set { _velocity = value; } }
    public bool OnGround { get { return _resting; } set { _resting = value; } }
    public bool HasGravity { get { return _hasGravity; } set { _hasGravity = value; } }
    public bool Collides { get { return _collides; } set { _collides = value; } }
    public float AirFriction { get { return _airFriction; } set { _airFriction = value; } }

    public bool HasLight { get { return _hasLight; } set { _hasLight = value; } }
    public float LightRadius { get { return _lightRadius; } set { _lightRadius = value; } }
    public vec3 LightColor { get { return _lightColor; } set { _lightColor = value; } }
    public float LightPower { get { return _lightPower; } set { _lightPower = value; } }
    public LightType LightType { get { return _lightType; } set { _lightType = value; } }

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
    [DataMember] private quat _animatedRotation = quat.identity();
    [DataMember] private vec3 _animatedScale = new vec3(1, 1, 1);
    [DataMember] private vec3 _animatedPosition = new vec3(0, 0, 0);
    [DataMember] private mat4 _local = mat4.Identity;
    [DataMember] private mat4 _bind = mat4.Identity;
    [DataMember] private mat4 _inverse_bind = mat4.Identity;
    [DataMember] private vec3 _basisX_World = new vec3(1, 0, 0);
    [DataMember] private vec3 _basisY_World = new vec3(0, 1, 0);
    [DataMember] private vec3 _basisZ_World = new vec3(0, 0, 1);
    [DataMember] private vec3 _positionWorld = vec3.Zero;
    [DataMember] private quat _rotationWorld = quat.Identity;
    [DataMember] private vec3 _scaleWorld = vec3.Zero;
    [DataMember] private vec4 _color = new vec4(1, 1, 1, 1);
    [DataMember] private OOBox3f? _boundBoxMeshOO = null;//Transformed mesh
    [DataMember] protected Box3f? _boundBoxMeshAA = null;//Bound box of this object with all base meshes
    [DataMember] private int _treeDepth = 0; //used to check for DAG cycles
    [DataMember] private bool _hasPhysics = false;
    [DataMember] private vec3 _velocity = new vec3(0, 0, 0);
    [DataMember] private bool _resting = false;
    [DataMember] private bool _hasGravity = true;
    [DataMember] private bool _collides = false;
    [DataMember] private float _airFriction = 0.0f;//friction with the air i.e. movement damping in m/s
    [DataMember] private bool _hasLight = false;
    [DataMember] private float _lightRadius = 100;//Distance in the case of directional light
    [DataMember] private vec3 _lightColor = vec3.One;
    [DataMember] private float _lightPower = 10;
    [DataMember] private LightType _lightType = LightType.Point;
    [DataMember] private bool _canDestroy = true;

    //Refs
    [DataMember] private WorldObject? _parent = null;
    [DataMember] private List<WorldObject>? _children = null;//new List<DataReference<WorldObject>>();
    [DataMember] private List<Component>? _components = null;//new List<DataReference<Component>>();
    //[DataMember] private List<Constraint>? _constraints = null;//new List<DataReference<Constraint>>();
    [DataMember] private IObjectScript? _script = null;

    //Temps/generated
    private WorldObjectState _state = WorldObjectState.Created;
    private bool _transformChanged = false;
    private WeakReference<RenderView>? _excludeFromRenderView = null; //DO NOT render in THIS context. Used for an FPS seeing other characters.

    //Junk
    public object LoaderTempData = null;
    public int LoaderTempDataNodeId = -1;
    public bool DebugBreakRender = false;
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

      UpdateComponents(dt);
      ApplyConstraints();
      CompileLocalMatrix();
      ApplyParentMatrix();
      //Hacking this in..
      // if (_lookAtConstraint != null)
      // {
      //   vec3 t = _world.ExtractTranslation();
      //   _world = _world * mat4.getLookAt(t, _lookAtConstraint.Value, _basisY_World).inverseOf();//why are matrix multiplies backwards 
      // }

      //Basis calculuation must come after the world is computed
      _basisX_World = (WorldMatrix * new vec4(1, 0, 0, 0)).xyz.normalized();
      _basisY_World = (WorldMatrix * new vec4(0, 1, 0, 0)).xyz.normalized();
      _basisZ_World = (WorldMatrix * new vec4(0, 0, 1, 0)).xyz.normalized();

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
    public override void MakeUnique()
    {
      Gu.BRThrowNotImplementedException();
      // base.MakeUnique();
      // _meshData = _meshData.Ref.Clone().GetDataReference<MeshData>();
      // _material = _material.Ref.Clone().GetDataReference<Material>();
      // _components = this._components.Clone(false);
      // _constraints = this._constraints.Clone(false);
    }
    public override void GetSubResources(List<DataBlock?> deps)
    {
      base.GetSubResources(deps);

      if (_children != null) deps.AddRange(_children);
      if (_components != null) deps.AddRange(_components);
      //if (_constraints != null) deps.AddRange(_constraints);
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
      // if (other._constraints != null)
      // {

      //   this._constraints = new List<Constraint>();
      //   other.IterateComponentsSafe((c) =>
      //   {
      //     _constraints.Add((Constraint)c.Clone());
      //     return LambdaBool.Continue;
      //   });
      // }

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
    public AnimationComponent GrabFirstAnimation()
    {
      AnimationComponent found = null;
      //Test - assume tool has just one component
      IterateComponentsSafe((c) =>
      {
        if (c is AnimationComponent)
        {
          found = c as AnimationComponent;
          return LambdaBool.Break;
        }
        return LambdaBool.Continue;
      });
      return found;
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
          vec4 v = WorldMatrix * _boundBoxMeshOO.Verts[vi].toVec4(1);
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

      if (HasLight)
      {
        _boundBox.genExpandByPoint(Position_Local - LightRadius);
        _boundBox.genExpandByPoint(Position_Local + LightRadius);
      }

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
    public void ApplyParentMatrix()
    {
      //TODO: Parent types
      //if isBoneNode
      if (Parent != null)
      {
        _world = _local * Parent.WorldMatrix;
      }
      else
      {
        _world = _local;
      }
    }
    public bool Component<T>(out T comp) where T : class
    {
      //Gets the first component of the given template type
      bool res = false;
      comp = null;
      T found = null;
      IterateComponentsSafe((c) =>
      {
        if (c is T)
        {
          found = c as T;
          res = true;
          return LambdaBool.Break;
        }
        return LambdaBool.Continue;
      });
      comp = found;
      return res;
    }
    public T Component<T>() where T : class
    {
      Component<T>(out var x);
      return x;
    }
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

    private void UpdateComponents(double dt)
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
    private void CompileLocalMatrix()
    {
      if (TransformChanged == false)
      {
        return;
      }

      mat4 mSclA = mat4.getScale(AnimatedScale);
      mat4 mRotA = mat4.getRotation(AnimatedRotation);
      mat4 mPosA = mat4.getTranslation(AnimatedPosition);

      mat4 mScl = mat4.getScale(Scale_Local);
      mat4 mRot = mat4.getRotation(Rotation_Local);
      mat4 mPos = mat4.getTranslation(Position_Local);

      _local = (mScl * mSclA) * (mRot * mRotA) * (mPos * mPosA);
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
      mat4 tmprot;
      vec4 pw;
      vec4 sw;
      WorldMatrix.decompose(out pw, out tmprot, out sw);
      _positionWorld = pw.xyz;
      _scaleWorld = sw.xyz;
      _rotationWorld = tmprot.toQuat();
    }

    #endregion

  }



}//ns 
