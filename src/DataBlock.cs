using System.Runtime.Serialization;

namespace Loft
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

    [DataMember] protected string _name = Lib.UnsetName;

    protected NamedObject() { }//copy/clone/serilze
    public NamedObject(string name)
    {
      _name = name;
    }
  }
  [DataContract]
  public abstract class DataBlock
  {
    //Base class for serialization/saving
    #region Public: Members

    public string AAAAName { get { return _name; } set { _name = value; } }//dont remove -  intellisense magic
    public string Name { get { return _name; } set { _name = value; } }
    public UInt64 UniqueID { get { return _uniqueID; } set { _uniqueID = value; } }
    public DataSource? DataSource { get { return _dataSource; } set { _dataSource = value; } }
    public int LoadCount { get { return _loadCount; } set { _loadCount = value; } }
    public int UnloadCount { get { return _unloadCount; } set { _unloadCount = value; } }
    public ResourceLoadResult LoadResult { get { return _loadResult; } set { _loadResult = value; } }
    public DateTime LastModifyTime { get { return _lastModifyTime; } set { _lastModifyTime = value; } }
    public DataPersistence Persistence { get { return _persistance; } set { _persistance = value; } }
    public bool Modified { get { return _modified; } set { if (!_modified && value) _lastModifyTime = DateTime.Now; _modified = value; } }
    public ResourceType ResourceType { get { return _resType; } set { _resType = value; } }
    public bool IsEmbedded { get { return _isEmbedded; } set { _isEmbedded = value; } }
    #endregion
    #region Private: Members

    [DataMember] private DataSource? _dataSource = null;
    [DataMember] private DateTime _lastModifyTime = DateTime.MinValue;
    [DataMember] private ResourceType _resType = ResourceType.Undefined;
    [DataMember] protected UInt64 _uniqueID = Lib.c_iNullID;
    [DataMember] protected string _name = Lib.UnsetName;
    [DataMember] private DataPersistence _persistance = DataPersistence.Temporary;//All resources are temporary unless WorldObject references it or we specify that it must be saved.
    [DataMember] private bool _isEmbedded = false;
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
      _uniqueID = Gu.Lib.GetUniqueId();
      //   Gu.Lib.AddResource(this);
    }
    public void SetModified()
    {
      _modified = true;
    }
    public void ClearModified()
    {
      _modified = false;
    }

    public virtual void MakeUnique()
    {
      SetModified();
    }


    #endregion
  }



}