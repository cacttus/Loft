using System;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.Serialization;
//File Loaders / Generators / Data sources
namespace PirateCraft
{
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
    public enum LoadState
    {
      None,
      Loaded,
      Unloaded
    }

    [DataMember] private SourceFormat _format = SourceFormat.File;
    private LoadState _state = LoadState.None;
    private int _dbgCreateCount = 0;//just debug

    public LoadState State { get { return _state; } protected set { _state = value; } }

    protected DataSource() { }//clone/serialize
    public DataSource(string name, SourceFormat type) : base(name)
    {
      _format = type;
    }

    public T? Load<T>(string name) where T : DataBlock
    {
      var x = Load(name);
      Gu.Assert(x is T);
      return (T?)x;
    }
    //Creatre a new resource with the given name. 
    //The returned resource will be a resource instance with the given datablock attached
    protected abstract DataBlock? Create(string name);
    protected abstract void Destroy();
    //Load the resource data required to create the resource
    public DataBlock? Load(string name)
    {
      if (this._state == LoadState.Loaded)
      {
        Gu.Log.Warn($"Tried ot load aloready loaded asset {name}");
        Gu.DebugBreak();
      }
      var d = Create(name);
      d.DataSource = this;
      this._state = LoadState.Loaded;
      return d;
    }
    //Unload the heavy data for the resource
    public void Unload()
    {
      Destroy();
      this._state = LoadState.Unloaded;
    }
    public override string ToString()
    {
      string json = SerializeTools.SerializeJSON(this);
      return json;
    }
  }
  [DataContract]
  public class SerializedDataSource : DataSource
  {
    //default DS items get their internal contens serialized into the Library
    public SerializedDataSource(string name) : base(name, SourceFormat.Serialized) { }
    protected override DataBlock? Create(string name)
    {
      return null;
    }
    protected override void Destroy()
    {
    }
  }

  [DataContract]
  public abstract class FileDataSource : DataSource
  {
    [DataMember] private FileLoc? _file = null;
    [DataMember] public FileLoc? File { get { return _file; } }
    public FileDataSource(string name, FileLoc loc) : base(name, SourceFormat.File)
    {
      _file = loc;
    }
  }
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
    protected override DataBlock? Create(string name)
    {
      Gu.Assert(_params != null);
      _params.Generate(ref _data);
      var img = new Image(name, _params._width, _params._height, _data, _params._format);
      return img;
    }
    protected override void Destroy()
    {
      _data = null;
    }
  }
  [DataContract]
  public class ImageFile : FileDataSource
  {
    Image? _image = null;

    public static Image? LoadImage(string name, FileLoc loc)
    {
      ImageFile f = new ImageFile(name, loc);
      var img = f.Load<Image>(name);
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

    public ImageFile(string name, FileLoc loc) : base(name, loc) { }

    protected override DataBlock? Create(string name)
    {
      _image = LoadImageFile(name);
      _image.DataSource = this;
      return _image;
    }
    protected override void Destroy()
    {
      _image = null;
    }
    private Image? LoadImageFile(string name)
    {
      Image? ret = null;

      using (var fs = File.OpenRead())
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
    protected override DataBlock? Create(string name)
    {
      //Returns an instance of the shader.
      _shader = new Shader(name, _generic_name, _storage, _primType);
      return _shader;
    }
    protected override void Destroy()
    {
    }
  }
  [DataContract]
  public class GLTFFile : FileDataSource
  {
    private List<WorldObject> _objects = null;
    public Dictionary<string, object>? _loadedData = null;
    [DataMember] private bool _flipTris = true;

    public List<WorldObject> Objects { get { return _objects; } }
    public Dictionary<string, object>? LoadedData { get { return _loadedData; } }
    public bool FlipTris { get { return _flipTris; } set { _flipTris = value; } }

    public GLTFFile(string name, FileLoc loc, bool flip_tris = true) : base(name, loc)
    {
      _flipTris = flip_tris;
    }
    protected override DataBlock? Create(string objName)
    {
      Gu.Assert(File != null);
      _objects = LoadObjects(File, _flipTris);
      foreach (var ob in _objects)
      {
        ob.DataSource = this;
      }
      Gu.Assert(_objects != null);
      Model wo = new Model(objName);
      foreach (var ob in _objects)
      {
        wo.AddChild(ob);
      }
      return wo;
    }
    protected override void Destroy()
    {
      _objects = null;
    }
    private static List<WorldObject> LoadObjects(FileLoc loc, bool flip_tris = true)
    {
      //Load GLTF object
      //Returns a single object that is persistent with other objects as data refs.
      //@param flip_tris - flip the triangle winding (from blender .. etc)
      //@Note this is not an optimal loading - it loads the entire mesh into memory.For small meshes, small games. 
      List<WorldObject> objs = null;
      try
      {
        //GLTF has 2 parts the model info and the binary data.
        //It also has 3 file formats: data only, data + metadata, and metadata with embedded data (json)
        string path = loc.QualifiedPath;
        byte[]? model_bytes = null;
        glTFLoader.Schema.Gltf myModel = null;

        using (Stream? stream = loc.OpenRead())
        {
          if (stream != null)
          {
            myModel = glTFLoader.Interface.LoadModel(stream);
          }
          else
          {
            Gu.BRThrowException("Stream from '" + path + "'was null");
          }
        }
        using (Stream? stream = loc.OpenRead())
        {
          if (stream != null)
          {
            model_bytes = glTFLoader.Interface.LoadBinaryBuffer(stream);
          }
          else
          {
            Gu.BRThrowException("Stream from '" + path + "'was null");
          }
        }

        objs = LoadObjectNodes(myModel);
        foreach (var ob in objs)
        {
          LoadModelNode(ob, myModel, model_bytes, flip_tris);
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Load model failed: ", ex);
      }

      return objs;
    }
    private static void ParseNodes(WorldObject parent, int[] nodeIndexes, glTFLoader.Schema.Gltf myModel, List<WorldObject> worldobjs_ordered_toplevel)
    {
      if (nodeIndexes != null)
      {
        foreach (var iNode in nodeIndexes)
        {
          var node = myModel.Nodes[iNode];
          //glTFLoader.Schema.Node 
          if (node != null)
          {
            vec3 trans = new vec3(node.Translation);
            vec4 rot = new vec4(node.Rotation);
            vec3 scale = new vec3(node.Scale);
            WorldObject wo = new WorldObject(Gu.Lib.GetUniqueName(ResourceType.WorldObject, node.Name));
            wo.Position_Local = trans;  
            wo.Rotation_Local = new quat(rot.x, rot.y, rot.z, rot.w);
            wo.Scale_Local = scale;
            wo.LoaderTempData = (object)node;
            wo.LoaderTempDataNodeId = iNode;
            if (parent == null)
            {
              worldobjs_ordered_toplevel.Add(wo);
            }
            else
            {
              parent.AddChild(wo);
            }
            ParseNodes(wo, node.Children, myModel, worldobjs_ordered_toplevel);
          }
          else
          {
            Gu.Log.Error("A node was was null loading GLTF.");
          }
        }
      }
    }
    private static List<WorldObject> LoadObjectNodes(glTFLoader.Schema.Gltf myModel)
    {
      //Loads a hierarchy of objects no mesh data (yet)
      List<WorldObject> worldobjs_ordered_toplevel = new List<WorldObject>();
      if (myModel != null)
      {
        //Only use first scene
        if (myModel.Scenes.Length == 0)
        {
          Gu.Log.Error("There were no scenes in the GLTF.");
          return worldobjs_ordered_toplevel;
        }
        var scene = myModel.Scenes[0];
        if (myModel.Scenes.Length > 1)
        {
          Gu.Log.Warn("There was more than 1 scene in GLTF. Only 1 scene is supported.");
        }

        ParseNodes(null, scene.Nodes, myModel, worldobjs_ordered_toplevel);
      }
      else
      {
        Gu.Log.Error("GLTF model was null upon loading nodes");
      }

      return worldobjs_ordered_toplevel;
    }
    private static void LoadModelNode(WorldObject root, glTFLoader.Schema.Gltf myModel, byte[] gltf_data, bool flip_tris)
    {
      //TODO: there can be more than 1 buffer her, so the gltf_data would need to be a list of buffer
      //and we would use the BufferView.Buffer to select the correct buffer.
      glTFLoader.Schema.Node? node = root.LoaderTempData as glTFLoader.Schema.Node;
      if (node == null)
      {
        Gu.Log.Error("Loader temp data was not set for a node.");
        return;
      }
      if (node.Mesh == null)
      {
        Gu.Log.Warn("Given node '" + node.Name + "' had no mesh.");
        return;
      }
      //Get the mesh at the index. 
      var mesh = myModel.Meshes[node.Mesh.Value];

      if (mesh.Primitives.Length == 0)
      {
        Gu.Log.Error("No mesh primitives were available for mesh..");
        return;
      }
      if (mesh.Primitives.Length > 1)
      {
        Gu.Log.Warn("Only one mesh primitive of triangles is supported.");
      }
      var prim = mesh.Primitives[0];

      LoadGLTFMesh(myModel, mesh, prim, gltf_data, root, flip_tris);

      LoadGLTFMaterial(myModel, prim, gltf_data, root);

      LoadGLTFAnimation(myModel, gltf_data, root);

      //Recur children.
      root.IterateChildrenSafe((child) =>
      {
        LoadModelNode(child, myModel, gltf_data, flip_tris);
        return LambdaBool.Continue;
      });
    }
    private static void LoadGLTFAnimation(glTFLoader.Schema.Gltf myModel, byte[] gltf_data, WorldObject root)
    {
      AnimationComponent anim_comp = null;
      AnimationData adat = null;

      if (myModel.Animations != null && myModel.Animations.Length > 0)
      {
        foreach (var anim in myModel.Animations)
        {
          Gu.Assert(anim != null);
          if (anim.Channels != null && anim.Channels.Length > 0)
          {
            foreach (var channel in anim.Channels)
            {
              if (channel.Target != null)
              {
                if (channel.Target.Node != null)
                {
                  int nodeId = channel.Target.Node.Value;
                  if (root.LoaderTempDataNodeId == nodeId)
                  {
                    var sampler = anim.Samplers[channel.Sampler];

                    //Translate interpolation enum
                    KeyframeInterpolation interp = KeyframeInterpolation.Linear;
                    if (sampler.Interpolation == glTFLoader.Schema.AnimationSampler.InterpolationEnum.LINEAR) { interp = KeyframeInterpolation.Linear; }
                    else if (sampler.Interpolation == glTFLoader.Schema.AnimationSampler.InterpolationEnum.CUBICSPLINE) { interp = KeyframeInterpolation.Cubic; }
                    else if (sampler.Interpolation == glTFLoader.Schema.AnimationSampler.InterpolationEnum.STEP) { interp = KeyframeInterpolation.Step; }
                    else
                    {
                      Gu.BRThrowNotImplementedException();
                    }

                    //time
                    //Note: time values (seemingly) translated into seconds.
                    var samp_acc = myModel.Accessors[sampler.Input];
                    Gu.Assert(samp_acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                    Gu.Assert(samp_acc.Type == glTFLoader.Schema.Accessor.TypeEnum.SCALAR);
                    var off = myModel.BufferViews[samp_acc.BufferView.Value].ByteOffset;
                    float[] times = ByteArrayUtils.ParseFloatArray(gltf_data, samp_acc.Count, off);

                    //value
                    samp_acc = myModel.Accessors[sampler.Output];
                    off = myModel.BufferViews[samp_acc.BufferView.Value].ByteOffset;

                    if (channel.Target.Path == glTFLoader.Schema.AnimationChannelTarget.PathEnum.rotation)
                    {
                      //quaternion
                      Gu.Assert(samp_acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                      Gu.Assert(samp_acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC4);
                      Gu.Log.Debug($"Loading Model: {samp_acc.Count} rotation keys.");
                      quat[] vals = ByteArrayUtils.ParseQuatArray(gltf_data, samp_acc.Count, off);
                      if (adat == null)
                      {
                        adat = new AnimationData(anim.Name);
                        anim_comp = new AnimationComponent(adat);
                      }
                      adat.FillRot(times, vals, interp, false, false);
                    }
                    else if (channel.Target.Path == glTFLoader.Schema.AnimationChannelTarget.PathEnum.translation)
                    {
                      //v3
                      Gu.Assert(samp_acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                      Gu.Assert(samp_acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3);
                      Gu.Log.Debug($"Loading Model: {samp_acc.Count} position keys.");
                      vec3[] vals = ByteArrayUtils.ParseVec3fArray(gltf_data, samp_acc.Count, off).ToArray();
                      if (adat == null)
                      {
                        adat = new AnimationData(anim.Name);
                        anim_comp = new AnimationComponent(adat);
                      }
                      adat.FillPos(times, vals, interp);
                    }
                    else if (channel.Target.Path == glTFLoader.Schema.AnimationChannelTarget.PathEnum.scale)
                    {
                      //v3
                      Gu.Assert(samp_acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                      Gu.Assert(samp_acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3);
                      Gu.Log.Debug($"Loading Model: {samp_acc.Count} scale keys.");
                      vec3[] vals = ByteArrayUtils.ParseVec3fArray(gltf_data, samp_acc.Count, off).ToArray();
                      if (adat == null)
                      {
                        adat = new AnimationData(anim.Name);
                        anim_comp = new AnimationComponent(adat);
                      }
                      adat.FillScale(times, vals, interp);
                    }
                    else if (channel.Target.Path == glTFLoader.Schema.AnimationChannelTarget.PathEnum.weights)
                    {
                      Gu.Log.Error("We do not support skin yet.. todo..");
                      Gu.DebugBreak();
                    }
                    else
                    {
                      Gu.BRThrowNotImplementedException();
                    }
                  }
                }
                else
                {
                  Gu.Log.Error($"Animation '{anim.Name}' channel target had no node.");
                }
              }
              else
              {
                Gu.Log.Error($"Animation '{anim.Name}' channel had no target.");
              }
            }
          }
          else
          {
            Gu.Log.Error($"Animation '{anim.Name}' had no channels.");
          }
        }
      }

      if (anim_comp != null)
      {
        root.AddComponent(anim_comp);
      }
    }
    private static void LoadGLTFMesh(glTFLoader.Schema.Gltf myModel, glTFLoader.Schema.Mesh? mesh, glTFLoader.Schema.MeshPrimitive prim, byte[] gltf_data, WorldObject root, bool flip_tris)
    {
      const string position_att = "POSITION";
      const string normal_att = "NORMAL";
      const string texcoord0_att = "TEXCOORD_0";
      const string texcoord1_att = "TEXCOORD_1";
      const string texcoord2_att = "TEXCOORD_2";
      const string texcoord3_att = "TEXCOORD_3";
      const string texcoord4_att = "TEXCOORD_4";
      const string tangent_att = "TANGENT";

      glTFLoader.Schema.MeshPrimitive.ModeEnum mode = prim.Mode;
      OpenTK.Graphics.OpenGL4.PrimitiveType mesh_prim_type = OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles;
      if (mode != glTFLoader.Schema.MeshPrimitive.ModeEnum.TRIANGLES)
      {
        Gu.Log.Error("Primitive mode for mesh not supported..");
        return;
      }

      //Hoist raw data into buffers.
      //This is slow - we could just use some raw index offsets to create v_.. but my brain is not working... later we fix this. No need for these buffers.
      vec3[] positions = null;
      vec3[] normals = null;
      vec2[] texs_0 = null;
      vec3[] tangents = null;

      foreach (var attr in prim.Attributes)
      {
        //3.7.2.1. Overview - these are all valid as part of the spec
        //https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html

        string attr_name = attr.Key;
        int accessor_index = attr.Value;
        if (accessor_index < myModel.Accessors.Length)
        {
          var attribute_accessor = myModel.Accessors[accessor_index];
          int? bufferViewID = myModel.Accessors[accessor_index].BufferView;
          if (bufferViewID != null && bufferViewID.Value < myModel.BufferViews.Length)
          {
            var buffer_view = myModel.BufferViews[bufferViewID.Value];
            Gu.Assert(buffer_view.ByteStride == null || buffer_view.ByteStride.Value == 0);
            Gu.Assert(buffer_view.Buffer == 0);
            if (buffer_view != null)
            {
              if (attr_name.Equals(position_att))
              {
                if (attribute_accessor.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT)
                {
                  if (attribute_accessor.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3)
                  {
                    positions = ByteArrayUtils.ParseVec3fArray(gltf_data, attribute_accessor.Count, buffer_view.ByteOffset);
                  }
                  else
                  {
                    Gu.Log.Error("Attribute: " + attr_name + " - Invalid data type");
                  }
                }
                else
                {
                  Gu.Log.Error("Attribute: " + attr_name + " - Invalid component type");
                }
              }
              else if (attr_name.Equals(tangent_att))
              {
                if (attribute_accessor.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT)
                {
                  if (attribute_accessor.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3)
                  {
                    tangents = ByteArrayUtils.ParseVec3fArray(gltf_data, attribute_accessor.Count, buffer_view.ByteOffset);
                  }
                  else
                  {
                    Gu.Log.Error("Attribute: " + attr_name + " - Invalid data type");
                  }
                }
                else
                {
                  Gu.Log.Error("Attribute: " + attr_name + " - Invalid component type");
                }
              }
              else if (attr_name.Equals(normal_att))
              {
                if (attribute_accessor.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT)
                {
                  if (attribute_accessor.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3)
                  {
                    normals = ByteArrayUtils.ParseVec3fArray(gltf_data, attribute_accessor.Count, buffer_view.ByteOffset);
                  }
                  else
                  {
                    Gu.Log.Error("Attribute: " + attr_name + " - Invalid data type");
                  }
                }
                else
                {
                  Gu.Log.Error("Attribute: " + attr_name + " - Invalid component type");
                }
              }
              else if (attr_name.Equals(texcoord0_att))
              {
                if (attribute_accessor.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT)
                {
                  if (attribute_accessor.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC2)
                  {
                    texs_0 = ByteArrayUtils.ParseVec2fArray(gltf_data, attribute_accessor.Count, buffer_view.ByteOffset);
                  }
                  else
                  {
                    Gu.Log.Error("Attribute: " + attr_name + " - Invalid data type");
                  }
                }
                else
                {
                  Gu.Log.Error("Attribute: " + attr_name + " - Invalid component type");
                }
              }
              else
              {
                Gu.Log.Error("Invalid attribute name " + attr_name);
                Gu.BRThrowNotImplementedException();
              }
            }
            else
            {
              Gu.Log.Error("Buffer view was null.");
            }
          }
          else
          {
            Gu.Log.Error("Buffer view ID was null, or out of bounds.");
          }
        }
        else
        {
          Gu.Log.Error("attribute index outside of accessor");
          return;
        }
      }

      ushort[] indices_ushort = null;
      uint[] indices_uint = null;

      if (prim.Indices == null || prim.Indices.Value > myModel.Accessors.Length)
      {
        Gu.Log.Error("Mesh had no index data or outside bounds. This is not supported.");
        return;
      }
      {
        var index_accessor = myModel.Accessors[prim.Indices.Value];
        int? bufferViewID = index_accessor.BufferView;
        if (bufferViewID != null && bufferViewID.Value < myModel.BufferViews.Length)
        {
          //According to the spec target should not be null - element array or array buf
          var buffer_view = myModel.BufferViews[bufferViewID.Value];
          Gu.Assert(buffer_view.ByteStride == null || buffer_view.ByteStride.Value == 0);
          Gu.Assert(buffer_view.Buffer == 0);//TODO:
          if (index_accessor.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
          {
            indices_ushort = ByteArrayUtils.ParseUInt16Array(gltf_data, index_accessor.Count, buffer_view.ByteOffset);
          }
          else if (index_accessor.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.UNSIGNED_INT)
          {
            indices_uint = ByteArrayUtils.ParseUInt32Array(gltf_data, index_accessor.Count, buffer_view.ByteOffset);
          }
          else
          {
            Gu.Log.Error("Invalid index type.");
            return;
          }
        }
        else
        {
          Gu.Log.Error("Buffer View ID was null or empty for indexes.");
          return;
        }
      }

      if (positions == null)
      {
        Gu.Log.Error("No position information specified on mesh.");
      }
      else if (normals != null && positions.Length != normals.Length)
      {
        Gu.Log.Error($"normals ordinal {tangents.Length} did not equal position {positions.Length}");
      }
      else if (texs_0 != null && positions.Length != texs_0.Length)
      {
        Gu.Log.Error($"texs_0 ordinal {tangents.Length} did not equal position {positions.Length}");
      }
      else if (tangents != null && positions.Length != tangents.Length)
      {
        Gu.Log.Error($"Tangents ordinal {tangents.Length} did not equal position {positions.Length}");
      }
      else
      {
        FillGLTFMeshData(root, mesh_prim_type, mesh.Name, indices_ushort, indices_uint, positions, normals, texs_0, tangents, flip_tris);
      }
    }
    private static void FillGLTFMeshData(WorldObject root, OpenTK.Graphics.OpenGL4.PrimitiveType mesh_prim_type, string mesh_name,
     ushort[] indices_ushort, uint[] indices_uint, vec3[] positions, vec3[] normals, vec2[] texs_0, vec3[] tangents, bool flip_tris)
    {
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
        Gu.Assert(verts.Length % 3 == 0);
        if (flip_tris)
        {
          for (int vi = 0; vi < verts.Length; vi += 3)
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

      if (indices_uint == null && indices_ushort == null)
      {
        var fd = MeshGen.ComputeNormalsAndTangents(verts, null, normals == null, tangents == null);

        root.MeshView = new MeshView( 
          new MeshData(mesh_name, mesh_prim_type,
          Gpu.CreateVertexBuffer(mesh_name, verts),
          Gpu.CreateVertexBuffer(mesh_name, fd),
          Gpu.CreateShaderStorageBuffer(mesh_name, fd),
          true
          ));
      }
      else
      {
        var fd = MeshGen.ComputeNormalsAndTangents(verts, indices_uint != null ? indices_uint : indices_ushort.AsUIntArray(), normals == null, tangents == null);
        root.MeshView =new MeshView( 
          new MeshData(Gu.Lib.GetUniqueName(ResourceType.MeshData, mesh_name), mesh_prim_type,
          Gpu.CreateVertexBuffer(mesh_name, verts),
          indices_uint != null ? Gpu.CreateIndexBuffer(mesh_name, indices_uint) : Gpu.CreateIndexBuffer(mesh_name, indices_ushort),
          Gpu.CreateShaderStorageBuffer(mesh_name, fd),
          true
          ));
      }
    }
    private static void LoadGLTFMaterial(glTFLoader.Schema.Gltf myModel, glTFLoader.Schema.MeshPrimitive prim, byte[] gltf_data, WorldObject root)
    {
      root.Material = null;
      if (root.LoaderTempData != null)
      {
        if (prim.Material != null)
        {
          var ind = prim.Material.Value;
          var mat = myModel.Materials[ind];
          if (mat != null)
          {
            root.Material = new Material(Gu.Lib.GetUniqueName(ResourceType.Material, mat.Name), Gu.Lib.GetShader(Rs.Shader.DefaultObjectShader));

            if (mat.OcclusionTexture != null)
            {
              Gu.Log.Error("occlusion tex Not supoported");
              Gu.DebugBreak();
            }
            if (mat.EmissiveTexture != null)
            {
              Gu.Log.Error("emissive tex Not supoported");
              Gu.DebugBreak();
            }
            if (mat.NormalTexture != null)
            {
              LoadGLTFTexture(myModel, mat, gltf_data, mat.NormalTexture.Index, root, root.Material.NormalSlot);
            }
            if (mat.PbrMetallicRoughness != null)
            {
              root.Material.Roughness = mat.PbrMetallicRoughness.RoughnessFactor;
              root.Material.Metallic = mat.PbrMetallicRoughness.MetallicFactor;
              root.Material.AlphaMode = mat.AlphaMode;
              root.Material.BaseColor = new vec4(
                 mat.PbrMetallicRoughness.BaseColorFactor[0],
                 mat.PbrMetallicRoughness.BaseColorFactor[1],
                 mat.PbrMetallicRoughness.BaseColorFactor[2],
                 mat.PbrMetallicRoughness.BaseColorFactor[3]);

              if (mat.PbrMetallicRoughness.BaseColorTexture != null)
              {
                LoadGLTFTexture(myModel, mat, gltf_data, mat.PbrMetallicRoughness.BaseColorTexture.Index, root, root.Material.AlbedoSlot);
              }
              if (mat.PbrMetallicRoughness.MetallicRoughnessTexture != null)
              {
                LoadGLTFTexture(myModel, mat, gltf_data, mat.PbrMetallicRoughness.MetallicRoughnessTexture.Index, root, root.Material.RoughnessSlot);
              }
            }
          }
        }
      }

      if (root.Material == null)
      {
        root.Material = Gu.Lib.GetMaterial(Rs.Material.DefaultObjectMaterial);
      }
    }
    private static void LoadGLTFTexture(glTFLoader.Schema.Gltf myModel, glTFLoader.Schema.Material mat, byte[] gltf_data, int tind, WorldObject root, TextureSlot slot)
    {
      Gu.Assert(slot != null);
      Gu.Assert(root != null);
      Gu.Assert(myModel != null);
      Gu.Assert(myModel.Textures != null && myModel.Textures.Length >= tind);
      var md_tex = myModel.Textures[tind];

      if (md_tex.Source != null)
      {
        var md_img = myModel.Images[md_tex.Source.Value];
        if (md_img.BufferView != null)
        {
          TextureWrapMode ws = TextureWrapMode.Repeat;
          TextureWrapMode wt = TextureWrapMode.Repeat;
          TextureMinFilter minf = TextureMinFilter.Linear;
          TextureMagFilter magf = TextureMagFilter.Linear;
          LoadGLTFSamplerOrDefaults(myModel, md_tex, out ws, out wt, out minf, out magf);

          var bv = myModel.BufferViews[md_img.BufferView.Value];
          Gu.Assert(bv.ByteStride == null || bv.ByteStride.Value == 0);
          Gu.Assert(bv.Buffer == 0);
          var imgData = new byte[bv.ByteLength];
          System.Buffer.BlockCopy(gltf_data, bv.ByteOffset, imgData, 0, bv.ByteLength);
          Image? m = ImageFile.LoadImageRaw(imgData, md_img.Name);//Could use URI here, but it's not specified in a packed GLB
          string name = md_img.Name;
#if DEBUG
          {
            var p = System.IO.Path.Combine(Gu.LocalTmpPath, name + ".png");
            Gu.Log.Debug("Saving debug image " + p.ToString());
            Lib.SaveImage(p, m, true);
          }
#endif
          if (md_img.Name.ToLower().Contains("bump"))
          {
            Gu.Log.Info("Converting bump map to normal map because it contains 'bump' in the name.. " + md_img.Name);
            m = m.CreateNormalMap(true);
            name = name + "-normalized";
#if DEBUG
            {
              var p = System.IO.Path.Combine(Gu.LocalTmpPath, name + ".png");
              Gu.Log.Debug("Saving debug image " + p.ToString());
              Lib.SaveImage(p, m, true);
            }
#endif
          }

          slot.Texture = new Texture(name, m, true, minf, magf, ws, wt);
        }
        else
        {
          Gu.Log.Error($"Bufferview was null for image texture {md_img.Name}. Possibly disk texture? (uri={md_img.Uri})");
        }
      }
      else
      {
        Gu.Log.Error("Texture source was null");
      }
    }
    private static void LoadGLTFSamplerOrDefaults(glTFLoader.Schema.Gltf myModel, glTFLoader.Schema.Texture? md_tex, out TextureWrapMode ws, out TextureWrapMode wt, out TextureMinFilter minf, out TextureMagFilter magf)
    {
      ws = TextureWrapMode.Repeat;
      wt = TextureWrapMode.Repeat;
      minf = TextureMinFilter.Linear;
      magf = TextureMagFilter.Linear;

      if (md_tex.Sampler != null)
      {
        var samp = myModel.Samplers[md_tex.Sampler.Value];

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
  }
  [DataContract]
  public class AnimationLoader : DataSource
  {
    public AnimationLoader(string name) : base(name, SourceFormat.Generated)
    {
      throw new NotImplementedException();
    }
    protected override DataBlock? Create(string name)
    {
      throw new NotImplementedException();
      return null;
    }
    protected override void Destroy()
    {
      throw new NotImplementedException();
    }
  }
  [DataContract]
  public class MeshDataLoader : DataSource
  {
    public MeshDataLoader(string name) : base(name, SourceFormat.Generated)
    {
      throw new NotImplementedException();
    }
    protected override DataBlock? Create(string name)
    {
      throw new NotImplementedException();
      return null;
    }
    protected override void Destroy()
    {
      throw new NotImplementedException();
    }
  }
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
    protected override DataBlock? Create(string name)
    {
      _meshData = _params.Generate(name);
      _meshData.DataSource = this;
      return _meshData;
    }
    protected override void Destroy()
    {
      _meshData = null;
    }
    #region Public Static: Resource generators

    public static MeshData GenEllipsoidResource(string name, vec3 radius, int slices = 128, int stacks = 128, bool smooth = false, bool flip_tris = false)
    {
      MeshGen g = new MeshGen(name, new MeshGenEllipsoidParams()
      {
        _radius = radius,
        _slices = slices,
        _stacks = stacks,
        _smooth = smooth,
        _flip_tris = flip_tris,
      });
      return g.Load<MeshData>(name);
    }
    public static MeshData GenSphereResource(string name, float radius, int slices = 128, int stacks = 128, bool smooth = false, bool flip_tris = false)
    {
      MeshGen g = new MeshGen(name, new MeshGenEllipsoidParams()
      {
        _radius = new vec3(radius, radius, radius),
        _slices = slices,
        _stacks = stacks,
        _smooth = smooth,
        _flip_tris = flip_tris,
      });
      return g.Load<MeshData>(name);
    }
    public static MeshData GenPlaneResource(string name, float w, float h, vec2[]? side = null)
    {
      MeshGen g = new MeshGen(name, new MeshGenPlaneParams()
      {
        _w = w,
        _h = h,
        _side = side,
      });
      return g.Load<MeshData>(name);
    }
    public static MeshData GenBoxResource(string name, float w, float h, float d, vec2[] top = null, vec2[] side = null, vec2[] bot = null, vec3? translate = null)
    {
      MeshGen g = new MeshGen(name, new MeshGenBoxParams()
      {
        _w = w,
        _h = h,
        _d = d,
        _top = top,
        _side = side,
        _bot = bot,
        _translate = translate,
      });
      return g.Load<MeshData>(name);
    }

    #endregion
    #region Public Static: Raw generators

    public static MeshData GenPlane(string name, float w, float h, vec2[]? side)
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

      return new MeshData(name, PrimitiveType.Triangles,
        Gpu.CreateVertexBuffer(name, verts),
        Gpu.CreateIndexBuffer(name, qinds),
        Gpu.CreateShaderStorageBuffer(name, fd)
        );
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

      return new MeshData(name, PrimitiveType.Triangles,
        Gpu.CreateVertexBuffer(name, verts),
        Gpu.CreateIndexBuffer(name, inds),
        Gpu.CreateShaderStorageBuffer(name, fd)
        );
    }
    public static void GenEllipsoid(out v_v3n3x2t3u1[] verts, out ushort[] inds, vec3 radius, int slices = 128, int stacks = 128, bool smooth = false, bool flip_tris = false)
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

      return new MeshData(name, PrimitiveType.Triangles,
        Gpu.CreateVertexBuffer(name, verts),
        Gpu.CreateIndexBuffer(name, qinds),
        Gpu.CreateShaderStorageBuffer(name, fd)
        );
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