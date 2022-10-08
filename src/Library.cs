using System;
using System.Reflection;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace PirateCraft
{
  public enum ResourceLoadResult
  {
    NotLoaded,
    Loaded,
    LoadFailed
  }
  public class RName
  {
    public static string Image_DefaultNormalPixelZUp = "Image_DefaultNormalPixelZUp";
    public static string Image_DefaultNormalPixelYUp = "Image_DefaultNormalPixelYUp";
    public static string Image_DefaultFailedPixel = "Image_DefaultFailedPixel";
    public static string Image_DefaultWhitePixel = "Image_DefaultWhitePixel";

    public static string Tex2D_DefaultFailedTexture = "DefaultFailedTexture";
    public static string Tex2D_DefaultWhitePixel = "Tex2D_DefaultWhitePixel";
    public static string Tex2D_DefaultBlackPixelNoAlpha = "Tex2D_DefaultBlackPixelNoAlpha";
    public static string Tex2D_DefaultNormalPixel = "Tex2D_DefaultNormalPixel";

    public static string Shader_DebugDraw = "Shader_DebugDraw";
    public static string Shader_GuiShader = "Shader_GuiShader";
    public static string Shader_DefaultObjectShader = "Shader_DefaultObjectShader";
    public static string Shader_DefaultFlatColorShader = "Shader_DefaultFlatColorShader";
    public static string Shader_DefaultBillboardPoints = "Shader_DefaultBillboardPoints";

    public static string Material_DefaultFlatColorMaterial = "Material_DefaultFlatColorMaterial";
    public static string Material_DefaultObjectMaterial = "Material_DefaultObjectMaterial";
    public static string Material_DebugDraw_VertexNormals_FlatColor = "Material_DebugDraw_VertexNormals_FlatColor";
    public static string Material_DebugDrawMaterial = "Material_DebugDrawMaterial";

    public static string Mesh_DefaultBox = "Mesh_DefaultBox";

    public static string WorldObject_Camera = "WorldObject_Camera";
    public static string WorldObject_Gear = "WorldObject_Gear";
  }


  public class ResourceType
  {
    //must come at top
    private static Dictionary<Type, ResourceType> _resourceTypesArray = new Dictionary<Type, ResourceType>();


    public static ResourceType GpuTexture = new ResourceType("GpuTexture", typeof(GpuTexture), "-gputx");
    public static ResourceType GPUBuffer = new ResourceType("GPUBuffer", typeof(GPUBuffer), "-gpubf");
    public static ResourceType ShaderStage = new ResourceType("ShaderStage", typeof(ShaderStage), "-shrst");
    public static ResourceType OpenGLResource = new ResourceType("OpenGLResource", typeof(OpenGLResource), "-glrsc");
    public static ResourceType FramebufferGeneric = new ResourceType("FramebufferGeneric", typeof(FramebufferGeneric), "-fbgen");
    public static ResourceType ContextShader = new ResourceType("ContextShader", typeof(GpuShader), "-shrct");
    public static ResourceType ShaderUniformBlock = new ResourceType("ShaderUniformBlock", typeof(ShaderUniformBlock), "-shrbo");
    public static ResourceType ShaderMemoryBlock = new ResourceType("ShaderMemoryBlock", typeof(ShaderMemoryBlock), "-shrmb");
    public static ResourceType ShaderStorageBlock = new ResourceType("ShaderStorageBlock", typeof(ShaderStorageBlock), "-shrsb");
    public static ResourceType VertexArrayObject = new ResourceType("VertexArrayObject", typeof(VertexArrayObject), "-vao");

    //This must be the concrete (final) class of the given resource
    //Type names can change, which is why we are using constant values.
    public static ResourceType Undefined = new ResourceType("Undefined", typeof(DataBlock), "-undf");

    public static ResourceType MeshContextManager = new ResourceType("MeshContextManager", typeof(OpenGLContextDataManager<VertexArrayObject>), "-ctmgrm");
    public static ResourceType ShaderContextManager = new ResourceType("ShaderContextManager", typeof(OpenGLContextDataManager<Dictionary<int, GpuShader>>), "-ctmgrs");
    public static ResourceType TextureContextManager = new ResourceType("TextureContextManager", typeof(OpenGLContextDataManager<GpuTexture>), "-ctmgrt");
   
    public static ResourceType Image = new ResourceType("Image", typeof(Image), "-image");
    public static ResourceType ImageGenerator = new ResourceType("ImageGenerator", typeof(ImageGen), "-imagegen");
    public static ResourceType ImageFile = new ResourceType("ImageFile", typeof(ImageFile), "-imagefile");
    public static ResourceType Texture = new ResourceType("Texture", typeof(Texture), "-texture");
    public static ResourceType Material = new ResourceType("Material", typeof(Material), "-mat");

    public static ResourceType Shader = new ResourceType("Shader", typeof(Shader), "-shr");
    public static ResourceType GLSLFile = new ResourceType("GLSLFile", typeof(ShaderDataSource), "-glslfile");

    public static ResourceType MeshData = new ResourceType("MeshData", typeof(MeshData), "-mesh");
    public static ResourceType MeshDataLoader = new ResourceType("MeshDataLoader", typeof(MeshDataLoader), "-meshld");
    public static ResourceType GLTFFile = new ResourceType("GLTFFile", typeof(GLTFFile), "-gltffile");

    public static ResourceType WorldObject = new ResourceType("WorldObject", typeof(WorldObject), "-obj");
    public static ResourceType CameraObject = new ResourceType("CameraObject", typeof(Camera3D), "-cam");
    public static ResourceType LightObject = new ResourceType("LightObject", typeof(Light), "-light");

    public static ResourceType FPSInputComponent = new ResourceType("FPSInputComponent", typeof(FPSInputComponent), "-fpscmp");
    public static ResourceType EventComponent = new ResourceType("EventComponent", typeof(EventComponent), "-eventcmp");
    public static ResourceType AnimationComponent = new ResourceType("AnimationComponent", typeof(AnimationComponent), "-animcmp");

    public static ResourceType AnimationLoader = new ResourceType("AnimationLoader", typeof(AnimationLoader), "-animld");
    public static ResourceType AnimationData = new ResourceType("AnimationData", typeof(AnimationData), "-anim");

    public static ResourceType WorldProps = new ResourceType("WorldProps", typeof(WorldProps), "-wprops");
    //public static ResourceType RenderView = new ResourceType(81, typeof(RenderView), "renderview");
    //public static ResourceType UiWindowBase = new ResourceType(82, typeof(UiWindowBase), "window");
    //public static ResourceType MainWindow = new ResourceType(83, typeof(MainWindow), "mainwindow");
    public static ResourceType Gui2dManager = new ResourceType("Gui2dManager", typeof(Gui2dManager), "-gui2d");

    public static ResourceType? GetResourceType(Type dbb)
    {
      
      if (ResourceType.ResourceTypesArray.TryGetValue(dbb, out var rt))
      {
        return rt;
      }
      Gu.BRThrowException($"Resource type does not exist for type {dbb.ToString()}");
      return null;
    }

    private int _id = 0;
    private Type? _classType = null;
    private string _suffix = "";
    private string _name = "";

    public string Name { get { return _name; } }
    public int ID { get { return _id; } }
    public Type? ClassType { get { return _classType; } }
    public string Suffix { get { return _suffix; } }
    public static Dictionary<Type, ResourceType> ResourceTypesArray { get { return _resourceTypesArray; } }

    public ResourceType(string name, Type classType, string suffix)
    {
      _name = name;
      _classType = classType;
      _suffix = suffix;
      _id = _name.GetHashCode();
      //Make sure the enums are not duplicated
      foreach (var rt in _resourceTypesArray)
      {
        Gu.Assert(rt.Value.ID != _id, $"Duplicate resource type ID '{_id}'");
        Gu.Assert(rt.Value.ClassType != classType, $"Duplicate resource class type '{classType.ToString()}'");
        Gu.Assert(rt.Value.Suffix != suffix, $"Duplicate resource suffix type '{suffix.ToString()}'");
        Gu.Assert(rt.Value.Name != name, $"Duplicate resource suffix type '{suffix.ToString()}'");
      }

      _resourceTypesArray.Add(classType, this);
    }
  }
  [Serializable]
  [DataContract]
  public class ResourceTable
  {
    #region Members

    [NonSerialized] public static SerializedFileVersion c_fileVersion = new SerializedFileVersion(10000);

    [DataMember] private Dictionary<UInt64, DataBlock> _resourcesById = new Dictionary<UInt64, DataBlock>();//mapped to uniqueid
    [NonSerialized] private Dictionary<ResourceType, Dictionary<string, DataBlock>> _resourcesByTypeAndName = new Dictionary<ResourceType, Dictionary<string, DataBlock>>();

    #endregion
    #region Methods
    // private void SaveResourceFile(FileMode savemode = FileMode.Text)
    // {
    //   CleanResources();
    //   _resourcesById = _resourcesById.ConstructIfNeeded();
    //   var fl = GetResourceFileLoc(savemode);
    //   Gu.BackupFile(fl);

    //   if (savemode == FileMode.Text)
    //   {
    //     string json = JsonConvert.SerializeObject(_resourcesById, Formatting.Indented);
    //     fl.WriteAllText(json);
    //   }
    //   else
    //   {
    //     //This is not tested
    //     Gu.MustTest();
    //     var enc = Encoding.GetEncoding("iso-8859-1");
    //     using (var fs = fl.OpenRead())
    //     using (var bwFile = new System.IO.BinaryWriter(fs, enc))
    //     {
    //       SerializeTools.SerializeDictionary<UInt64, DataBlock>(bwFile, this._resourcesById);
    //     }
    //   }
    // }

    private static string ResourceMsg(DataBlock? d, string msg)
    {
      string n = d != null ? d.Name : "null block!";
      string s = d != null ? d.ToString() : "null block!";
      string m = $"{n}: {msg}\n  data:\n  {s}";
      return m;
    }
    public static void ResourceError(DataBlock? d, string msg)
    {
      string m = ResourceMsg(d, msg);
      Gu.Log.Error(m);
      Gu.BRThrowException(m);
    }
    public static void ResourceWarning(DataBlock? d, string msg)
    {
      string m = ResourceMsg(d, msg);
      Gu.Log.Warn(m);
      Gu.DebugBreak();
    }
    public void IterateResource<T>(Func<T, LambdaBool> func) where T : DataBlock
    {
      var rt = ResourceType.GetResourceType(typeof(T));
      if (_resourcesByTypeAndName.TryGetValue(rt, out var ob_by_name))
      {
        foreach (var name_ob in ob_by_name)
        {
          if (func((T)name_ob.Value) == LambdaBool.Break)
          {
            break;
          }
        }
      }
    }
    public void CreateResource(DataBlock baseClass, string name)
    {
      var rsc = GetResourceByName(ResourceType.GetResourceType(baseClass.GetType()), name);
      if (rsc != null)
      {
        Gu.BRThrowException(
          $"Tried to create resource with duplicate name '{name}':\n {rsc.ToString()} \n"
        + $" *Use GetUniqueName() to ensure unique name.\n");
      }
      //ResourceNode rn = new ResourceNode(name, baseClass.ResourceType, id);
      //we have to add new resources being that, we need to know what id's/names are unique and not
      AddResource(baseClass);
      //type/file is only valid for type resources
    }
    public bool DeleteResource(DataBlock n)
    {
      Gu.Assert(n != null);
      bool ret = true;
      ret = ret && _resourcesById.Remove(n.UniqueID);
      if (_resourcesByTypeAndName.TryGetValue(ResourceType.GetResourceType(n.GetType()), out var dict))
      {
        ret = ret && dict.Remove(n.Name);
        if (dict.Count == 0)
        {
          _resourcesByTypeAndName.Remove(ResourceType.GetResourceType(n.GetType()));
        }
      }
      else
      {
        ret = false;
      }
      if (ret == false)
      {
        Gu.Log.Error($"Failed to delete resource '{n.Name}', id='{n.UniqueID}'");
      }
      return ret;
    }
    public DataBlock? GetResourceByName(ResourceType type, string name)
    {
      Gu.Assert(_resourcesByTypeAndName != null);
      DataBlock? ret = null;
      if (_resourcesByTypeAndName.TryGetValue(type, out var file_res))
      {
        if (file_res.TryGetValue(name, out var val))
        {
          ret = val;
        }
      }
      return ret;
    }
    public string GetUniqueName(ResourceType rt, string desired_name)
    {
      //Gew new unique name, and increment the name index, if present
      //based on '-0' identifier, like Blender 
      string final_name = desired_name;

      if (_resourcesByTypeAndName.ContainsKey(rt))
      {
        int index = 1;
        if (GetResourceNameIndex(final_name, out var name, out index))
        {
          final_name = name;
          index = index + 1;
        }

        for (int ind = index; Gu.WhileTrueGuard(ind, Gu.c_intMaxWhileTrueLoop); ind++)
        {
          final_name = $"{desired_name}-{(ind)}"; //restart index if not found

          var found = _resourcesByTypeAndName[rt].Keys.Where(x => x == final_name).FirstOrDefault();
          if (found == null)
          {
            Gu.Log.Info($"Duplicate name '{desired_name}' found => renaming to '{final_name}'");
            break;
          }
        }
      }
      return final_name;
    }
    public DataBlock? ChangeResourceName(string name, string value)
    {
      //TODO:
      Gu.BRThrowNotImplementedException();

      return null;
    }
    public DataBlock? GetResourceById(UInt64 id)
    {
      if (_resourcesById.TryGetValue(id, out var rsc))
      {
        return rsc;
      }
      return null;
    }

    public UInt64 GetNewUniqueId()
    {
      UInt64 id = Library.c_iIDStart;
      if (_resourcesById.Count > 0)
      {
        id = _resourcesById.MaxBy(k => k.Key).Key + 1;
        if (id == Library.c_iUntypedUnique)//unlikely
        {
          id += 1;
        }
      }
      return id;
    }
    public void CleanResources()
    {
      //remove resources with empty refs
      //_resourcesById = this._resourcesById.Where(x => x.Value.HasRef).ToDictionary(pair => pair.Key, pair => pair.Value);
      //TODO:
    }
    private void AddResource(DataBlock res)
    {
      Gu.Assert(res != null);
      //var rn = resource.Resource;

      if (GetResourceById(res.UniqueID) != null)
      {
        Gu.Log.Error($"Duplicate: Resource type '{ResourceType.GetResourceType(res.GetType()).ToString()}' id '{res.UniqueID}' was already found in resource ID list: {res.ToString()}");
        Gu.DebugBreak();
      }
      else
      {
        _resourcesById.Add(res.UniqueID, res);
      }
      if (GetResourceByName(ResourceType.GetResourceType(res.GetType()), res.Name) != null)
      {
        Gu.Log.Error($"Duplicate: Resource type '{ResourceType.GetResourceType(res.GetType()).ToString()}' id '{res.UniqueID}' was already found in resource ID list: {res.ToString()}");
        Gu.DebugBreak();
      }
      else
      {
        var rt = ResourceType.GetResourceType(res.GetType());

        Dictionary<string, DataBlock>? byname = null;
        if (!_resourcesByTypeAndName.TryGetValue(rt, out byname))
        {
          byname = new Dictionary<string, DataBlock>();
          _resourcesByTypeAndName.Add(rt, byname);
        }
        byname.Add(res.Name, res);
      }
    }
    private bool GetResourceNameIndex(string desired_name, out string name_without_suffix, out int index)
    {
      //return true if we the name had a valid suffix, false otherwise
      //Return suffix parts in the out params
      index = 1;
      name_without_suffix = desired_name;
      var ind = desired_name.LastIndexOf('-');
      if (ind >= 0)
      {
        var suffix = desired_name.Substring(ind, desired_name.Length - ind);
        if (suffix.Length > 1)
        {
          var valuestr = suffix.Substring(1, suffix.Length - 1);
          if (Int32.TryParse(valuestr, out var value_int))
          {
            name_without_suffix = desired_name.Substring(0, ind);
            index = value_int;
            return true;
          }
        }
      }
      return false;
    }
    #endregion
  }

  [DataContract]
  public class Library : ResourceTable
  {
    //Resource Database / asset manager / Library
    #region Constants

    [NonSerialized] public const UInt64 NullID = 0;
    [NonSerialized] public const string UnsetName = "<unset>";
    [NonSerialized] public const string CopyName = "-copy";
    [NonSerialized] public const Int32 c_idTypeMultiplier = 1000000;
    [NonSerialized] public const UInt64 c_iUntypedUnique = 9999999999999; //untyped
    [NonSerialized] public const UInt64 c_iIDStart = 200; // prevent low ids for debugging
    [NonSerialized] public const string ResourceFileNameText = "resources.json";
    [NonSerialized] public const string ResourceFileNameBinary = "resources.dat";

    #endregion
    #region Members

    [NonSerialized] private DeltaTimer _shaderChangedTimer = null;
    [NonSerialized] private float _checkForShaderFileChangeUpdatesTimeSeconds = 0.5f;
    [NonSerialized] private Dictionary<ulong, Dictionary<object, List<string>>> PointerFixUp = null;

    #endregion
    #region Methods

    public Library()
    {
      //Clear temp, or cache directories if needed
      if (Gu.EngineConfig.ClearCacheOnStart)
      {
        Gu.ClearDirectory(Gu.LocalCachePath);
      }
      if (Gu.EngineConfig.ClearTmpOnStart)
      {
        Gu.ClearDirectory(Gu.LocalTmpPath);
      }
      var dir = Path.GetDirectoryName(Gu.LocalTmpPath);
      if (!Directory.Exists(dir))
      {
        Directory.CreateDirectory(dir);
      }

      //Start the shader poller, to update shaders so we don't gotta compile and run again 
      //We can poll for engine config, or any other file realyl, but that is a much larger system.
      if (_shaderChangedTimer == null)
      {
        _shaderChangedTimer = new DeltaTimer(_checkForShaderFileChangeUpdatesTimeSeconds, ActionRepeat.Repeat, ActionState.Run);
      }

      //LoadResourceFile();
    }
    public void FixUpPointers()
    {
      foreach (var idp in PointerFixUp)
      {
        var res = GetResourceById(idp.Key);

        foreach (var ob_f in idp.Value)
        {
          Type typeInQuestion = ob_f.Key.GetType();
          foreach (var fieldstr in ob_f.Value)
          {
            FieldInfo? field = typeInQuestion.GetField(fieldstr, BindingFlags.NonPublic | BindingFlags.Instance);
            Gu.Assert(field != null, $"Field {fieldstr} was null.");
            field.SetValue(ob_f.Key, res);
          }
        }
      }
      PointerFixUp = null;
    }
    public void AddFixUp(ulong id, object obj, string field)
    {
      PointerFixUp = new Dictionary<ulong, Dictionary<object, List<string>>>();
      Dictionary<object, List<string>>? dict = null;
      if (!PointerFixUp.TryGetValue(id, out dict))
      {
        dict = new Dictionary<object, List<string>>();
        PointerFixUp.Add(id, dict);
      }
      List<string>? fields = null;
      if (!dict.TryGetValue(obj, out fields))
      {
        fields = new List<string>();
        dict.Add(obj, fields);
      }
      fields.Add(field);
    }
    public static string MakeDatapathName(string baseName, Type subclassType)
    {
      //The datapath name is mostly for debugging with OpenGL ObjectLabel on the Gpu.
      string suffixname = baseName;
      var rt = ResourceType.GetResourceType(subclassType);
      suffixname += rt.Suffix;
      var super = subclassType.BaseType;
      if (super != null && super != typeof(System.Object) && super != typeof(HasGpuResources))
      {
        suffixname = MakeDatapathName(suffixname, super);
      }
      return suffixname;
    }
    private FileLoc GetResourceFileLoc(FileMode savemode)
    {
      string fn = "";
      if (savemode == FileMode.Text) { fn = Library.ResourceFileNameText; }
      else if (savemode == FileMode.Binary) { fn = Library.ResourceFileNameBinary; }
      FileLoc fl = new FileLoc(Gu.SavePath, fn, FileStorage.Embedded);
      if (!fl.Exists)
      {
        fl = new FileLoc(Gu.SavePath, fn, FileStorage.Disk);
      }
      return fl;
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

          using (Stream stream = File.Open(disk_path, System.IO.FileMode.Open, FileAccess.Read, FileShare.None))
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
    public static void SaveImage(string path, Image image, bool flipOpenGL)
    {
      //flipOpenGL - in OpenGL texture origin is the bottom left of screen, setting this will flip it upright.
      var dir = Path.GetDirectoryName(path);
      if (!Directory.Exists(dir))
      {
        Directory.CreateDirectory(dir);
      }
      if (image.Format != Image.ImagePixelFormat.RGBA32ub)
      {
        Gu.Log.Warn($"Image '{path}' format was not rgba32");
      }

      string ext = System.IO.Path.GetExtension(path).ToLower();
      try
      {
        using (Stream fs = File.OpenWrite(path))
        {
          StbImageWriteSharp.ImageWriter writer = new StbImageWriteSharp.ImageWriter();

          Image img2 = (Image)image.Clone();
          if (image.Format != Image.ImagePixelFormat.RGBA32ub)
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
    public static void SaveTexture(FileLoc loc, Texture tex, bool formatNonRGBAtoRGBA, bool flipOpenGL = true, int cubemapside = -1, bool setAlphatoOne = false)
    {
      //formatNonRGBAtoRGBA - Convert things (like R32) to RGBA so we can see it.

      PixelFormat fmt = PixelFormat.AbgrExt;
      PixelType type = PixelType.Byte;
      PixelInternalFormat internalFmt = PixelInternalFormat.Alpha;

      Image img = Gpu.GetTextureDataFromGpu(tex.GlId, tex.TextureTarget, ref fmt, ref type, ref internalFmt, cubemapside);

      if (formatNonRGBAtoRGBA)
      {
        if (fmt == PixelFormat.RedInteger && type == PixelType.UnsignedInt)
        {
          for (int iy = 0; iy < img.Height; iy++)
          {
            for (int ix = 0; ix < img.Width; ix++)
            {
              var p = img.GetPixel_RGBA32ub(ix, iy);
              //r32ui is in agbr -> rgba
              var tmp = p.a;
              p.a = p.r;
              p.r = tmp;
              tmp = p.b;
              p.b = p.g;
              p.g = tmp;
              img.SetPixel_RGBA32ub(ix, iy, p);
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
            var p = img.GetPixel_RGBA32ub(ix, iy);
            float f = BitConverter.ToSingle(new byte[] { p.r, p.g, p.b, p.a }, 0);
            //r32ui is in agbr -> rgba
            p.r = p.g = p.b = (byte)((f / 1.0f) * 255.0f);
            p.a = 255;
            img.SetPixel_RGBA32ub(ix, iy, p);
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
            var p = img.GetPixel_RGBA32ub(ix, iy);
            p.a = byte.MaxValue;
            img.SetPixel_RGBA32ub(ix, iy, p);
          }
        }
      }
      SaveImage(loc.QualifiedPath, img, flipOpenGL);
    }
    public void Update(double dt)
    {
      _shaderChangedTimer.Update(dt, () =>
        {
          IterateResource<Shader>((s) =>
          {
            s.CheckSourceChanged();
            return LambdaBool.Continue;
          });

        });
      // _cleanupTimer.Update(dt, () =>
      //   {
      //     foreach (var pair in Shaders)
      //     {
      //       if (pair.Value.TryGetTarget(out var s))
      //       {
      //         s.CheckSourceChanged();
      //       }
      //     }
      //   });
    }

    //Load functions:
    // Return the resource if it is already loaded
    // Load the resource if it doesn't exist
    // resources are identified by name
    public bool TryLoadModel(string name, FileLoc loc, out WorldObject? model, bool fliptris = true)
    {
      return TryLoad<WorldObject>(name, out model, () => { return LoadModel(name, loc, fliptris); });
    }
    public bool TryLoadImage(string name, FileLoc loc, out Image? image)
    {
      return TryLoad<Image>(name, out image, () => { return LoadImage(name, loc); });
    }
    public bool TryLoadShader(string name, string generic_name, bool hasgs, FileStorage storage, PrimitiveType? gsprimtype, out Shader? sh)
    {
      return TryLoad<Shader>(name, out sh, () => { return LoadShader(name, generic_name, hasgs, storage, gsprimtype); });
    }
    public bool TryLoadMaterial(string name, Shader s, out Material? mat)
    {
      return TryLoad<Material>(name, out mat, () => { return LoadMaterial(name, s); });
    }
    public bool TryLoadTexture(string name, Image? img, bool mipmaps, TexFilter filter, out Texture? tex)
    {
      return TryLoad<Texture>(name, out tex, () => { return LoadTexture(name, img, mipmaps, filter); });
    }
    public bool TryLoadTexture(string name, FileLoc loc, bool mipmaps, TexFilter filter, out Texture? tex)
    {
      return TryLoad<Texture>(name, out tex, () => { return LoadTexture(name, loc, mipmaps, filter); });
    }
    public bool TryLoadMesh(string name, MeshGenParams p, out MeshData? mesh)
    {
      return TryLoad<MeshData>(name, out mesh, () => { return LoadMesh(name, p); });
    }
    //The methods below will throw if load fails
    private T? LoadThing<T>(string name) where T : DataBlock
    {
      var rt = ResourceType.GetResourceType(typeof(T));
      var rn = GetResourceByName(rt, name);

      Gu.Assert(rn != null, $"Failed to load '{name}'");
      Gu.Assert(rn is T, $"Resource '{name}' was not of type '{typeof(T).ToString()}'");
      return rn as T;
    }
    public Image? LoadImage(string name)
    {
      return LoadThing<Image>(name);
    }
    public Texture? LoadTexture(string name)
    {
      return LoadThing<Texture>(name);
    }
    public Material? LoadMaterial(string name)
    {
      return LoadThing<Material>(name);
    }
    public Shader? LoadShader(string name)
    {
      return LoadThing<Shader>(name);
    }
    public WorldObject? LoadModel(string name)
    {
      return LoadThing<WorldObject>(name);
    }
    public MeshData? LoadMesh(string name)
    {
      return LoadThing<MeshData>(name);
    }
    public bool TryLoad<T>(string name, out T? outref, Func<T?> act) where T : DataBlock
    {
      bool ret = true;
      outref = null;
      try
      {
        outref = act();
      }
      catch (Exception ex)
      {
        Gu.Log.Error($"Failed to load type '{typeof(T).ToString()}' name:'{name}'");
        Gu.DebugBreak();
      }
      return ret;
    }
    public Image? LoadImage(string name, FileLoc loc)
    {
      Image? image = null;
      var rn = GetResourceByName(ResourceType.WorldObject, name);
      if (rn == null)
      {
        ImageFile f = new ImageFile(name, loc);
        image = f.Load<Image>(name);
      }
      else
      {
        Gu.Assert(rn is Image);
        return rn as Image;
      }
      return image;
    }
    public Texture? LoadTexture(string name, Image? img, bool mipmaps, TexFilter filter)
    {
      Texture? ret = null;
      var rn = GetResourceByName(ResourceType.Texture, name);
      if (rn == null)
      {
        ret = new Texture(name, img, mipmaps, filter);
        ret.PromoteResource(ResourcePromotion.LibraryAdd, new SerializedDataSource(name));
      }
      else
      {
        Gu.Assert(rn is Texture);
        return rn as Texture;
      }
      return ret;
    }
    public Texture? LoadTexture(string name, FileLoc loc, bool mipmaps, TexFilter filter)
    {
      Texture? ret = null;
      var rn = GetResourceByName(ResourceType.Texture, name);
      if (rn == null)
      {
        try
        {
          var img = LoadImage(name, loc);
          Gu.Assert(img != null);
          ret = new Texture(name, img, mipmaps, filter);
          ret.PromoteResource(ResourcePromotion.LibraryAdd, new SerializedDataSource(name));
        }
        catch (Exception ex)
        {
          Gu.Log.Error($"Failed to load texture {name}", ex);
          ret = LoadTexture(RName.Tex2D_DefaultFailedTexture);
        }
      }
      else
      {
        Gu.Assert(rn is Texture);
        return rn as Texture;
      }
      return ret;
    }
    public Material? LoadMaterial(string name, Shader s)
    {
      Gu.Assert(s != null);
      Material? ret = null;
      var rn = GetResourceByName(ResourceType.Material, name);
      if (rn == null)
      {
        ret = new Material(name, s);
        ret.PromoteResource(ResourcePromotion.LibraryAdd, new SerializedDataSource(name));
      }
      else
      {
        Gu.Assert(rn is Material);
        ret = rn as Material;
      }
      return ret;
    }
    public Shader? LoadShader(string name, string generic_name, bool hasgs, FileStorage storage, PrimitiveType? gsprimtype = null)
    {
      Shader? ret = null;
      var rn = GetResourceByName(ResourceType.Shader, name);
      if (rn == null)
      {
        ret = new Shader(name, generic_name, hasgs, storage, gsprimtype);
        ret.PromoteResource(ResourcePromotion.LibraryAdd, new SerializedDataSource(name));
      }
      else
      {
        Gu.Assert(rn is Shader);
        ret = rn as Shader;
      }
      return ret;
    }
    public MeshData? LoadMesh(string name, MeshGenParams p)
    {
      MeshData? ret = null;
      var rn = GetResourceByName(ResourceType.MeshData, name);
      if (rn == null)
      {
        MeshGen mg = new MeshGen(name, p);
        ret = mg.Load<MeshData>(name);
        ret.PromoteResource(ResourcePromotion.LibraryAdd, new SerializedDataSource(name));
      }
      else
      {
        Gu.Assert(rn is MeshData);
        ret = rn as MeshData;
      }
      return ret;
    }
    public WorldObject? LoadModel(string name, FileLoc loc, bool fliptris)
    {
      WorldObject? ret = null;
      var rn = GetResourceByName(ResourceType.WorldObject, name);
      if (rn == null)
      {
        GLTFFile gf = new GLTFFile(name, loc, fliptris);
        ret = gf.Load<WorldObject>(name);
        ret.PromoteResource(ResourcePromotion.LibraryAdd, new SerializedDataSource(name));
      }
      else
      {
        Gu.Assert(rn is WorldObject);
        ret = rn as WorldObject;
      }
      return ret;
    }


    #endregion

  }
}
