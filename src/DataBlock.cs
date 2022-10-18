using System.Runtime.Serialization;

namespace PirateCraft
{

  public enum DataCreateMode
  {
    CreateNewOnly,
    UseExistingOnly,
    CreateNewOrUseExisting_ByName
  }
  public enum DataPersistence
  {
    Temporary = 1, // temporary variable (name/id = none)
    Scene = 2, //attached to the scene (name/id = UT only)
    Library = 3, //library resrouce (name/id = RT + UT)
    LibraryDependency = 4, //depends on a library resource (name/id = RT only)
  }
  public enum ResourcePromotion
  {
    SceneAdd, //create resource
    SceneRemove,
    LibraryAdd,
    LibraryRemove,
    DataSource, //set the DS
  }
  public enum ResourceUpdateFilter
  {
    All,
    TemporaryOnly,//only update temps
  }
  [DataContract]
  public class NamedObject
  {
    public string Name { get { return _name; } set { _name = value; } }

    [DataMember] protected string _name = Library.UnsetName;

    protected NamedObject() { }//copy/clone/serilze
    public NamedObject(string name)
    {
      _name = name;
    }
  }
  [DataContract]
  public abstract class DataBlock : IMutableState, ISerializeBinary, ICopy<DataBlock>
  {
    //Base class for serialization/saving
    #region Public: Members

    public string Name { get { return _name; } set { _name = value; } }
    public UInt64 UniqueID { get { return _uniqueID; } set { _uniqueID = value; } }
    public DataSource? DataSource { get { return _dataSource; } }
    public int LoadCount { get { return _loadCount; } set { _loadCount = value; } }
    public int UnloadCount { get { return _unloadCount; } set { _unloadCount = value; } }
    public ResourceLoadResult LoadResult { get { return _loadResult; } set { _loadResult = value; } }
    public DateTime LastModifyTime { get { return _lastModifyTime; } set { _lastModifyTime = value; } }
    public DataPersistence Persistence { get { return _persistance; } set { _persistance = value; } }
    public bool Modified { get { return _modified; } set { if (!_modified && value) _lastModifyTime = DateTime.Now; _modified = value; } }

    #endregion
    #region Private: Members

    [DataMember] private DataSource? _dataSource = null;
    [DataMember] private DateTime _lastModifyTime = DateTime.MinValue;
    [DataMember] private ResourceType _resType = ResourceType.Undefined;
    [DataMember] protected UInt64 _uniqueID = Library.NullID;
    [DataMember] protected string _name = Library.UnsetName;
    [DataMember] private DataPersistence _persistance = DataPersistence.Temporary;//All resources are temporary unless WorldObject references it or we specify that it must be saved.
    private bool _modified = false;
    private DateTime _lastSaveOrLoadTime = DateTime.MinValue;
    private ResourceLoadResult _loadResult = ResourceLoadResult.NotLoaded;
    private int _loadCount = 0;
    private int _unloadCount = 0;

    #endregion
    #region Methods

    protected DataBlock() { } //clone/serialize ctor
    public DataBlock(string name)
    {
      /* Critical to not put anything big here, we inherit temporary
       data from datablock, so an empty string name is all we want */
      _name = name;
    }
    public virtual void GetSubResources(List<DataBlock?>? block)
    {
    }
    public virtual void PromoteResource(ResourcePromotion m, DataSource? source = null)
    {
      /*
        ** resource promotion logic
        sub = sub-resource (object->material->texture->image)
        UTable - unique name table for global resources --on GU.lib
        RTable - resource local table -- on datasource

          Namespaces
            temp - none 
            scene - UT only - created in the application/script
            libroot - root library object - RT and UT - generated/loaded
            libdep - RT only - generated/loaded
          Serialization
          temp - none
          scene - all nodes
          libroot - DS
          libdep - none

          libadd 
            if sub is lib - no change, do not traverse children
            if sub is scene - error - must clone resource to add to library
            if sub is temp -  set DS (for all) - if no DS - then is raw
                              if is root resource
                                set lib - item will exist in UTable
                                gets uid/uname (creteresrouce)
                              else
                                set libdep
                                rname from DataSource RTable (must be unique within resource context) - usually loaded from file.
                                if has no DS - raw serialized
                                if has DS - not serialized
          sceneadd
            if lib - no change, do not traverse
            if scene - error - sub-object already added to scene, should not be possible
            if temp - set to scene, make uid (createresource) for all temp sub-items
          sceneremove
            if sub is lib - no change
            if sub is scene - remove uid/un 
            if sub is temp - error
          libremove
            if sub is lib - remove uid/un
            if sub is scene - error
            if sub is temp - error
      Overview
        Resource library saves resource generators, and loaders (data sources)
          -> unique ID, uname
          LoadModel, LoadImage, LoadTexture, LoadMaterial, LoadShader, LoadMesh
          -> creates a new shader if the UName does not exist
          -> if uname does exist, load the DS->
            IF -> parameters are the same (DataSource::Equals)
            ELSE -> destroy the DS, create a new DS with the changed parameters, **keep the existing name/ID
          
        Temps:
          Temporary data created through constructors are not saved, unless created through Load* attached to a worldobject node
            Material() Shader() Texture() Image() MeshData()..
            So to update an image/shader etc - create with Load* 

          Ok so what about WorldObject?
            -> Saved only when attached to the scene, or, marked as a library item.

      */

      try
      {
        PromoteResourceInternal(m, source, null);
      }
      catch (Exception ex)
      {
        Gu.Log.Error($"Failed to promote resource '{this.Name}' from '{this.Persistence.ToString()}' with '{m.ToString()}'", ex);
        Gu.DebugBreak();
      }

    }
    private void PromoteResourceInternal(ResourcePromotion m, DataSource? source = null, DataBlock? parent = null)
    {
      bool traverse = false;
      if (m == ResourcePromotion.DataSource)
      {
        if (_persistance == DataPersistence.Temporary)
        {
          _dataSource = source;
          traverse = true;
        }
        else if (_persistance == DataPersistence.Scene)
        {
          if (parent == null)
          {
            _dataSource = source;
            _persistance = DataPersistence.Library;
          }
          else
          {
            _dataSource = source;
            _persistance = DataPersistence.LibraryDependency;
          }
          traverse = true;
        }
        else if (_persistance == DataPersistence.Library)
        {
          traverse = false;
        }
        else if (_persistance == DataPersistence.LibraryDependency)
        {
          traverse = false;
        }
      }
      // else if (m == ResourcePromotion.LibraryAdd)
      // {
      //   if (_persistance == DataPersistence.Temporary)
      //   {
      //     if (_dataSource == null)
      //     {
      //       Gu.Assert(source != null);
      //       _dataSource = source;
      //     }
      //     _dataSource = source;
      //     if (parent == null)
      //     {
      //       _persistance = DataPersistence.Library;

      //       traverse = true;
      //     }
      //     else
      //     {
      //       _persistance = DataPersistence.LibraryDependency;
      //     }
      //   }
      //   else if (_persistance == DataPersistence.Scene)
      //   {
      //     Library.ResourceError(this, "Scene resource found in library resource.");
      //     traverse = false;
      //   }
      //   else if (_persistance == DataPersistence.Library)
      //   {
      //     //no change, just stop traversing
      //     traverse = false;
      //   }
      //   else if (_persistance == DataPersistence.LibraryDependency)
      //   {
      //     Library.ResourceError(this, "tried to add library dependency to existing library");
      //     traverse = false;
      //   }
      //   else
      //   {
      //     Gu.BRThrowNotImplementedException();
      //   }
      // }
      // else if (m == ResourcePromotion.LibraryRemove)
      // {
      //   if (_persistance == DataPersistence.Temporary)
      //   {
      //     Library.ResourceError(this, "tried to remove temp from Lib");
      //     traverse = false;
      //   }
      //   else if (_persistance == DataPersistence.Scene)
      //   {
      //     Library.ResourceError(this, "tried to remove scene node from Lib");
      //     traverse = false;
      //   }
      //   else if (_persistance == DataPersistence.Library)
      //   {
      //     Gu.Lib.DeleteResource(this);
      //     _uniqueID = Library.NullID;
      //     _persistance = DataPersistence.Temporary;
      //     traverse = false;
      //   }
      //   else if (_persistance == DataPersistence.LibraryDependency)
      //   {
      //     _persistance = DataPersistence.Temporary;
      //     traverse = true;
      //   }
      //   else
      //   {
      //     Gu.BRThrowNotImplementedException();
      //   }
      // }
      // else if (m == ResourcePromotion.SceneAdd)
      // {
      //   if (_persistance == DataPersistence.Temporary)
      //   {
      //     _persistance = DataPersistence.Scene;
      //     _uniqueID = Gu.Lib.GetNewUniqueId();
      //     Gu.Lib.CreateResource(this, _name);
      //     traverse = true;
      //   }
      //   else if (_persistance == DataPersistence.Scene)
      //   {
      //     Library.ResourceWarning(this, "tried to add existing scene resource to scene");
      //     traverse = true;
      //   }
      //   else if (_persistance == DataPersistence.Library)
      //   {
      //     //no change
      //     traverse = false;
      //   }
      //   else if (_persistance == DataPersistence.LibraryDependency)
      //   {
      //     Library.ResourceError(this, "tried to add library dependency to scene");
      //     traverse = false;
      //   }
      //   else
      //   {
      //     Gu.BRThrowNotImplementedException();
      //   }
      // }
      // else if (m == ResourcePromotion.SceneRemove)
      // {
      //   if (_persistance == DataPersistence.Temporary)
      //   {
      //     Library.ResourceWarning(this, "tried to remove temp from scene, should be set to 'scene'");
      //     traverse = true;
      //   }
      //   else if (_persistance == DataPersistence.Scene)
      //   {
      //     Gu.Lib.DeleteResource(this);
      //     _uniqueID = Library.NullID;
      //     _persistance = DataPersistence.Temporary;
      //     traverse = true;
      //   }
      //   else if (_persistance == DataPersistence.Library)
      //   {
      //     //no change
      //     traverse = false;
      //   }
      //   else if (_persistance == DataPersistence.LibraryDependency)
      //   {
      //     Library.ResourceError(this, "tried to remove library dependency from scene");
      //     traverse = false;
      //   }
      //   else
      //   {
      //     Gu.BRThrowNotImplementedException();
      //   }
      // }
      // else
      // {
      //   Gu.BRThrowNotImplementedException();
      // }

      if (traverse)
      {
        List<DataBlock?> deps = new  List<DataBlock?>();
        GetSubResources(deps);
        foreach (var child in deps)
        {
          child?.PromoteResourceInternal(m, source, this == Gu.World.SceneRoot ? null : this);
        }
      }

    }
    public void SetModified()
    {
      _modified = true;
    }
    public void ClearModified()
    {
      _modified = false;
    }
    public DataRef<T>? GetRef<T>() where T : DataBlock
    {
      //We shouldn't have to create a new reference every time right?
      return new DataRef<T>(this as T);
    }
    public virtual void CopyFrom(DataBlock? other, bool? shallow = null)
    {
      Gu.Assert(other != null);
      SetModified();
    }
    public virtual void MakeUnique()
    {
      SetModified();
    }
    public virtual void Serialize(BinaryWriter br)
    {
      // br.Write(_resource.Name);
      // br.Write(_resource.UniqueID);
    }
    public virtual void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      // //We must make sure the uniquid is in the resource manager
      // Gu.MustTest();

      // var name = br.ReadString();
      // var id = br.ReadUInt64();
      // _resource = Gu.Lib.GetResourceById(id);
      // if (_resource == null)
      // {
      //   //Hmm..rt may be invalid..
      //   var byname = Gu.Lib.GetResourceByName(ResourceType, name);
      //   if (byname != null)
      //   {
      //     Gu.Log.Error($"Resource name='{name}' id='{id}' was not found, but found similar resource '{byname.Name}', id='{byname.UniqueID}'.");
      //   }
      //   else
      //   {
      //     Gu.Log.Error($"Resource name='{name}' id='{id}' was not found.");
      //   }
      //   //Fix up resource JSON changing it to the correct ID
      //   Gu.DebugBreak();
      // }
    }

    #endregion
  }



}