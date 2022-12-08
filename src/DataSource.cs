using System;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Text;
//File Loaders / Generators / Data sources
namespace Loft
{
  using GLTF_DATA_TYPE = glTFLoader.Schema.Accessor.TypeEnum;
  using GLTF_COMP_TYPE = glTFLoader.Schema.Accessor.ComponentTypeEnum;

  [DataContract]
  public abstract class DataSource : DataBlock
  {
    //Data source of data can be generated or loaded
    public enum SourceFormat
    {
      File = 1,
      Serialized = 2,
      Generated = 3,
    }
    // public enum LoadState
    // {
    //   None,
    //   Loaded,
    //   Unloaded
    // }

    [DataMember] private SourceFormat _format = SourceFormat.File;
    // private LoadState _state = LoadState.None;
    // private int _dbgCreateCount = 0;//just debug

    // public LoadState State { get { return _state; } protected set { _state = value; } }

    protected DataSource() { }//clone/serialize
    public DataSource(string name, SourceFormat type) : base(name)
    {
      _format = type;
    }

    // public T? Load<T>(string name) where T : DataBlock
    // {
    //   var x = Load(name);
    //   Gu.Assert(x is T);
    //   return (T?)x;
    // }
    //Creatre a new resource with the given name. 
    //The returned resource will be a resource instance with the given datablock attached
    public abstract void OnLoad();
    public abstract void OnDestroy();
    //Load the resource data required to create the resource
    // public DataBlock? Load(string name)
    // {
    //   if (this._state == LoadState.Loaded)
    //   {
    //     Gu.Log.Warn($"Tried ot load aloready loaded asset {name}");
    //     Gu.DebugBreak();
    //   }
    //   var d = Create(name);
    //   d.DataSource = this;
    //   this._state = LoadState.Loaded;
    //   return d;
    // }
    // //Unload the heavy data for the resource
    // public void Unload()
    // {
    //   Destroy();
    //   this._state = LoadState.Unloaded;
    // }
    public override string ToString()
    {
      string json = SerializeTools.SerializeJSON(this);
      return json;
    }
  }
  // [DataContract]
  // public class SerializedDataSource : DataSource
  // {
  //   //default DS items get their internal contens serialized into the Library
  //   public SerializedDataSource(string name) : base(name, SourceFormat.Serialized) { }
  //   // protected override DataBlock? Create(string name)
  //   // {
  //   //   return null;
  //   // }
  //   // protected override void Destroy()
  //   // {
  //   // }
  // }

  [DataContract]
  public abstract class ImageGenParams
  {
    [DataMember] public int _width = 0;
    [DataMember] public int _height = 0;
    [DataMember] public Image.ImagePixelFormat _format = Image.ImagePixelFormat.RGBA32ub;

    public abstract void Generate(ref byte[] data);

    public ImageGenParams(int w, int h)
    {
      _width = w;
      _height = h;
    }
  }
  [DataContract]
  public class ImageGenFlat : ImageGenParams
  {
    [DataMember] public vec4ub? _color = null;
    public ImageGenFlat(int w, int h, vec4? color) : base(w, h)
    {
    }
    public override void Generate(ref byte[] data)
    {
      Gu.Assert(_width > 0);
      Gu.Assert(_height > 0);
      Gu.Assert(_color != null);
      data = new byte[_width * _height * 4];
      for (var y = 0; y < _height; y++)
      {
        for (var x = 0; x < _width; x++)
        {
          data[Image.vofftos(x, y, _width) + 0] = _color.Value.r;
          data[Image.vofftos(x, y, _width) + 1] = _color.Value.g;
          data[Image.vofftos(x, y, _width) + 2] = _color.Value.b;
          data[Image.vofftos(x, y, _width) + 3] = _color.Value.a;
        }
      }
    }
  }
  [DataContract]
  public class ImageGen : DataSource
  {
    //image generator
    [DataMember] private ImageGenParams _params = null;
    private byte[]? _data = null;

    public ImageGen(string name, ImageGenParams p) : base(name, SourceFormat.Generated)
    {
      Gu.Assert(p != null);
      _params = p;
    }
    public override void OnLoad()
    {
      Gu.Assert(_params != null);
      _params.Generate(ref _data);
      var img = new Image(this.Name, _params._width, _params._height, _data, _params._format);
    }
    public override void OnDestroy()
    {
      _data = null;
    }
  }
  [DataContract]
  public class ImageFile : DataSource
  {
    public Image? TheImage = null;

    public static Image? LoadImage(string name, FileLoc loc)
    {

      ImageFile f = new ImageFile(name, loc);
      var img = f.LoadImageFile(name);
      return img;
    }
    public static Image? LoadImageRaw(byte[] raw_png, string name)
    {
      //load image bypassing datasource
      Image? img = null;
      try
      {
        StbImageSharp.ImageResult image = StbImageSharp.ImageResult.FromMemory(raw_png, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
        if (image != null)
        {
          var fmt = ProcessImage(image);
          img = new Image(name, image.Width, image.Height, image.Data, fmt);
        }
      }
      catch (Exception ex)
      {
        img = ImageLoadFailed_GetDefault($"raw:{name}", ex);
      }
      return img;
    }

    public ImageFile(string name, FileLoc loc) : base(name, SourceFormat.File) { _file = loc; }
    [DataMember] private FileLoc? _file = null;

    bool loaded = false;
    public override void OnLoad()
    {
      if (!loaded)
      {
        TheImage = LoadImageFile(this.Name);
        TheImage.DataSource = this;
      }
    }
    public override void OnDestroy()
    {
      TheImage = null;
    }
    private Image? LoadImageFile(string name)
    {
      Image? ret = null;

      using (var fs = _file.OpenRead())
      {
        if (fs != null)
        {
          StbImageSharp.ImageResult image = StbImageSharp.ImageResult.FromStream(fs, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
          if (image != null)
          {
            var fmt = ProcessImage(image);
            ret = new Image(name, image.Width, image.Height, image.Data, fmt);
          }
        }
      }

      return ret;
    }
    private static Image? ImageLoadFailed_GetDefault(string rawpath, Exception ex)
    {

      Gu.Log.Error("failed to load image: ", ex);
      Gu.DebugBreak();
      return null;
      //return Gu.Lib.LoadImage(Rs.Tex2D.DefaultFailedTexture);
    }
    private static Image.ImagePixelFormat ProcessImage(StbImageSharp.ImageResult image)
    {
      Gu.Assert(image != null);
      var pf = Image.ImagePixelFormat.RGBA32ub;
      if (image.SourceComp == StbImageSharp.ColorComponents.RedGreenBlueAlpha)
      {
        //RGBA is the basic texture2d format. We convert everything to RGBA for simplicity.
        pf = Image.ImagePixelFormat.RGBA32ub;
      }
      else if (image.SourceComp == StbImageSharp.ColorComponents.RedGreenBlue)
      {
        // ** Note : STB converts RGB images to RGBA wiht the above function's parameter so the nagive sourceComp is RGB, the input format is still RGBA.
        pf = Image.ImagePixelFormat.RGBA32ub;
      }
      else
      {
        //We don't handle images not stored as RGBAyet. Use some kind of flip routine to create RGBA.
        // b.FlipBA();
        // b.FlipBR();
        Gu.DebugBreak();
      }
      return pf;
    }

  }
  //this class really isn't needed for now.
  [DataContract]
  public class ShaderDataSource : DataSource
  {
    [DataMember] private string _generic_name = null;
    [DataMember] private PrimitiveType? _primType = null;
    [DataMember] private bool _gs = false;
    [DataMember] private FileStorage _storage = FileStorage.Embedded;
    private Shader? _shader = null;

    public ShaderDataSource(string name, string generic_name, bool gs, FileStorage storage, PrimitiveType? gs_type) : base(name, SourceFormat.File)
    {
      _generic_name = generic_name;
      _primType = gs_type;
      _gs = gs;
      _storage = storage;
    }
    public override void OnLoad()
    {
      //Returns an instance of the shader.
      _shader = new Shader(this.Name, _generic_name, _primType);
    }
    public override void OnDestroy()
    {
    }
  }
  [DataContract]
  public class ModelFile : DataSource
  {
    //@class ModelFile
    //@desc: model file class

    #region Public: Classes

    private const int MAX_JOINTS_OR_WEIGHTS_BUFFERS = 4;

    public enum ModelLoadState
    {
      None,
      Loading,
      Failed,
      Success
    }
    public enum IncludeMode
    {
      DefsOnly,
      AllObjects
    }

    public class ImportInfo
    {
      //Locates an object inside model file
      //Specify parameters to process and drop model in scene
      //note: Any undefined objects will not be created unless LoadAll is set
      public string _nameInFile = Lib.UnsetName;
      public vec3 _pos = vec3.Zero;
      public vec3 _scale = vec3.One;
      public quat _rot = quat.Identity;
      public string? _playAnimation = null;
      public bool _playAnimationRepeat = true;
      public bool _flipTris = true; // flip triangles
      public bool _rootOnly = true; // search only root objects by name, false= search for any object by name
      public WorldObject? _object = null;
      public bool _generated = false; //was generated by loader
      public bool _visible = true;
      public ImportInfo() { }
    }

    #endregion

    [DataMember] private FileLoc? _file = null;
    [DataMember] private List<ImportInfo> _importInfos;
    [DataMember] private IncludeMode _loadMode = IncludeMode.DefsOnly;

    //temps
    private bool _initialized = false;
    private ModelLoadState _loadState = ModelLoadState.None;
    private DynamicFileLoader _loader;
    private class ObjTemp
    {
      public ImportInfo _info;
      public int _gltfNodeId;
      public glTFLoader.Schema.Node _gltfNode;
      public int _gltfSkinId;
    }
    private class TempsData
    {
      public Dictionary<WorldObject, ObjTemp> _worldobj_temp = new Dictionary<WorldObject, ObjTemp>();
      public Dictionary<int, WorldObject> _gltf_id_to_worldobj = new Dictionary<int, WorldObject>();
      public Dictionary<string, WorldObject> _name_to_worldobj = new Dictionary<string, WorldObject>();
      public Dictionary<int, NodeType> _nodeid_to_nodetype = new Dictionary<int, NodeType>();
      public Dictionary<int, Armature> _joint_nodeid_to_armature = new Dictionary<int, Armature>();
      public Dictionary<int, int> _nodeid_to_jointid = new Dictionary<int, int>();
      public Dictionary<Armature, List<int>> _arm_joint_jointid_to_parentid = new Dictionary<Armature, List<int>>();//list is indexed by jointid => parentid
      public Dictionary<Armature, List<int>> _arm_to_jointid = new Dictionary<Armature, List<int>>();
      public Dictionary<Armature, int> _arm_to_jid_gen = new Dictionary<Armature, int>();
      public List<WorldObject> _objects = new List<WorldObject>();
    }
    private TempsData? _temp = null;
    private glTFLoader.Schema.Gltf? _myModel = null;
    private byte[] _gltf_data = null;
    private ClassLog _log;

    #region Public: Methods

    public ModelFile(string name, FileLoc loc, List<ImportInfo>? defs = null, IncludeMode mode = IncludeMode.DefsOnly) : base(name, SourceFormat.File)
    {
      //Ctor is just for storing parameters, call Load to load the asset
      _file = loc;
      _loadMode = mode;
      if (defs == null)
      {
        _importInfos = new List<ImportInfo>();
        _loadMode = IncludeMode.AllObjects;
      }
      else
      {
        _importInfos = defs;
      }
    }
    public override void OnLoad()
    {
      if (!_initialized)
      {
        Gu.Assert(_file != null);

        ReloadEverything();

        _loader = new DynamicFileLoader(new List<FileLoc>() { _file }, (files) =>
        {
          ReloadEverything();
          return this._loadState == ModelLoadState.Success;
        });
      }
    }
    public override void OnDestroy()
    {
      ClearData();
    }
    public WorldObject CreateObject(string name, vec3? pos = null, quat? rot = null, vec3? scl = null)
    {
      MakeSureLoadedAndDataSet();
      if (_temp._name_to_worldobj.TryGetValue(name, out var wo))
      {
        if (pos != null) { wo.Position_Local = wo.Position_Local + pos.Value; }
        if (rot != null) { wo.Rotation_Local = wo.Rotation_Local * rot.Value; }
        if (scl != null) { wo.Scale_Local = wo.Scale_Local * scl.Value; }
        Gu.World.AddObject(wo);
        return wo;
      }
      // var def = _importInfos.Where(x => x._nameInFile == name).FirstOrDefault();
      // if (def != null)
      // {
      //   var wo = def._object;
      //   if (wo != null)
      //   {
      //     if (pos != null) { wo.Position_Local = wo.Position_Local + pos.Value; }
      //     if (rot != null) { wo.Rotation_Local = wo.Rotation_Local * rot.Value; }
      //     if (scl != null) { wo.Scale_Local = wo.Scale_Local * scl.Value; }

      //     Gu.World.AddObject(wo);

      //     return wo;
      //   }
      //   else
      //   {
      //     Gu.Log.Error($"Def '{name}' had no object");
      //     Gu.DebugBreak();
      //   }
      // }
      // else
      // {
      //   Gu.Log.Error($"Could not find definition for name '{name}'");
      //   Gu.DebugBreak();
      // }
      return null;
    }
    private void MakeSureLoadedAndDataSet()
    {
      if (_loadState == ModelLoadState.None)
      {
        OnLoad();
      }
      // Gu.Assert(_importInfos != null);
      // if (_importInfos.Count == 0)
      // {
      //   Gu.Log.Error($"Model '{Name}' had no object defs");
      //   Gu.DebugBreak();
      // }
      Gu.Assert(_temp != null);
    }
    public void CreateObjects(vec3? pos = null, quat? rot = null, vec3? scl = null)
    {
      //Processing a loaded scene, create the objects based on the definitions, and other generation parameters
      MakeSureLoadedAndDataSet();

      foreach (var kvp in _temp._worldobj_temp)
      {
        var wo = kvp.Key;
        //var def = kvp.Value._info;
        //wo.Visible = def._visible;
        //wo.Position_Local = wo.Position_Local + def._pos;
        //wo.Rotation_Local = wo.Rotation_Local * def._rot;
        //wo.Scale_Local = def._scale;
        if (pos != null) { wo.Position_Local = wo.Position_Local + pos.Value; }
        if (rot != null) { wo.Rotation_Local = wo.Rotation_Local * rot.Value; }
        if (scl != null) { wo.Scale_Local = wo.Scale_Local * scl.Value; }
        Gu.World.AddObject(wo);
      }
    }

    #endregion
    #region Private: Methods

    private void OnFilesChanged()
    {
      ReloadEverything();
    }
    private void ReloadEverything()
    {
      _loadState = ModelLoadState.Loading;

      var msa = Gu.Milliseconds();

      _log = new ClassLog(this.Name, Gu.EngineConfig.Debug_Log_GLTF_Details, true);
      _log.AppendLine("\n---------------------------------------------------------------------------------");
      _log.AppendLine("---------------------------------------------------------------------------------");
      _log.AppendLine($"Loading '{this._file.FileName}'");

      if (_file.Extension == ".glb")
      {
        LoadGLTFScene(Name);
        _initialized = true;
      }
      else
      {
        _log.Error($"Unknown file format '{_file.Extension}' must be .glb");
      }

      _log.Debug($"..{_loadState.ToString()} {Gu.Milliseconds() - msa}ms");

      _log.Print();
    }


    #endregion
    #region Private: Load Methods

    private void LoadGLTFScene(string name)
    {
      try
      {
        _log.Debug($" Loading {name}");

        ClearData();

        if (LoadGLTFIntoMemory(_file))
        {
          BuildSceneHierarchy();
          ProcessObjects();
        }
      }
      catch (Exception ex)
      {
        _log.Error("Load model failed: " + Gu.GetAllException(ex));
      }
    }
    private void ClearData()
    {
      _temp = new TempsData();
      _myModel = null;
      _gltf_data = null;
      _loadState = ModelLoadState.None;

      foreach (var d in this._importInfos)
      {
        d._object = null;
      }
    }
    private bool LoadGLTFIntoMemory(FileLoc loc)
    {
      _gltf_data = null;
      _myModel = null;

      string path = loc.QualifiedPath;
      using (Stream? stream = loc.OpenRead())
      {
        if (stream != null)
        {
          _myModel = glTFLoader.Interface.LoadModel(stream);
        }
        else
        {
          _log.Error($"Stream '{path}' was null");
          return false;
        }
      }
      using (Stream? stream = loc.OpenRead())
      {
        if (stream != null)
        {
          _gltf_data = glTFLoader.Interface.LoadBinaryBuffer(stream);
        }
        else
        {
          _log.Error($"Stream '{path}' was null");
          return false;
        }
      }

      return true;
    }
    private void BuildSceneHierarchy()
    {
      if (_myModel != null)
      {
        if (_myModel.Scenes.Length == 0)
        {
          _log.Error("There were no scenes in the GLTF.");
        }
        var scene = _myModel.Scenes[0];
        if (_myModel.Scenes.Length > 1)
        {
          _log.Warn("There was more than 1 scene in GLTF. Only 1 scene is supported.");
        }

        ParseNodes(null, scene.Nodes);
      }
      else
      {
        _log.Error("GLTF model was null upon loading nodes");
      }
    }
    private bool IsJoint(int nodeid)
    {
      bool isJoint = false;
      foreach (var skin in _myModel.Skins)
      {
        if (skin.Joints.Contains(nodeid))
        {
          isJoint = true;
          break;
        }
      }
      return isJoint;
    }
    private bool IsArmature(string name)
    {
      //fromw hat i can see at least in blender the exported 
      //name of the skin is also the name of the armature node
      bool isarm = false;
      foreach (var skin in _myModel.Skins)
      {
        if (skin.Name == name)
        {
          isarm = true;
          break;
        }
      }
      return isarm;
    }
    private void SetJointParent(Armature arm, int jparent_id)
    {
      List<int>? d = null;
      if (!_temp._arm_joint_jointid_to_parentid.TryGetValue(arm, out d))
      {
        d = new List<int>();
        _temp._arm_joint_jointid_to_parentid[arm] = d;
      }
      d.Add(jparent_id);//this should end up being in order if we generate ids correctly.
    }
    private void AddArmJoint(Armature arm, int j_nodeid, int j_id)
    {
      _temp._joint_nodeid_to_armature.Add(j_nodeid, arm);
      if (!_temp._arm_to_jointid.TryGetValue(arm, out var jids))
      {
        jids = new List<int>();
        _temp._arm_to_jointid.Add(arm, jids);
      }
      _temp._arm_to_jointid[arm].Add(j_id);
      _temp._nodeid_to_jointid.Add(j_nodeid, j_id);
    }
    private void DoJoint(Armature arm, int[] child_nodes, int nodeid_parent, int jid_parent, ref int jid_next)
    {
      //start at 1 to signal 0 as being null parent (root)
      //breadth first generation of joint ids iterating joints during processing will iterate the hierarchy
      Gu.Assert(arm != null);

      //it is possible for control bones to not have parents.
      //[control bones..][root][..]
      AddArmJoint(arm, nodeid_parent, jid_parent);

      //build hierarchy breadth first so linear traversal guarantees parent processing
      if (child_nodes != null)
      {
        var ch_jids = new List<int>();
        foreach (var ch_nodeid in child_nodes)
        {
          var joint_node = _myModel.Nodes[ch_nodeid];
          var ch_jid = jid_next++;
          SetJointParent(arm, jid_parent);
          ch_jids.Add(ch_jid);
        }
        int i = 0;
        foreach (var ch_nodeid in child_nodes)
        {
          var joint_node = _myModel.Nodes[ch_nodeid];
          //breadth first
          DoJoint(arm, joint_node.Children, ch_nodeid, ch_jids[i], ref jid_next);
          i++;
        }
      }

    }
    private void ParseNodes(WorldObject parent, int[] nodes)
    {
      Gu.Assert(nodes != null);

      foreach (var nodeid in nodes)
      {
        var node = _myModel.Nodes[nodeid];
        Gu.Assert(node != null);

        // if (CanAddNode(node.Name, parent, out var def))
        // {
        ///Gu.Assert(def != null);
        WorldObject? wo = null;
        if (IsArmature(Gu.Lib.GetUniqueName(ResourceType.Armature, node.Name)))
        {
          wo = new Armature(node.Name);
        }
        else if (IsJoint(nodeid))
        {
          //i give up
          var arm = parent as Armature;
          int jid_next = 1;
          if (!_temp._arm_to_jid_gen.ContainsKey(arm))
          {
            _temp._arm_to_jid_gen.Add(arm, jid_next);
          }
          jid_next = _temp._arm_to_jid_gen[arm];
          SetJointParent(arm, 0);

          DoJoint(arm, node.Children, nodeid, 0, ref jid_next);

          _temp._arm_to_jid_gen[arm] = jid_next;
        }
        else
        {
          wo = new WorldObject(Gu.Lib.GetUniqueName(ResourceType.WorldObject, node.Name));
        }
        if (wo != null)
        {
          wo.DataSource = this;
          wo.Position_Local = new vec3(node.Translation);
          wo.Rotation_Local = new quat(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
          wo.Scale_Local = new vec3(node.Scale);
          _temp._objects.Add(wo);
          _temp._worldobj_temp.Add(wo, new ObjTemp()
          {
            _gltfNode = node,
            // _info = def,
            _gltfNodeId = nodeid,
            _gltfSkinId = node.Skin == null ? -1 : node.Skin.Value
          });
          _temp._gltf_id_to_worldobj.Add(nodeid, wo);
          _temp._name_to_worldobj.Add(node.Name, wo);
          if (parent != null)
          {
            parent.AddChild(wo);
          }
          if (node.Children != null)
          {
            ParseNodes(wo, node.Children);
          }
        }


        // }
        // else
        // {
        //   _clog.Debug($"Note: Undefined object '{node.Name}'");
        // }

      }
    }
    private bool CanAddNode(string nodeName, WorldObject parent, out ImportInfo? def)
    {
      if (_loadMode == IncludeMode.AllObjects)
      {
        def = new ImportInfo()
        {
          _generated = true
        };
        _importInfos.Add(def);

        return true;
      }
      else
      {
        //find node by nname
        def = _importInfos.Where(x => x._nameInFile == nodeName).FirstOrDefault();

        if (def != null)
        {
          if (parent == null && def._rootOnly)
          {
            return true;
          }
        }

        return false;
      }
    }
    private void ProcessObjects()
    {
      //TODO: fix
      // if (_importInfos != null && _importInfos.Count > 0)
      // {
      //   foreach (var d in _importInfos)
      //   {
      //     if (d._object != null)
      //     {
      foreach (var wo in _temp._objects)
      {
        LoadModelNode(wo);
      }
      //   }
      //   else
      //   {
      //     _log.Warn($" '{d._nameInFile}' Node was not found (generated={d._generated})");
      //   }
      // }

      //skin all objs      
      LoadGLTFSkin();
      LoadGLTFAnimation();
      // }
      // else
      // {
      //   _log.Error($"No import definitions supplied, or file had no objects (loadMode={this._loadMode.ToString()})");
      // }
      if (_loadState == ModelLoadState.Loading)
      {
        _loadState = ModelLoadState.Success;
      }
    }
    private void LoadModelNode(WorldObject wo)
    {
      LoadGLTFMeshAndMaterial(wo);
      wo.IterateChildrenSafe((child) =>
      {
        LoadModelNode(child);
        return LambdaBool.Continue;
      });
    }
    private void LoadGLTFAnimation()
    {
      Dictionary<int, AnimationData> node_anims = new Dictionary<int, AnimationData>();

      if (_myModel.Animations != null && _myModel.Animations.Length > 0)
      {
        foreach (var anim in _myModel.Animations)
        {
          string n = anim.Name;
          if (anim != null && anim.Channels != null && anim.Channels.Length > 0)
          {
            foreach (var channel in anim.Channels)
            {
              int nodeid = channel.Target.Node.Value;

              //this is an issue bc gltf does not distinguish bones and nodes
              AnimationData? adat = null;
              if (!node_anims.TryGetValue(nodeid, out adat))
              {
                adat = new AnimationData();
                node_anims.Add(nodeid, adat);
              }
              KeyframeData? kdat = null;
              if (!adat.Animations.TryGetValue(anim.Name, out kdat))
              {
                kdat = new KeyframeData(anim.Name);
                adat.Animations.Add(anim.Name, kdat);
              }

              ParseKeyFrames(anim.Name, channel, anim.Samplers[channel.Sampler], ref kdat);
            }
          }
          else
          {
            _log.Error($"Animation '{anim.Name}' had no channels.");
          }
        }
      }

      foreach (var kvp in node_anims)
      {
        int nodeId = kvp.Key;
        AnimationData adat = kvp.Value;

        if (_temp._joint_nodeid_to_armature.TryGetValue(nodeId, out var arm))
        {
          //joint
          Gu.Assert(arm.Data != null);
          arm.Data.JointAnims[nodeId] = adat;
        }
        else
        {
          //node
          Gu.Assert(_temp._gltf_id_to_worldobj.TryGetValue(nodeId, out var wo));
          wo.AnimationData = adat;
        }

      }




    }
    private void ParseKeyFrames(string name, glTFLoader.Schema.AnimationChannel? channel, glTFLoader.Schema.AnimationSampler? sampler, ref KeyframeData adat)
    {
      if (!_log.Assert(adat != null))
      {
        return;
      }
      if (!_log.Assert(sampler != null))
      {
        return;
      }
      if (!_log.Assert(channel != null))
      {
        return;
      }

      // "channels" : [
      //     {
      //         "sampler" : 0,
      //         "target" : {
      //             "node" : 4,
      //             "path" : "translation"
      //         }
      //     },
      // ],
      // "name" : "TestAnimation1",
      // "samplers" : [
      //     { 
      //        //sampler 0
      //         "input" : 20, // time
      //         "interpolation" : "LINEAR",
      //         "output" : 21 //prs, floats, mats, ..
      //     },
      // ]

      //time
      //Note: time values (seemingly) translated into seconds.
      var samp_acc = _myModel.Accessors[sampler.Input];
      if (!_log.Assert(samp_acc.ComponentType == GLTF_COMP_TYPE.FLOAT))
      {
        return;
      }
      if (!_log.Assert(samp_acc.Type == GLTF_DATA_TYPE.SCALAR))
      {
        return;
      }
      var off = _myModel.BufferViews[samp_acc.BufferView.Value].ByteOffset;
      float[] times = SerializeTools.DeserializeFrom<float>(_gltf_data, off, samp_acc.Count);

      //Translate interpolation enum
      KeyframeInterpolation interp = KeyframeInterpolation.Linear;
      if (sampler.Interpolation == glTFLoader.Schema.AnimationSampler.InterpolationEnum.LINEAR) { interp = KeyframeInterpolation.Linear; }
      else if (sampler.Interpolation == glTFLoader.Schema.AnimationSampler.InterpolationEnum.CUBICSPLINE) { interp = KeyframeInterpolation.Cubic; }
      else if (sampler.Interpolation == glTFLoader.Schema.AnimationSampler.InterpolationEnum.STEP) { interp = KeyframeInterpolation.Step; }
      else
      {
        Gu.BRThrowNotImplementedException();
      }


      //value
      samp_acc = _myModel.Accessors[sampler.Output];
      off = _myModel.BufferViews[samp_acc.BufferView.Value].ByteOffset;
      if (channel.Target.Path == glTFLoader.Schema.AnimationChannelTarget.PathEnum.rotation)
      {
        //quaternion
        if (!_log.Assert(samp_acc.ComponentType == GLTF_COMP_TYPE.FLOAT))
        {
          return;
        }
        if (!_log.Assert(samp_acc.Type == GLTF_DATA_TYPE.VEC4))
        {
          return;
        }
        _log.Debug($"Loading {samp_acc.Count} rotation keys.");
        quat[] vals = SerializeTools.DeserializeFrom<quat>(_gltf_data, off, samp_acc.Count);

        adat.FillRot(times, vals, interp, false, false);
      }
      else if (channel.Target.Path == glTFLoader.Schema.AnimationChannelTarget.PathEnum.translation)
      {
        //v3
        if (!_log.Assert(samp_acc.ComponentType == GLTF_COMP_TYPE.FLOAT))
        {
          return;
        }
        if (!_log.Assert(samp_acc.Type == GLTF_DATA_TYPE.VEC3))
        {
          return;
        }

        _log.Debug($"Loading {samp_acc.Count} position keys.");

        vec3[] vals = SerializeTools.DeserializeFrom<vec3>(_gltf_data, off, samp_acc.Count).ToArray();

        adat.FillPos(times, vals, interp);
      }
      else if (channel.Target.Path == glTFLoader.Schema.AnimationChannelTarget.PathEnum.scale)
      {
        //v3
        if (!_log.Assert(samp_acc.ComponentType == GLTF_COMP_TYPE.FLOAT))
        {
          return;
        }
        if (!_log.Assert(samp_acc.Type == GLTF_DATA_TYPE.VEC3))
        {
          return;
        }
        _log.Debug($"Loading {samp_acc.Count} scale keys.");
        vec3[] vals = SerializeTools.DeserializeFrom<vec3>(_gltf_data, off, samp_acc.Count);

        adat.FillScale(times, vals, interp);
      }
      else if (channel.Target.Path == glTFLoader.Schema.AnimationChannelTarget.PathEnum.weights)
      {
        _log.Error("We do not support skin yet.. todo..");
        Gu.DebugBreak();
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      adat.SortAndCalculate();

    }
    private class MeshTemp
    {
      public vec3[]? positions = null;
      public vec3[]? normals = null;
      public vec2[]? texs_0 = null;
      public vec3[]? tangents = null;
      public List<Svec4>[]? jointsn = new List<Svec4>[MAX_JOINTS_OR_WEIGHTS_BUFFERS];
      public List<vec4>[]? weightsn = new List<vec4>[MAX_JOINTS_OR_WEIGHTS_BUFFERS];
      public bool has_weights = false;
      public bool has_joints = false;
    }
    private void LoadGLTFMeshAndMaterial(WorldObject wo)
    {
      Gu.Assert(wo != null);
      Gu.Assert(_myModel != null);
      Gu.Assert(_temp != null);
      if (!_temp._worldobj_temp.TryGetValue(wo, out var obtemp))
      {
        _log.Error("Loader temp data was not set for a node.");
        return;
      }
      if (obtemp == null)
      {
        _log.Error("Loader temp data was not set for a node.");
        return;
      }
      var node = obtemp._gltfNode;
      if (node.Mesh == null)
      {
        _log.Warn($"'{node.Name}' had no mesh.");
        return;
      }

      //Get the mesh at the index. 
      var mesh = _myModel.Meshes[node.Mesh.Value];

      if (mesh.Primitives.Length == 0)
      {
        _log.Error("No mesh primitives were available for mesh..");
        return;
      }
      if (mesh.Primitives.Length > 1)
      {
        _log.Warn("Only one mesh primitive of triangles is supported.");
      }
      var prim = mesh.Primitives[0];
      LoadGLTFMesh(mesh, prim, wo);

      //Material
      LoadGLTFMaterial(prim, wo);
    }
    private void LoadGLTFMesh(glTFLoader.Schema.Mesh? mesh, glTFLoader.Schema.MeshPrimitive prim, WorldObject wo)
    {
      //https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#meshes-overview
      //https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#skinned-mesh-attributes
      //The number of joints that influence one vertex is limited to 4 per set
      const string position_att = "POSITION";
      const string normal_att = "NORMAL";
      const string texcoord0_att = "TEXCOORD_0";
      const string color0_att = "COLOR_0";
      const string texcoord1_att = "TEXCOORD_1";
      const string texcoord2_att = "TEXCOORD_2";
      const string texcoord3_att = "TEXCOORD_3";
      const string texcoord4_att = "TEXCOORD_4";
      const string tangent_att = "TANGENT";
      const string joints_att = "JOINTS_";
      const string weights_att = "WEIGHTS_";

      _log.Debug($"Parsing mesh '{mesh.Name}'");

      glTFLoader.Schema.MeshPrimitive.ModeEnum mode = prim.Mode;
      OpenTK.Graphics.OpenGL4.PrimitiveType mesh_prim_type = OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles;
      if (mode != glTFLoader.Schema.MeshPrimitive.ModeEnum.TRIANGLES)
      {
        _log.Error("Primitive mode for mesh not supported..");
        return;
      }

      MeshTemp mt = new MeshTemp();
      foreach (var attr in prim.Attributes)
      {
        //3.7.2.1. Overview - these are all valid as part of the spec
        //https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html
        string attr_name = attr.Key;

        if (attr_name.Equals(position_att))
        {
          mt.positions = ParseBuf_Vec3f(attr.Value, attr_name);
        }
        else if (attr_name.Equals(tangent_att))
        {
          mt.tangents = ParseBuf_Vec3f(attr.Value, attr_name);
        }
        else if (attr_name.Equals(normal_att))
        {
          mt.normals = ParseBuf_Vec3f(attr.Value, attr_name);
        }
        else if (attr_name.Equals(texcoord0_att))
        {
          mt.texs_0 = ParseBuf_Vec2f(attr.Value, attr_name);
        }
        else if (attr_name.Equals(color0_att))
        {
          _log.Warn("Ignoring Color atrib");
        }
        else if (attr_name.StartsWith(joints_att))
        {
          int idx = Int32.Parse(attr_name.Substring(joints_att.Length, 1));
          Gu.Assert(idx < MAX_JOINTS_OR_WEIGHTS_BUFFERS);

          if (mt.jointsn[idx] == null)
          {
            mt.jointsn[idx] = new List<Svec4>();
          }

          ReadJointData(mt.jointsn[idx], attr_name, attr.Value);
          mt.has_joints = true;
        }
        else if (attr_name.StartsWith(weights_att))
        {
          int idx = Int32.Parse(attr_name.Substring(weights_att.Length, 1));
          Gu.Assert(idx < MAX_JOINTS_OR_WEIGHTS_BUFFERS);

          if (mt.weightsn[idx] == null)
          {
            mt.weightsn[idx] = new List<vec4>();
          }
          mt.weightsn[idx] = ParseBuf_Vec4f(attr.Value, attr_name).ToList();
          mt.has_weights = true;
        }
        else
        {
          _log.Error($"'{attr_name}' Invalid attribute name or not supported.");
          Gu.BRThrowNotImplementedException();
        }
      }

      SkinWeights? sw = null;
      if (mt.has_joints && mt.has_weights)
      {
        sw = BuildSkin(mesh.Name, mt);
      }

      ReadIndexes(prim, out var indices_ushort, out var indices_uint);

      if (mt.positions == null)
      {
        _log.Error("No position information specified on mesh.");
      }
      else if (mt.normals != null && mt.positions.Length != mt.normals.Length)
      {
        _log.Error($"normals ordinal {mt.tangents.Length} did not equal position {mt.positions.Length}");
      }
      else if (mt.texs_0 != null && mt.positions.Length != mt.texs_0.Length)
      {
        _log.Error($"texs_0 ordinal {mt.tangents.Length} did not equal position {mt.positions.Length}");
      }
      else if (mt.tangents != null && mt.positions.Length != mt.tangents.Length)
      {
        _log.Error($"Tangents ordinal {mt.tangents.Length} did not equal position {mt.positions.Length}");
      }
      else
      {
        FillGLTFMeshData(wo, mesh_prim_type, mesh.Name, indices_ushort, indices_uint, mt.positions, mt.normals, mt.texs_0, mt.tangents, sw);
      }
    }
    private void ReadJointData(List<Svec4> jointsn, string attr_name, int attr_idx)
    {
      //"The number of joints that influence one vertex is limited to 4 per set, so the referenced accessors MUST have VEC4 type and following component types:
      //  JOINTS_n: unsigned byte or unsigned short
      //  WEIGHTS_n: float, or normalized unsigned byte, or normalized unsigned short"
      //"When any of the vertices are influenced by more than four joints, the additional joint and weight information are
      // stored in subsequent sets. For example, JOINTS_1 and WEIGHTS_1 if present will reference the accessor for up to 4 
      // additional joints that influence the vertices."
      //https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#skinned-mesh-attributes

      if (GetAccessor(attr_idx, out var joint_acc))
      {
        //for GLTF joints type is always vec4 
        Gu.Assert(joint_acc.Type == GLTF_DATA_TYPE.VEC4);

        //shorts should be big enough for all joint indexes. technically bytes would be, but 255 is somewhat a low number.
        if (joint_acc.ComponentType == GLTF_COMP_TYPE.UNSIGNED_BYTE)
        {
          if (AccessBuffer(attr_idx, attr_name, GLTF_DATA_TYPE.VEC4, GLTF_COMP_TYPE.UNSIGNED_BYTE, out int count, out int byteoff))
          {
            var vals = SerializeTools.DeserializeFrom<vec4ub>(_gltf_data, byteoff, count);
            foreach (var val in vals)
            {
              jointsn.Add(new Svec4((ushort)val.x, (ushort)val.y, (ushort)val.z, (ushort)val.w));
            }
          }
        }
        else if (joint_acc.ComponentType == GLTF_COMP_TYPE.UNSIGNED_SHORT)
        {
          if (AccessBuffer(attr_idx, attr_name, GLTF_DATA_TYPE.VEC4, GLTF_COMP_TYPE.UNSIGNED_SHORT, out int count, out int byteoff))
          {
            var vals = SerializeTools.DeserializeFrom<Svec4>(_gltf_data, byteoff, count);
            foreach (var val in vals)
            {
              jointsn.Add(new Svec4((ushort)val.x, (ushort)val.y, (ushort)val.z, (ushort)val.w));
            }
          }
        }
        else if (joint_acc.ComponentType == GLTF_COMP_TYPE.UNSIGNED_INT)
        {
          if (AccessBuffer(attr_idx, attr_name, GLTF_DATA_TYPE.VEC4, GLTF_COMP_TYPE.UNSIGNED_INT, out int count, out int byteoff))
          {
            var vals = SerializeTools.DeserializeFrom<uvec4>(_gltf_data, byteoff, count);
            foreach (var val in vals)
            {
              Gu.Assert(val.x < ushort.MaxValue);
              Gu.Assert(val.y < ushort.MaxValue);
              Gu.Assert(val.z < ushort.MaxValue);
              Gu.Assert(val.w < ushort.MaxValue);
              jointsn.Add(new Svec4((ushort)val.x, (ushort)val.y, (ushort)val.z, (ushort)val.w));
            }
          }
        }
        else
        {
          //unhandled type
          _log.Error($"Unhandled joint component type '{joint_acc.ComponentType.ToString()}'");
        }

      }
    }
    private void ReadIndexes(glTFLoader.Schema.MeshPrimitive prim, out ushort[] indices_ushort, out uint[] indices_uint)
    {
      indices_ushort = null;
      indices_uint = null;

      if (prim.Indices == null || prim.Indices.Value > _myModel.Accessors.Length)
      {
        _log.Error("Mesh had no index data or outside bounds. This is not supported.");
        return;
      }

      var index_accessor = _myModel.Accessors[prim.Indices.Value];
      int? bufferViewID = index_accessor.BufferView;
      if (bufferViewID != null && bufferViewID.Value < _myModel.BufferViews.Length)
      {
        //According to the spec target should not be null - element array or array buf
        var buffer_view = _myModel.BufferViews[bufferViewID.Value];
        Gu.Assert(buffer_view.ByteStride == null || buffer_view.ByteStride.Value == 0);
        Gu.Assert(buffer_view.Buffer == 0);//TODO:
        if (index_accessor.ComponentType == GLTF_COMP_TYPE.UNSIGNED_SHORT)
        {
          indices_ushort = SerializeTools.DeserializeFrom<ushort>(_gltf_data, buffer_view.ByteOffset, index_accessor.Count);
        }
        else if (index_accessor.ComponentType == GLTF_COMP_TYPE.UNSIGNED_INT)
        {
          indices_uint = SerializeTools.DeserializeFrom<uint>(_gltf_data, buffer_view.ByteOffset, index_accessor.Count);
        }
        else
        {
          _log.Error($"Invalid index type '{index_accessor.ComponentType}'");
          return;
        }
      }
    }
    private SkinWeights? BuildSkin(string meshname, MeshTemp mt)
    {
      //Build the GPU skin buffers
      //these are ordinal by vertex id
      Gu.Assert(mt.weightsn != null);
      Gu.Assert(mt.jointsn != null);
      Gu.Assert(mt.positions != null);

      SkinWeights? sw = null;

      try
      {
        int vcount = mt.positions.Length;
        var offs = new wd_in_st[vcount];
        var weights = new jw_in_st[0];
        int woff = 0;
        for (int vi = 0; vi < vcount; ++vi)
        {
          int vjcount = 0;
          for (int jbufi = 0; jbufi < mt.jointsn.Length; jbufi++)
          {
            var jbuf = mt.jointsn[jbufi];
            var wbuf = mt.weightsn[jbufi];
            if (jbuf != null)
            {
              Gu.Assert(jbuf.Count == vcount);
              Gu.Assert(wbuf != null);
              Gu.Assert(wbuf.Count == jbuf.Count);

              if (jbuf[vi].x > 0 && wbuf[vi].x > 0) { weights.Append(new jw_in_st() { joff = jbuf[vi].x, wt = wbuf[vi].x }); vjcount++; }
              if (jbuf[vi].y > 0 && wbuf[vi].y > 0) { weights.Append(new jw_in_st() { joff = jbuf[vi].y, wt = wbuf[vi].y }); vjcount++; }
              if (jbuf[vi].z > 0 && wbuf[vi].z > 0) { weights.Append(new jw_in_st() { joff = jbuf[vi].z, wt = wbuf[vi].z }); vjcount++; }
              if (jbuf[vi].w > 0 && wbuf[vi].w > 0) { weights.Append(new jw_in_st() { joff = jbuf[vi].w, wt = wbuf[vi].w }); vjcount++; }
            }
          }

          offs.Append(new wd_in_st() { wc = vjcount, wo = woff });

          woff += vjcount;
        }

        if (weights.Length > 0)
        {
          sw = new SkinWeights(meshname, offs, weights);
        }

      }
      catch (Exception ex)
      {
        _log.Error("error - " + Gu.GetAllException(ex));
        sw = null;
      }

      return sw;
    }
    private void FillGLTFMeshData(WorldObject wo, OpenTK.Graphics.OpenGL4.PrimitiveType mesh_prim_type, string mesh_name,
         ushort[]? indices_ushort, uint[]? indices_uint, vec3[] positions, vec3[]? normals, vec2[]? texs_0, vec3[]? tangents, SkinWeights? sw)
    {
      Gu.Assert(positions != null);

      //honestly
      bool flip_tris = true;// _temp._worldobj_temp[wo]._info._flipTris;

      //Pack everything
      v_v3n3x2t3u1[] verts = new v_v3n3x2t3u1[positions.Length];
      for (int ivert = 0; ivert < positions.Length; ivert++)
      {
        verts[ivert] = new v_v3n3x2t3u1();
        verts[ivert]._v = positions[ivert];
        if (normals != null)
        {
          verts[ivert]._n = normals[ivert];
        }
        if (texs_0 != null)
        {
          verts[ivert]._x = texs_0[ivert];
        }
        if (tangents != null)
        {
          verts[ivert]._t = tangents[ivert];
        }
      }

      //Fill mesh / flip tris.
      if (indices_uint == null && indices_ushort == null)
      {
        int vlen = verts.Length;
        if (vlen % 3 != 0)
        {
          _log.Error($"Uneven vertex count %3!=0 '{verts.Length}'.");
          return;
        }
        if (flip_tris)
        {
          for (int vi = 0; vi < vlen; vi += 3)
          {
            v_v3n3x2t3u1 vert = verts[vi];
            verts[vi] = verts[vi + 1];
            verts[vi + 1] = vert;
          }
        }
      }
      else if (indices_uint != null)
      {
        if (flip_tris)
        {
          for (int vi = 0; vi < indices_uint.Length; vi += 3)
          {
            uint idx = indices_uint[vi];
            indices_uint[vi] = indices_uint[vi + 1];
            indices_uint[vi + 1] = idx;
          }
        }
      }
      else if (indices_ushort != null)
      {
        if (flip_tris)
        {
          for (int vi = 0; vi < indices_ushort.Length; vi += 3)
          {
            ushort idx = indices_ushort[vi];
            indices_ushort[vi] = indices_ushort[vi + 1];
            indices_ushort[vi + 1] = idx;
          }
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      //Fill buffers
      MeshData? md = null;
      if (indices_uint == null && indices_ushort == null)
      {
        //no indices
        var fd = MeshGen.ComputeNormalsAndTangents(verts, null, normals == null, tangents == null);
        md = new MeshData(mesh_name, mesh_prim_type,
          Gpu.CreateVertexBuffer(mesh_name, verts),
          Gpu.CreateVertexBuffer(mesh_name, fd),
          Gpu.CreateShaderStorageBuffer(mesh_name, fd),
          true);
      }
      else
      {
        //indices
        var fd = MeshGen.ComputeNormalsAndTangents(verts, indices_uint != null ? indices_uint : indices_ushort.AsUIntArray(), normals == null, tangents == null);
        md = new MeshData(Gu.Lib.GetUniqueName(ResourceType.MeshData, mesh_name), mesh_prim_type,
          Gpu.CreateVertexBuffer(mesh_name, verts),
          indices_uint != null ? Gpu.CreateIndexBuffer(mesh_name, indices_uint) : Gpu.CreateIndexBuffer(mesh_name, indices_ushort),
          Gpu.CreateShaderStorageBuffer(mesh_name, fd),
          true);
      }

      //Make view and skin
      if (_log.Assert(md != null))
      {
        wo.MeshView = new MeshView(md);
        wo.MeshView.DataSource = this;
        wo.MeshView.MeshData.DataSource = this;
        md.SkinWeights = sw;
      }

    }
    private void LoadGLTFSkin()
    {
      //Fill out armature data and joint data for previously created nodes
      //https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-skin
      if (_myModel.Skins != null)
      {
        int skinid = 0;
        foreach (var skin in _myModel.Skins)
        {
          var dat = _temp._worldobj_temp.Where((x) => x.Key is Armature && x.Key.Name == skin.Name).FirstOrDefault();
          Gu.Assert(dat.Key != null);
          var wo_obj = dat.Key;
          Gu.Assert(wo_obj is Armature);
          var arm = wo_obj as Armature;
          if (arm.Data == null)
          {
            arm.Data = new ArmatureData(skin.Name);
          }
          var jids = _temp._arm_to_jointid[arm];

          arm.Data.InvBinds = ParseBuf_Mat4(skin.InverseBindMatrices.Value, "InverseBindMatrices");
          arm.Data.JointAnims = new AnimationData[jids.Count];
          arm.Data.JointParents = _temp._arm_joint_jointid_to_parentid[arm].ToArray();

          Gu.Assert(jids.Count == arm.Data.InvBinds.Length);
          Gu.Assert(jids.Count == arm.Data.JointParents.Length);

          //add skin modifier to objs that are bound to this armature "skin"
          foreach (var skinned in _temp._worldobj_temp)
          {
            if (skinned.Value._gltfSkinId == skinid)
            {
              skinned.Key.Modifiers = skinned.Key.Modifiers.ConstructIfNeeded();
              skinned.Key.Modifiers.Add(new ArmatureModifier(arm));
            }
          }

          skinid++;
        }
      }
    }
    private void LoadGLTFMaterial(glTFLoader.Schema.MeshPrimitive prim, WorldObject wo)
    {
      wo.Material = null;
      if (prim.Material != null)
      {
        var ind = prim.Material.Value;
        var mat = _myModel.Materials[ind];
        if (mat != null)
        {
          wo.Material = new Material(Gu.Lib.GetUniqueName(ResourceType.Material, mat.Name), Gu.Lib.GetShader(Rs.Shader.DefaultObjectShader));
          wo.Material.DataSource = this;

          if (mat.OcclusionTexture != null)
          {
            _log.Error("occlusion tex Not supoported");
          }
          if (mat.EmissiveTexture != null)
          {
            _log.Error("emissive tex Not supoported");
          }
          if (mat.NormalTexture != null)
          {
            LoadGLTFTexture(mat, mat.NormalTexture.Index, wo, wo.Material.NormalSlot);
          }
          wo.Material.DoubleSided = mat.DoubleSided;
          if (mat.PbrMetallicRoughness != null)
          {
            wo.Material.Roughness = mat.PbrMetallicRoughness.RoughnessFactor;
            wo.Material.Metallic = mat.PbrMetallicRoughness.MetallicFactor;
            wo.Material.AlphaMode = mat.AlphaMode;
            wo.Material.BaseColor = new vec4(
               mat.PbrMetallicRoughness.BaseColorFactor[0],
               mat.PbrMetallicRoughness.BaseColorFactor[1],
               mat.PbrMetallicRoughness.BaseColorFactor[2],
               mat.PbrMetallicRoughness.BaseColorFactor[3]);

            if (mat.PbrMetallicRoughness.BaseColorTexture != null)
            {
              LoadGLTFTexture(mat, mat.PbrMetallicRoughness.BaseColorTexture.Index, wo, wo.Material.AlbedoSlot);
            }
            if (mat.PbrMetallicRoughness.MetallicRoughnessTexture != null)
            {
              LoadGLTFTexture(mat, mat.PbrMetallicRoughness.MetallicRoughnessTexture.Index, wo, wo.Material.RoughnessSlot);
            }
          }
        }
      }

      if (wo.Material == null)
      {
        wo.Material = Gu.Lib.GetMaterial(Rs.Material.DefaultObjectMaterial);
      }
    }
    private void LoadGLTFTexture(glTFLoader.Schema.Material mat, int tind, WorldObject root, TextureSlot slot)
    {
      Gu.Assert(slot != null);
      Gu.Assert(root != null);
      Gu.Assert(_myModel != null);
      Gu.Assert(_myModel.Textures != null && _myModel.Textures.Length >= tind);
      var md_tex = _myModel.Textures[tind];

      if (md_tex.Source != null)
      {
        var md_img = _myModel.Images[md_tex.Source.Value];
        if (md_img.BufferView != null)
        {
          TextureWrapMode ws = TextureWrapMode.Repeat;
          TextureWrapMode wt = TextureWrapMode.Repeat;
          TextureMinFilter minf = TextureMinFilter.Linear;
          TextureMagFilter magf = TextureMagFilter.Linear;
          LoadGLTFSamplerOrDefaults(md_tex, out ws, out wt, out minf, out magf);

          var bv = _myModel.BufferViews[md_img.BufferView.Value];
          if (!_log.Assert(bv.ByteStride == null || bv.ByteStride.Value == 0))
          {
            return;
          }
          if (!_log.Assert(bv.Buffer == 0))
          {
            return;
          }
          var imgData = new byte[bv.ByteLength];
          System.Buffer.BlockCopy(_gltf_data, bv.ByteOffset, imgData, 0, bv.ByteLength);
          Image? m = ImageFile.LoadImageRaw(imgData, md_img.Name);//Could use URI here, but it's not specified in a packed GLB
          string name = md_img.Name;
#if DEBUG
          {
            var p = System.IO.Path.Combine(Gu.LocalTmpPath, name + ".png");
            _log.Debug("Saving debug image " + p.ToString());
            Lib.SaveImage(p, m, true);
          }
#endif
          if (md_img.Name.ToLower().Contains("bump"))
          {
            _log.Debug("Converting bump map to normal map because it contains 'bump' in the name.. " + md_img.Name);
            m = m.CreateNormalMap(true);
            name = name + "-normalized";
#if DEBUG
            {
              var p = System.IO.Path.Combine(Gu.LocalTmpPath, name + ".png");
              _log.Debug("Saving debug image " + p.ToString());
              Lib.SaveImage(p, m, true);
            }
#endif
          }

          slot.Texture = new Texture(name, m, true, minf, magf, ws, wt);
          slot.Texture.DataSource = this;
        }
        else
        {
          _log.Error($"Bufferview was null for image texture {md_img.Name}. Possibly disk texture? (uri={md_img.Uri})");
        }
      }
      else
      {
        _log.Error("Texture source was null");
      }
    }
    private void LoadGLTFSamplerOrDefaults(glTFLoader.Schema.Texture? md_tex, out TextureWrapMode ws, out TextureWrapMode wt, out TextureMinFilter minf, out TextureMagFilter magf)
    {
      ws = TextureWrapMode.Repeat;
      wt = TextureWrapMode.Repeat;
      minf = TextureMinFilter.Linear;
      magf = TextureMagFilter.Linear;

      if (md_tex.Sampler != null)
      {
        var samp = _myModel.Samplers[md_tex.Sampler.Value];

        if (samp.WrapS != null)
        {
          if (samp.WrapS == glTFLoader.Schema.Sampler.WrapSEnum.REPEAT) { ws = TextureWrapMode.Repeat; }
          else if (samp.WrapS == glTFLoader.Schema.Sampler.WrapSEnum.CLAMP_TO_EDGE) { ws = TextureWrapMode.ClampToEdge; }
          else if (samp.WrapS == glTFLoader.Schema.Sampler.WrapSEnum.MIRRORED_REPEAT) { ws = TextureWrapMode.MirroredRepeat; }
          else { Gu.BRThrowNotImplementedException(); }
        }
        if (samp.WrapT != null)
        {
          if (samp.WrapT == glTFLoader.Schema.Sampler.WrapTEnum.REPEAT) { wt = TextureWrapMode.Repeat; }
          else if (samp.WrapT == glTFLoader.Schema.Sampler.WrapTEnum.CLAMP_TO_EDGE) { wt = TextureWrapMode.ClampToEdge; }
          else if (samp.WrapT == glTFLoader.Schema.Sampler.WrapTEnum.MIRRORED_REPEAT) { wt = TextureWrapMode.MirroredRepeat; }
          else { Gu.BRThrowNotImplementedException(); }
        }

        if (samp.MinFilter != null)
        {
          if (samp.MinFilter.Value == glTFLoader.Schema.Sampler.MinFilterEnum.LINEAR) { minf = TextureMinFilter.Linear; }
          else if (samp.MinFilter.Value == glTFLoader.Schema.Sampler.MinFilterEnum.LINEAR_MIPMAP_LINEAR) { minf = TextureMinFilter.LinearMipmapLinear; }
          else if (samp.MinFilter.Value == glTFLoader.Schema.Sampler.MinFilterEnum.LINEAR_MIPMAP_NEAREST) { minf = TextureMinFilter.LinearMipmapNearest; }
          else if (samp.MinFilter.Value == glTFLoader.Schema.Sampler.MinFilterEnum.NEAREST) { minf = TextureMinFilter.Nearest; }
          else if (samp.MinFilter.Value == glTFLoader.Schema.Sampler.MinFilterEnum.NEAREST_MIPMAP_LINEAR) { minf = TextureMinFilter.NearestMipmapLinear; }
          else if (samp.MinFilter.Value == glTFLoader.Schema.Sampler.MinFilterEnum.NEAREST_MIPMAP_NEAREST) { minf = TextureMinFilter.NearestMipmapNearest; }
          else { Gu.BRThrowNotImplementedException(); }
        }
        if (samp.MagFilter != null)
        {
          if (samp.MagFilter.Value == glTFLoader.Schema.Sampler.MagFilterEnum.LINEAR) { magf = TextureMagFilter.Linear; }
          else if (samp.MagFilter.Value == glTFLoader.Schema.Sampler.MagFilterEnum.NEAREST) { magf = TextureMagFilter.Nearest; }
          else { Gu.BRThrowNotImplementedException(); }
        }

      }

    }

    #endregion
    #region GLTF Data Handling

    private bool GetAccessor(int accessor_index, out glTFLoader.Schema.Accessor? accessor)
    {
      accessor = null;
      if (accessor_index < _myModel.Accessors.Length)
      {
        accessor = _myModel.Accessors[accessor_index];
        Gu.Assert(accessor != null);
        return true;
      }
      else
      {
        _log.Error("attribute index outside of accessor");
        return false;
      }
    }
    private bool AccessBuffer(int accessor_index, string name, GLTF_DATA_TYPE datatype, GLTF_COMP_TYPE compType, out int count, out int byteoffset)
    {
      count = 0;
      byteoffset = 0;
      if (!GetAccessor(accessor_index, out var accessor))
      {
        return false;
      }
      if (accessor.ComponentType != compType)
      {
        _log.Error("Attribute: " + name + " - Invalid component type");
      }
      if (accessor.Type != datatype)
      {
        _log.Error("Attribute: " + name + " - Invalid data type");
      }
      int? bufferViewID = _myModel.Accessors[accessor_index].BufferView;
      if (bufferViewID != null && bufferViewID.Value < _myModel.BufferViews.Length)
      {
        var buffer_view = _myModel.BufferViews[bufferViewID.Value];
        Gu.Assert(buffer_view.ByteStride == null || buffer_view.ByteStride.Value == 0);
        Gu.Assert(buffer_view.Buffer == 0);
        if (buffer_view != null)
        {
          count = accessor.Count;
          byteoffset = buffer_view.ByteOffset;
          return true;
        }
        else
        {
          _log.Error("Buffer view was null.");
        }
      }
      else
      {
        _log.Error("Buffer view ID was null, or out of bounds.");
      }

      return false;
    }
    private vec2[] ParseBuf_Vec2f(int accessor_index, string attr_name)
    {
      vec2[]? vals = null;
      if (AccessBuffer(accessor_index, attr_name, GLTF_DATA_TYPE.VEC2, GLTF_COMP_TYPE.FLOAT, out int count, out int byteoff))
      {
        vals = SerializeTools.DeserializeFrom<vec2>(_gltf_data, byteoff, count);
      }
      return vals;
    }
    private vec3[] ParseBuf_Vec3f(int accessor_index, string attr_name)
    {
      vec3[]? vals = null;
      if (AccessBuffer(accessor_index, attr_name, GLTF_DATA_TYPE.VEC3, GLTF_COMP_TYPE.FLOAT, out int count, out int off))
      {
        vals = SerializeTools.DeserializeFrom<vec3>(_gltf_data, off, count);
      }
      return vals;
    }
    private vec4[] ParseBuf_Vec4f(int accessor_index, string attr_name)
    {
      vec4[]? vals = null;
      if (AccessBuffer(accessor_index, attr_name, GLTF_DATA_TYPE.VEC4, GLTF_COMP_TYPE.FLOAT, out int count, out int off))
      {
        vals = SerializeTools.DeserializeFrom<vec4>(_gltf_data, off, count);
      }
      return vals;
    }
    private mat4[] ParseBuf_Mat4(int accessor_index, string attr_name)
    {
      mat4[]? vals = null;
      if (AccessBuffer(accessor_index, attr_name, GLTF_DATA_TYPE.MAT4, GLTF_COMP_TYPE.FLOAT, out int count, out int off))
      {
        vals = SerializeTools.DeserializeFrom<mat4>(_gltf_data, off, count);
      }
      return vals;
    }


    #endregion

  }//cls
  // [DataContract]
  // public class AnimationLoader : DataSource
  // {
  //   public AnimationLoader(string name) : base(name, SourceFormat.Generated)
  //   {
  //     throw new NotImplementedException();
  //   }
  //   protected override DataBlock? Create(string name)
  //   {
  //     throw new NotImplementedException();
  //     return null;
  //   }
  //   protected override void Destroy()
  //   {
  //     throw new NotImplementedException();
  //   }
  // }
  // [DataContract]
  // public class MeshDataLoader : DataSource
  // {
  //   public MeshDataLoader(string name) : base(name, SourceFormat.Generated)
  //   {
  //     throw new NotImplementedException();
  //   }
  //   protected override DataBlock? Create(string name)
  //   {
  //     throw new NotImplementedException();
  //     return null;
  //   }
  //   protected override void Destroy()
  //   {
  //     throw new NotImplementedException();
  //   }
  // }
  [DataContract]
  public abstract class MeshGenParams
  {
    //Default model format.
    [DataMember] private GPUDataFormat _vertexFormat = GPUDataFormat.GetDataFormat<v_v3n3x2t3u1>();
    public GPUDataFormat VertexFormat { get { return _vertexFormat; } }
    public abstract MeshData Generate(string name);
    public MeshGenParams()
    {
    }
  }
  [DataContract]
  public class MeshGenPlaneParams : MeshGenParams
  {
    [DataMember] public float _w;
    [DataMember] public float _h;
    [DataMember] public vec2[]? _side = null;
    public override MeshData Generate(string name)
    {
      return MeshGen.GenPlane(name, _w, _h, _side);
    }
  }
  [DataContract]
  public class MeshGenEllipsoidParams : MeshGenParams
  {
    //Ellipsoid, or sphere, or ellip--whatever
    [DataMember] public vec3 _radius = new vec3(1, 1, 1);
    [DataMember] public int _slices = 128;
    [DataMember] public int _stacks = 128;
    [DataMember] public bool _smooth = false;
    [DataMember] public bool _flip_tris = false;

    public override MeshData Generate(string name)
    {
      return MeshGen.GenEllipsoid(name, _radius, _slices, _stacks, _smooth, _flip_tris);
    }
  }
  [DataContract]
  public class MeshGenBoxParams : MeshGenParams
  {
    [DataMember] public float _w = 1;
    [DataMember] public float _h = 1;
    [DataMember] public float _d = 1;
    [DataMember] public vec2[] _top = null;
    [DataMember] public vec2[] _side = null;
    [DataMember] public vec2[] _bot = null;
    [DataMember] public vec3? _translate = null;
    public override MeshData? Generate(string name)
    {
      return MeshGen.GenBox(name, _w, _h, _d, _top, _side, _bot, _translate);
    }
  }
  [DataContract]
  public class MeshGen : DataSource
  {
    private MeshData? _meshData = null;
    [DataMember] MeshGenParams _params = null;

    public MeshData? MeshData { get { return _meshData; } }

    public MeshGen(string name, MeshGenParams mgparams) : base(name, SourceFormat.Generated)
    {
      _params = mgparams;
    }
    public override void OnLoad()
    {
      _meshData = _params.Generate(this.Name);
      _meshData.DataSource = this;
    }
    public override void OnDestroy()
    {
      _meshData = null;
    }

    #region Public Static: Generators

    public static MeshData GenPlane(string name, float w, float h, vec2[]? side = null)
    {
      //Left Righ, Botom top, back front
      vec3[] box = new vec3[4];
      float w2 = w * 0.5f;
      float h2 = h * 0.5f;
      box[0] = new vec3(-w2, 0, -h2);
      box[1] = new vec3(w2, 0, -h2);
      box[2] = new vec3(-w2, 0, h2);
      box[3] = new vec3(w2, 0, h2);

      vec3[] norms = new vec3[1];//lrbtaf
      norms[0] = new vec3(0, 1, 0);

      vec2[] texs = new vec2[4];
      texs[0] = new vec2(0, 0);
      texs[1] = new vec2(1, 0);
      texs[2] = new vec2(0, 1);
      texs[3] = new vec2(1, 1);

      v_v3n3x2t3u1[] verts = new v_v3n3x2t3u1[4];
      verts[0 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[0], _n = norms[0], _x = (side != null) ? side[0] : texs[0] };
      verts[0 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[1], _n = norms[0], _x = (side != null) ? side[1] : texs[1] };
      verts[0 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[2], _n = norms[0], _x = (side != null) ? side[2] : texs[2] };
      verts[0 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[3], _n = norms[0], _x = (side != null) ? side[3] : texs[3] };

      ushort[] qinds = MeshGen.GenerateQuadIndices(verts.Length / 4, false);
      var fd = MeshGen.ComputeNormalsAndTangents(verts, qinds.AsUIntArray());

      var md = new MeshData(name, PrimitiveType.Triangles,
        Gpu.CreateVertexBuffer(name, verts),
        Gpu.CreateIndexBuffer(name, qinds),
        Gpu.CreateShaderStorageBuffer(name, fd)
        );

      md.DataSource = new MeshGen(name, new MeshGenPlaneParams()
      {
        _w = w,
        _h = h,
        _side = side,
      });
      return md;
    }
    public static MeshData GenSphere(string name, float radius, int slices = 128, int stacks = 128, bool smooth = false, bool flip_tris = false)
    {
      return MeshGen.GenEllipsoid(name, new vec3(radius, radius, radius), slices, stacks, smooth, flip_tris);
    }
    public static MeshData GenEllipsoid(string name, vec3 radius, int slices = 128, int stacks = 128, bool smooth = false, bool flip_tris = false)
    {
      v_v3n3x2t3u1[] verts;
      ushort[] inds;

      GenEllipsoid(out verts, out inds, radius, slices, stacks, smooth, flip_tris);

      var fd = MeshGen.ComputeNormalsAndTangents(verts, inds.AsUIntArray(), false, true);

      var md = new MeshData(name, PrimitiveType.Triangles,
        Gpu.CreateVertexBuffer(name, verts),
        Gpu.CreateIndexBuffer(name, inds),
        Gpu.CreateShaderStorageBuffer(name, fd)
        );

      md.DataSource = new MeshGen(name, new MeshGenEllipsoidParams()
      {
        _radius = radius,
        _slices = slices,
        _stacks = stacks,
        _smooth = smooth,
        _flip_tris = flip_tris,
      });

      return md;
    }
    private static void GenEllipsoid(out v_v3n3x2t3u1[] verts, out ushort[] inds, vec3 radius, int slices = 128, int stacks = 128, bool smooth = false, bool flip_tris = false)
    {

      int vcount = slices * stacks * 4;
      verts = new v_v3n3x2t3u1[vcount];

      //Use a 2D grid as a sphere. This is less optimal but doesn't mess up the tex coords.
      for (int stack = 0; stack < stacks; stack++)
      {
        for (int slice = 0; slice < slices; slice++)
        {
          float[] phi = new float[2];
          float[] theta = new float[2];
          phi[0] = MathUtils.M_PI * ((float)stack / (float)stacks);
          phi[1] = MathUtils.M_PI * ((float)(stack + 1) / (float)stacks); //0<phi<pi
          theta[0] = MathUtils.M_2PI * ((float)slice / (float)slices);
          theta[1] = MathUtils.M_2PI * ((float)(slice + 1) / (float)slices);//0<theta<2pi

          int vind = (stack * slices + slice) * 4;
          for (int p = 0; p < 2; ++p)
          {
            for (int t = 0; t < 2; ++t)
            {
              // 2 3
              // 0 1  
              // >x ^y
              int voff = vind + p * 2 + t;

              verts[voff]._v = new vec3(
                  radius.x * MathUtils.sinf(phi[p]) * MathUtils.cosf(theta[t]),
                  radius.y * MathUtils.cosf(phi[p]),
                  radius.z * MathUtils.sinf(phi[p]) * MathUtils.sinf(theta[t])
              );
              verts[voff]._n = verts[voff]._v.normalized();

              //del f = x2/a2+y2/b2+z2/c2=1
              verts[voff]._n = new vec3(
                  2.0f * MathUtils.sinf(phi[p]) * MathUtils.cosf(theta[t]) / radius.x * radius.x,
                  2.0f * MathUtils.cosf(phi[p]) / radius.y * radius.y,
                  2.0f * MathUtils.sinf(phi[p]) * MathUtils.sinf(theta[t]) / radius.z * radius.z
              ).normalized();

            }
          }

          if (smooth)
          {
            //TODO:
            verts[vind + 0]._n = verts[vind + 0]._v.normalized();
            verts[vind + 1]._n = verts[vind + 1]._v.normalized();
            verts[vind + 2]._n = verts[vind + 2]._v.normalized();
            verts[vind + 3]._n = verts[vind + 3]._v.normalized();
          }
          else
          {
            vec3 n = (verts[vind + 1]._v - verts[vind + 0]._v).cross(verts[vind + 2]._v - verts[vind + 0]._v).normalized();
            verts[vind + 0]._n = n;
            verts[vind + 1]._n = n;
            verts[vind + 2]._n = n;
            verts[vind + 3]._n = n;
          }

          //Caps
          float tx0 = (float)slice / (float)slices;
          float ty0 = (float)stack / (float)stacks;
          float tx1 = (float)(slice + 1) / (float)slices;
          float ty1 = (float)(stack + 1) / (float)stacks;
          verts[vind + 0]._x = new vec2(tx0, ty0);
          verts[vind + 1]._x = new vec2(tx1, ty0);
          verts[vind + 2]._x = new vec2(tx0, ty1);
          verts[vind + 3]._x = new vec2(tx1, ty1);

        }
      }

      inds = GenerateQuadIndices(verts.Length / 4, !flip_tris);
    }
    public static void GenBoxVerts(ref v_v3n3x2t3u1[] verts, float w, float h, float d,
    vec2[]? top = null, vec2[]? side = null, vec2[]? bot = null, vec3? translate = null, bool origin_bot_left = false)
    {
      //Create box verts ADDing to the array if is not creatd, or making a new one.
      // translate = translate the box.
      //origin_bot_left - if true, origin is moved to bot left, otherwise it is center

      //Left Righ, Botom top, back front
      vec3[] box = new vec3[8];
      float w2 = w * 0.5f, h2 = h * 0.5f, d2 = d * 0.5f;
      box[0] = new vec3(-w2, -h2, -d2);
      box[1] = new vec3(w2, -h2, -d2);
      box[2] = new vec3(-w2, h2, -d2);
      box[3] = new vec3(w2, h2, -d2);
      box[4] = new vec3(-w2, -h2, d2);
      box[5] = new vec3(w2, -h2, d2);
      box[6] = new vec3(-w2, h2, d2);
      box[7] = new vec3(w2, h2, d2);

      if (origin_bot_left)
      {
        for (var bi = 0; bi < 8; ++bi)
        {
          box[bi].x += w2;
          box[bi].y += h2;
          box[bi].z += d2;
        }
      }

      vec3[] norms = new vec3[6];//lrbtaf
      norms[0] = new vec3(-1, 0, 0);
      norms[1] = new vec3(1, 0, 0);
      norms[2] = new vec3(0, -1, 0);
      norms[3] = new vec3(0, 1, 0);
      norms[4] = new vec3(0, 0, -1);
      norms[5] = new vec3(0, 0, 1);

      vec2[] texs = new vec2[4];
      texs[0] = new vec2(0, 1);
      texs[1] = new vec2(1, 1);
      texs[2] = new vec2(0, 0);
      texs[3] = new vec2(1, 0);

      //     6       7
      // 2      3
      //     4       5
      // 0      1
      int vertCount = 6 * 4;
      int off = 0;
      if (verts == null)
      {
        verts = new v_v3n3x2t3u1[vertCount];//lrbtaf
      }
      else
      {
        off = verts.Length;
        Array.Resize(ref verts, verts.Length + vertCount);
      }
      verts[off + 0 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[4], _n = norms[0], _x = (side != null) ? side[0] : texs[0] };
      verts[off + 0 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[0], _n = norms[0], _x = (side != null) ? side[1] : texs[1] };
      verts[off + 0 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[6], _n = norms[0], _x = (side != null) ? side[2] : texs[2] };
      verts[off + 0 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[2], _n = norms[0], _x = (side != null) ? side[3] : texs[3] };

      verts[off + 1 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[1], _n = norms[1], _x = (side != null) ? side[0] : texs[0] };
      verts[off + 1 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[5], _n = norms[1], _x = (side != null) ? side[1] : texs[1] };
      verts[off + 1 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[3], _n = norms[1], _x = (side != null) ? side[2] : texs[2] };
      verts[off + 1 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[7], _n = norms[1], _x = (side != null) ? side[3] : texs[3] };

      verts[off + 2 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[4], _n = norms[2], _x = (bot != null) ? bot[0] : texs[0] };
      verts[off + 2 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[5], _n = norms[2], _x = (bot != null) ? bot[1] : texs[1] };
      verts[off + 2 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[0], _n = norms[2], _x = (bot != null) ? bot[2] : texs[2] };
      verts[off + 2 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[1], _n = norms[2], _x = (bot != null) ? bot[3] : texs[3] };

      verts[off + 3 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[2], _n = norms[3], _x = (top != null) ? top[0] : texs[0] };
      verts[off + 3 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[3], _n = norms[3], _x = (top != null) ? top[1] : texs[1] };
      verts[off + 3 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[6], _n = norms[3], _x = (top != null) ? top[2] : texs[2] };
      verts[off + 3 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[7], _n = norms[3], _x = (top != null) ? top[3] : texs[3] };

      verts[off + 4 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[0], _n = norms[4], _x = (side != null) ? side[0] : texs[0] };
      verts[off + 4 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[1], _n = norms[4], _x = (side != null) ? side[1] : texs[1] };
      verts[off + 4 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[2], _n = norms[4], _x = (side != null) ? side[2] : texs[2] };
      verts[off + 4 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[3], _n = norms[4], _x = (side != null) ? side[3] : texs[3] };

      verts[off + 5 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[5], _n = norms[5], _x = (side != null) ? side[0] : texs[0] };
      verts[off + 5 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[4], _n = norms[5], _x = (side != null) ? side[1] : texs[1] };
      verts[off + 5 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[7], _n = norms[5], _x = (side != null) ? side[2] : texs[2] };
      verts[off + 5 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[6], _n = norms[5], _x = (side != null) ? side[3] : texs[3] };

      if (translate != null)
      {
        for (var vi = off; vi < verts.Length; vi++)
        {
          verts[vi]._v += translate.Value;
        }
      }
    }
    public static MeshData GenBox(string name, float w, float h, float d, vec2[] top = null, vec2[] side = null, vec2[] bot = null, vec3? translate = null)
    {
      v_v3n3x2t3u1[]? verts = null;
      GenBoxVerts(ref verts, w, h, d, top, side, bot, translate);

      ushort[] qinds = GenerateQuadIndices(verts.Length / 4, false);
      var fd = ComputeNormalsAndTangents(verts, qinds.AsUIntArray());

      var md = new MeshData(name, PrimitiveType.Triangles,
        Gpu.CreateVertexBuffer(name, verts),
        Gpu.CreateIndexBuffer(name, qinds),
        Gpu.CreateShaderStorageBuffer(name, fd)
        );

      md.DataSource = new MeshGen(name, new MeshGenBoxParams()
      {
        _w = w,
        _h = h,
        _d = d,
        _top = top,
        _side = side,
        _bot = bot,
        _translate = translate,
      });
      return md;
    }
    public static MeshData CreateScreenQuadMesh(string name, float fw, float fh)
    {
      v_v3x2[] verts = new v_v3x2[] {
        new v_v3x2() { _v = new vec3(0, 0, 0), _x = new vec2(0, 1) } ,
        new v_v3x2() { _v = new vec3(fw, 0, 0), _x = new vec2(1, 1)} ,
        new v_v3x2() { _v = new vec3(0, fh, 0), _x = new vec2(0, 0)} ,
        new v_v3x2() { _v = new vec3(fw, fh, 0), _x = new vec2(1, 0)} ,
      };

      ushort[] inds = new ushort[] { 0, 1, 3, 0, 3, 2, };

      return new MeshData(name, PrimitiveType.Triangles,
        Gpu.CreateVertexBuffer(name, verts.ToArray()),
        Gpu.CreateIndexBuffer(name, inds),
        null,
        true
        );
    }

    #endregion
    #region Public Static: Addl

    public static GpuFaceData[] ComputeNormalsAndTangents(object verts_in, uint[] inds = null, bool doNormals = true, bool doTangents = true)
    {
      Gu.Assert(inds != null);
      Gu.Assert(verts_in != null);

      VertexPointer verts = new VertexPointer(verts_in);

      GpuFaceData[] faceData = null;
      if (inds.Length == 0)
      {
        return null;
      }
      int ilen = (inds != null ? inds.Length : verts.Length);
      if (ilen % 3 != 0)
      {
        Gu.Log.Error("Index or vertex length was not a multiple of 3");
      }
      faceData = new GpuFaceData[ilen / 3];
      float[] count = new float[verts.Length];
      for (int vi = 0; vi < verts.Length; vi++)
      {
        count[vi] = 0;
        if (doNormals)
        {
          verts[vi]._n = vec3.Zero;
        }
        if (doTangents)
        {
          verts[vi]._t = vec3.Zero;
        }
      }
      for (int vi = 0; vi < ilen; vi += 3)
      {
        int vi0 = 0, vi1 = 0, vi2 = 0;
        if (inds != null)
        {
          vi0 = (int)inds[vi + 0];
          vi1 = (int)inds[vi + 1];
          vi2 = (int)inds[vi + 2];
        }
        else
        {
          vi0 = vi + 0;
          vi1 = vi + 1;
          vi2 = vi + 2;
        }

        vec3 out_n, out_t;
        MeshGen.ComputeNormalAndTangent(
          verts[vi0]._v, verts[vi1]._v, verts[vi2]._v,
          verts[vi0]._x, verts[vi1]._x, verts[vi2]._x,
          out out_n, out out_t);
        uint faceId = (uint)(vi / 3);

        faceData[faceId]._index = faceId;
        faceData[faceId]._normal = out_n;
        faceData[faceId]._tangent = out_t;

        if (doNormals)
        {
          verts[vi0]._n += out_n;
          verts[vi1]._n += out_n;
          verts[vi2]._n += out_n;
        }
        if (doTangents)
        {
          verts[vi0]._t += out_t;
          verts[vi1]._t += out_t;
          verts[vi2]._t += out_t;
        }
        verts[vi0]._u = faceId;
        verts[vi1]._u = faceId;
        verts[vi2]._u = faceId;

        count[vi0] += 1;
        count[vi1] += 1;
        count[vi2] += 1;
      }

      //Average vertex normals and tangents
      for (int vi = 0; vi < verts.Length; vi++)
      {
        if (doTangents)
        {
          if (count[vi] > 0)
          {
            verts[vi]._t = (verts[vi]._t / count[vi]).normalize();
          }
          else
          {
            verts[vi]._t = vec3.Zero;
          }
        }
        if (doNormals)
        {
          if (count[vi] > 0)
          {
            verts[vi]._n = (verts[vi]._n / count[vi]).normalize();
          }
          else
          {
            verts[vi]._n = vec3.Zero;
          }
        }
      }

      return faceData;
    }
    public static void ComputeNormalAndTangent(vec3 p0, vec3 p1, vec3 p2, vec2 x0, vec2 x1, vec2 x2, out vec3 normal, out vec3 tangent)
    {
      //https://learnopengl.com/Advanced-Lighting/Normal-Mapping
      vec3 dp0 = p1 - p0;
      vec3 dp1 = p2 - p0;
      vec2 dx0 = x1 - x0;
      vec2 dx1 = x2 - x0;

      normal = dp1.cross(dp0).normalize();

      float f = 1.0f / (dx0.x * dx1.y - dx1.x * dx0.y);
      tangent = new vec3();
      tangent.x = f * (dx1.y * dp0.x - dx0.y * dp1.x);
      tangent.y = f * (dx1.y * dp0.y - dx0.y * dp1.y);
      tangent.z = f * (dx1.y * dp0.z - dx0.y * dp1.z);
      tangent.normalize();
    }
    public static ushort[] GenerateQuadIndices(int numQuads, bool flip = false)
    {
      //Generate proper winding quad indexes
      //0  1
      //2  3
      ushort idx = 0;
      ushort[] inds = new ushort[numQuads * 6];
      for (int face = 0; face < numQuads; ++face)
      {
        inds[face * 6 + 0] = (ushort)(idx + 0);
        inds[face * 6 + 1] = (ushort)(idx + (flip ? 2 : 3));
        inds[face * 6 + 2] = (ushort)(idx + (flip ? 3 : 2));
        inds[face * 6 + 3] = (ushort)(idx + 0);
        inds[face * 6 + 4] = (ushort)(idx + (flip ? 3 : 1));
        inds[face * 6 + 5] = (ushort)(idx + (flip ? 1 : 3));
        idx += 4;
      }
      return inds;
    }

    #endregion


  }

}