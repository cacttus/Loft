using System;
using System.Reflection;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Loft
{
  public enum ResourceLoadResult
  {
    NotLoaded,
    Loaded,
    LoadFailed
  }
  public class Rs
  {
    public class Image
    {
      public static string DefaultNormalPixelZUp = "Image_DefaultNormalPixelZUp";
      public static string DefaultNormalPixelYUp = "Image_DefaultNormalPixelYUp";
      public static string DefaultFailedPixel = "Image_DefaultFailedPixel";
      public static string DefaultWhitePixel = "Image_DefaultWhitePixel";
    }
    public class Tex2D
    {
      public static string DefaultFailedTexture = "DefaultFailedTexture";
      public static string DefaultWhitePixel = "Tex2D_DefaultWhitePixel";
      public static string DefaultBlackPixelNoAlpha = "Tex2D_DefaultBlackPixelNoAlpha";
      public static string DefaultNormalPixel = "Tex2D_DefaultNormalPixel";
    }
    public class Shader
    {
      public static string DebugDraw_Lines = "Shader_DebugDraw_Lines";
      public static string DebugDraw_Points = "Shader_DebugDraw_Points";
      public static string DebugDraw_Tris = "Shader_DebugDraw_Tris";
      public static string Wireframe = "Shader_Wireframe";
      public static string Solid = "dbgSolid";
      public static string GuiShader = "Shader_GuiShader";
      public static string DefaultObjectShader = "Shader_DefaultObjectShader";
      public static string DefaultFlatColorShader = "Shader_DefaultFlatColorShader";
      public static string DefaultBillboardPoints = "Shader_DefaultBillboardPoints";
      public static string VertexFaceNormals = "Shader_VertexFaceNormals";
    }
    public class Material
    {
      public static string DefaultObjectMaterial = "Material_DefaultObjectMaterial";
      public static string DebugDraw_VertexNormals_FlatColor = "Material_DebugDraw_VertexNormals_FlatColor";
      public static string DebugDrawMaterial_Lines = "Material_DebugDrawMaterial_Lines";
      public static string DebugDrawMaterial_Points = "Material_DebugDrawMaterial_Points";
      public static string DebugDrawMaterial_Tris = "Material_DebugDrawMaterial_Tris";
      public static string DebugDraw_Wireframe_FlatColor = "DebugDraw_Wireframe_FlatColor";
      public static string DebugDraw_Solid_FlatColor = "DebugDraw_Solid_FlatColor";
    }
    public class Mesh
    {
      public static string DefaultBox = "Mesh_DefaultBox";
    }
    public class Model
    {
      public static string Camera = "Camera";
      public static string Gear = "Gear";
      public static string Barrel = "Barrel";
    }
  }
  public enum ResourceType
  {
    //this enum is almost purely just for grouping object names into namespaces e.g. GLTFFile, OBJFile are one namespace
    //if we didn't need that, we coudl just use the System.Type of the object, but Blender has a namespace sort of thing so that is bieng copied.
    Undefined,
    Shader,
    MeshView,
    MeshData,
    Model,
    WorldObject,
    Armature,
    Material,
    Image,
    Texture,
    Component,
    Constraint,
  }

  [DataContract]
  public class DynamicFileLoader
  {
    //dynamically check for file changes
    public DateTime MaxModifyTime { get { return _maxModifyTime; } }
    public long PollIntervalMs { get { return _poll; } }

    public List<FileLoc> Files { get { return _files; } set { _files = value; } }
    private List<FileLoc> _files = new List<FileLoc>();
    private Func<List<FileLoc>, bool>? _onFilesChanged = null;//return false if the loading/compiling of the new file failed.
    private DateTime _maxModifyTime = DateTime.MinValue;
    private long _poll = 500;
    private DateTime _lastFailTime = DateTime.MinValue;

    protected DynamicFileLoader() { } //clone/serialize
    public DynamicFileLoader(List<FileLoc> files, Func<List<FileLoc>, bool> onFilesChanged, long pollInterval = 500)
    {
      _files = files;
      _poll = pollInterval;
      Gu.Assert(onFilesChanged != null);
      _onFilesChanged = onFilesChanged;

      CheckFilesChanged(true);

      //Registers a synchronous timer for each file type, not each file - to prevent too many timers
      // possibly, we could use a timer for each file, async, ect, but this neds to be tested
      // (see AsyncTimer)
      Lib.AddDynamicLoader(this);
    }
    public void CheckFilesChanged(bool initialCheck = false)
    {
      //initialCheck = updates modify time
      if (_files != null)
      {
        List<FileLoc> changed = new List<FileLoc>();
        foreach (var f in this._files)
        {
          var wt = f.GetLastWriteTime(true);
          if (wt > _maxModifyTime && wt > _lastFailTime)
          {
            // ** Set the modify time to the maximum file mod - even if compile fails. This prevents infinite checking
            _maxModifyTime = wt;
            changed.Add(f);
          }
        }
        if (changed.Count > 0 && initialCheck == false)
        {
          Gu.Log.Info($"Resource '{changed.ToString()}' has changed, hot-re-loading");
          if (_onFilesChanged != null)
          {
            if (!_onFilesChanged.Invoke(changed))
            {
              _lastFailTime = DateTime.Now;
            }
          }
        }
      }
    }
  }


  [DataContract]
  public class Lib
  {
    //Resource Database / asset manager / Library

    #region Classes

    private class DynamicLoaderInfo
    {
      //update compiled things so we don't gotta compile and run again and we can easily see changes
      public List<WeakReference<DynamicFileLoader>> _loaders = new List<WeakReference<DynamicFileLoader>>();
      private DeltaTimer _timer;
      public Type _type;

      System.Threading.Timer t = new Timer((x) => { });

      public DynamicLoaderInfo(Type t, long pollIntervalMS)
      {
        _type = t;
        _timer = new DeltaTimer(pollIntervalMS, ActionRepeat.Repeat, ActionState.Run, () =>
        {
          for (int iitem = _loaders.Count - 1; iitem >= 0; iitem--)
          {
            var item = _loaders[iitem];
            if (item.TryGetTarget(out var loader))
            {
              loader.CheckFilesChanged();
            }
            else
            {
              _loaders.RemoveAt(iitem);
            }
          }
        });
      }
      public void AddLoader(DynamicFileLoader ll)
      {
        //allow system to dispose the object
        var loaderref = new WeakReference<DynamicFileLoader>(ll);
        _loaders.Add(loaderref);
      }
      public void Update(double dt)
      {
        _timer.Update(dt);
      }
    }

    #endregion
    #region Constants

    public static SerializedFileVersion c_fileVersion = new SerializedFileVersion(10000);
    public const UInt64 c_iNullID = 0;
    public const UInt64 c_iIDStart = 2000; // prevent low ids for debugging
    public const string UnsetName = "<unset>";
    public const string CopyName = "-copy";

    #endregion
    #region Members

    private ulong _iMaxId = c_iIDStart;

    private Dictionary<UInt64, DataBlock> _resourcesById = new Dictionary<UInt64, DataBlock>();
    private Dictionary<ResourceType, Dictionary<string, DataBlock>> _resourcesByTypeAndName = new Dictionary<ResourceType, Dictionary<string, DataBlock>>();
    private float _checkForShaderFileChangeUpdatesTimeSeconds = 0.5f;
    private Dictionary<ulong, Dictionary<object, List<string>>> PointerFixUp = null;
    private static Dictionary<Type, DynamicLoaderInfo> _dynamicLoaders = new Dictionary<Type, DynamicLoaderInfo>();

    #endregion

    public Lib()
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
      if (setAlphatoOne && (
        internalFmt == PixelInternalFormat.Rgba ||
        internalFmt == PixelInternalFormat.Rgba32f ||
        internalFmt == PixelInternalFormat.Rgba8 ||
        internalFmt == PixelInternalFormat.Rgba16 ||
        internalFmt == PixelInternalFormat.Rgba16f ||
        internalFmt == PixelInternalFormat.Rgba32f ||
        internalFmt == PixelInternalFormat.Rgba32i
       ))
      {
        for (int iy = 0; iy < img.Height; iy++)
        {
          for (int ix = 0; ix < img.Width; ix++)
          {
            var p = img.GetPixel_RGBA32ub(ix, iy);
            p.a = (byte)(Math.Clamp((float)p.a / 255.0f, 0, 1) * 255.0f);// byte.MaxValue;

            img.SetPixel_RGBA32ub(ix, iy, p);
          }
        }
      }
      SaveImage(loc.QualifiedPath, img, flipOpenGL);
    }
    public void Update(double dt)
    {
      foreach (var kvp in _dynamicLoaders)
      {
        if (kvp.Value != null)
        {
          kvp.Value.Update(dt);
        }
      }
    }
    public static void AddDynamicLoader(DynamicFileLoader loader)
    {
      Gu.Assert(loader != null);
      var t = loader.GetType();
      DynamicLoaderInfo? inf = null;
      if (!_dynamicLoaders.TryGetValue(t, out inf))
      {
        inf = new DynamicLoaderInfo(t, loader.PollIntervalMs);
        _dynamicLoaders.Add(t, inf);
      }
      inf.AddLoader(loader);
    }
    public bool DeleteResource(DataBlock n)
    {
      Gu.Assert(n != null);
      bool ret = true;
      ret = ret && _resourcesById.Remove(n.UniqueID);
      if (_resourcesByTypeAndName.TryGetValue(n.ResourceType, out var dict))
      {
        ret = ret && dict.Remove(n.Name);
        if (dict.Count == 0)
        {
          _resourcesByTypeAndName.Remove(n.ResourceType);
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
    public string GetUniqueName(Type t, string desired_name)
    {
      return GetUniqueName(GetResourceType(t), desired_name);
    }
    public string GetUniqueName(ResourceType rt, string desired_name)
    {
      //Gew new unique name, and increment the name index, if present
      //based on '-0' identifier, like Blender 
      string final_name = desired_name;

      if (_resourcesByTypeAndName.TryGetValue(rt, out var sdb))
      {
        if (sdb.TryGetValue(desired_name, out var db))
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
      }
      return final_name;
    }
    public DataBlock? ChangeResourceName(string name, string value)
    {
      //TODO:
      Gu.BRThrowNotImplementedException();

      return null;
    }
    private DataBlock? GetResourceById(UInt64 id)
    {
      if (_resourcesById.TryGetValue(id, out var rsc))
      {
        return rsc;
      }
      return null;
    }
    //at 1000 objs per second - 9 million years to wrap a ulong
    public UInt64 GetUniqueId()
    {
      return _iMaxId++;
    }
    public void CleanResources()
    {
      //remove resources with empty refs
      //_resourcesById = this._resourcesById.Where(x => x.Value.HasRef).ToDictionary(pair => pair.Key, pair => pair.Value);
      //TODO:
    }
    public T AddE<T>(T res) where T : DataBlock
    {
      //add an embedded resource
      //ensures we do not duplicate embedded resources on Build()
      res.IsEmbedded = true;
      Add(res);
      return res;
    }
    public void Add(DataBlock res)
    {
      Gu.Assert(res != null);
      if (res.ResourceType == ResourceType.Undefined)
      {
        res.ResourceType = GetResourceType(res.GetType());
      }
      Gu.Assert(res.ResourceType != ResourceType.Undefined);

      if (res.IsEmbedded == false)
      {
        res.Name = GetUniqueName(res.ResourceType, res.Name);
      }

      Dictionary<string, DataBlock>? byname = null;
      if (!_resourcesByTypeAndName.TryGetValue(res.ResourceType, out byname))
      {
        byname = new Dictionary<string, DataBlock>();
        _resourcesByTypeAndName.Add(res.ResourceType, byname);
      }

      byname.Add(res.Name, res);
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
    private string ResourceMsg(DataBlock? d, string msg)
    {
      string n = d != null ? d.Name : "null block!";
      string s = d != null ? d.ToString() : "null block!";
      string m = $"{n}: {msg}\n  data:\n  {s}";
      return m;
    }
    public void ResourceError(DataBlock? d, string msg)
    {
      string m = ResourceMsg(d, msg);
      Gu.Log.Error(m);
      //   Gu.BRThrowException(m);
    }
    public void ResourceWarning(DataBlock? d, string msg)
    {
      string m = ResourceMsg(d, msg);
      Gu.Log.Warn(m);
      Gu.DebugBreak();
    }

    // private bool TryGet<T>(string name, out T? outref, ResourceType rt) where T : DataBlock
    // {
    //   bool ret = true;
    //   outref = null;
    //   try
    //   {
    //     outref = Get<T>(name, rt);
    //   }
    //   catch (Exception ex)
    //   {
    //     Gu.Log.Error($"Failed to load type '{typeof(T).ToString()}' name:'{name}'");
    //     Gu.DebugBreak();
    //   }
    //   return ret;
    // }
    // public bool TryGetModel(string name, out WorldObject? model)
    // {
    //   return TryGet<WorldObject>(name, out model, ResourceType.Model);
    // }
    // public bool TryGetImage(string name, out Image? image)
    // {
    //   return TryGet<Image>(name, out image, ResourceType.Image);
    // }
    // public bool TryGetShader(string name, out Shader? sh)
    // {
    //   return TryGet<Shader>(name, out sh, ResourceType.Shader);
    // }
    // public bool TryGetMaterial(string name, out Material? mat)
    // {
    //   return TryGet<Material>(name, out mat, ResourceType.Material);
    // }
    // public bool TryGetTexture(string name, out Texture? tex)
    // {
    //   return TryGet<Texture>(name, out tex, ResourceType.MeshData);
    // }
    // public bool TryGetMesh(string name, out MeshData? mesh)
    // {
    //   return TryGet<MeshData>(name, out mesh, ResourceType.MeshData);
    // }
    private T Get<T>(string name, ResourceType rt) where T : DataBlock
    {
      var rn = GetResourceByName(rt, name);
      T r = rn as T;
      if (r == null)
      {
        Gu.DebugBreak();
      }
      System.Diagnostics.Debug.Assert(r != null);
      return r;
    }
    public Image GetImage(string name)
    {
      return Get<Image>(name, ResourceType.Image);
    }
    public Texture GetTexture(string name)
    {
      return Get<Texture>(name, ResourceType.Texture);
    }
    public Material GetMaterial(string name)
    {
      return Get<Material>(name, ResourceType.Material);
    }
    public Shader GetShader(string name)
    {
      return Get<Shader>(name, ResourceType.Shader);
    }
    public MeshData GetMesh(string name)
    {
      return Get<MeshData>(name, ResourceType.MeshData);
    }
    public ModelFile GetOrLoadModel(string name)
    {
      //This should return the constructed data, not the file.
      var mx = Get<ModelFile>(name, ResourceType.Model);
      mx.OnLoad();
      return mx;
    }
    public Image GetOrLoadImage(FileLoc loc)
    {
      ImageFile f = new ImageFile(loc.FileName, loc);
      f.OnLoad();
      return f.TheImage;
    }

    private ResourceType GetResourceType(Type t)
    {
      if (t == typeof(MeshData)) { return ResourceType.MeshData; }
      else if (t == typeof(Shader)) { return ResourceType.Shader; }
      else if (t == typeof(MeshView)) { return ResourceType.MeshView; }
      else if (t == typeof(ModelFile)) { return ResourceType.Model; }
      //else if (t == typeof(WorldObject)) { return ResourceType.WorldObject; }
      else if (t == typeof(Material)) { return ResourceType.Material; }
      else if (t == typeof(Image)) { return ResourceType.Image; }
      else if (t == typeof(Texture)) { return ResourceType.Texture; }
      else if (t == typeof(Component)) { return ResourceType.Component; }
      else if (t == typeof(Constraint)) { return ResourceType.Constraint; }
      Gu.BRThrowNotImplementedException();
      return ResourceType.Undefined;
    }
  }
}
