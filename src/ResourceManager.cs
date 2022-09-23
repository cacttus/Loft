using System.Reflection;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
namespace PirateCraft
{
  public class ResourceManager
  {
    #region Public: Members

    //Not sure if we want weak or strong here. 
    //If an object goes away.. well, we may need it later right? Particles, etc.
    // then we can manually unload all resources when we load a new scene.
    public Dictionary<FileLoc, WeakReference<Shader>> Shaders { get; private set; } = new Dictionary<FileLoc, WeakReference<Shader>>(new FileLoc.EqualityComparer());
    public Dictionary<FileLoc, WeakReference<Texture2D>> Textures { get; private set; } = new Dictionary<FileLoc, WeakReference<Texture2D>>(new FileLoc.EqualityComparer());
    private DeltaTimer _shaderChangedTimer = null;
    private float _checkForShaderFileChangeUpdatesTimeSeconds = 0.5f;

    #endregion
    #region Public: Methods

    public ResourceManager()
    {
      //Clear temp, or cache directories if needed
      var dir = Path.GetDirectoryName(Gu.LocalTmpPath);
      if (!Directory.Exists(dir))
      {
        Directory.CreateDirectory(dir);
      }
      if (Gu.EngineConfig.ClearCacheOnStart)
      {
        ResourceManager.ClearDataDir(Gu.LocalCachePath);
      }
      if (Gu.EngineConfig.ClearTmpOnStart)
      {
        ResourceManager.ClearDataDir(Gu.LocalTmpPath);
      }

      //Start the shader poller, to update shaders so we don't gotta compile and run again 
      //We can poll for engine config, or any other file realyl, but that is a much larger system.
      if (_shaderChangedTimer == null)
      {
        _shaderChangedTimer = new DeltaTimer(_checkForShaderFileChangeUpdatesTimeSeconds, ActionRepeat.Repeat, ActionState.Run);
      }
    }
    public List<WorldObject> LoadObjects(FileLoc loc, bool flip_tris = true)
    {
      //Load GLTF object
      //Note: this is not an optimal loading - it loads the entire mesh into memory.
      //For small meshes, small games. 
      //@param flip_tris - flip the triangle winding (from blender .. etc)
      List<WorldObject> objs = new List<WorldObject>();
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
    public bool LoadObject(FileLoc loc, out WorldObject ob, bool flip_tris = true)
    {
      Gu.Log.Debug($"Loading object {loc.QualifiedPath}");
      ob = null;
      var objs = LoadObjects(loc, flip_tris);
      if (objs?.Count > 0)
      {
        ob = objs[0];
        return true;
      }
      return false;
    }
    public Shader LoadShader(string generic_name, bool gs, FileStorage storage, OpenTK.Graphics.OpenGL4.PrimitiveType? gs_primType = null)
    {
      //Load an empty shader generic name and storage. 
      //We create the actual Gpu shader for the given context+pipeline stage when it is needed.
      var cache_loc = new FileLoc(generic_name, storage);

      Shader ret = FindItem(cache_loc, Shaders);

      if (ret == null)
      {
        ret = new Shader(generic_name, gs, storage, gs_primType);
        Shaders.Add(cache_loc, new WeakReference<Shader>(ret));
      }
      return ret;
    }
    public static string ReadTextFile(FileLoc loc, bool useDiskVersionIfAvailable)
    {
      //Returns empty string when failSilently is true.
      //The disk version thing is if we have an embedded file, but (specifically for development) we want to use
      //the files in the /data directory.

      string data = "";
      loc.AssertExists();

      if (loc.FileStorage == FileStorage.Embedded || loc.FileStorage == FileStorage.Disk)
      {
        bool mustUseDiskVersion = false;
        if (useDiskVersionIfAvailable == true)
        {
          if (System.IO.File.Exists(loc.WorkspacePath))
          {
            mustUseDiskVersion = true;
          }
        }

        if (loc.FileStorage == FileStorage.Embedded && !mustUseDiskVersion)
        {
          using (Stream stream = loc.OpenRead())
          {
            using (StreamReader reader = new StreamReader(stream))
            {
              data = reader.ReadToEnd();
            }
          }
        }
        else //we are on disk, or, we must use the version on disk.
        {
          string disk_path = mustUseDiskVersion ? loc.WorkspacePath : loc.RawPath;

          if (!System.IO.File.Exists(disk_path))
          {
            Gu.BRThrowException("File '" + disk_path + "' does not exist.");
          }

          using (Stream stream = File.Open(disk_path, FileMode.Open, FileAccess.Read, FileShare.None))
          using (StreamReader reader = new StreamReader(stream))
          {
            data = reader.ReadToEnd();
          }
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      return data;
    }
    public static void SaveImage(string path, Img32 image, bool flipOpenGL)
    {
      //flipOpenGL - in OpenGL texture origin is the bottom left of screen, setting this will flip it upright.
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
          if (image.Format != Img32.ImagePixelFormat.RGBA32ub)
          {
            Gu.Log.Error("Invalid pixel format when saving image");
            Gu.DebugBreak();
            //          img2.FlipBR(); //flip back before saving
          }
          if (flipOpenGL)
          {
            img2.Flip(false, true);
          }

          writer.WritePng(img2.Data, image.Width, image.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, fs);
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Failed to save image: ", ex);
        Gu.DebugBreak();
      }
    }
    public static Img32 LoadImage(byte[] raw_png, string name)
    {
      Img32 img = null;

      try
      {
        StbImageSharp.ImageResult image = StbImageSharp.ImageResult.FromMemory(raw_png, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
        img = ProcessImage(image, name);
      }
      catch (Exception ex)
      {
        img = ImageLoadFailed_GetDefault(ex);
      }
      return img;
    }
    public static Img32 LoadImage(FileLoc loc)
    {
      Img32 img = null;
      loc.AssertExists();

      try
      {
        using (var fs = loc.OpenRead())
        {
          if (fs != null)
          {
            StbImageSharp.ImageResult image = StbImageSharp.ImageResult.FromStream(fs, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
            img = ProcessImage(image, loc.RawPath);
            fs.Close();
          }
        }
      }
      catch (Exception ex)
      {
        img = ImageLoadFailed_GetDefault(ex);
      }

      return img;
    }
    private static Img32 ImageLoadFailed_GetDefault(Exception ex)
    {
      Gu.Log.Error("failed to load image: ", ex);
      Gu.DebugBreak();
      return Img32.Default1x1_RGBA32ub(255, 0, 255, 255, "imgloadfailed");
    }
    private static Img32 ProcessImage(StbImageSharp.ImageResult image, string imagName)
    {
      Img32 img = null;
      if (image != null)
      {
        Img32.ImagePixelFormat pf = Img32.ImagePixelFormat.RGBA32ub;
        if (image.SourceComp == StbImageSharp.ColorComponents.RedGreenBlueAlpha)
        {
          //RGBA is the basic texture2d format. We convert everything to RGBA for simplicity.
          pf = Img32.ImagePixelFormat.RGBA32ub;
        }
        else if (image.SourceComp == StbImageSharp.ColorComponents.RedGreenBlue)
        {
          // ** Note : STB converts RGB images to RGBA wiht the above function's parameter so the nagive sourceComp is RGB, the input format is still RGBA.
          pf = Img32.ImagePixelFormat.RGBA32ub;
        }
        else
        {
          //We don't handle images not stored as RGBAyet. Use some kind of flip routine to create RGBA.
          // b.FlipBA();
          // b.FlipBR();
          Gu.DebugBreak();
        }
        //so now image name is impoartnet.. so .. 
        //Uh..ok so qualified path .. raw path ..
        // Qualified path is long and has  / / / \ \ \
        // Raw.. (filename) simpler.. but POSSIBLE conflicts. Very rare though.
        // Raw it is..
        img = new Img32(imagName, image.Width, image.Height, image.Data, pf);

      }
      return img;
    }
    public Texture2D LoadTexture(FileLoc loc, bool mipmaps, TexFilter filter, TextureWrapMode wrap = TextureWrapMode.Repeat)
    {
      Texture2D ret = FindItem(loc, Textures);

      if (ret == null)
      {
        ret = new Texture2D(loc, true, TexFilter.Bilinear, wrap);
      }
      return ret;
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
    public static void SaveTexture(FileLoc loc, Texture2D tex, bool formatNonRGBAtoRGBA, bool flipOpenGL = true, int cubemapside = -1, bool setAlphatoOne = false)
    {
      //formatNonRGBAtoRGBA - Convert things (like R32) to RGBA so we can see it.

      PixelFormat fmt = PixelFormat.AbgrExt;
      PixelType type = PixelType.Byte;
      PixelInternalFormat internalFmt = PixelInternalFormat.Alpha;

      Img32 img = Gpu.GetTextureDataFromGpu(tex.GetGlId(), tex.TextureTarget, ref fmt, ref type, ref internalFmt, cubemapside);

      if (formatNonRGBAtoRGBA)
      {
        if (fmt == PixelFormat.RedInteger && type == PixelType.UnsignedInt)
        {
          for (int iy = 0; iy < img.Height; iy++)
          {
            for (int ix = 0; ix < img.Width; ix++)
            {
              var p = img.GetPixel32(ix, iy);
              //r32ui is in agbr -> rgba
              var tmp = p.a;
              p.a = p.r;
              p.r = tmp;
              tmp = p.b;
              p.b = p.g;
              p.g = tmp;
              img.SetPixel32(ix, iy, p);
            }
          }
        }
      }
      if (internalFmt == PixelInternalFormat.DepthComponent32f ||
      internalFmt == PixelInternalFormat.DepthComponent32)
      {
        for (int iy = 0; iy < img.Height; iy++)
        {
          for (int ix = 0; ix < img.Width; ix++)
          {
            var p = img.GetPixel32(ix, iy);
            float f = BitConverter.ToSingle(new byte[] { p.r, p.g, p.b, p.a }, 0);
            //r32ui is in agbr -> rgba
            p.r = p.g = p.b = (byte)((f / 1.0f) * 255.0f);
            p.a = 255;
            img.SetPixel32(ix, iy, p);
          }
        }
      }
      else if (internalFmt == PixelInternalFormat.DepthComponent16 ||
      internalFmt == PixelInternalFormat.DepthComponent24)
      {
        Gu.Log.Error("save 16/24 bit depth texture not supported.");
        Gu.DebugBreak();
      }
      if (setAlphatoOne && (internalFmt == PixelInternalFormat.Rgba ||
      internalFmt == PixelInternalFormat.Rgba32f ||
       internalFmt == PixelInternalFormat.Rgba8 ||
       internalFmt == PixelInternalFormat.Rgba16 ||
       internalFmt == PixelInternalFormat.Rgba32f ||
       internalFmt == PixelInternalFormat.Rgba32i
       ))
      {
        for (int iy = 0; iy < img.Height; iy++)
        {
          for (int ix = 0; ix < img.Width; ix++)
          {
            var p = img.GetPixel32(ix, iy);
            p.a = byte.MaxValue;
            img.SetPixel32(ix, iy, p);
          }
        }
      }


      SaveImage(loc.QualifiedPath, img, flipOpenGL);
    }
    public static void ClearDataDir(string dir)
    {
      Gu.Log.Info($"Clearing dir {dir}");
      var fs = System.IO.Directory.GetFiles(dir);
      foreach (var f in fs)
      {
        try
        {
          System.IO.File.Delete(f);
        }
        catch (Exception ex)
        {
          Gu.Log.Error("Clear: Could not delete '" + f + "'", ex);
        }
      }
    }
    public void Update(double dt)
    {
      _shaderChangedTimer.Update(dt, () =>
        {
          foreach (var pair in Shaders)
          {
            if (pair.Value.TryGetTarget(out var s))
            {
              s.CheckSourceChanged();
            }
          }
        });
    }

    #endregion
    #region Private: Methods

    private Tx FindItem<Tx>(FileLoc loc, Dictionary<FileLoc, WeakReference<Tx>> dict) where Tx : class
    {
      Tx ret = null;
      dict.TryGetValue(loc, out var tex);

      if (tex != null && tex.TryGetTarget(out var tex_target))
      {
        ret = tex_target;
      }
      else
      {
        //Remove the std::weak_reference item as it is no longer used
        dict.Remove(loc);
      }
      return ret;
    }
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
    private class ByteArrayUtils
    {
      public static unsafe vec2[] ParseVec2fArray(byte[] Data, int item_count, int byte_offset)
      {
        vec2[] ret = new vec2[item_count];
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
            ret[ioff] = v;
          }
        }
        return ret;
      }
      public static unsafe vec3[] ParseVec3fArray(byte[] Data, int item_count, int byte_offset)
      {
        vec3[] ret = new vec3[item_count];
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
            ret[ioff] = v;
          }
        }
        return ret;
      }
      public static unsafe vec4[] ParseVec4fArray(byte[] Data, int item_count, int byte_offset)
      {
        vec4[] ret = new vec4[item_count];
        fixed (byte* raw = Data)
        {
          int component_byte_size = 4;//sizeof float
          int tensor_rank = 4;// 4 scalars
          int tensor_byte_size = component_byte_size * tensor_rank;
          //***** not sure if count is number of bytes, or components
          for (int ioff = 0; ioff < item_count; ioff++)
          {
            int offset = byte_offset + ioff * tensor_byte_size;
            Gu.Assert(offset < Data.Length);
            vec4 v = *((vec4*)(raw + offset));
            ret[ioff] = v;
          }
        }
        return ret;
      }
      public static unsafe quat[] ParseQuatArray(byte[] Data, int item_count, int byte_offset)
      {
        quat[] ret = new quat[item_count];
        fixed (byte* raw = Data)
        {
          int component_byte_size = 4;//sizeof float
          int tensor_rank = 4;// 4 scalars
          int tensor_byte_size = component_byte_size * tensor_rank;
          //***** not sure if count is number of bytes, or components
          for (int ioff = 0; ioff < item_count; ioff++)
          {
            int offset = byte_offset + ioff * tensor_byte_size;
            Gu.Assert(offset < Data.Length);
            quat v = *((quat*)(raw + offset));
            ret[ioff] = v;
          }
        }
        return ret;
      }
      public static unsafe ushort[] ParseUInt16Array(byte[] Data, int item_count, int byte_offset)
      {
        ushort[] ret = new ushort[item_count];
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
            ret[ioff] = v;
          }
        }
        return ret;
      }
      public static unsafe uint[] ParseUInt32Array(byte[] Data, int item_count/*not sure*/, int byte_offset)
      {
        uint[] ret = new uint[item_count];
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
            ret[ioff] = v;
          }
        }
        return ret;
      }
      public static unsafe float[] ParseFloatArray(byte[] Data, int scalar_count/*not sure*/, int byte_offset)
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
    private void LoadModelNode(WorldObject root, glTFLoader.Schema.Gltf myModel, byte[] gltf_data, bool flip_tris)
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
      foreach (var child in root.Children)
      {
        LoadModelNode(child, myModel, gltf_data, flip_tris);
      }
    }
    private static void LoadGLTFAnimation(glTFLoader.Schema.Gltf myModel, byte[] gltf_data, WorldObject root)
    {
      AnimationComponent anim_comp = null;

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
                      anim_comp = anim_comp.ConstructIfNeeded();
                      anim_comp.FillRot(times, vals, interp, false, false);
                    }
                    else if (channel.Target.Path == glTFLoader.Schema.AnimationChannelTarget.PathEnum.translation)
                    {
                      //v3
                      Gu.Assert(samp_acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                      Gu.Assert(samp_acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3);
                      Gu.Log.Debug($"Loading Model: {samp_acc.Count} position keys.");
                      vec3[] vals = ByteArrayUtils.ParseVec3fArray(gltf_data, samp_acc.Count, off).ToArray();
                      anim_comp = anim_comp.ConstructIfNeeded();
                      anim_comp.FillPos(times, vals, interp);
                    }
                    else if (channel.Target.Path == glTFLoader.Schema.AnimationChannelTarget.PathEnum.scale)
                    {
                      //v3
                      Gu.Assert(samp_acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                      Gu.Assert(samp_acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3);
                      Gu.Log.Debug($"Loading Model: {samp_acc.Count} scale keys.");
                      vec3[] vals = ByteArrayUtils.ParseVec3fArray(gltf_data, samp_acc.Count, off).ToArray();
                      anim_comp = anim_comp.ConstructIfNeeded();
                      anim_comp.FillScale(times, vals, interp);
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
        root.Components.Add(anim_comp);
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
        FillMeshData(root, mesh_prim_type, mesh.Name, indices_ushort, indices_uint, positions, normals, texs_0, tangents, flip_tris);
      }
    }
    private static void FillMeshData(WorldObject root, OpenTK.Graphics.OpenGL4.PrimitiveType mesh_prim_type, string mesh_name,
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
        var fd = MeshData.ComputeNormalsAndTangents(verts, null, normals == null, tangents == null);

        root.Mesh = new MeshData(mesh_name, mesh_prim_type,
          Gpu.CreateVertexBuffer(mesh_name, verts),
          Gpu.CreateVertexBuffer(mesh_name, fd),
          Gpu.CreateShaderStorageBuffer(mesh_name, fd),
          true
          );
      }
      else
      {
        var fd = MeshData.ComputeNormalsAndTangents(verts, indices_uint != null ? indices_uint : indices_ushort.AsUIntArray(), normals == null, tangents == null);
        root.Mesh = new MeshData(mesh_name, mesh_prim_type,
          Gpu.CreateVertexBuffer(mesh_name, verts),
          indices_uint != null ? Gpu.CreateIndexBuffer(mesh_name, indices_uint) : Gpu.CreateIndexBuffer(mesh_name, indices_ushort),
          Gpu.CreateShaderStorageBuffer(mesh_name, fd),
          true
          );
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
            root.Material = new Material(mat.Name, Shader.DefaultObjectShader());
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
              LoadTexture(myModel, mat, gltf_data, mat.NormalTexture.Index, root, root.Material.NormalSlot);
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
                LoadTexture(myModel, mat, gltf_data, mat.PbrMetallicRoughness.BaseColorTexture.Index, root, root.Material.AlbedoSlot);
              }
              if (mat.PbrMetallicRoughness.MetallicRoughnessTexture != null)
              {
                LoadTexture(myModel, mat, gltf_data, mat.PbrMetallicRoughness.MetallicRoughnessTexture.Index, root, root.Material.RoughnessSlot);
              }
            }
          }
        }
      }

      if (root.Material == null)
      {
        root.Material = Material.DefaultObjectMaterial;
      }
    }
    private static void LoadTexture(glTFLoader.Schema.Gltf myModel, glTFLoader.Schema.Material mat, byte[] gltf_data, int tind, WorldObject root, TextureInput slot)
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
          GLTFGetSamplerOrDefaults(myModel, md_tex, out ws, out wt, out minf, out magf);

          var bv = myModel.BufferViews[md_img.BufferView.Value];
          Gu.Assert(bv.ByteStride == null || bv.ByteStride.Value == 0);
          Gu.Assert(bv.Buffer == 0);
          var imgData = new byte[bv.ByteLength];
          System.Buffer.BlockCopy(gltf_data, bv.ByteOffset, imgData, 0, bv.ByteLength);
          Img32 m = ResourceManager.LoadImage(imgData, md_img.Name);//Could use URI here, but it's not specified in a packed GLB
          string name = md_img.Name;
#if DEBUG
          {
            var p = System.IO.Path.Combine(Gu.LocalTmpPath, name + ".png");
            Gu.Log.Debug("Saving debug image " + p.ToString());
            ResourceManager.SaveImage(p, m, true);
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
              ResourceManager.SaveImage(p, m, true);
            }
#endif            
          }

          slot.Texture = new Texture2D(name, m, true, minf, magf, ws, wt);
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
    private static void GLTFGetSamplerOrDefaults(glTFLoader.Schema.Gltf myModel, glTFLoader.Schema.Texture? md_tex, out TextureWrapMode ws, out TextureWrapMode wt, out TextureMinFilter minf, out TextureMagFilter magf)
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

    #endregion
  }
}
