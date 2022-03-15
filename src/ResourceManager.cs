using System.Reflection;
using System.Runtime.InteropServices;
namespace PirateCraft
{
  public class ResourceManager
  {
    public Dictionary<FileLoc, WeakReference<Shader>> Shaders { get; private set; } = new Dictionary<FileLoc, WeakReference<Shader>>(new FileLoc.Comparer());
    public Dictionary<FileLoc, WeakReference<Texture2D>> Textures { get; private set; } = new Dictionary<FileLoc, WeakReference<Texture2D>>(new FileLoc.Comparer());
    //public Dictionary<FileLoc, AudioData> Sounds { get; private set; } = new Dictionary<FileLoc, AudioData>(new FileLoc.Comparer());

    private void ParseNodes(WorldObject parent, int[] nodeIndexes, glTFLoader.Schema.Gltf myModel, List<WorldObject> worldobjs_ordered_toplevel)
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
            WorldObject wo = new WorldObject(node.Name);
            wo.Position_Local = trans;
            wo.Rotation_Local = new quat(rot.x, rot.y, rot.z, rot.w);
            wo.Scale_Local = scale;
            wo.LoaderTempData = (object)node;
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
    private List<WorldObject> LoadObjectNodes(glTFLoader.Schema.Gltf myModel)
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
    private class MeshRaw
    {
      public byte[] Data;
      public MeshRaw(byte[] data)
      {
        Data = data;
      }
      public unsafe List<vec2> ParseVec2fArray(int item_count, int byte_offset)
      {
        List<vec2> ret = new List<vec2>();
        fixed (byte* raw = Data)
        {
          int component_byte_size = 4;//sizeof float
          int tensor_rank = 2;// 2 sca
          int tensor_byte_size = component_byte_size * tensor_rank;
          //***** not sure if count is number of bytes, or components
          for (int ioff = 0; ioff < item_count; ioff++)
          {
            int offset = byte_offset + ioff * tensor_byte_size;
            Gu.Assert(offset < Data.Length);
            vec2 v = *((vec2*)(raw + offset));
            ret.Add(v);
          }
        }
        return ret;
      }
      public unsafe List<vec3> ParseVec3fArray(int item_count, int byte_offset)
      {
        List<vec3> ret = new List<vec3>();
        fixed (byte* raw = Data)
        {
          int component_byte_size = 4;//sizeof float
          int tensor_rank = 3;// 3 scalars
          int tensor_byte_size = component_byte_size * tensor_rank;
          //***** not sure if count is number of bytes, or components
          for (int ioff = 0; ioff < item_count; ioff++)
          {
            int offset = byte_offset + ioff * tensor_byte_size;
            Gu.Assert(offset < Data.Length);
            vec3 v = *((vec3*)(raw + offset));
            ret.Add(v);
          }
        }
        return ret;
      }
      public unsafe List<ushort> ParseUInt16Array(int item_count, int byte_offset)
      {
        List<ushort> ret = new List<ushort>();
        fixed (byte* raw = Data)
        {
          int component_byte_size = 2;//sizeof ushort
          int tensor_rank = 1;// 3 scalars
          int tensor_byte_size = component_byte_size * tensor_rank;
          //***** not sure if count is number of bytes, or components
          for (int ioff = 0; ioff < item_count; ioff++)
          {
            int offset = byte_offset + ioff * tensor_byte_size;
            Gu.Assert(offset < Data.Length);
            ushort v = *((ushort*)(raw + offset));
            ret.Add(v);
          }
        }
        return ret;
      }
      public unsafe List<uint> ParseUInt32Array(int item_count/*not sure*/, int byte_offset)
      {
        List<uint> ret = new List<uint>();
        fixed (byte* raw = Data)
        {
          int component_byte_size = 4;//sizeof uint
          int tensor_rank = 1;// 3 scalars
          int tensor_byte_size = component_byte_size * tensor_rank;
          //***** not sure if count is number of bytes, or components
          for (int ioff = 0; ioff < item_count; ioff++)
          {
            int offset = byte_offset + ioff * tensor_byte_size;
            Gu.Assert(offset < Data.Length);
            uint v = *((uint*)(raw + offset));
            ret.Add(v);
          }
        }
        return ret;
      }
      public unsafe float[] ParseFloatArray(int scalar_count/*not sure*/, int byte_offset)
      {
        float[] floats = new float[scalar_count];
        fixed (byte* raw = Data)
        {
          int component_byte_size = 4;//sizeof uint
          int tensor_rank = 1;// 3 scalars
          int tensor_byte_size = component_byte_size * tensor_rank;
          //***** not sure if count is number of bytes, or components
          for (int ioff = 0; ioff < scalar_count; ioff++)
          {
            int offset = byte_offset + ioff * tensor_byte_size;
            Gu.Assert(offset < Data.Length);
            float v = *((float*)(raw + offset));
            floats[ioff] = v;
          }
        }
        return floats;
      }
    }
    private void LoadMesh(WorldObject root, glTFLoader.Schema.Gltf myModel, byte[] gltf_data)
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
      MeshRaw meshRaw = new MeshRaw(gltf_data);

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
      glTFLoader.Schema.MeshPrimitive.ModeEnum mode = prim.Mode;
      OpenTK.Graphics.OpenGL4.PrimitiveType mesh_prim_type = OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles;
      if (mode != glTFLoader.Schema.MeshPrimitive.ModeEnum.TRIANGLES)
      {
        Gu.Log.Error("Primitive mode for mesh not supported..");
        return;
      }

      //Hoist raw data into buffers.
      //This is slow - we could just use some raw index offsets to create v_.. but my brain is not working... later we fix this. No need for these buffers.
      List<vec3> positions = new List<vec3>();
      List<vec3> normals = new List<vec3>();
      List<vec2> texs_0 = new List<vec2>();

      //float[] positions = null;
      //float[] normals = null;
      //float[] texs_0 = null;

      foreach (var attr in prim.Attributes)
      {
        //3.7.2.1. Overview - these are all valid as part of the spec
        //https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html
        string position_att = "POSITION";
        string normal_att = "NORMAL";
        string texcoord0_att = "TEXCOORD_0";
        string texcoord1_att = "TEXCOORD_1";
        string texcoord2_att = "TEXCOORD_2";
        string texcoord3_att = "TEXCOORD_3";
        string texcoord4_att = "TEXCOORD_4";

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
                    //positions = meshRaw.ParseFloatArray(attribute_accessor.Count * 3, buffer_view.ByteOffset);// meshRaw.ParseVec3fArray(attribute_accessor.Count, buffer_view.ByteOffset);
                    positions = meshRaw.ParseVec3fArray(attribute_accessor.Count, buffer_view.ByteOffset);// meshRaw.ParseVec3fArray(attribute_accessor.Count, buffer_view.ByteOffset);
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
                    //normals = meshRaw.ParseFloatArray(attribute_accessor.Count * 3, buffer_view.ByteOffset);// = meshRaw.ParseVec3fArray(attribute_accessor.Count, buffer_view.ByteOffset);
                    normals = meshRaw.ParseVec3fArray(attribute_accessor.Count, buffer_view.ByteOffset);// meshRaw.ParseVec3fArray(attribute_accessor.Count, buffer_view.ByteOffset);
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
                    //texs_0 = meshRaw.ParseFloatArray(attribute_accessor.Count * 2, buffer_view.ByteOffset);//= meshRaw.ParseVec2fArray(attribute_accessor.Count, buffer_view.ByteOffset);
                    texs_0 = meshRaw.ParseVec2fArray(attribute_accessor.Count, buffer_view.ByteOffset);// meshRaw.ParseVec3fArray(attribute_accessor.Count, buffer_view.ByteOffset);
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

      List<ushort> indices_ushort = null;
      List<uint> indices_uint = null;

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
            indices_ushort = meshRaw.ParseUInt16Array(index_accessor.Count, buffer_view.ByteOffset);
          }
          else if (index_accessor.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.UNSIGNED_INT)
          {
            indices_uint = meshRaw.ParseUInt32Array(index_accessor.Count, buffer_view.ByteOffset);
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
      //Ugh..
      if (indices_ushort != null)
      {
        indices_uint = new List<uint>();
        foreach (var ind in indices_ushort)
        {
          indices_uint.Add((UInt32)ind);
        }
        indices_ushort = null;
      }
      //When indices property is not defined, the number of vertex indices to render is
      //defined by count of attribute accessors (with the implied values from range [0..count));
      //when indices property is defined, the number of vertex indices to render is defined by count
      //of accessor referred to by indices. In either case, the number of vertex indices MUST be valid
      //for the topology type used:
      if (positions.Count != normals.Count || positions.Count != texs_0.Count)
      {
        Gu.Log.Error("Count of one or more components did not match. Position will be the only component used. This may result in invalid vetexes.");
      }
      List<v_v3n3x2> verts = new List<v_v3n3x2>();
      for (int ivert = 0; ivert < positions.Count; ivert++)
      {
        v_v3n3x2 v = new v_v3n3x2();
        v._v = positions[ivert];
        v._n = normals[ivert];
        v._x = texs_0[ivert];
        verts.Add(v);
      }
      bool flip = true; //vertex winding is opposite in blender.


      if (indices_uint == null)
      {
        if (flip)
        {
          for (int vi = 0; vi < verts.Count; vi += 3)
          {
            v_v3n3x2 vert = verts[vi];
            verts[vi] = verts[vi + 1];
            verts[vi + 1] = vert;
          }
        }

        root.Mesh = new MeshData(mesh.Name, mesh_prim_type,
            v_v3n3x2.VertexFormat, Gpu.GetGpuDataPtr(verts.ToArray()));
      }
      else
      {
        if (flip)
        {
          for (int vi = 0; vi < indices_uint.Count; vi += 3)
          {
            uint idx = indices_uint[vi];
            indices_uint[vi] = indices_uint[vi + 1];
            indices_uint[vi + 1] = idx;
          }
        }
        root.Mesh = new MeshData(mesh.Name, mesh_prim_type,
           v_v3n3x2.VertexFormat, Gpu.GetGpuDataPtr(verts.ToArray()),
           IndexFormatType.Uint32, Gpu.GetGpuDataPtr(indices_uint.ToArray()));
      }

      //TODO: materials
      Gu.Log.Warn("TODO: must create materials for objects");
      root.Material = Material.DefaultDiffuse();

      if (root.LoaderTempData != null)
      {
        if (prim.Material != null)
        {
          var ind = prim.Material.Value;
          var mat = myModel.Materials[ind];
          if (mat != null)
          {
            if (mat.PbrMetallicRoughness != null)
            {
              root.Color = new vec4(
                 mat.PbrMetallicRoughness.BaseColorFactor[0],
                 mat.PbrMetallicRoughness.BaseColorFactor[1],
                 mat.PbrMetallicRoughness.BaseColorFactor[2],
                 mat.PbrMetallicRoughness.BaseColorFactor[3]);
            }
          }
        }
        //var nn = root.LoaderTempData as glTFLoader.Schema.Node;
        //nn.
      }

      //Recur children.
      foreach (var child in root.Children)
      {
        LoadMesh(child, myModel, gltf_data);
      }
    }
    public List<WorldObject> LoadObjects(FileLoc loc)
    {
      //Load GLTF object
      //Note: this is not an optimal loading - it loads the entire mesh into memory.
      //For small meshes, small games. Not for research.

      List<WorldObject> objs = new List<WorldObject>();

      try
      {
        //GLTF has 2 parts the model info and the binary data.
        //It also has 3 file formats: data only, data + metadata, and metadata with embedded data (json)
        string path = loc.QualifiedPath;
        byte[]? model_bytes = null;
        glTFLoader.Schema.Gltf myModel = null;

        using (Stream? stream = loc.GetStream())
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
        using (Stream? stream = loc.GetStream())
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
          LoadMesh(ob, myModel, model_bytes);
        }



      }
      catch (Exception ex)
      {
        Gu.Log.Error(ex.ToString());
      }

      return objs;
    }
    private Tx FindItem<Tx>(FileLoc loc, Dictionary<FileLoc, WeakReference<Tx>> dict) where Tx : class
    {
      Tx ret = null;
      dict.TryGetValue(loc, out var tex);
      if (tex != null)
      {
        if (tex.TryGetTarget(out var tex_target))
        {
          ret = tex_target;
        }
        else
        {
          //Remove the std::weak_reference item as it is no longer used
          dict.Remove(loc);
        }
      }
      return ret;
    }
    private Tx FindItem<Tx>(FileLoc loc, Dictionary<FileLoc, Tx> dict) where Tx : class
    {
      Tx ret = null;
      dict.TryGetValue(loc, out var tex);
      if (tex != null)
      {
        return tex;
      }
      return ret;
    }
    public Texture2D LoadTexture(FileLoc loc, bool mipmaps, TexFilter filter)
    {
      Texture2D ret = FindItem(loc, Textures);

      if (ret == null)
      {
        ret = new Texture2D(loc, true, TexFilter.Bilinear);
      }
      return ret;
    }
    public Shader LoadShader(string generic_name, bool gs, FileStorage storage, bool use_cached = true)
    {
      string vert_name = generic_name + ".vs.glsl";
      string geom_name = gs ? generic_name + ".fs.glsl" : "";
      string frag_name = generic_name + ".fs.glsl";
      string fileloc_name = vert_name + "-" + geom_name + "-" + frag_name; //hacky, but it will work
      var cache_loc = new FileLoc(vert_name, storage);

      Shader ret = FindItem(cache_loc, Shaders);

      if (ret == null)
      {
        //string vert = Gu.ReadTextFile(new FileLoc(vert_name, FileStorage.Embedded));
        //string geom = gs ? Gu.ReadTextFile(new FileLoc(geom_name, FileStorage.Embedded)) : "";
        //string frag = Gu.ReadTextFile(new FileLoc(frag_name, FileStorage.Embedded));


        string vert = Shader.ProcessFile(new FileLoc(vert_name, FileStorage.Embedded));
        string geom = gs ? Shader.ProcessFile(new FileLoc(geom_name, FileStorage.Embedded)) : "";
        string frag = Shader.ProcessFile(new FileLoc(frag_name, FileStorage.Embedded));


        ret = new Shader(generic_name, vert, frag, geom);
        Shaders.Add(cache_loc, new WeakReference<Shader>(ret));
      }
      return ret;
    }
    public static string ReadTextFile(FileLoc loc)
    {
      //Returns empty string when failSilently is true.
      string data = "";
      loc.AssertExists();

      if (loc.FileStorage == FileStorage.Embedded)
      {
        using (Stream stream = loc.GetStream())
        {
          using (StreamReader reader = new StreamReader(stream))
          {
            data = reader.ReadToEnd();
          }
        }
      }
      else if (loc.FileStorage == FileStorage.Disk)
      {
        if (!System.IO.File.Exists(loc.RawPath))
        {
          Gu.BRThrowException("File '" + loc.RawPath + "' does not exist.");
        }

        using (Stream stream = File.Open(loc.RawPath, FileMode.Open, FileAccess.Read, FileShare.None))
        using (StreamReader reader = new StreamReader(stream))
        {
          data = reader.ReadToEnd();
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      return data;
    }
    public static void SaveImage(string path, Img32 image)
    {
      var dir = Path.GetDirectoryName(path);
      if (!Directory.Exists(dir))
      {
        Directory.CreateDirectory(dir);
      }

      string ext = System.IO.Path.GetExtension(path).ToLower();
      try
      {
        using (Stream fs = File.OpenWrite(path))
        {
          StbImageWriteSharp.ImageWriter writer = new StbImageWriteSharp.ImageWriter();

          Img32 img2 = image.Clone();
          img2.FlipBR(); //flip back before saving

          writer.WritePng(img2.Data, image.Width, image.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, fs);
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Failed to save image: " + ex.ToString());
        Gu.DebugBreak();
      }
    }
    public static Img32 LoadImage(FileLoc loc)
    {
      Img32 b = null;

      loc.AssertExists();

      try
      {
        using (var fs = loc.GetStream())
        {
          if (fs != null)
          {
            StbImageSharp.ImageResult image = StbImageSharp.ImageResult.FromStream(fs, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
            if (image != null)
            {
              b = new Img32(image.Width, image.Height, image.Data);
              if(image.SourceComp == StbImageSharp.ColorComponents.RedGreenBlueAlpha)
              {
                b.FlipBA();
              }
              b.FlipBR();
              //   else if(image.SourceComp == StbImageSharp.ColorComponents.RedGreenBlue)
              //  {
            //  }
            }
            fs.Close();
          }
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error("failed to load image: " + ex.ToString());
        Gu.DebugBreak();
        b = Img32.Default1x1(255, 0, 255, 255);
      }

      return b;
    }
    public static unsafe byte[] Serialize<T>(T[] data) where T : struct
    {
      //This is .. terrible.
      var size = Marshal.SizeOf(data[0]);
      var bytes = new byte[size * data.Length];
      for (int di = 0; di < data.Length; di++)
      {
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(data[di], ptr, true);
        Marshal.Copy(ptr, bytes, di * size, size);
        Marshal.FreeHGlobal(ptr);
      }

      return bytes;
    }
    public static T[] Deserialize<T>(byte[] data) where T : struct
    {
      var tsize = Marshal.SizeOf(default(T));

      //Must be a multiple of the struct.
      Gu.Assert(data.Length % tsize == 0);

      var count = data.Length / tsize;
      T[] ret = new T[count];

      for (int di = 0; di < data.Length; di += tsize)
      {
        var ptr_struct = Marshal.AllocHGlobal(tsize);
        Marshal.StructureToPtr(data[di], ptr_struct, true);
        ret[di / tsize] = (T)Marshal.PtrToStructure(ptr_struct, typeof(T));
        Marshal.FreeHGlobal(ptr_struct);
      }

      return ret;
    }

    //public Font LoadFont(FileLoc loc)
    //{
    //  Font font = new Font();

    //}

    //public AudioData LoadSound(FileLoc loc)
    //{
    //  AudioData audioData = null;
    //  if (!Sounds.TryGetValue(loc, out audioData))
    //  {
    //    var bytes = loc.GetBytes();
    //    if (bytes != null)
    //    {
    //      //Ok so we need to know how to stream data with openAL
    //      var v = StbVorbisSharp.Vorbis.FromMemory(bytes);
    //      audioData = new AudioData(v);
    //      this.Sounds.Add(loc, audioData);
    //    }
    //    else
    //    {
    //        Gu.Log.Error("Could not load sound " + loc.QualifiedPath);
    //    }
    //  }

    //  return audioData;
    //}


  }
}
