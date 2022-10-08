using System.Runtime.Serialization;

namespace PirateCraft
{
  public enum WorldObjectState
  {
    Created,
    Active,
    Removed, //pending removal from the scene, or removed
    //Destroyed//pending removal from scene, and unlinking
  }
  public interface IDrawable
  {
    public MeshData? Mesh { get; set; }
    public Material? Material { get; set; }
    public mat4 WorldMatrix { get; set; }
    public uint PickId { get; set; }
    public bool Selected { get; set; }
    public bool Picked { get; set; }
    public bool Pickable { get; set; }
  }
  public class SoloMesh : IDrawable
  {
    //lightweight version of WorldObject (matrix+mesh)
    //for rendering transformed geometry without the extra data

    mat4 _worldMatrix = mat4.Identity;

    public MeshData? Mesh { get; set; } = null;
    public Material? Material { get; set; } = null;
    public mat4 WorldMatrix
    {
      get { return _worldMatrix; }
      set
      {
        _worldMatrix = value;
      }
    }
    public uint PickId { get; set; } = 0;
    public UInt64 UniqueID { get { return Library.c_iUntypedUnique; } }
    public bool Selected { get; set; } = false;
    public bool Picked { get; set; } = false;
    public bool Pickable { get; set; } = false;

    public SoloMesh(MeshData? mesh, Material? mat, mat4 mworld, uint pickId)
    {
      Mesh = mesh;
      Material = mat;
      _worldMatrix = mworld;
      PickId = pickId;
    }
  }

  [Serializable]
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
  }

  [Serializable]
  [DataContract]
  public class WorldObject : DataBlock, IDrawable, ISerializeBinary, ICopy<WorldObject>, IClone
  {
    // main object that stores matrix for pos/rot/scale, and components for mesh, sound, script .. GameObject ..
    #region Public:Members

    public WorldObjectState State { get { return _state; } set { _state = value; } }
    public bool TransformChanged { get { return _transformChanged; } private set { _transformChanged = value; } }
    public bool Hidden { get { return _hidden; } private set { _hidden = value; } }

    public OOBox3f BoundBoxMeshTransform { get { return _boundBoxTransform; } } //Transformed bound box
    public Box3f BoundBox { get { return _boundBox; } } //Entire AABB with all meshes and children inside

    //public RefList<WorldObject> Children { get { return _children; } private set { _children = value; } }

    public vec3 Position_Local { get { return _position; } set { _position = value; SetTransformChanged(); } }
    public quat Rotation_Local { get { return _rotation; } set { _rotation = value; SetTransformChanged(); } }//xyz,angle
    public vec3 Scale_Local { get { return _scale; } set { _scale = value; SetTransformChanged(); } }

    public vec3 Position_World { get { return _positionWorld; } private set { _positionWorld = value; } }
    public quat Rotation_World { get { return _rotationWorld; } private set { _rotationWorld = value; } }
    public vec3 Scale_World { get { return _scaleWorld; } private set { _scaleWorld = value; } }

    public vec3 AnimatedPosition { get { return _animatedPosition; } set { _animatedPosition = value; SetTransformChanged(); } }
    public quat AnimatedRotation { get { return _animatedRotation; } set { _animatedRotation = value; SetTransformChanged(); } }
    public vec3 AnimatedScale { get { return _animatedScale; } set { _animatedScale = value; SetTransformChanged(); } }

    public mat4 BindMatrix { get { return _bind; } } // Skinned Bind matrix
    public mat4 InverseBindMatrix { get { return _inverse_bind; } } // Skinned Inverse Bind
    public mat4 LocalMatrix { get { return _local; } }
    public mat4 WorldMatrix { get { return _world; } set { Gu.BRThrowException("WorldMatrix cannot be set on WorldObject."); } }
    //public RefList<Component> Components { get { return _components; } private set { _components = value; } }
    //public List<DataReference<Constraint>> Constraints { get { return _constraints; } private set { _constraints = value; } }// *This is an ordered list they come in order

    public vec3 BasisX { get { return _basisX; } }
    public vec3 BasisY { get { return _basisY; } }
    public vec3 BasisZ { get { return _basisZ; } }
    public vec3 ForwardNormalVector { get { return _basisZ; } }
    public vec3 Heading { get { return _basisZ; } }

    public MeshData? Mesh { get { return _meshData; } set { _meshData = value; } }
    public Material? Material { get { return _material; } set { _material = value; } }

    //Script system should be for this
    public Action<WorldObject>? OnUpdate { get; set; } = null;
    public Action<WorldObject, RenderView>? OnView { get; set; } = null;
    public Action<WorldObject>? OnAddedToScene { get; set; } = null;
    public Action<WorldObject>? OnDestroyed { get; set; } = null;

    public bool HasPhysics { get { return _hasPhysics; } set { _hasPhysics = value; } }
    public vec3 Velocity { get { return _velocity; } set { _velocity = value; } }
    public bool OnGround { get { return _resting; } set { _resting = value; } }
    public bool HasGravity { get { return _hasGravity; } set { _hasGravity = value; } }
    public bool Collides { get { return _collides; } set { _collides = value; } }
    public float AirFriction { get { return _airFriction; } set { _airFriction = value; } }

    public bool Selectable { get { return _selectable; } set { _selectable = value; } }
    public bool Selected { get { return _selected; } set { _selected = value; } }//May not be necessary adds extra data, we can use flags or just use WorldEditor.SelectedObjects
    public bool Picked { get { return _picked; } set { _picked = value; } }//Same
    //Active - eventually
    public uint PickId { get { return _pickId; } set { Gu.BRThrowException("PickId cannot be set on WorldObject."); } }

    //public WindowContext ExclusiveRenderContext { get; set; } = null; //ONLY render this object in THIS context, regardless of whether it is visible. This is for multiple-windows. If null: render in any context.
    //public WindowContext ExcludeFromRenderContext { get; set; } = null; //DO NOT render in THIS context. Used for an FPS seeing other characters.
    public WeakReference<RenderView>? ExcludeFromRenderView//DO NOT render in THIS context. Used for an FPS seeing other characters.
    {
      get { return _excludeFromRenderView; }
      set { _excludeFromRenderView = value; }
    }

    //public List<WorldObject> Instances = null;// To make an Instance's object data  unique call MakeUnique

    #endregion
    #region Public:Propfuncs

    public PRS GetPRS_Local()
    {
      return new PRS()
      {
        Position = this.Position_Local,
        Rotation = this.Rotation_Local,
        Scale = this.Scale_Local,
      };
    }
    public void SetPRS_Local(PRS p)
    {
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
          //NOTE: the pick IDs are from the context.. and they should be basd on eadch context.
          // This is INVALID
          _pickId = Gu.Context.Renderer.Picker.GenPickId();
        }
        else if (value == false)
        {
          _pickId = 0;
        }
        _pickable = value;
      }
    }
    public Box3f BoundBoxMeshBind
    {
      get
      {
        if (Mesh != null)
        {
          //TODO: - apply animation bind matrix
          return Mesh.BoundBox_Extent;
        }
        else
        {
          return Box3f.Default; // No mesh, return 1,1,1
        }
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

    #endregion
    #region Private:Members

    //Serializable
    [DataMember] private quat _rotation = new quat(0, 0, 0, 1); //Axis-Angle xyz,ang
    [DataMember] private vec3 _scale = new vec3(1, 1, 1);
    [DataMember] private vec3 _position = new vec3(0, 0, 0);
    [DataMember] private quat _animatedRotation = quat.identity();
    [DataMember] private vec3 _animatedScale = new vec3(1, 1, 1);
    [DataMember] private vec3 _animatedPosition = new vec3(0, 0, 0);
    [DataMember] private mat4 _world = mat4.Identity;
    [DataMember] private mat4 _local = mat4.Identity;
    [DataMember] private mat4 _bind = mat4.Identity;
    [DataMember] private mat4 _inverse_bind = mat4.Identity;
    [DataMember] private vec3 _basisX = new vec3(1, 0, 0);
    [DataMember] private vec3 _basisY = new vec3(0, 1, 0);
    [DataMember] private vec3 _basisZ = new vec3(0, 0, 1);
    [DataMember] private vec3 _positionWorld = vec3.Zero;
    [DataMember] private quat _rotationWorld = quat.Identity;
    [DataMember] private vec3 _scaleWorld = vec3.Zero;
    [DataMember] private vec4 _color = new vec4(1, 1, 1, 1);
    [DataMember] private OOBox3f _boundBoxTransform = new OOBox3f(new vec3(0, 0, 0), new vec3(1, 1, 1));
    [DataMember] protected Box3f _boundBox = new Box3f(new vec3(0, 0, 0), new vec3(1, 1, 1));
    [DataMember] private bool _pickable = true;
    [DataMember] private bool _selectable = true;
    [DataMember] private bool _hidden = false;
    [DataMember] private int _treeDepth = 0; //used to check for DAG cycles
    [DataMember] private vec3 _velocity = new vec3(0, 0, 0);
    [DataMember] private bool _resting = false;
    [DataMember] private bool _hasGravity = true;
    [DataMember] private bool _collides = false;
    [DataMember] private float _airFriction = 0.0f;//friction with the air i.e. movement damping in m/s
    [DataMember] private bool _hasPhysics = false;
    [DataMember] private bool _selected = false;
    [DataMember] private bool _picked = false;

    //Temps/generated
    [NonSerialized] private WorldObjectState _state = WorldObjectState.Created;
    [NonSerialized] private bool _transformChanged = false;
    [NonSerialized] private uint _pickId = 0;
    [NonSerialized] private WeakReference<RenderView>? _excludeFromRenderView = null; //DO NOT render in THIS context. Used for an FPS seeing other characters.

    //Junk
    [NonSerialized] public object LoaderTempData = null;
    [NonSerialized] public int LoaderTempDataNodeId = -1;
    [NonSerialized] public bool DebugBreakRender = false;

    //Refs
    /*[DataMember]*/
    [DataMember] private WorldObject? _parent = null;
    [DataMember] private List<WorldObject>? _children = null;//new List<DataReference<WorldObject>>();
    [DataMember] private List<Component>? _components = null;//new List<DataReference<Component>>();
    [DataMember] private List<Constraint>? _constraints = null;//new List<DataReference<Constraint>>();
    [DataMember] private MeshData? _meshData = null;
    [DataMember] private Material? _material = null;

    #endregion
    #region Public:Methods

    protected WorldObject() { }//Clone ctor
    public WorldObject(string name) : base(name)
    {
      //by default all world objects persist if attached to the scene, this makes all their data references persist as well
      //For optimization, nothing shoudl be here. WorldObject is new'd a lot each frame.
      Gu.Assert(Gu.Context != null);
      Gu.Assert(Gu.Context.Renderer != null);
      //For now, everything gets a pick color. Debug reasons.
      //NOTE: the pick IDs are from the context.. and they should be basd on eadch context.
      // This is INVALID
      if (_pickable)
      {
        _pickId = Gu.Context.Renderer.Picker.GenPickId();
      }
      _color = Random.NextVec4(new vec4(0.2f, 0.2f, 0.2f, 1), new vec4(1, 1, 1, 1));
    }
    public virtual void Update(World world, double dt, ref Box3f parentBoundBox)
    {
      if (Hidden)
      {
        return;
      }
      OnUpdate?.Invoke(this);

      UpdateComponents(dt);
      ApplyConstraints();
      CompileLocalMatrix();
      ApplyParentMatrix();

      //Basis calculuation must come after the world is computed
      _basisX = (WorldMatrix * new vec4(1, 0, 0, 0)).xyz().normalized();
      _basisY = (WorldMatrix * new vec4(0, 1, 0, 0)).xyz().normalized();
      _basisZ = (WorldMatrix * new vec4(0, 0, 1, 0)).xyz().normalized();

      // bleh. We should just compute these if we need them. _bComputedWorldDecompose
      mat4 tmprot;
      vec4 pw;
      vec4 sw;
      WorldMatrix.decompose(out pw, out tmprot, out sw);
      _positionWorld = pw.xyz();
      _scaleWorld = sw.xyz();
      _rotationWorld = tmprot.toQuat();

      _boundBox.genResetLimits();
      IterateChildrenSafe((child) =>
      {
        Gu.Assert(child != null);
        child.Update(world, dt, ref _boundBox);
        return LambdaBool.Continue;
      });
      CalcBoundBox(ref parentBoundBox);

      SanitizeTransform();
    }
    private static bool skip_transform_validation_check = false;
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
    public void Pick()
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
    public object? Clone(bool? shallow = null)
    {
      WorldObject w = new WorldObject();
      w.CopyFrom(this, shallow);
      return w;
    }
    public override List<DataBlock?> GetSubResources()
    {
      var subs = new List<DataBlock?>(){
         _meshData
        ,_material
       };
      if (_children != null) subs.AddRange(_children);
      if (_components != null) subs.AddRange(_components);
      if (_constraints != null) subs.AddRange(_constraints);
      return subs;
    }
    public void CopyFrom(WorldObject? other, bool? shallow = null)
    {
      Gu.Assert(other != null);
      base.CopyFrom(other);

      this._rotation = other._rotation;
      this._scale = other._scale;
      this._position = other._position;
      this._animatedRotation = other._animatedRotation;
      this._animatedScale = other._animatedScale;
      this._animatedPosition = other._animatedPosition;
      //this._parent = _parent; //Do not clone
      this._world = other._world;
      this._local = other._local;
      this._bind = other._bind;
      this._inverse_bind = other._inverse_bind;
      this._basisX = other._basisX;
      this._basisY = other._basisY;
      this._basisZ = other._basisZ;
      this._boundBoxTransform = other._boundBoxTransform;
      this._boundBox = other._boundBox;
      this._color = other._color;
      this._transformChanged = other._transformChanged;
      this._hidden = other._hidden;
      this._state = other._state;
      //this._treeDepth = other._treeDepth; //Do not clone
      this.OnUpdate = other.OnUpdate;
      this.OnAddedToScene = other.OnAddedToScene;
      this.OnDestroyed = other.OnDestroyed;
      this._velocity = other._velocity;
      this._resting = other._resting;
      this._hasGravity = other._hasGravity;
      this._collides = other._collides;
      this._airFriction = other._airFriction;
      this._hasPhysics = other._hasPhysics;
      this._pickable = other._pickable;
      this._pickId = Gu.Context.Renderer.Picker.GenPickId();
      this._selectable = other._selectable;

      //Create an instance copy of the data blocks.
      if (shallow == null || (shallow != null && shallow == true))
      {
        this._meshData = other._meshData;
        this._material = other._material;
        this._components = other._components;
        this._constraints = other._constraints;
      }
      else
      {
        if (other._meshData != null) { this._meshData = (MeshData)other._meshData.Clone(false); }
        if (other._material != null) { this._material = (Material)other._material.Clone(false); }
        if (other._components != null)
        {
          this._components = new List<Component>();
          other.IterateComponentsSafe((c) =>
          {
            _components.Add((Component)c.Clone());
            return LambdaBool.Continue;
          });
        }
        if (other._constraints != null)
        {

          this._constraints = new List<Constraint>();
          other.IterateComponentsSafe((c) =>
          {
            _constraints.Add((Constraint)c.Clone());
            return LambdaBool.Continue;
          });
        }
      }

      IterateChildrenSafe((ch) =>
      {
        var wo = (WorldObject)ch.Clone();
        this.AddChild(wo);
        return LambdaBool.Continue;
      });
    }
    public void View(RenderView rv)
    {
      OnView?.Invoke(this, rv);
      var that = this;
      IterateComponentsSafe((cmp) =>
      {
        cmp.OnView(that, rv);
        return LambdaBool.Continue;
      });

      IterateChildrenSafe((child) =>
      {
        child.View(rv);
        return LambdaBool.Continue;
      });
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
    public void ApplyConstraints()
    {
      // foreach (var c in Constraints)
      // {
      //   c.Apply(this);
      // }
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
      Gu.Assert(child != Gu.World.SceneRoot);
      Gu.Assert(child != this);
      Gu.Assert(child.Persistence != DataPersistence.LibraryDependency);

      if (child.Parent != null)
      {
        child.Parent.RemoveChild(child);
      }
      _children = _children.ConstructIfNeeded();
      _children.Add(child);
      child.Parent = this;

      child.UpdateTreeDepth();

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
        _boundBoxTransform = new OOBox3f(BoundBoxMeshBind._min, BoundBoxMeshBind._max);
        for (int vi = 0; vi < OOBox3f.VertexCount; ++vi)
        {
          vec4 v = WorldMatrix * _boundBoxTransform.Verts[vi].toVec4(1);
          _boundBoxTransform.Verts[vi] = v.xyz();
          _boundBox.genExpandByPoint(_boundBoxTransform.Verts[vi]);
        }
      }
      else
      {
        _boundBox.genExpandByPoint(this.Position_World);
      }

      //So for now, I'm saying every object has a mesh of some kind. This makes things simpler.
      //If you don't want to draw it set Visible=false.
      //Bound boxes can be no voluem.. if a plane..
      if (!_boundBox.Validate(false, false))
      {
        Gu.Log.ErrorCycle($"'{this.Name}' BoundBox was invalid.");
        Gu.DebugBreak();
      }

      parent.genExpandByPoint(_boundBox._min);
      parent.genExpandByPoint(_boundBox._max);
    }
    public void CompileLocalMatrix()
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
    public void IterateConstraintsSafe(Func<Constraint, LambdaBool> act)
    {
      _constraints?.IterateSafe(act);
    }
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
    #endregion
    #region Private:Methods

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

    #endregion

    public void Serialize(BinaryWriter bw)
    {
      base.Serialize(bw);
      Gu.BRThrowNotImplementedException();

      // //Serializable
      // bw.Write((quat)_rotation);
      // bw.Write((vec3)_scale);
      // bw.Write((vec3)_position);
      // bw.Write((quat)_animatedRotation);
      // bw.Write((vec3)_animatedScale);
      // bw.Write((vec3)_animatedPosition);
      // bw.Write((mat4)_world);
      // bw.Write((mat4)_local);
      // bw.Write((mat4)_bind);
      // bw.Write((mat4)_inverse_bind);
      // bw.Write((vec3)_basisX);
      // bw.Write((vec3)_basisY);
      // bw.Write((vec3)_basisZ);
      // bw.Write((vec3)_positionWorld);
      // bw.Write((quat)_rotationWorld);
      // bw.Write((vec3)_scaleWorld);
      // bw.Write((vec4)_color);
      // bw.Write((OOBox3f)_boundBoxTransform);
      // bw.Write((Box3f)_boundBox);
      // bw.Write((Boolean)_pickable);
      // bw.Write((Boolean)_selectable);
      // bw.Write((Boolean)_hidden);
      // bw.Write((Int32)_treeDepth);
      // bw.Write((vec3)_velocity);
      // bw.Write((Boolean)_resting);
      // bw.Write((Boolean)_hasGravity);
      // bw.Write((Boolean)_collides);
      // bw.Write((Single)_airFriction);
      // bw.Write((Boolean)_hasPhysics);

      // SerializeTools.SerializeDataBlockRef(bw, _meshData);
      // SerializeTools.SerializeDataBlockRef(bw, _material);

      // SerializeTools.SerializeDataBlockRefList(bw, _components);
      // SerializeTools.SerializeDataBlockRef(bw, _parent);
      // SerializeTools.SerializeDataBlockRefList(bw, _children);

    }//Serialize
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      base.Deserialize(br, version);
      Gu.BRThrowNotImplementedException();
      // //Serializable
      // _rotation = br.ReadQuat();
      // _scale = br.ReadVec3();
      // _position = br.ReadVec3();
      // _animatedRotation = br.ReadQuat();
      // _animatedScale = br.ReadVec3();
      // _animatedPosition = br.ReadVec3();
      // _world = br.ReadMat4();
      // _local = br.ReadMat4();
      // _bind = br.ReadMat4();
      // _inverse_bind = br.ReadMat4();
      // _basisX = br.ReadVec3();
      // _basisY = br.ReadVec3();
      // _basisZ = br.ReadVec3();
      // _positionWorld = br.ReadVec3();
      // _rotationWorld = br.ReadQuat();
      // _scaleWorld = br.ReadVec3();
      // _color = br.ReadVec4();
      // _boundBoxTransform = br.ReadOOBox3f();
      // _boundBox = br.ReadBox3f();
      // _pickable = br.ReadBoolean();
      // _selectable = br.ReadBoolean();
      // _hidden = br.ReadBoolean();
      // _treeDepth = br.ReadInt32();
      // _velocity = br.ReadVec3();
      // _resting = br.ReadBoolean();
      // _hasGravity = br.ReadBoolean();
      // _collides = br.ReadBoolean();
      // _airFriction = br.ReadSingle();
      // _hasPhysics = br.ReadBoolean();

      // _meshData = SerializeTools.DeserializeRef<MeshData>(br);
      // _material = SerializeTools.DeserializeRef<Material>(br);

      // _components = SerializeTools.DeserializeRefList<Component>(br, version);
      // _parent = SerializeTools.DeserializeRef<WorldObject>(br);
      // _children = SerializeTools.DeserializeRefList<WorldObject>(br, version);

    }//deserialze
  }

  //So another idea is to jsut have a component instead of htis, so we can serialize just component types and not WO types
  // LightComponent
  //*Note this is a test of billboarded quads.
  //for optimiz, We need to use
  // 1 single model matrix
  // 2 instancing
  // 3 megatex
  public enum LightType
  {
    Point, Direction
  }
  public class Light : WorldObject
  {
    private float _radius = 100;//Distance in the case of directional light
    private vec3 _color = vec3.One;
    private float _power = 10;
    private LightType _type = LightType.Point;

    public float Radius { get { return _radius; } set { _radius = value; } }
    public vec3 Color { get { return _color; } set { _color = value; } }
    public float Power { get { return _power; } set { _power = value; } }
    public LightType Type { get { return _type; } set { _type = value; } }

    public Light(string name) : base(name)
    {
      Texture light = Gu.Lib.LoadTexture(
        Gu.Lib.GetUniqueName(ResourceType.Texture, "light"),
      Gu.Lib.LoadImage(Gu.Lib.GetUniqueName(ResourceType.Image, "light"), new FileLoc("bulb.png", FileStorage.Embedded)), true, TexFilter.Bilinear);
      Pickable = true;
    }

    public override void Update(World world, double dt, ref Box3f parentBoundBox)
    {
      base.Update(world, dt, ref parentBoundBox);
      this._boundBox.genExpandByPoint(Position_Local - Radius);
      this._boundBox.genExpandByPoint(Position_Local + Radius);
    }
  }


}//ns 
