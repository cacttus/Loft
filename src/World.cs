using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;
using OpenTK.Graphics.OpenGL4;
using System.IO.Compression;

namespace PirateCraft
{
  //The effigious block
  [StructLayout(LayoutKind.Sequential)]
  public struct Block
  {
    public static Block Empty = new Block(0);
    public Block(UInt16 val) { Value = val; }
    public UInt16 Value;
    public bool IsEmpty()
    {
      //Nothing but air
      return Value == 0;
    }
    public bool IsItem()
    {
      //Items are special case cullables since they may not, or may cull entire faces.
      return false;
    }
    public bool IsSolidBlockNotTransparent()
    {
      //This should return whether the block is a *solid (non-item) *non-transparent *block (6 solid sides). 
      // Used for face culling.
      //Technically it should index into a LUT to see whether this block-item is solid or not.
      //For now - we are just rendering blocks so we can return HasDensity for this.
      return !IsEmpty();
    }
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct RegionState
  {
    //This class determines what regions of globs/dromes have data, or are solid, this is used to
    //optimize the topology generator, and, unload empty regions of blocks, as empty areas have no data.
    public const int Partial = 0;
    public const int SolidBlocksOnly = 1;
    public const int SolidItems = 2;
    public const int Empty_AndNoData = 3;//Partial = renderable, Solid = fully solid, Empty = empty

    private int _solid = 0;
    private int _empty = 0;
    private int _items = 0;

    public void Reset()
    {
      _solid = _empty = _items = 0;
    }

    public int Solid { get { return _solid; } }
    public int Empty { get { return _empty; } }
    public int Items { get { return _items; } }
    public void Deserialize(BinaryReader br)
    {
      _solid = br.ReadInt32();
      _empty = br.ReadInt32();
      _items = br.ReadInt32();
    }
    public void Serialize(BinaryWriter br)
    {
      br.Write((Int32)_solid);
      br.Write((Int32)_empty);
      br.Write((Int32)_items);
    }

    public RegionState() { }
    public int State
    {
      get
      {
        if (Solid > 0 && Empty > 0)
        {
          return Partial;
        }
        else if (Empty == 0)
        {
          return SolidBlocksOnly;
        }
        else if (Solid == 0)
        {
          //Later we delete block data to save space for empty globs.
          return Empty_AndNoData;
        }
        return Partial;
      }
    }
    public void UpdateInitialGenAddedBlock(Block block)
    {
      if (block.IsSolidBlockNotTransparent())
      {
        _solid++;
      }
      else
      {
        _empty++;
      }
      if (block.IsItem())
      {
        _items++;
      }
    }
    public void UpdateBlockModified(Block old, Block block)
    {
      //Modified a block, our state may have changed
      if (old.IsSolidBlockNotTransparent())
      {
        if (!block.IsSolidBlockNotTransparent())
        {
          _empty++;
          _solid--;
        }
      }
      else
      {
        if (block.IsSolidBlockNotTransparent())
        {
          _solid++;
          _empty--;
        }
      }

      if (old.IsItem())
      {
        if (!block.IsItem())
        {
          _items--;
        }
      }
      else
      {
        if (block.IsItem())
        {
          _items++;
        }
      }

    }
  }
  public class BlockItemCode
  {
    //Blocks
    public const ushort Empty = 0;
    public const ushort Value = 1;
    public const ushort Grass = 2;
    public const ushort Dirt = 3;
    public const ushort Brick = 4;
    public const ushort Brick2 = 5;
    public const ushort Gravel = 6;
    public const ushort Sand = 7;
    public const ushort Cedar = 8;
    public const ushort Cedar_Needles = 9;
    public const ushort Feldspar = 10;
    //Items
    //...
  }
  public enum TileImage
  {
    Grass, GrassSide, Dirt, Plank, Brick, Brick2, Gravel, Sand, Cedar, Cedar_Needles, Cedar_Top, Feldspar
  }
  public class BlockTileUVSide
  {
    public const int Top = 0;
    public const int Side = 1;
    public const int Bottom = 2;
  }
  //Unit box for creating mesh cubes, Tiles, Material
  public class WorldStaticData
  {
    public static vec3[] GlobNeighborOffsets = new vec3[6]
    {
         new vec3(-World.GlobWidthX, 0, 0),
         new vec3(World.GlobWidthX, 0, 0),
         new vec3(0, -World.GlobWidthY, 0),
         new vec3(0, World.GlobWidthY, 0),
         new vec3(0, 0, -World.GlobWidthZ),
         new vec3(0, 0, World.GlobWidthZ),
    };
    public static vec3[] BlockNeighborOffsets = new vec3[6]
    {
         new vec3(-World.BlockSizeX, 0, 0),
         new vec3(World.BlockSizeX, 0, 0),
         new vec3(0, -World.BlockSizeY, 0),
         new vec3(0, World.BlockSizeY, 0),
         new vec3(0, 0, -World.BlockSizeZ),
         new vec3(0, 0, World.BlockSizeZ),
    };

    public static Dictionary<TileImage, FileLoc> TileImages = new Dictionary<TileImage, FileLoc>() {
            { TileImage.Grass, new FileLoc("tx64_grass.png", FileStorage.Embedded) },
            { TileImage.GrassSide, new FileLoc("tx64_grass_side.png", FileStorage.Embedded) },
            { TileImage.Dirt, new FileLoc("tx64_dirt.png", FileStorage.Embedded) },
            { TileImage.Plank, new FileLoc("tx64_plank.png", FileStorage.Embedded) },
            { TileImage.Brick, new FileLoc("tx64_brick.png", FileStorage.Embedded) },
            { TileImage.Brick2, new FileLoc("tx64_brick2.png", FileStorage.Embedded) },
            { TileImage.Gravel, new FileLoc("tx64_gravel.png", FileStorage.Embedded) },
            { TileImage.Sand, new FileLoc("tx64_sand.png", FileStorage.Embedded) },
            { TileImage.Cedar, new FileLoc("tx64_cedar.png", FileStorage.Embedded) },
            { TileImage.Cedar_Needles, new FileLoc("tx64_cedar_needles.png", FileStorage.Embedded) },
            { TileImage.Cedar_Top, new FileLoc("tx64_cedar_top.png", FileStorage.Embedded) },
            { TileImage.Feldspar, new FileLoc("tx64_plagioclase_feldspar.png", FileStorage.Embedded) },
         };

    private static vec3[] bx_box = new vec3[8];
    private static vec3[] bx_norms = new vec3[6];//lrbtaf
    private static vec2[] bx_texs = new vec2[4];
    public static v_v3n3x2[,] bx_verts_face { get; private set; } = new v_v3n3x2[6, 4];//lrbtaf
    public static uint[] bx_face_inds { get; private set; }

    public static void Generate()
    {
      //Left Righ, Botom top, back front
      float w2 = World.BlockSizeX, h2 = World.BlockSizeY, d2 = World.BlockSizeZ;
      bx_box[0] = new vec3(0, 0, 0);
      bx_box[1] = new vec3(w2, 0, 0);
      bx_box[2] = new vec3(0, h2, 0);
      bx_box[3] = new vec3(w2, h2, 0);
      bx_box[4] = new vec3(0, 0, d2);
      bx_box[5] = new vec3(w2, 0, d2);
      bx_box[6] = new vec3(0, h2, d2);
      bx_box[7] = new vec3(w2, h2, d2);

      bx_norms[0] = new vec3(-1, 0, 0);
      bx_norms[1] = new vec3(1, 0, 0);
      bx_norms[2] = new vec3(0, -1, 0);
      bx_norms[3] = new vec3(0, 1, 0);
      bx_norms[4] = new vec3(0, 0, -1);
      bx_norms[5] = new vec3(0, 0, 1);

      bx_texs[0] = new vec2(0, 1);
      bx_texs[1] = new vec2(1, 1);
      bx_texs[2] = new vec2(0, 0);
      bx_texs[3] = new vec2(1, 0);

      //     6       7
      // 2      3
      //     4       5
      // 0      1
      //Order of faces: Left, Right, Bottom, Top, Back Front (LRBTAF)
      bx_verts_face[0, 0] = new v_v3n3x2() { _v = bx_box[4], _n = bx_norms[0], _x = bx_texs[0] };
      bx_verts_face[0, 1] = new v_v3n3x2() { _v = bx_box[0], _n = bx_norms[0], _x = bx_texs[1] };
      bx_verts_face[0, 2] = new v_v3n3x2() { _v = bx_box[6], _n = bx_norms[0], _x = bx_texs[2] };
      bx_verts_face[0, 3] = new v_v3n3x2() { _v = bx_box[2], _n = bx_norms[0], _x = bx_texs[3] };

      bx_verts_face[1, 0] = new v_v3n3x2() { _v = bx_box[1], _n = bx_norms[1], _x = bx_texs[0] };
      bx_verts_face[1, 1] = new v_v3n3x2() { _v = bx_box[5], _n = bx_norms[1], _x = bx_texs[1] };
      bx_verts_face[1, 2] = new v_v3n3x2() { _v = bx_box[3], _n = bx_norms[1], _x = bx_texs[2] };
      bx_verts_face[1, 3] = new v_v3n3x2() { _v = bx_box[7], _n = bx_norms[1], _x = bx_texs[3] };

      bx_verts_face[2, 0] = new v_v3n3x2() { _v = bx_box[4], _n = bx_norms[2], _x = bx_texs[0] };
      bx_verts_face[2, 1] = new v_v3n3x2() { _v = bx_box[5], _n = bx_norms[2], _x = bx_texs[1] };
      bx_verts_face[2, 2] = new v_v3n3x2() { _v = bx_box[0], _n = bx_norms[2], _x = bx_texs[2] };
      bx_verts_face[2, 3] = new v_v3n3x2() { _v = bx_box[1], _n = bx_norms[2], _x = bx_texs[3] };

      bx_verts_face[3, 0] = new v_v3n3x2() { _v = bx_box[2], _n = bx_norms[3], _x = bx_texs[0] };
      bx_verts_face[3, 1] = new v_v3n3x2() { _v = bx_box[3], _n = bx_norms[3], _x = bx_texs[1] };
      bx_verts_face[3, 2] = new v_v3n3x2() { _v = bx_box[6], _n = bx_norms[3], _x = bx_texs[2] };
      bx_verts_face[3, 3] = new v_v3n3x2() { _v = bx_box[7], _n = bx_norms[3], _x = bx_texs[3] };

      bx_verts_face[4, 0] = new v_v3n3x2() { _v = bx_box[0], _n = bx_norms[4], _x = bx_texs[0] };
      bx_verts_face[4, 1] = new v_v3n3x2() { _v = bx_box[1], _n = bx_norms[4], _x = bx_texs[1] };
      bx_verts_face[4, 2] = new v_v3n3x2() { _v = bx_box[2], _n = bx_norms[4], _x = bx_texs[2] };
      bx_verts_face[4, 3] = new v_v3n3x2() { _v = bx_box[3], _n = bx_norms[4], _x = bx_texs[3] };

      bx_verts_face[5, 0] = new v_v3n3x2() { _v = bx_box[5], _n = bx_norms[5], _x = bx_texs[0] };
      bx_verts_face[5, 1] = new v_v3n3x2() { _v = bx_box[4], _n = bx_norms[5], _x = bx_texs[1] };
      bx_verts_face[5, 2] = new v_v3n3x2() { _v = bx_box[7], _n = bx_norms[5], _x = bx_texs[2] };
      bx_verts_face[5, 3] = new v_v3n3x2() { _v = bx_box[6], _n = bx_norms[5], _x = bx_texs[3] };

      bool flip = false;
      bx_face_inds = new uint[6] {
            0,
            (uint)(flip ? 2 : 3),
            (uint)(flip ? 3 : 2),
            0,
            (uint)(flip ? 3 : 1),
            (uint)(flip ? 1 : 3)
         };

    }
  }

  //Topology units
  public class Glob
  {
    public enum GlobState
    {
      None, LoadedAndQueued, Topologized
    }

    public Int64 GeneratedFrameStamp { get; private set; } = 0;
    public MeshData Transparent = null;
    public MeshData Opaque = null;
    //public Block[] Blocks = null;
    public ivec3 Pos = new ivec3(0, 0, 0);
    public GlobState State = GlobState.None;
    public Drome Drome = null;
    //public int Solid = 0;
    //public int Empty = 0;
    //public int Items = 0;
    public vec3 OriginR3
    {
      get
      {
        vec3 r = new vec3(Pos.x * World.GlobWidthX, Pos.y * World.GlobWidthY, Pos.z * World.GlobWidthZ);
        return r;
      }
    }
    public Glob(ivec3 pos, Int64 genframeStamp, Drome drom)
    {
      Pos = pos;
      GeneratedFrameStamp = genframeStamp;
      Drome = drom;
    }
  }
  //Density / Block units
  public class Drome
  {
    //public Dictionary<ivec3, WeakReference<Glob>> Globs_Weak = new Dictionary<ivec3, WeakReference<Glob>>();
    public RegionState RegionState = new RegionState();
    public Block[] Blocks = null;
    public RegionState[] GlobRegionStates = null;
    public ivec3 Pos = new ivec3(0, 0, 0);
    public Int64 GeneratedFrameStamp { get; private set; } = 0;
    public Drome(ivec3 pos, Int64 genframeStamp)
    {
      Pos = pos;
      GeneratedFrameStamp = genframeStamp;
    }
    public vec3 OriginR3
    {
      get
      {
        vec3 r = new vec3((float)Pos.x * World.DromeWidthX, (float)Pos.y * World.DromeWidthY, (float)Pos.z * World.DromeWidthZ);
        return r;
      }
    }
  }

  //GlobWorld
  public class World
  {
    public const float MaxTotalGlobs = 4096 * 2 * 2 * 2;
    public const float MaxRenderGlobs = 4096;
    public int MaxGlobsToGeneratePerFrameShell = 10;
    public const float BlockSizeX = 4.0f;
    public const float BlockSizeY = 4.0f;
    public const float BlockSizeZ = 4.0f;
    public const int GlobBlocksX = 16;
    public const int GlobBlocksY = 16;
    public const int GlobBlocksZ = 16;
    public const float GlobWidthX = GlobBlocksX * BlockSizeX;
    public const float GlobWidthY = GlobBlocksY * BlockSizeY;
    public const float GlobWidthZ = GlobBlocksZ * BlockSizeZ;
    public const int DromeGlobsX = 16;
    public const int DromeGlobsY = 8;
    public const int DromeGlobsZ = 16;
    public const int DromeBlocksX = GlobBlocksX * DromeGlobsX;
    public const int DromeBlocksY = GlobBlocksY * DromeGlobsY;
    public const int DromeBlocksZ = GlobBlocksZ * DromeGlobsZ;
    public const float DromeWidthX = GlobWidthX * DromeGlobsX;
    public const float DromeWidthY = GlobWidthY * DromeGlobsY;
    public const float DromeWidthZ = GlobWidthZ * DromeGlobsZ;
    public const int MaxGenerationShells = 20;

    public readonly vec3 BlockRadiusR3 = new vec3(BlockSizeX * 0.5f, BlockSizeY * 0.5f, BlockSizeZ * 0.5f);//Radius from center of glob to the corner.
    public readonly vec3 GlobRadiusR3 = new vec3(GlobWidthX * 0.5f, GlobWidthY * 0.5f, GlobWidthZ * 0.5f);//Radius from center of glob to the corner.
    public vec3 GlobDiameterR3 { get { return GlobRadiusR3 * 2; } }
    public vec3 RenderRadiusShell { get { return GlobDiameterR3; } }
    //  public float MaxRenderDistance { get { return GlobDiameterR3 * 5; } } //Render all the nodes we can see.

    private bool _firstGeneration = true; //Whether this is the initial generation, where we would need to generate everything around the player.
    private int _currentShell = 1;
    private ivec3 playerLastGlob = new ivec3(0, 0, 0);
    private WorldObject dummy = new WorldObject("dummy_beginrender");
    private WorldObject _debugDrawLines = null;
    private WorldObject _debugDrawPoints = null;
    private Dictionary<DrawOrder, List<WorldObject>> _renderObs_Ordered = null;
    private Dictionary<ivec3, Glob> _globs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //All globs
    private Dictionary<ivec3, Glob> _renderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
    private Dictionary<ivec3, Glob> _visibleRenderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
    private Dictionary<ivec3, Glob> _globsToUpdate = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
    private Dictionary<string, WorldObject> Objects { get; set; } = new Dictionary<string, WorldObject>();
    private Dictionary<ivec3, Drome> _dromes = new Dictionary<ivec3, Drome>(new ivec3.ivec3EqualityComparer()); //All globs


    public int Dbg_N_OB_Culled = 0;
    public int NumGlobs { get { return _globs.Count; } }
    public int NumRenderGlobs { get { return _renderGlobs.Count; } }
    public int NumVisibleRenderGlobs { get { return _visibleRenderGlobs.Count; } }
    public WorldObject SceneRoot { get; private set; } = new WorldObject("Scene_Root");
    public WorldObject Sky { get; set; } = null;

    public enum GlobCollection
    {
      All, Render, VisibleRender
    }

    //TODO:players
    public WorldObject Player = null;

    //Thread GlobGenerator;
    //object GlobMutex;

    Material _worldMaterial = null;
    //Texture2D _worldTexture = null;
    //Texture2D _worldBump = null;
    MegaTex _worldMegatex = new MegaTex("tex", true);
    public string WorldSavePath = "";
    public string WorldName = "";
    private const string SaveWorldVersion = "0.01";
    private const string SaveWorldHeader = "WorldFilev" + SaveWorldVersion;
    private const int DromeFileVersion = 1;
    private double AutoSaveTimeoutSeconds = 5;//
    private double AutoSaveTimeout = 0;
    public World()
    {
    }

    private FileLoc GetTileFile(TileImage img)
    {
      WorldStaticData.TileImages.TryGetValue(img, out var loc);
      Gu.Assert(loc != null);
      return loc;
    }
    public void Initialize(WorldObject player, string worldName, bool delete_world_start_fresh)
    {
      Player = player;
      WorldName = worldName;

      if (!MathUtils.IsPowerOfTwo(GlobBlocksX) || !MathUtils.IsPowerOfTwo(GlobBlocksY) || !MathUtils.IsPowerOfTwo(GlobBlocksZ))
      {
        Gu.BRThrowException("Glob blocks x,y,z must be a power of 2.");
      }

      //Init draw array.
      _renderObs_Ordered = new Dictionary<DrawOrder, List<WorldObject>>();
      for (int do_i = 0; do_i < (int)DrawOrder.MaxDrawOrders; do_i++)
      {
        _renderObs_Ordered.Add((DrawOrder)do_i, new List<WorldObject>());
      }

      CreateWorldMaterial();

      //Generate the mesh data we use to create cubess
      WorldStaticData.Generate();

      InitWorldDiskFile(delete_world_start_fresh);

      //Asynchronous generator .. (TODO)
      // Task.Factory.StartNew(() => {
      //});

      Gu.Log.Info("Building initail grid");
      BuildGlobGridAndTopologize(player.World.extractTranslation(), RenderRadiusShell * 5, true);
    }
    public void Update(double dt, Camera3D cam)
    {
      BuildWorld();
      UpdateObjects(dt);
      CollectVisibleObjects(cam);
      CollectVisibleGlobs(cam);
      AutoSaveWorld(dt);
    }

    #region Objects

    public WorldObject FindObject(string name)
    {
      WorldObject obj = null;
      Objects.TryGetValue(name, out obj);
      return obj;
    }
    public Camera3D CreateCamera(string name, int w, int h, vec3 pos)
    {
      Camera3D c = new Camera3D(name, w, h);
      c.Position = pos;
      Box3f dummy = Box3f.Zero;
      c.Update(0, ref dummy);
      AddObject(c);
      return c;
    }
    public WorldObject CreateObject(string name, MeshData mesh, Material material, vec3 pos = default(vec3))
    {
      WorldObject ob = new WorldObject(name);
      ob.Name = name;
      ob.Position = pos;
      ob.Mesh = mesh;
      ob.Material = material;
      return ob;
    }
    public WorldObject CreateAndAddObject(string name, MeshData mesh, Material material, vec3 pos = default(vec3))
    {
      return AddObject(CreateObject(name, mesh, material, pos));
    }
    public void RemoveObject(string name)
    {
      if (Objects.TryGetValue(name, out WorldObject wo))
      {
        SceneRoot.RemoveChild(wo);
        Objects.Remove(name);
      }
      else
      {
        Gu.Log.Error("Object '" + name + "' was not found.");
      }
    }
    public WorldObject AddObject(WorldObject ob)
    {
      //Use a suffix if there is a duplicate object
      int suffix = 0;
      string name_suffix = ob.Name;
      while (FindObject(name_suffix) != null)
      {
        suffix++;
        name_suffix = ob.Name + "-" + suffix.ToString();
      }
      ob.Name = name_suffix;
      Objects.Add(name_suffix, ob);
      SceneRoot.AddChild(ob);
      return ob;
    }
    private void UpdateObjects(double dt)
    {
      Box3f dummy = Box3f.Zero;
      dummy.genResetLimits();
      foreach (var ob in Objects.Values)
      {
        ob.Update(dt, ref dummy);
      }
    }
    private void CollectVisibleObjects(Camera3D camera)
    {
      foreach (var layer in _renderObs_Ordered)
      {
        layer.Value.Clear();
      }
      Dbg_N_OB_Culled = 0;
      //Add sky as it must come first.
      if (Sky != null)
      {
        CollectObjects(camera, Sky);
      }
      CollectObjects(camera, SceneRoot);
    }
    private void CollectVisibleGlobs(Camera3D cam)
    {
      //Get All Grids
      //Well this is broken. Fix it later.
      //SweepGridFrustum((ivec3 node_ipos, Box3f node_box) =>
      //{
      //   var glob = GetGlobAtPos(node_ipos);
      //   if (glob != null)
      //   {
      //      Frustum frust = cam.Frustum;
      //      if (cam.Frustum.HasBox(node_box))
      //      {
      //         _visibleRenderGlobs.Add(node_ipos, glob);
      //      }
      //   }
      //}, cam.Frustum, MaxRenderDistance);

      //Honestly, this isn't too slow. We usually have maybe 500 globs visible at a time.
      _visibleRenderGlobs.Clear();
      foreach (var g in _renderGlobs)
      {
        if (cam.Frustum.HasBox(GetGlobBoxGlobalI3(g.Key)))
        {
          _visibleRenderGlobs.Add(g.Key, g.Value);
        }
      }

    }
    private void AutoSaveWorld(double dt)
    {
      AutoSaveTimeout += dt;
      if (AutoSaveTimeout > AutoSaveTimeoutSeconds)
      {
        AutoSaveTimeout = 0;
        SaveWorld();
      }
    }

    #endregion

    #region Rendering

    public void Render(double Delta, Camera3D camera)
    {
      //Render to this camera.
      camera.BeginRender();
      {
        if (_renderObs_Ordered.Keys.Contains(DrawOrder.First))
        {
          foreach (var ob in _renderObs_Ordered[DrawOrder.First])
          {
            DrawObMesh(ob, Delta, camera);
          }
        }
        //Objects
        if (_renderObs_Ordered.Keys.Contains(DrawOrder.Mid))
        {
          foreach (var ob in _renderObs_Ordered[DrawOrder.Mid])
          {
            DrawObMesh(ob, Delta, camera);
          }
        }


        //Globs
        List<MeshData> visible_op = new List<MeshData>();
        List<MeshData> visible_tp = new List<MeshData>();
        foreach (var g in _visibleRenderGlobs)
        {
          //No PVS, render all at first
          if (g.Value.Opaque != null)
          {
            visible_op.Add(g.Value.Opaque);
          }
          if (g.Value.Transparent != null)
          {
            visible_tp.Add(g.Value.Transparent);
          }
        }

        _worldMaterial.BeginRender(Delta, camera, dummy);
        foreach (var md in visible_op)
        {
          md.Draw();
        }
        _worldMaterial.EndRender();
      }

      if (_renderObs_Ordered.Keys.Contains(DrawOrder.Last))
      {
        foreach (var ob in _renderObs_Ordered[DrawOrder.Last])
        {
          DrawObMesh(ob, Delta, camera);
        }
      }

      camera.EndRender();
    }
    public void RenderDebug(double Delta, Camera3D camera)
    {
      var frame = Gu.Context.FrameStamp;

      if (Gu.Context.DebugDraw.DrawBoundBoxes)
      {
        vec4 bbcolor = new vec4(1, 0, 0, 1);
        foreach (var ob in Objects.Values)
        {
          Gu.Context.DebugDraw.Box(ob.BoundBoxMeshTransform, ob.Color);
          Gu.Context.DebugDraw.Box(ob.BoundBox, bbcolor);
        }
      }

      if (Gu.Context.DebugDraw.Lines.Count > 0)
      {
        GL.LineWidth(1.5f);
        Gpu.CheckGpuErrorsDbg();
        if (_debugDrawLines == null)
        {
          _debugDrawLines = CreateAndAddObject("debug_lines", null, new Material(Gu.ResourceManager.LoadShader("v_v3c4_debugdraw", false)));
          RemoveObject(_debugDrawLines.Name);//Doesn't actually destroy it
          _debugDrawLines.Mesh = new MeshData("Debugasfd", PrimitiveType.Lines, DebugDraw.VertexFormat);
        }
        _debugDrawLines.Mesh.CreateBuffers(Gpu.GetGpuDataPtr(Gu.Context.DebugDraw.Lines.ToArray()), null, false);
        DrawObMesh(_debugDrawLines, Delta, camera);
      }
      if (Gu.Context.DebugDraw.Points.Count > 0)
      {
        GL.PointSize(5);
        Gpu.CheckGpuErrorsDbg();
        if (_debugDrawPoints == null)
        {
          _debugDrawPoints = CreateAndAddObject("debug_points", null, new Material(Gu.ResourceManager.LoadShader("v_v3c4_debugdraw", false)));
          RemoveObject(_debugDrawPoints.Name);//Doesn't actually destroy it
          _debugDrawPoints.Mesh = new MeshData("Debugds", PrimitiveType.Points, DebugDraw.VertexFormat);
        }
        _debugDrawPoints.Mesh.CreateBuffers(Gpu.GetGpuDataPtr(Gu.Context.DebugDraw.Points.ToArray()), null, false);
        DrawObMesh(_debugDrawPoints, Delta, camera);
      }
    }
    private void CollectObjects(Camera3D cam, WorldObject ob)
    {
      Gu.Assert(ob != null);
      if (ob.Mesh != null)
      {
        if (cam.Frustum.HasBox(ob.BoundBox))
        {
          _renderObs_Ordered[ob.Mesh.DrawOrder].Add(ob);
        }
        else
        {
          Dbg_N_OB_Culled++;
        }
      }

      foreach (var c in ob.Children)
      {
        CollectObjects(cam, c);
      }
    }
    private void DrawObMesh(WorldObject ob, double Delta, Camera3D camera)
    {
      if (ob.Mesh != null)
      {
        ob.Material.BeginRender(Delta, camera, ob);
        ob.Mesh.Draw();
        ob.Material.EndRender();
      }
      else
      {
        //this is technically an error
      }
    }

    private Dictionary<ushort, List<FileLoc>> _blockTiles;
    private Dictionary<ushort, List<MtTex>> _tileUVs;
    private void CreateWorldMaterial()
    {
      //_blockTiles - Manual array that specifies which tiles go on the top, side, bottom
      //The tiles are specified by FileLoc structure which must be a class type.
      //This is used to index into the megatex to find the generated UV coordinates.
      _blockTiles = new Dictionary<ushort, List<FileLoc>>()
         {
            { BlockItemCode.Grass, new List<FileLoc>(){ GetTileFile(TileImage.Grass), GetTileFile(TileImage.GrassSide), GetTileFile(TileImage.Dirt) } },
            { BlockItemCode.Dirt, new List<FileLoc>(){ GetTileFile(TileImage.Dirt), GetTileFile(TileImage.Dirt), GetTileFile(TileImage.Dirt) } },
            { BlockItemCode.Brick, new List<FileLoc>(){ GetTileFile(TileImage.Brick), GetTileFile(TileImage.Brick), GetTileFile(TileImage.Brick) } },
            { BlockItemCode.Brick2, new List<FileLoc>(){ GetTileFile(TileImage.Brick2), GetTileFile(TileImage.Brick2), GetTileFile(TileImage.Brick2) } },
            { BlockItemCode.Gravel, new List<FileLoc>(){ GetTileFile(TileImage.Gravel), GetTileFile(TileImage.Gravel), GetTileFile(TileImage.Gravel) } },
            { BlockItemCode.Sand, new List<FileLoc>(){ GetTileFile(TileImage.Sand), GetTileFile(TileImage.Sand), GetTileFile(TileImage.Sand) } },
            { BlockItemCode.Cedar_Needles, new List<FileLoc>(){ GetTileFile(TileImage.Cedar_Needles), GetTileFile(TileImage.Cedar_Needles), GetTileFile(TileImage.Cedar_Needles) } },
            { BlockItemCode.Cedar, new List<FileLoc>(){ GetTileFile(TileImage.Cedar_Top), GetTileFile(TileImage.Cedar), GetTileFile(TileImage.Cedar_Top) } },
            { BlockItemCode.Feldspar, new List<FileLoc>(){ GetTileFile(TileImage.Feldspar), GetTileFile(TileImage.Feldspar), GetTileFile(TileImage.Feldspar) } },
         };

      //Create empty array that matches BlockTiles for the tile UVs
      _tileUVs = new Dictionary<ushort, List<MtTex>>();
      foreach (var block in _blockTiles)
      {
        List<MtTex> texs = new List<MtTex>();
        _tileUVs.Add(block.Key, texs);
        foreach (var floc in block.Value)
        {
          texs.Add(null);
        }
        //Count must be 3 for all sides of the block.
        Gu.Assert(texs.Count == 3);
      }

      //Create the atlas.
      //Must be called after context is set.
      foreach (var resource in WorldStaticData.TileImages)
      {
        MtTexPatch p = _worldMegatex.getTex(resource.Value);
        if (p.getTexs().Count > 0)
        {
          MtTex mtt = p.getTexs()[0];
          foreach (var block in _blockTiles)
          {
            //It's late.
            for (int ifloc = 0; ifloc < block.Value.Count; ifloc++)
            {
              if (block.Value[ifloc] == resource.Value)
              {
                _tileUVs[block.Key][ifloc] = mtt;
              }
            }
          }
        }
        else
        {
          Gu.Log.Warn("Megatex resource generated no textures.");
          Gu.DebugBreak();
        }

      }
      _worldMegatex.loadImages();
      var maps = _worldMegatex.compile(true);
      _worldMaterial = new Material(Shader.DefaultDiffuse(), maps.Albedo, maps.Normal);
    }

    #endregion

    #region Private: Globs

    private void BuildWorld()
    {
      Gu.Assert(Player != null);

      if (_globs.Count >= MaxTotalGlobs || _renderGlobs.Count >= MaxRenderGlobs)
      {
        return;
      }

      if (_firstGeneration)
      {
        playerLastGlob = R3toI3Glob(Player.Position);
      }

      if (Gu.Context.FrameStamp % 3 == 0)
      {
        //Quick-n-dirty "don't kill me"
        ivec3 newPlayerGlob = R3toI3Glob(Player.Position);

        vec3 awareness_radius = RenderRadiusShell * _currentShell;

        vec3 ppos = Player.World.extractTranslation();
        List<Glob> newGlobs = BuildGlobGridAndTopologize(ppos, awareness_radius);

        if ((newPlayerGlob != playerLastGlob))
        {
          _currentShell = 1;
          playerLastGlob = newPlayerGlob;
        }
        else if (newGlobs.Count == 0 && _currentShell < MaxGenerationShells)
        {
          //Only increase shell if we're done generating for this particular shell.
          _currentShell++;
        }

      }

      //We are no longer initially generating the world.
      _firstGeneration = false;

      //TODO: 
      //   //Destroy globs
      //   for all globs in _globs
      //   {
      //      if (min point of glob BB is > this.RenderRadius)
      //     {
      //      // Destroy abandoned blocks after a certain time.
      //      if (g.Loaded)
      //      {
      //         g.AbandonTime = Current_Time
      //           g.Loaded = false;
      //      }
      //   }
      //}

      //TODO: 
      //  for all globs
      //{
      //  if g.Loaded == false
      //      if g.AbandonTime > 3 seconds or so.
      //          remove g from globs.
      //          unload mesh data

      //}

    }
    private List<Glob> BuildGlobGrid(vec3 origin, vec3 awareness_radius, bool logprogress = false)
    {
      //Build a grid of globs in the volume specified by origin/radius
      List<Glob> newGlobs = new List<Glob>();

      //TODO: we use a cube here, we should check against an actual sphere below. It looks nicer.
      Box3f bf = new Box3f(origin - awareness_radius, origin + awareness_radius);

      Box3i ibox;
      ibox._min = new ivec3((int)(bf._min.x / GlobWidthX), (int)(bf._min.y / GlobWidthY), (int)(bf._min.z / GlobWidthZ));
      ibox._max = new ivec3((int)(bf._max.x / GlobWidthX), (int)(bf._max.y / GlobWidthY), (int)(bf._max.z / GlobWidthZ));

      //Limit Y axis ..  DEBUG ONLY
      Gu.Log.WarnCycle("Limiting debug Y axis for testing");
      int ylimit = 3;
      if (ibox._min.y > ylimit) { ibox._min.y = ylimit; }
      if (ibox._min.y < -ylimit) { ibox._min.y = -ylimit; }
      if (ibox._max.y > ylimit) { ibox._max.y = ylimit; }
      if (ibox._max.y < -ylimit) { ibox._max.y = -ylimit; }
      if (ibox._min.y > ibox._max.y) { ibox._min.y = ibox._max.y; }

      int dbg_totalCount = 0;
      for (int z = ibox._min.z; z <= ibox._max.z; z++)
      {
        for (int y = ibox._min.y; y <= ibox._max.y; y++)
        {
          for (int x = ibox._min.x; x <= ibox._max.x; x++)
          {
            dbg_totalCount++;
          }
        }
      }

      int dbg_current = 0;
      for (int z = ibox._min.z; z <= ibox._max.z; z++)
      {
        for (int y = ibox._min.y; y <= ibox._max.y; y++)
        {
          for (int x = ibox._min.x; x <= ibox._max.x; x++)
          {
            dbg_current++;
            if (logprogress)
            {
              if (dbg_current % 100 == 0)
              {
                Gu.Log.Info("" + dbg_current + "/" + dbg_totalCount);
              }
            }
            if (_renderGlobs.Count >= MaxRenderGlobs)
            {
              return newGlobs;
            }
            if (_globs.Count + newGlobs.Count >= MaxTotalGlobs)
            {
              return newGlobs;
            }
            if (newGlobs.Count >= MaxGlobsToGeneratePerFrameShell && _firstGeneration == false)
            {
              return newGlobs;
            }

            ivec3 gpos = new ivec3(x, y, z);
            if (!_globs.ContainsKey(gpos))
            {
              Glob g = GetNewGlob(gpos);
              _globs.Add(gpos, g);
              newGlobs.Add(g);
            }

          }
        }
      }
      return newGlobs;
    }
    public int dbg_nSkippedStitch = 0;
    public int dbg_nEmptyNoData = 0;
    private List<Glob> BuildGlobGridAndTopologize(vec3 origin, vec3 awareness_radius, bool logprogress = false)
    {
      List<Glob> newGlobs = BuildGlobGrid(origin, awareness_radius, logprogress);

      if (logprogress)
      {
        Gu.Log.Info("Topologizing " + newGlobs.Count);
      }

      //We need to add a sufficient grow algorithm.

      foreach (Glob g in _globsToUpdate.Values)
      {
        //  g.Opaque = null;
        //  g.Transparent = null;

      //  Gu.Log.Warn("Commented out density state optimizations until we index into the drome array for states");
     //   if (g.DensityState != Glob.GlobDensityState.Empty_AndNoData)
        {
          TopologizeGlob(g);
          dbg_nEmptyNoData++;
        }
      //  else
        {
          //The block is empty, the inside of the block has no topology. No data.
          // g.Blocks = null;
        }
     //   if (g.DensityState != Glob.GlobDensityState.SolidBlocksOnly)
        {
          //No neighboring blocks would be visible, so stitchin gisn't needed
          StitchGlobTopology(g);
          dbg_nSkippedStitch++;
        }
      }
      _globsToUpdate.Clear();

      return newGlobs;
    }
    private UInt16 CreateBlock(ushort item, vec3 world_pos)
    {
      //Coming in we have just value/empty blocks now generate the block type.
      if (item != BlockItemCode.Empty)
      {

        //Testing..
        //We have stuff. Default to grassy grass.
        if (world_pos.y < BlockSizeY * -10)
        {
          item = BlockItemCode.Feldspar;
        }
        else if (world_pos.y < BlockSizeY * -4)
        {
          item = BlockItemCode.Gravel;
        }
        else if (world_pos.y < 0)
        {
          item = BlockItemCode.Sand;
        }
        else
        {
          //if (block_index.y < GlobBlocksY)
          //{
          //   GetBlock(g, block_index);
          //}
          //else
          //{

          //}
          item = BlockItemCode.Grass;
        }
      }
      //  Random.Next() > 0.3f ?
      //BlockItemCode.Grass :
      //(Random.Next() > 0.6f ? BlockItemCode.Brick2 : BlockItemCode.Brick);
      return item;
    }
    private Glob GetNewGlob(ivec3 gpos)
    {
      ivec3 dromePos = GlobPosToDromePos(gpos);
      Drome drome = GenerateOrLoadDrome(dromePos);
      Glob g = new Glob(gpos, Gu.Context.FrameStamp, drome);

      _globsToUpdate.Add(gpos, g);

      return g;
    }
    private Drome GenerateOrLoadDrome(ivec3 dromePos)
    {
      Drome drome = null;
      _dromes.TryGetValue(dromePos, out drome);
      if (drome == null)
      {
        drome = TryLoadDrome(dromePos);
        if (drome == null)
        {
          drome = GenerateDrome(dromePos);
          SaveDrome(drome);
        }
        _dromes.Add(dromePos, drome);
      }
      return drome;
    }

    private string GetDromeFileName(ivec3 gpos)
    {
      //[8][8][8]
      Gu.Assert(System.IO.Directory.Exists(WorldSavePath));
      string sx = (gpos.x < 0 ? "" : "+") + gpos.x.ToString("D8");
      string sy = (gpos.y < 0 ? "" : "+") + gpos.y.ToString("D8");
      string sz = (gpos.z < 0 ? "" : "+") + gpos.z.ToString("D8");
      return System.IO.Path.Combine(WorldSavePath, sx + sy + sz + ".drome");
    }
    private string GetWorldFileName()
    {
      Gu.Assert(System.IO.Directory.Exists(WorldSavePath));
      string worldfile = WorldName + ".world";
      return System.IO.Path.Combine(WorldSavePath, worldfile);
    }
    private Drome GenerateDrome(ivec3 gpos)
    {
      Drome d = new Drome(gpos, Gu.Context.FrameStamp);
      vec3 dromeOriginR3 = d.OriginR3;// new vec3(DromeWidthX * gpos.x, DromeWidthY * gpos.y, DromeWidthZ * gpos.z);

      d.RegionState.Reset();

      System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
      s.Start();

      for (int z = 0; z < DromeBlocksZ; z++)
      {
        for (int y = 0; y < DromeBlocksY; y++)
        {
          for (int x = 0; x < DromeBlocksX; x++)
          {
            //Computing density from block center instead of corner
            vec3 block_world = dromeOriginR3 + new vec3(
               x * BlockSizeX + BlockSizeX * 0.5f,
               y * BlockSizeY + BlockSizeY * 0.5f,
               z * BlockSizeZ + BlockSizeZ * 0.5f);
            ushort dens = Density(block_world);
            UInt16 created_value = CreateBlock(dens, block_world);
            var block = new Block(created_value);
            SetBlock(d, new ivec3(x, y, z), block, true);

          }
        }
      }
      s.Stop();
      long ms = s.ElapsedMilliseconds;

      return d;
    }
    private Drome FindDromeI3(ivec3 pos)
    {
      Drome d = null;
      _dromes.TryGetValue(pos, out d);
      return d;
    }
    private Glob FindGlobI3(ivec3 pos, GlobCollection c)
    {
      Glob g = null;
      if (c == GlobCollection.All)
      {
        _globs.TryGetValue(pos, out g);
      }
      else if (c == GlobCollection.Render)
      {
        _renderGlobs.TryGetValue(pos, out g);
      }
      else if (c == GlobCollection.VisibleRender)
      {
        _visibleRenderGlobs.TryGetValue(pos, out g);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return g;
    }
    private Glob GetNeighborGlob(Glob g, int i, GlobCollection c)
    {
      //Gets neighbor i=left,right,bottom,top,back,front
      vec3[] glob_offs = new vec3[] {
            new vec3(-World.GlobWidthX, 0, 0),
            new vec3(World.GlobWidthX, 0, 0),
            new vec3(0, -World.GlobWidthY, 0),
            new vec3(0, World.GlobWidthY, 0),
            new vec3(0, 0, -World.GlobWidthZ),
            new vec3(0, 0, World.GlobWidthZ),
         };
      vec3 glob_center_R3 = g.OriginR3 + new vec3(World.GlobWidthX * 0.5f, World.GlobWidthY * 0.5f, World.GlobWidthZ * 0.5f);
      vec3 neighbor_center_R3 = glob_center_R3 + glob_offs[i];
      Glob ret = FindGlobR3(neighbor_center_R3, c);
      return ret;
    }
    private void StitchGlobTopology(Glob g)
    {
      for (int ni = 0; ni < 6; ++ni)
      {
        Glob gn = GetNeighborGlob(g, ni, GlobCollection.All);
        if (gn != null)
        {
          if (gn.GeneratedFrameStamp == g.GeneratedFrameStamp)
          {
            //They were generated on the same frame, and don't need to be stitched.
          }
          else
          {
            //It's way faster to do a cutsom stitching of just the borders, but this is a Q&D thing right now.
            TopologizeGlob(gn);
          }
        }
      }
    }
    private void TopologizeGlob(Glob glob)
    {
      bool globTopologyBefore = glob.Opaque != null || glob.Transparent != null;

      glob.Opaque = null;
      glob.Transparent = null;
      //    6    7
      // 2    3
      //    4    5
      // 0    1
      //Mesh
      List<v_v3n3x2> verts = new List<v_v3n3x2>();
      List<uint> inds = new List<uint>();
      vec3[] face_offs = new vec3[] {
            new vec3(-World.BlockSizeX, 0, 0),
            new vec3(World.BlockSizeX, 0, 0),
            new vec3(0, -World.BlockSizeY, 0),
            new vec3(0, World.BlockSizeY, 0),
            new vec3(0, 0, -World.BlockSizeZ),
            new vec3(0, 0, World.BlockSizeZ),
         };
      Block our_block;
      vec2[] texs = new vec2[4];
      List<MtTex> patches = new List<MtTex>();

      Gu.Assert(glob.Drome != null);
      Drome drome = glob.Drome;

      for (int z = 0; z < GlobBlocksZ; z++)
      {
        for (int y = 0; y < GlobBlocksY; y++)
        {
          for (int x = 0; x < GlobBlocksX; x++)
          {
            ivec3 drome_block_z3 = LocalGlobBlockPos_To_LocalDromeBlockPos(glob.Pos, new ivec3(x, y, z));
            our_block = GetBlock(drome, drome_block_z3);
            if (our_block.IsEmpty())
            {
              continue;
            }

            patches = null;
            _tileUVs.TryGetValue(our_block.Value, out patches);

            for (int face = 0; face < 6; ++face)
            {
              //Bottom left corner
              //This is the exact block center location in R3. It's less efficent but it's easier to use
              vec3 block_pos_rel_R3 = new vec3(World.BlockSizeX * x, World.BlockSizeY * y, World.BlockSizeZ * z);
              vec3 block_pos_rel_R3_Center = block_pos_rel_R3 + new vec3(World.BlockSizeX * 0.5f, World.BlockSizeY * 0.5f, World.BlockSizeZ * 0.5f);
              vec3 block_pos_abs_R3_Center = block_pos_rel_R3_Center + glob.OriginR3;
              vec3 block_pos_abs_R3_Center_Neighbor = block_pos_abs_R3_Center + face_offs[face];
              ivec3 drome_n = R3toI3Drome(block_pos_abs_R3_Center_Neighbor);

              Block? b_n;
              if (drome_n == drome.Pos)
              {
                //We are same glob - globs may not be added to the list yet.
                ivec3 bpos = R3toI3BlockLocal_Drome(block_pos_abs_R3_Center_Neighbor);
                b_n = GetBlock(glob.Drome, bpos);
              }
              else
              {
                //Get the block from a neighbor glob, or drome
                b_n = FindBlockR3_Drome(block_pos_abs_R3_Center_Neighbor);
                if (face == 3 && b_n != null)
                {
                  int n = 0;
                  n++;
                }
              }

              if (b_n == null || b_n.Value.IsSolidBlockNotTransparent())
              {
                //no verts
                int n = 0;
                n++;
              }
              else
              {
                uint foff = (uint)verts.Count;

                //b.Value
                if (patches != null && patches.Count == 3)
                {
                  if ((face == 0) || (face == 1) || (face == 4) || (face == 5))
                  {
                    //LRAF
                    texs[0] = new vec2(patches[BlockTileUVSide.Side].uv0.x, patches[BlockTileUVSide.Side].uv0.y);
                    texs[1] = new vec2(patches[BlockTileUVSide.Side].uv1.x, patches[BlockTileUVSide.Side].uv0.y);
                    texs[2] = new vec2(patches[BlockTileUVSide.Side].uv0.x, patches[BlockTileUVSide.Side].uv1.y);
                    texs[3] = new vec2(patches[BlockTileUVSide.Side].uv1.x, patches[BlockTileUVSide.Side].uv1.y);
                  }
                  else if (face == 2)
                  {
                    //B
                    texs[0] = new vec2(patches[BlockTileUVSide.Bottom].uv0.x, patches[BlockTileUVSide.Bottom].uv0.y);
                    texs[1] = new vec2(patches[BlockTileUVSide.Bottom].uv1.x, patches[BlockTileUVSide.Bottom].uv0.y);
                    texs[2] = new vec2(patches[BlockTileUVSide.Bottom].uv0.x, patches[BlockTileUVSide.Bottom].uv1.y);
                    texs[3] = new vec2(patches[BlockTileUVSide.Bottom].uv1.x, patches[BlockTileUVSide.Bottom].uv1.y);
                  }
                  else if (face == 3)
                  {
                    //T
                    texs[0] = new vec2(patches[BlockTileUVSide.Top].uv0.x, patches[BlockTileUVSide.Top].uv0.y);
                    texs[1] = new vec2(patches[BlockTileUVSide.Top].uv1.x, patches[BlockTileUVSide.Top].uv0.y);
                    texs[2] = new vec2(patches[BlockTileUVSide.Top].uv0.x, patches[BlockTileUVSide.Top].uv1.y);
                    texs[3] = new vec2(patches[BlockTileUVSide.Top].uv1.x, patches[BlockTileUVSide.Top].uv1.y);
                  }

                }
                else
                {
                  //The Top/Side/Bot tile images could not be found (were not created) - default to the whole megatexture [0,1]
                  texs[0] = WorldStaticData.bx_verts_face[face, 0]._x;
                  texs[1] = WorldStaticData.bx_verts_face[face, 1]._x;
                  texs[2] = WorldStaticData.bx_verts_face[face, 2]._x;
                  texs[3] = WorldStaticData.bx_verts_face[face, 3]._x;
                }

                //Verts + Indexes
                vec3 block_pos_abs_R3 = block_pos_rel_R3 + glob.OriginR3;
                for (int vi = 0; vi < 4; ++vi)
                {
                  verts.Add(new v_v3n3x2()
                  {
                    _v = WorldStaticData.bx_verts_face[face, vi]._v + block_pos_abs_R3,
                    _n = WorldStaticData.bx_verts_face[face, vi]._n,
                    _x = texs[vi],
                  });
                }

                for (int ii = 0; ii < 6; ++ii)
                {
                  inds.Add(foff + WorldStaticData.bx_face_inds[ii]);
                }

              }
            }

          }
        }
      }

      if (inds.Count > 0)
      {
        glob.Opaque = new MeshData("", OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles,
           v_v3n3x2.VertexFormat, Gpu.GetGpuDataPtr(verts.ToArray()),
           IndexFormatType.Uint32, Gpu.GetGpuDataPtr(inds.ToArray())
           );
      }

      //Update RenderGlobs
      bool globTopologyAfter = glob.Opaque != null || glob.Transparent != null;
      if (globTopologyBefore && !globTopologyAfter)
      {
        _renderGlobs.Remove(glob.Pos);
      }
      else if (!globTopologyBefore && globTopologyAfter)
      {
        _renderGlobs.Add(glob.Pos, glob);
      }

      glob.State = Glob.GlobState.Topologized;
    }
    private UInt16 Density(vec3 world_pos)
    {
      float d = -world_pos.y;

      //basic hilly thingy
      for (int ia = 1; ia <= 4; ++ia)
      {
        float a = (float)ia;
        float f = 1 / a;

        float sign = ia % 2 == 0 ? -1 : 1;//prevent huge hils

        d = d + (sign) * MathUtils.cosf(world_pos.x / World.BlockSizeX * 0.1f * f) * 3 * a + (sign) * MathUtils.sinf(world_pos.z / World.BlockSizeZ * 0.1f * f) * 3 * a * BlockSizeY;
      }

      ushort item = BlockItemCode.Empty;
      if (d > 0)
      {
        item = BlockItemCode.Value;
      }

      return item;
    }
    private void SweepGridFrustum(Action<ivec3, Box3f> func, Frustum pf, float fMaxDist)
    {
      vec3 cp = pf.NearCenter;
      int iDebugSweepCount = 0;
      List<ivec3> toCheck = new List<ivec3>();
      HashSet<ivec3> dchecked = new HashSet<ivec3>();

      int nPotentialGlobs = (int)((fMaxDist / GlobWidthX) * (fMaxDist / GlobWidthY) * (fMaxDist / GlobWidthZ));
      int nMaxPotentialGlobs = 5000;
      if (nPotentialGlobs > nMaxPotentialGlobs)
      {
        //This is technically an error, but we may also just hard limit the sweep routine if we weant.
        Gu.Log.WarnCycle("Warning: potential number of globs " + nPotentialGlobs + " exceeds " + nMaxPotentialGlobs);
        Gu.DebugBreak();
      }

      float fMaxDist2 = fMaxDist * fMaxDist;

      //Seed
      toCheck.Add(R3toI3Glob(cp));

      while (toCheck.Count > 0)
      {
        ivec3 vi = toCheck[0];
        toCheck.RemoveAt(0);// erase(toCheck.begin() + 0);
        iDebugSweepCount++;

        if (!dchecked.Contains(vi))
        {
          //TODO: fix this because we're getting stack overflows
          dchecked.Add(new ivec3(vi.x, vi.y, vi.z));

          // if the grid right here intersects the frustum
          Box3f box = GetGlobBoxGlobalI3(vi);

          vec3 node_center = box.center();

          float fDist2 = (pf.NearCenter - node_center).length2();

          if (fDist2 < fMaxDist2)
          {
            if (pf.HasBox(box))
            {
              func(vi, box);

              //Sweep Neighbors
              vec3[] n = new vec3[6];
              for (int ni = 0; ni < 6; ++ni)
              {
                n[ni] = node_center + WorldStaticData.GlobNeighborOffsets[ni];

              }

              toCheck.Add(R3toI3Glob(n[0]));
              toCheck.Add(R3toI3Glob(n[1]));
              toCheck.Add(R3toI3Glob(n[2]));
              toCheck.Add(R3toI3Glob(n[3]));
              toCheck.Add(R3toI3Glob(n[4]));
              toCheck.Add(R3toI3Glob(n[5]));
            }
            else
            {
              int nnn = 0;
              nnn++;
            }
          }
          else
          {
            int nnn = 0;
            nnn++;
          }
        }
      }

      dchecked.Clear();

    }
    private int DromeBlockDataOffset(ivec3 local_pos)
    {
      int ret = local_pos.z * World.DromeBlocksX * World.DromeBlocksY + local_pos.y * World.DromeBlocksX + local_pos.x;
      return ret;
    }
    public Drome GetDromeForGlob(Glob g)
    {
      Drome ret = null;
      ivec3 dvi = GlobPosToDromePos(g.Pos);
      _dromes.TryGetValue(dvi, out ret);
      return ret;
    }
    //public void SetBlock(Glob g, ivec3 local_pos_glob, Block block, bool bQueueForUpdate)
    //{
    //  //This may be unsafe considering all the wonky conversions. might be better to just set the drome directly.
    //  var d = GetDromeForGlob(g);
    //  ivec3 local_dr = LocalGlobBlockPos_To_LocalDromeBlockPos(d, g, local_pos_glob);
    //  SetBlock(d, local_dr, block, bQueueForUpdate);
    //}
    public void SetBlock(Drome d, ivec3 local_block_pos_in_drome, Block block, bool bInitialGen_Dont_Queue_For_Update)
    {
      //@DonoTretopologizeyet -- if you're setting a lot of blocks, avoid updating too many times
      //We may be empty, in which case we need to reallocate our data. If the block is empty, though, then setting it to empty does nothing, as we are already empty.
      if (block.IsEmpty() && d.RegionState.State == RegionState.Empty_AndNoData)
      {
        return;
      }
      if (d.Blocks == null)
      {
        //We cull blocks from empty globs to save memory.
        d.Blocks = new Block[World.DromeBlocksX * World.DromeBlocksY * World.DromeBlocksZ];
      }

      Block old = new Block(0);
      if (!bInitialGen_Dont_Queue_For_Update)
      {
        old = GetBlock(d, local_block_pos_in_drome);
      }
      else
      {
        d.RegionState.UpdateInitialGenAddedBlock(block);
      }
      d.Blocks[DromeBlockDataOffset(local_block_pos_in_drome)] = block;

      if (!bInitialGen_Dont_Queue_For_Update)
      {
        d.RegionState.UpdateBlockModified(old, block);
        Gu.Log.Warn("Commented out queue for update..");

        vec3 block_pos_r3 = d.OriginR3 + new vec3(
          (float)local_block_pos_in_drome.x * BlockSizeX,
          (float)local_block_pos_in_drome.y * BlockSizeY,
          (float)local_block_pos_in_drome.z * BlockSizeZ
          );

        ivec3 glob_pos = R3toI3Glob(block_pos_r3);
        var g = FindGlobI3(glob_pos, GlobCollection.All);

        //Assuming that SetBlock with bInitialGen_.. as false is only ever called when we mine a block.. Therefore the block must be in view. and loaded
        if (g != null)
        {
          QueueForUpdate(g);
        }
        else
        {
          Gu.Log.Warn("Glob was null for queue");
        }
      }
    }
    public void QueueForUpdate(Glob g)
    {
      if (!_globsToUpdate.ContainsKey(g.Pos))
      {
        _globsToUpdate.Add(g.Pos, g);
      }
    }
    //public Block GetBlock(Glob g, ivec3 local_pos_glob)
    //{
    //  //If we are empty, then we have deleted our Block[] data to save space. Return an empty block

    //  Drome d = GetDromeForGlob(g);
    //  if (d != null)
    //  {
    //    return d.Blocks[GlobBlockDataOffset(d, local_pos_glob)];
    //  }
    //  else
    //  {
    //    Gu.BRThrowException("drome was nul");
    //  }
    //  return new Block(BlockItemCode.Empty);
    //}
    public Block GetBlock(Drome d, ivec3 local_pos_drome)
    {
      //If we are empty, then we have deleted our Block[] data to save space. Return an empty block
      if (d.RegionState.State == RegionState.Empty_AndNoData)
      {
        return new Block(BlockItemCode.Empty);
      }

      int off = DromeBlockDataOffset(local_pos_drome);
      return d.Blocks[off];
    }

    #region Index Functions

    private ivec3 R3ToI3BlockGlobal(vec3 pt)
    {
      //Return trhe integer location of a block in block units
      ivec3 v;
      v.x = (int)Math.Floor(pt.x / BlockSizeX);
      v.y = (int)Math.Floor(pt.y / BlockSizeY);
      v.z = (int)Math.Floor(pt.z / BlockSizeZ);
      return v;
    }
    private Block? FindBlockR3_Drome(vec3 R3_pos)
    {
      Drome d = FindDromeR3(R3_pos);
      if (d == null)
      {
        return null;
      }
      ivec3 bpos = R3toI3BlockLocal_Drome(R3_pos);
      Block b = GetBlock(d, bpos);

      return b;
    }
    private float R3toI3BlockComp(float R3, float BlocksAxis, float BlockWidth)
    {
      float bpos;
      if (R3 < 0)
      {
        bpos = (float)Math.Floor((R3 % BlocksAxis + BlocksAxis) / BlockWidth);
      }
      else
      {
        bpos = (float)Math.Floor((R3 % BlocksAxis) / BlockWidth);
      }
      return bpos;
    }
    private ivec3 R3ToI3BlockLocal_Any(vec3 R3, float cont_w_x, float cont_w_y, float cont_w_z)
    {
      vec3 bpos = new vec3(
       R3toI3BlockComp(R3.x, cont_w_x, BlockSizeX),
       R3toI3BlockComp(R3.y, cont_w_y, BlockSizeY),
       R3toI3BlockComp(R3.z, cont_w_z, BlockSizeZ));

      return new ivec3((int)bpos.x, (int)bpos.y, (int)bpos.z);
    }
    private ivec3 R3toI3BlockLocal_Glob(vec3 R3)
    {
      ivec3 bpos = R3ToI3BlockLocal_Any(R3, GlobWidthX, GlobWidthY, GlobWidthZ);

      if (bpos.x < 0 || bpos.y < 0 || bpos.z < 0 || bpos.x >= World.GlobBlocksX || bpos.y >= World.GlobBlocksY || bpos.z >= World.GlobBlocksZ)
      {
        Gu.DebugBreak();
      }
      return bpos;
    }
    private ivec3 R3toI3BlockLocal_Drome(vec3 R3)
    {
      ivec3 bpos = R3ToI3BlockLocal_Any(R3, DromeWidthX, DromeWidthY, DromeWidthZ);
      if (bpos.x < 0 || bpos.y < 0 || bpos.z < 0 || bpos.x >= World.DromeBlocksX || bpos.y >= World.DromeBlocksY || bpos.z >= World.DromeBlocksZ)
      {
        Gu.DebugBreak();
      }
      return bpos;
    }
    private ivec3 R3toI3Glob(vec3 R3)
    {
      //v3toi3Node
      ivec3 gpos = new ivec3(
         (int)Math.Floor(R3.x / World.GlobWidthX),
         (int)Math.Floor(R3.y / World.GlobWidthY),
         (int)Math.Floor(R3.z / World.GlobWidthZ));
      return gpos;
    }
    private Glob FindGlobR3(vec3 R3_pos, GlobCollection c)
    {
      ivec3 gpos = R3toI3Glob(R3_pos);

      return FindGlobI3(gpos, c);
    }
    private Drome FindDromeR3(vec3 R3_pos)
    {
      ivec3 dpos = R3toI3Drome(R3_pos);
      return FindDromeI3(dpos);
    }
    private ivec3 R3toI3Drome(vec3 R3)
    {
      ivec3 dpos = new ivec3(
         (int)Math.Floor(R3.x / World.DromeWidthX),
         (int)Math.Floor(R3.y / World.DromeWidthY),
         (int)Math.Floor(R3.z / World.DromeWidthZ));
      return dpos;
    }
    private Box3f GetGlobBoxGlobalI3(ivec3 pt)
    {
      //Return the bound box for the glob at the integer glob grid position
      Box3f box = new Box3f();
      box._min.x = (float)(pt.x + 0) * GlobWidthX;
      box._min.y = (float)(pt.y + 0) * GlobWidthY;
      box._min.z = (float)(pt.z + 0) * GlobWidthZ;
      box._max.x = (float)(pt.x + 1) * GlobWidthX;
      box._max.y = (float)(pt.y + 1) * GlobWidthY;
      box._max.z = (float)(pt.z + 1) * GlobWidthZ;
      return box;
    }
    public Box3f GetBlockBox(PickedBlock b, float padding)
    {
      return GetBlockBoxLocal(b.Drome, b.BlockPosLocal, padding);
    }
    private Box3f GetBlockBoxLocal(Drome d, ivec3 local, float padding)
    {
      //padding is an extra boundary we add to the box so it doesn't exactly coincide with the mesh geometry. Used for rendering.
      Gu.Assert(d != null);
      Box3f box = new Box3f();
      box._min = d.OriginR3 + local.toVec3() * new vec3(BlockSizeX, BlockSizeY, BlockSizeZ);
      box._max.x = box._min.x + BlockSizeX;
      box._max.y = box._min.y + BlockSizeY;
      box._max.z = box._min.z + BlockSizeZ;
      box._max += padding;
      box._min += padding;

      return box;
    }
    private Box3f GetBlockBoxGlobalR3(vec3 pt)
    {
      //Snap the point pt to the block grid, and return the bound box of that block
      Box3f box = new Box3f();
      box._min = R3ToI3BlockGlobal(pt).toVec3() * new vec3(BlockSizeX, BlockSizeY, BlockSizeZ);
      box._max.x = box._min.x + BlockSizeX;
      box._max.y = box._min.y + BlockSizeY;
      box._max.z = box._min.z + BlockSizeZ;
      return box;
    }
    private ivec3 GlobPosToDromePos(ivec3 globPos)
    {
      ivec3 ret = new ivec3((int)Math.Floor((double)globPos.x / (double)DromeGlobsX),
                            (int)Math.Floor((double)globPos.y / (double)DromeGlobsY),
                            (int)Math.Floor((double)globPos.z / (double)DromeGlobsZ));
      return ret;
    }
    private int GlobOffsetInDrome_LocalZ3_Comp(int Z3, int BlocksAxis)
    {
      int bpos;
      if (Z3 < 0)
      {
        bpos = (BlocksAxis - Math.Abs(Z3 % BlocksAxis)) % BlocksAxis;
      }
      else
      {
        bpos = (int)(Z3 % BlocksAxis);
      }
      return bpos;
    }
    private ivec3 GlobOffsetInDrome_LocalZ3(ivec3 pos_glob_global_z3)
    {
      ivec3 r = new ivec3(
        GlobOffsetInDrome_LocalZ3_Comp(pos_glob_global_z3.x, DromeGlobsX),
        GlobOffsetInDrome_LocalZ3_Comp(pos_glob_global_z3.y, DromeGlobsY),
        GlobOffsetInDrome_LocalZ3_Comp(pos_glob_global_z3.z, DromeGlobsZ)
        )
        ;
      return r;
    }
    private ivec3 LocalGlobBlockPos_To_LocalDromeBlockPos(ivec3 glob_pos_z3_global, ivec3 glob_block_pos_local)
    {
      ivec3 glob_off_local = GlobOffsetInDrome_LocalZ3(glob_pos_z3_global);

      Gu.Assert(
        glob_off_local.x >= 0 && glob_off_local.y >= 0 && glob_off_local.z >= 0 &&
        glob_off_local.x < DromeGlobsX && glob_off_local.y < DromeGlobsY && glob_off_local.z < DromeGlobsZ
        );

      ivec3 ret = new ivec3(
        glob_off_local.x * GlobBlocksX + glob_block_pos_local.x,
        glob_off_local.y * GlobBlocksY + glob_block_pos_local.y,
        glob_off_local.z * GlobBlocksZ + glob_block_pos_local.z
       );

      Gu.Assert(
        ret.x >= 0 && ret.y >= 0 && ret.z >= 0 &&
        ret.x < DromeBlocksX && ret.y < DromeBlocksY && ret.z < DromeBlocksZ
        );

      return ret;
    }

    #endregion

    public class PickedBlock
    {
      public bool Hit = false;
      //public Glob Glob;
      public Drome Drome;
      public Block Block;
      public ivec3 BlockPosLocal;
      public vec3 HitPos;
      public vec3 HitNormal;
    }
    public PickedBlock RaycastBlock(PickRay3D pr)
    {
      //TODO: much faster if we marched the globs first, then the blocks. We do only blocks
      PickedBlock pb = new PickedBlock();
      pb.Hit = false;
      //Snap point to block center
      vec3 center = R3ToI3BlockGlobal(pr.Origin).toVec3() *
         new vec3(BlockSizeX, BlockSizeY, BlockSizeZ) +
         new vec3(BlockSizeX * 0.5f, BlockSizeY * 0.5f, BlockSizeZ * 0.5f);
      List<vec3> toCheck = new List<vec3>() { center };
      Drome cur_drome = null;
      ivec3 icur_drome = default(ivec3);
      int dbg_count_loop = 0;
      BoxAAHit bh = new BoxAAHit();
      bool already_checked_glob = false;
      int dbg_nglobcheck = 0;
      int dbg_nblockcheck = 0;
      int dbg_checkarrmaxsiz = 0;

      while (toCheck.Count > 0)
      {
        if (toCheck.Count > dbg_checkarrmaxsiz)
        {
          dbg_checkarrmaxsiz = toCheck.Count;
        }

        vec3 cpos = toCheck[0]; //position of block
        toCheck.RemoveAt(0);
        if ((pr.Project(cpos) - pr.Origin).length2() > (pr.Length * pr.Length))
        {
          //We are beyond the ray.
          break;
        }

        ivec3 iglob = R3toI3Drome(cpos);
        if ((cur_drome == null && already_checked_glob == false) || (icur_drome != iglob))
        {
          cur_drome = FindDromeI3(iglob);
          icur_drome = iglob;
          already_checked_glob = true;
          dbg_nglobcheck++;
        }

        if ((cur_drome != null) && (cur_drome.Blocks != null) && (cur_drome.RegionState.State != RegionState.Empty_AndNoData))
        {
          dbg_nblockcheck++;
          ivec3 b3i = R3toI3BlockLocal_Drome(cpos);
          Block b = GetBlock(cur_drome, b3i);
          if (!b.IsEmpty())
          {
            pb.Drome = cur_drome;
            pb.Block = b;
            pb.BlockPosLocal = b3i;
            var blockbox = GetBlockBoxGlobalR3(cpos);
            if (blockbox.LineOrRayIntersectInclusive_EasyOut(pr, ref bh))
            {
              pb.HitPos = pr.Origin + pr.Dir * bh._t;
              pb.HitNormal = -pr.Dir;
              pb.Hit = true;
              break;
            }
            else
            {
              //Error - we did in fact collilde with this block but the box says otherwise
              Gu.DebugBreak();
            }
          }
        }

        Action<bool, vec3> recur_neighbor_by_dir = (dir_test, n_offset) =>
        {
          if (dir_test)
          {
            vec3 n_pos = cpos + n_offset;
            Box3f b = GetBlockBoxGlobalR3(n_pos);
            if (b.LineOrRayIntersectInclusive_EasyOut(pr, ref bh))
            {
              toCheck.Add(n_pos);
            }
          }
        };

        recur_neighbor_by_dir(pr.Dir.x < 0, WorldStaticData.BlockNeighborOffsets[0]);
        recur_neighbor_by_dir(pr.Dir.x > 0, WorldStaticData.BlockNeighborOffsets[1]);
        recur_neighbor_by_dir(pr.Dir.y < 0, WorldStaticData.BlockNeighborOffsets[2]);
        recur_neighbor_by_dir(pr.Dir.y > 0, WorldStaticData.BlockNeighborOffsets[3]);
        recur_neighbor_by_dir(pr.Dir.z < 0, WorldStaticData.BlockNeighborOffsets[4]);
        recur_neighbor_by_dir(pr.Dir.z > 0, WorldStaticData.BlockNeighborOffsets[5]);

        dbg_count_loop++;

      }

      return pb;
    }

    #endregion

    #region Private: Files

    private void InitWorldDiskFile(bool delete_world_start_fresh)
    {
      WorldSavePath = System.IO.Path.Combine(Gu.SavePath, WorldName);

      if (delete_world_start_fresh)
      {
        if (System.IO.Directory.Exists(WorldSavePath))
        {
          Gu.Log.Info("Starting Fresh - Deleting " + WorldSavePath);
          Directory.Delete(WorldSavePath, true);
        }
      }

      // This is the WORLD save file. Player position and stuff
      // This isn't the blocks data
      Gu.Log.Info("Creating world save directory " + WorldSavePath);
      if (!System.IO.Directory.Exists(WorldSavePath))
      {
        System.IO.Directory.CreateDirectory(WorldSavePath);
      }
      if (!TryLoadWorld())
      {
        SaveWorld();
      }
    }
    public void SaveWorld()
    {
      //We can call this if the player moves or something.
      if (Player == null)
      {
        Gu.BRThrowException("Player must not be null when creating world.");
      }

      string worldfn = GetWorldFileName();
      var enc = Encoding.GetEncoding("iso-8859-1");
      using (var fs = System.IO.File.OpenWrite(worldfn))
      using (var br = new System.IO.BinaryWriter(fs, enc))
      {
        br.Write((string)SaveWorldHeader);
        br.Write(Player.Position);
        br.Write(Player.Rotation);
      }
    }
    private bool TryLoadWorld()
    {
      if (Player == null)
      {
        Gu.BRThrowException("Player must not be null when creating world.");
      }

      string worldfn = GetWorldFileName();
      var enc = Encoding.GetEncoding("iso-8859-1");
      if (!System.IO.File.Exists(worldfn))
      {
        return false;
      }

      using (var fs = System.IO.File.OpenRead(worldfn))
      using (var br = new System.IO.BinaryReader(fs, enc))
      {
        string h = br.ReadString();
        if (h != SaveWorldHeader)
        {
          Gu.BRThrowException("World header '" + h + "' does not match current header version '" + SaveWorldHeader + "'");
        }
        Player.Position = br.ReadVec3();
        Player.Rotation = br.ReadQuat();
      }
      return true;
    }
    private string DromeFooter = "EndOfDrome";
    private void SaveDrome(Drome d)
    {
      string globfn = GetDromeFileName(d.Pos);
      var enc = Encoding.GetEncoding("iso-8859-1");
      bool append = false;
      if (System.IO.File.Exists(globfn))
      {
        append = true;
      }

      try
      {
        using (var fs = System.IO.File.OpenWrite(globfn))
        using (var br = new System.IO.BinaryWriter(fs, enc))
        {
          br.Write((Int32)DromeFileVersion);
          br.Write((Int32)d.Pos.x);
          br.Write((Int32)d.Pos.y);
          br.Write((Int32)d.Pos.z);
          d.RegionState.Serialize(br);


          // br.Write((Int32)d.DensityState);
          //if (d.GlobRegionStates == null)
          //{
          //  br.Write((Int32)0);
          //}
          //else
          //{
          //  var byteArr = new byte[Marshal.SizeOf(typeof(Block)) * d.GlobRegionStates.Length];
          //  var pinnedHandle = GCHandle.Alloc(d.GlobRegionStates, GCHandleType.Pinned);
          //  Marshal.Copy(pinnedHandle.AddrOfPinnedObject(), byteArr, 0, byteArr.Length);
          //  pinnedHandle.Free();
          //  br.Write((Int32)byteArr.Length);
          //  br.Write(byteArr);
          //}
          //  br.Write((Int32)d.Solid);
          //  br.Write((Int32)d.Empty);
          // br.Write((Int32)d.Items);
          if (d.Blocks == null)
          {
            br.Write((Int32)0);
          }
          else
          {

            var byteArr = new byte[Marshal.SizeOf(typeof(Block)) * d.Blocks.Length];
            var pinnedHandle = GCHandle.Alloc(d.Blocks, GCHandleType.Pinned);
            Marshal.Copy(pinnedHandle.AddrOfPinnedObject(), byteArr, 0, byteArr.Length);
            pinnedHandle.Free();
            byte[] compressed = Compress(byteArr);

            br.Write((Int32)compressed.Length);
            br.Write(compressed);

          }

          br.Write(DromeFooter);

          br.Close();

        }

      }
      catch (Exception ex)
      {
        //Delete corrupt files if we created corrupt one.
        if (append == false)
        {
          if (File.Exists(globfn))
          {
            File.Delete(globfn);
          }
        }
        throw ex;
      }
    }
    private Drome TryLoadDrome(ivec3 dpos)
    {
      //Return null if no glob file was found.
      string dromefn = GetDromeFileName(dpos);
      Drome d = null;
      try
      {
        if (File.Exists(dromefn))
        {
          d = new Drome(new ivec3(0, 0, 0), Gu.Context.FrameStamp);

          var enc = Encoding.GetEncoding("iso-8859-1");

          using (var fs = File.OpenRead(dromefn))
          using (var br = new System.IO.BinaryReader(fs, enc))
          {
            Int32 version = br.ReadInt32();
            if (version != DromeFileVersion)
            {
              Gu.BRThrowException("Glob file verison '" + version + "' does not match required version '" + DromeFileVersion + "'.");
            }

            //d.DensityState = (Glob.GlobDensityState)br.ReadInt32();
            d.Pos.x = br.ReadInt32();
            d.Pos.y = br.ReadInt32();
            d.Pos.z = br.ReadInt32();
            d.RegionState.Deserialize(br);

            int compressed_count = br.ReadInt32();
            if (compressed_count == 0)
            {
              d.Blocks = null;
            }
            else
            {
              var compressed = br.ReadBytes(compressed_count);

              byte[] decompressed = Decompress(compressed);
              var numStructs = decompressed.Length / Marshal.SizeOf(typeof(Block));

              Gu.Assert(numStructs == DromeBlocksX * DromeBlocksY * DromeBlocksZ);

              d.Blocks = new Block[numStructs];
              var pinnedHandle = GCHandle.Alloc(d.Blocks, GCHandleType.Pinned);
              Marshal.Copy(decompressed, 0, pinnedHandle.AddrOfPinnedObject(), decompressed.Length);
              pinnedHandle.Free();
            }

            string footer = br.ReadString();
            if (!footer.Equals(DromeFooter))
            {
              Gu.BRThrowException("Error: Invalid drome file. Incorrect footer.");
            }

            br.Close();
          }
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Drome " + dromefn + " had an error loading. " + ex.ToString());
        return null;
      }
      return d;
    }
    public static byte[] Compress(byte[] data)
    {
      MemoryStream output = new MemoryStream();
      using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
      {
        dstream.Write(data, 0, data.Length);
      }
      return output.ToArray();
    }

    public static byte[] Decompress(byte[] data)
    {
      MemoryStream input = new MemoryStream(data);
      MemoryStream output = new MemoryStream();
      using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
      {
        dstream.CopyTo(output);
      }
      return output.ToArray();
    }
    #endregion


  }
}
