using OpenTK.Graphics.OpenGL4;
using System.Text;

namespace PirateCraft
{

  public enum GenState
  {
    Created, Queued, GenStart, GenEnd, Ready, Deleted,
  }
  public enum GameMode
  {
    Play, Edit
  }
  public enum TileImage
  {
    Grass, GrassSide, Dirt, Plank, Brick, Brick2, Gravel, Sand, Cedar,
    Cedar_Needles, Cedar_Top, Feldspar,
    Tussock, Tussock2, Tussock_Stalk_Bot, Tussock_Stalk_Mid, Tussock_Stalk_Top,

    Blank, Dandilion, Cracks1, Cracks2, Cracks3, Cracks4, Feldspar_Coal, Marble_White, Marble_Green, Water, Seaweed, Clay, RedClay, RosePink, RoseRed,
    Oak_Top, Oak, Oak_Leaves,
  }
  public enum BeamFaceIndex
  {
    Left = 0, Right = 1, Bottom = 2, Top = 3, Back = 4, Front = 5
  }
  public enum BeamEdgeIndex
  {
    BL = 0, BR = 1, TL = 2, TR = 3
  }
  public class PickedTri
  {
    //A picked block from a raycast or other

    public bool IsHit { get { return _t1 >= 0.0f && _t1 <= 1.0f; } }
    // public Drome Drome = null;
    public ushort Block;
    public ivec3 BlockPosLocalZ3;
    public vec3 HitNormal;
    public vec3 HitPosR3;
    public vec3 BlockCenterR3;
    public float _t1 = float.MaxValue;//tmin
    public float _t2 = float.MaxValue;//tmax
    public List<Box3f> PickedBlockBoxes_Debug = null;
    public bool AddPickedBlockBoxes_Debug = false;
    public RaycastResult RaycastResult = RaycastResult.Unset;
  }
  public class BlockTileUVSide
  {
    public const int Top = 0;
    public const int Side = 1;
    public const int Bottom = 2;
  }
  public class HardnessValue
  {
    public const float Leaf = 0.1716f;
    public const float Dirt = 0.9716f;
    public const float Wood = 3.201f;
    public const float Gravel = 2.482f;
    public const float Rock = 4.15f;
    public const float DeepRock = 7.89f;
    public const float Diamond = 12.51f;
    public const float Carbide = 21.2101f;
    public const float Water = 1;
  }
  public class BlockItemCode
  {
    //Blocks
    public const ushort Missing = 0; //block is missing, maybe drome not loaded, not air /land/water
    public const ushort Land = 2; //catch-all for land
    public const ushort Grass = 5;
    public const ushort Dirt = 6;
    public const ushort Brick = 7;
    public const ushort Brick2 = 8;
    public const ushort Gravel = 9;
    public const ushort Sand = 10;
    public const ushort Cedar_Sapling = 11;
    public const ushort Cedar = 12;
    public const ushort Cedar_Needles = 13;
    public const ushort Feldspar = 14;
    public const ushort Tussock = 15;
    public const ushort Dandilion = 16;
    public const ushort Torch = 17;
    public const ushort Feldspar_Coal = 18;
    public const ushort Marble_White = 19;
    public const ushort Marble_Green = 20;
    public const ushort Water = 21; //This is not a block this is just a code to determine visibility if there is no solid block
    public const ushort Seaweed = 22;
    public const ushort Clay = 23;
    public const ushort RedClay = 24;
    public const ushort RosePink = 25;
    public const ushort RoseRed = 26;
    public const ushort Oak_Leaves = 27;
    public const ushort Oak = 28;

    //MAX ** 
    public const ushort MaxBlockItemCodes = 29;//This can be anywhere up to ushort - fog bits - water
                                               //Items
                                               //...
  }
  public enum BarVertFlags
  {
    //Flags for bars.
    /*
      //Vertex Monad for Neighbor beam vert

         V2-----V3 Quad Side /Top / bot order
    z    |      |      
    ^ |  |      |
      V1 V0-----V1      
     -V3 V2--      
      >x  |           
    */
    AttachedV0 = 0x01,//whether bar is attached
    AttachedV1 = 0x02,
    AttachedV2 = 0x04,
    AttachedV3 = 0x08,
    CapFlat_or_Overhang = 0x16, //if set, we are overhang
    AttachedALL = AttachedV0 | AttachedV1 | AttachedV2 | AttachedV3
  }

  public struct BeamEdge
  {
    public float Bot = 0;//Bottom y
    public float Top = 0;// Top Y
    public byte Flags = 0; //BarVertFlag

    public BeamEdge() { }
    public BeamEdge(float b, float t, byte flags = 0)
    {
      Bot = b;
      Top = t;
      Flags = flags;
    }
  }
  public struct Beam
  {
    //we could store BeamEdge verts in a grid and have "on/off" for the voxel, however, that is inefficient given what we're trying to achieve.
    public const int c_iEdgeCount = 4;
    public const int c_iSideCount = 6;

    public BeamEdge[] Edges = new BeamEdge[c_iEdgeCount];//Index with EdgeIndex
    public ushort[] Tiles = new ushort[c_iSideCount] { //Index with BeamSide
      //BeamFaceIndex = LRBTAF
      BlockItemCode.Dirt,
      BlockItemCode.Dirt,
      BlockItemCode.Dirt,
      BlockItemCode.Grass,
      BlockItemCode.Dirt,
      BlockItemCode.Dirt
    };
    public int Flags = 0;

    public BeamEdge EdgeBL { get { return Edges[(int)BeamEdgeIndex.BL]; } }
    public BeamEdge EdgeBR { get { return Edges[(int)BeamEdgeIndex.BR]; } }
    public BeamEdge EdgeTL { get { return Edges[(int)BeamEdgeIndex.TL]; } }
    public BeamEdge EdgeTR { get { return Edges[(int)BeamEdgeIndex.TR]; } }
    public float TopY { get { return Math.Max(EdgeBL.Top, Math.Max(EdgeBR.Top, Math.Max(EdgeTL.Top, EdgeTR.Top))); } }
    public float BotY { get { return Math.Min(EdgeBL.Bot, Math.Min(EdgeBR.Bot, Math.Min(EdgeTL.Bot, EdgeTR.Bot))); } }

    public Beam() { }
    public Beam(float bot, float top, byte? flags = null)
    {
      for (int i = 0; i < c_iEdgeCount; i++)
      {
        Edges[i] = new BeamEdge(bot, top, (flags != null ? flags.Value : (byte)0));
      }
    }

  }
  public class WorldStaticData
  {
    public static ivec3[] n_off = new ivec3[]
    {
      new ivec3(-1, 0, 0),
      new ivec3( 1, 0, 0),
      new ivec3( 0,-1, 0),
      new ivec3( 0, 1, 0),
      new ivec3( 0, 0,-1),
      new ivec3( 0, 0, 1),
    };
    //Unit box for creating mesh cubes, Tiles, Material

    // public static vec3[] BlockNeighborOffsets = new vec3[6]
    // {
    //   new vec3(-World.BlockSizeX, 0, 0),
    //   new vec3(World.BlockSizeX, 0, 0),
    //   new vec3(0, -World.BlockSizeY, 0),
    //   new vec3(0, World.BlockSizeY, 0),
    //   new vec3(0, 0, -World.BlockSizeZ),
    //   new vec3(0, 0, World.BlockSizeZ),
    // };

    public static Dictionary<TileImage, FileLoc> TileImages = new Dictionary<TileImage, FileLoc>() {
            { TileImage.Grass, new FileLoc("tx64_grass.png", FileStorage.Embedded) },
            //LOAD FASTER
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
            { TileImage.Tussock, new FileLoc("tx64_tussock.png", FileStorage.Embedded) },
            { TileImage.Tussock2, new FileLoc("tx64_tussock2.png", FileStorage.Embedded) },
            { TileImage.Tussock_Stalk_Bot, new FileLoc("tx64_tussock_stalk_bot.png", FileStorage.Embedded) },
            { TileImage.Tussock_Stalk_Mid, new FileLoc("tx64_tussock_stalk_mid.png", FileStorage.Embedded) },
            { TileImage.Tussock_Stalk_Top, new FileLoc("tx64_tussock_stalk_top.png", FileStorage.Embedded) },
            { TileImage.Blank, new FileLoc("tx64_blank.png", FileStorage.Embedded) },
            { TileImage.Dandilion, new FileLoc("tx64_dandilion.png", FileStorage.Embedded) },
            { TileImage.Cracks1, new FileLoc("tx64_cracks1.png", FileStorage.Embedded) },
            { TileImage.Cracks2, new FileLoc("tx64_cracks2.png", FileStorage.Embedded) },
            { TileImage.Cracks3, new FileLoc("tx64_cracks3.png", FileStorage.Embedded) },
            { TileImage.Cracks4, new FileLoc("tx64_cracks4.png", FileStorage.Embedded) },
            { TileImage.Feldspar_Coal, new FileLoc("tx64_plagioclase_coal.png", FileStorage.Embedded) },
            { TileImage.Marble_Green, new FileLoc("tx64_marble_green.png", FileStorage.Embedded) },
            { TileImage.Marble_White, new FileLoc("tx64_marble_white.png", FileStorage.Embedded) },
            { TileImage.Water, new FileLoc("tx64_water.png", FileStorage.Embedded) },
            { TileImage.Seaweed, new FileLoc("tx64_seaweed.png", FileStorage.Embedded) },
            { TileImage.Clay, new FileLoc("tx64_clay.png", FileStorage.Embedded) },
            { TileImage.RedClay, new FileLoc("tx64_red_clay.png", FileStorage.Embedded) },
            { TileImage.RosePink, new FileLoc("tx64_rose_pink.png", FileStorage.Embedded) },
            { TileImage.RoseRed, new FileLoc("tx64_rose_red.png", FileStorage.Embedded) },
            { TileImage.Oak, new FileLoc("tx64_oak_side.png", FileStorage.Embedded) },
            { TileImage.Oak_Leaves, new FileLoc("tx64_oak_leaves.png", FileStorage.Embedded) },
         };

    private static vec3[] bx_box = new vec3[8];
    public static vec3[] bx_norms = new vec3[6];//lrbtaf
    private static vec2[] bx_texs = new vec2[4];
    public static v_v3n3x2t3u1[,] bx_verts_face { get; private set; } = new v_v3n3x2t3u1[6, 4];//lrbtaf
    public static uint[] bx_face_inds { get; private set; }

    private static vec3[] bb_planes_Zup = new vec3[8];
    private static vec3[] bb_norms_Zup = new vec3[2];
    public static v_v3n3x2t3u1[,] bb_verts_face_zup { get; private set; } = new v_v3n3x2t3u1[2, 4];//normals point +x, +z
    public static uint[] bb_face_inds_zup { get; private set; }

  }

  public class BeamList : List<Beam>, ICanSerializeMyself
  {
    public void Serialize(BinaryWriter bw)
    {
      Gu.BRThrowNotImplementedException();
    }
    public void Deserialize(BinaryReader br)
    {
      Gu.BRThrowNotImplementedException();
    }
  }
  public class BarGrid : Grid2D<BeamList>, ICanSerializeMyself
  {
    private WeakReference<WorldInfo> _info = null;

    //Editable bar grid for generation
    public BarGrid(WorldInfo inf) : base(inf.GlobBlocksX, inf.GlobBlocksZ)
    {
      _info = new WeakReference<WorldInfo>(inf);
    }
    public void Edit_GenFlat(float y_base_rel, float y_height_rel)
    {
      if (_info.TryGetTarget(out var inf))
      {
        Iterate((g, x, z) =>
        {
          byte flags = (byte)BarVertFlags.AttachedALL;
          BeamList bl = new BeamList();
          bl.Add(new Beam(y_base_rel, y_height_rel + Random.Next(0, 3), flags));

          Set(new ivec2(x, z), bl);
          return LambdaBool.Continue;
        });
      }
    }
    public new void Serialize(BinaryWriter bw)
    {
      base.Serialize(bw);
      Gu.BRThrowNotImplementedException();
    }
    public new void Deserialize(BinaryReader br)
    {
      base.Deserialize(br);
      Gu.BRThrowNotImplementedException();
    }
  }

  public class Glob
  {
    public enum GlobState
    {
      None, Created, Loaded, Queued, Edited, Done
    }
    public Int64 GeneratedFrameStamp { get; private set; } = 0;
    public SoloMesh Transparent = null;
    //MeshData Walls
    public SoloMesh Opaque = null;
    public ivec3 Pos = new ivec3(0, 0, 0);
    private World _world = null;
    public BarGrid BarGrid = null; // Empty globs can have no bars.
    public GlobState State = GlobState.Created;

    public vec3 OriginR3
    {
      get
      {
        vec3 r = new vec3(Pos.x * _world.Info.GlobWidthX, Pos.y * _world.Info.GlobWidthY, Pos.z * _world.Info.GlobWidthZ);
        return r;
      }
    }
    public Glob(World w, ivec3 pos, Int64 genframeStamp)
    {
      _world = w;
      Pos = pos;
      GeneratedFrameStamp = genframeStamp;
    }
    public void DestroyGlob()
    {
      _world = null;
    }
    public void DoLiterallyEverything()
    {


    }

  }
  public enum BlockMeshType
  {
    Block,
    Billboard,
    Liquid
  }
  public enum TileVis
  {
    Opaque,
    Transparent,
    Decal
  }

  public class WorldTile
  {
    //Provides the visible information for a block. Images. Mesh type. Visibility.
    public const float BlockOpacity_Solid = 1.0f;
    public const float BlockOpacity_Billboard = 0.5f;
    public const float BlockOpacity_Liquid = 0.07f;
    public const float BlockOpacity_Transparent = 0.0f;

    private MtTex _mtTex = null;

    public ushort Code { get; private set; } = 0;
    public FileLoc Image { get; private set; }
    public TileVis Visibility { get; private set; }
    public vec2[] UV { get; private set; } = null;
    public bool Visible { get { return Opacity > 0 && Opacity < 1; } }
    public Minimax<int> GrowthHeight = new Minimax<int>(1, 1);
    public float MineTime_Pickaxe { get; private set; } = 4;
    public BlockMeshType MeshType { get; private set; } = BlockMeshType.Block;
    public WorldObject Entity { get; private set; } = null;
    public bool IsChainedPlant { get; private set; } = false;
    public float Opacity { get; private set; } = BlockOpacity_Transparent;
    public WorldTile(ushort code, FileLoc img, TileVis vis, float hardness_pickaxe, BlockMeshType meshType, float opacity)
    {
      Code = code;
      Image = img;
      Visibility = vis;
      MineTime_Pickaxe = hardness_pickaxe;
      MeshType = meshType;
      Opacity = opacity;
    }
    public void SetTex(MtTex tex)
    {
      _mtTex = tex;
      //BL BR TL TR
      //      texs[0] = new vec2(0, 1);
      //texs[1] = new vec2(1, 1);
      //texs[2] = new vec2(0, 0);
      //texs[3] = new vec2(1, 0);
      //For testing this is 4 values, but i think it's going to be 2 
      UV = new vec2[] {
        new vec2(_mtTex.uv0.x, _mtTex.uv1.y),
        _mtTex.uv1,
        _mtTex.uv0,
        new vec2(_mtTex.uv1.x, _mtTex.uv0.y)
       };
    }
  }
  public class EditState
  {
    public int EditView { get; set; } = 1;
    public const int c_MaxEditViews = 4;
  }

  public class WorldInfo
  {
    //Contains base metrics for creating a world, size, voxels .. 
    //Constants before, now drive the individual world areas.
    public string Name { get; private set; } = Gu.UnsetName;
    public FileLoc FileLoc { get; private set; } = null;

    public const float DropDestroyTime_Seconds = (60) * 3; // x minutes
    public const int MaxGlobsToGeneratePerFrame_Sync = 32;//number of glob copy operations per render side frame. This can slow down / speed up rendering.

    public float BlockSize { get; private set; } = 25.0f;
    public float HeightScale { get; private set; } = 0.25f; // Height of a block relative to BlockSize
    public float WallXFactor { get; private set; } = 0.1f; // Width of a wall / [0,1] = % of BlockSize
    public float WallYFactor { get; private set; } = 0.1f; // 
    public float WallZFactor { get; private set; } = 0.1f; // 

    //This will allow us to have variable width blocks, to make the topo less regular.
    public Func<WorldInfo, float, float> GlobWidthXFunction = (i, x) => { return i.BlockSizeX; };
    public Func<WorldInfo, float, float> GlobWidthZFunction = (i, z) => { return i.BlockSizeZ; };

    public float BlockSizeX { get; private set; } = 0;
    public float BlockSizeY { get; private set; } = 0; // BlockSizeX * HeightScale;
    public float BlockSizeZ { get; private set; } = 0; // BlockSizeX;
    public int GlobBlocksX { get; private set; } = 0;
    public int GlobBlocksYSnap { get; private set; } = 0; //No y, but we can have a Y Snap
    public int GlobBlocksZ { get; private set; } = 0;
    public float GlobWidthX { get; private set; } = 0;
    public float GlobWidthY { get; private set; } = 0;
    public float GlobWidthZ { get; private set; } = 0;

    public bool DeleteStartFresh = false;
    public int LimitYAxisGeneration = 0;//0 = off, >0 - limit globs generated along Y axis (faster generation)

    //Player
    public float PlayerHeight { get; private set; } = 2.0f;
    public float PlayerWidth { get; private set; } = 0.5f;
    public float PlayerDepth { get; private set; } = 0.1f;

    //Generation shell
    private int _currentShell = 1;
    private const int _maxShells = 4;//keep this < Min(DromeGlobs) to prevent generating more dromes
    public float GenRadiusShell { get { return GlobWidthX; } }
    public float DeleteMaxDistance { get { return (GenRadiusShell * (float)(_maxShells + 1)); } }//distance beyond which things are deleted, this must be greater than max gen distance to prevent ping pong loading
    public float GenerateDistance { get { return (GenRadiusShell * (float)_currentShell); } } //distance under which things are generated
    public float RenderDistance { get { return (GenRadiusShell) * _maxShells; /* (GlobWidthX * 16) * (GlobWidthX * 16); */ } }

    // public ushort QuantitizeHeight(float h, bool glob_relative = true)
    // {
    //   //return a short representing the input height, relative of the glob
    //   // [0,65535)
    //   ushort s = 0;
    //   if (!glob_relative)
    //   {
    //     h = HeightGlobalToHeightGlob(h);
    //   }

    //   Gu.Assert(h >= 0 && h < GlobWidthY);

    //   s = (ushort)((float)ushort.MaxValue * (h / GlobWidthY));
    //   return s;
    // }
    // public float UnquantitizeHeight(ushort h, bool glob_relative = true)
    // {

    // }
    public WorldInfo(string worldName, bool delete_world_start_fresh, int limit_y_axis = 0, float blockSize = 4.0f, float playerHeight = 2.0f, int globBlocksX = 16)
    {
      Name = worldName;
      LimitYAxisGeneration = limit_y_axis;
      DeleteStartFresh = delete_world_start_fresh;

      BlockSizeX = blockSize;

      PlayerHeight = playerHeight;

      GlobBlocksX = globBlocksX;

      Compute();
    }
    public void Compute()
    {
      float playerSizeChangeRatio = PlayerHeight / 2.0f;
      PlayerWidth = PlayerWidth * playerSizeChangeRatio;
      PlayerDepth = PlayerDepth * playerSizeChangeRatio;

      BlockSizeY = BlockSizeX * HeightScale;
      BlockSizeZ = BlockSizeX;
      //Relative to player who is 2 meters tlal

      GlobBlocksYSnap = (int)(BlockSizeX / BlockSizeY * GlobBlocksX);
      GlobBlocksZ = GlobBlocksX;

      GlobWidthX = GlobBlocksX * BlockSizeX;
      GlobWidthY = GlobWidthX;
      GlobWidthZ = GlobBlocksZ * BlockSizeZ;

      if (!MathUtils.IsPowerOfTwo(GlobBlocksX) || !MathUtils.IsPowerOfTwo(GlobBlocksYSnap) || !MathUtils.IsPowerOfTwo(GlobBlocksZ))
      {
        Gu.BRThrowException("Glob blocks x,y,z must be a power of 2.");
      }
    }

    //indexing stuff

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
    public ivec3 R3ToI3BlockLocal_Any(vec3 R3, float cont_w_x, float cont_w_y, float cont_w_z)
    {
      vec3 bpos = new vec3(
       R3toI3BlockComp(R3.x, cont_w_x, BlockSizeX),
       R3toI3BlockComp(R3.y, cont_w_y, BlockSizeY),
       R3toI3BlockComp(R3.z, cont_w_z, BlockSizeZ));

      return new ivec3((int)bpos.x, (int)bpos.y, (int)bpos.z);
    }
    public ivec3 R3toI3BlockLocal_Glob(vec3 R3)
    {
      ivec3 bpos = R3ToI3BlockLocal_Any(R3, GlobWidthX, GlobWidthY, GlobWidthZ);

      if (bpos.x < 0 || bpos.y < 0 || bpos.z < 0 || bpos.x >= GlobBlocksX || bpos.y >= GlobBlocksYSnap || bpos.z >= GlobBlocksZ)
      {
        Gu.DebugBreak();
      }
      return bpos;
    }
    public float HeightGlobalToHeightGlob(float h)
    {
      var ret = h % GlobWidthY;
      if (h < 0)
      {
        ret += GlobWidthY;
      }
      return ret;
    }
    public ivec3 R3GlobaltoI3Glob(vec3 R3)
    {
      //v3toi3Node
      ivec3 gpos = new ivec3(
         (int)Math.Floor(R3.x / GlobWidthX),
         (int)Math.Floor(R3.y / GlobWidthY),
         (int)Math.Floor(R3.z / GlobWidthZ));
      return gpos;
    }
    public vec3 GlobI3PosToGlobR3Pos(ivec3 i)
    {
      vec3 gpos = new vec3((float)i.x * GlobWidthX, (float)i.y * GlobWidthY, (float)i.z * GlobWidthZ);
      return gpos;
    }
    public Box3f GetGlobBoxGlobalI3(ivec3 pt)
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

  }

  public class World
  {
    #region Private:Constants

    private const long c_lngAbandon_DeleteTime_DromeNode_ms = 1000 * 5; // * X seconds
    private const long c_lngAbandon_DeleteTime_Drome_ms = 1000 * 10; // Dromes stay in memory longer than their nodes. We need the scalar field data more often. When they are fully generated they can be discarded.
    private const int c_intMaxInitialGenerationWaitTime_ms = 1000 * 15;
    private const string c_strSaveWorldVersion = "0.01";
    private const string c_strSaveWorldHeader = "WorldFilev" + c_strSaveWorldVersion;
    private const int c_intDromeFileVersion = 1;

    #endregion
    #region Public:Members

    public WorldInfo Info { get; private set; } = null;
    public WorldObject SceneRoot { get; private set; } = new WorldObject("Scene_Root");
    public int NumCulledObjects { get; private set; } = 0;
    public int NumGlobs { get { return _globs.Count; } }
    public int NumRenderGlobs { get { return _renderGlobs.Count; } }
    public int NumVisibleRenderGlobs { get { return _visibleRenderGlobs.Count; } }
    public WorldProps WorldProps { get { return _worldProps; } }


    public GameMode GameMode
    {
      get { return _eGameMode; }
      set
      {
        _eGameMode = value;
        if (_eGameMode == GameMode.Edit & EditState == null)
        {
          EditState = new EditState();//this may end up having a ton of data, only construct it if we need it
        }
      }
    }
    public EditState EditState { get; private set; } = null;

    #endregion
    #region Private:Members


    private long _lastShellIncrementTimer_ms = 0;
    private long _lastShellIncrementTimer_ms_Max = 500;
    private WorldObject dummy = new WorldObject("dummy_beginrender");
    private WorldObject _debugDrawLines = null;
    private WorldObject _debugDrawPoints = null;
    private DrawCall _visibleObsAll = new DrawCall();
    private DrawCall _visibleObsFirst_FW = new DrawCall();
    private DrawCall _visibleObsMid_FW = new DrawCall();
    private DrawCall _visibleObsLast_FW = new DrawCall();
    private DrawCall _visibleObsFirst_DF = new DrawCall();
    private DrawCall _visibleObsMid_DF = new DrawCall();
    private DrawCall _visibleObsLast_DF = new DrawCall();
    private Dictionary<ivec3, Glob> _globs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //All globs
    private Dictionary<ivec3, Glob> _renderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
    private Dictionary<ivec3, Glob> _visibleRenderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
    private Dictionary<string, WorldObject> _objects = new Dictionary<string, WorldObject>();//Flat list of all objects
    private Dictionary<ushort, WorldTile> _blockTiles = null;
    private MultiMap<float, Glob> _wipGlobs = new MultiMap<float, Glob>();
    private WorldProps _worldProps = null; //Environment props.
    private string _worldSavePath = "";
    private Material _worldMaterial_Op = null;
    private Material _worldMaterial_Tp = null;
    private MegaTex _worldMegatex = null;
    private Material _blockObjectMaterial = null;
    private double _autoSaveTimeoutSeconds = 2;
    private double _autoSaveTimeout = 0;
    private GameMode _eGameMode = GameMode.Edit;
    public WindowContext UpdateContext { get; private set; } = null;

    #endregion

    public World(WindowContext updateContext)
    {
      UpdateContext = updateContext;
    }
    public void Initialize(WorldInfo info)
    {
      Info = info;

      _worldProps = new WorldProps("WorldProps");

      GameMode = Gu.EngineConfig.StartInEditMode ? GameMode.Edit : GameMode.Play;

      //This would actually be incorrect world OBs should be instanced
      //Init draw array.
      // _visible_objects_ordered = new Dictionary<DrawOrder, List<WorldObject>>();
      // for (int do_i = 0; do_i < (int)DrawOrder.MaxDrawOrders; do_i++)
      // {
      //   _visible_objects_ordered.Add((DrawOrder)do_i, new List<WorldObject>());
      // }

      DefineWorldTiles();
      CreateMaterials();

      InitWorldDiskFile(info.DeleteStartFresh);

      _worldProps.EnvironmentMap = Gu.Resources.LoadTexture(new FileLoc("hilly_terrain_01_2k.hdr", FileStorage.Embedded), true, TexFilter.Nearest);
      _worldProps.DayNightCycle = new DayNightCycle();
      _worldProps.DayNightCycle.Update(0);

      //Gu.Log.Info("Building initail grid");
      //* BuildDromeGrid(Player.WorldMatrix.extractTranslation(), GenRadiusShell, true);
      //I'm assuming since this is cube voxesl we're going to do physics on the integer grid, we don't need triangle data then.
      //* WaitForAllDromesToGenerate();
      //* UpdateLiterallyEverything_Blockish(Camera); // This will generate the globs
      //* WaitForAllGlobsToGenerate();
    }
    public void View(RenderView rv)
    {
      foreach (var obj in this._objects)
      {
        obj.Value.View(rv);
      }
    }
    public void Update(double dt)
    {
      if (UpdateContext != Gu.Context)
      {
        Gu.Log.Error("Tried to call update twice between two windows. Update must be called once on a single window (or, we could put it on its own thread, unless we do end up with OpenGL stuff.)");
        Gu.DebugBreak();
      }

      UpdateObjects(dt);

      //UpdateLiterallyEverything_Blockish(cam);
      //LaunchGlobAndDromeQueues();
      AutoSaveWorld(dt);

      PickGLobs();
      //topo
      foreach (var g in _globs)
      {
        if (g.Value != null)
        {
          if (
            g.Value.State == Glob.GlobState.Edited
          || g.Value.State == Glob.GlobState.Created
          || g.Value.State == Glob.GlobState.Loaded
          || g.Value.State == Glob.GlobState.Queued)
          {
            if (g.Value.BarGrid != null)
            {
              TopologizeGlob(g.Value);
            }
          }
        }
      }

      _worldProps.DayNightCycle.Update(dt);
    }
    private void PickGLobs()
    {
      //TODO: picking code is copied from worldobject  we can share this..sloppy fn
      foreach (var g in _visibleRenderGlobs)
      {
        if (Gu.Context.Renderer.Picker.PickedObjectFrame != null)
        {
          //Picking is pixel perfect, so the first picked object is the exact object.
          //However objects may have children, and components which can also be picked, and may not be in the global list.
          //Obviously, a list of pickid->obj would be the fastest.
          break;
        }
        if (g.Value.Opaque != null)
        {
          if (g.Value.Opaque.PickId != Picker.c_iInvalidPickId)
          {
            var pixid = Gu.Context.Renderer.Picker.GetSelectedPixelId();
            if (pixid != 0)
            {
              if (pixid == g.Value.Opaque.PickId)
              {
                Gu.Context.Renderer.Picker.PickedObjectFrame = g.Value.Opaque;
              }
            }
          }
        }
        if (g.Value.Transparent != null)
        {
          if (g.Value.Transparent.PickId != Picker.c_iInvalidPickId)
          {
            var pixid = Gu.Context.Renderer.Picker.GetSelectedPixelId();
            if (pixid != 0)
            {
              if (pixid == g.Value.Transparent.PickId)
              {
                Gu.Context.Renderer.Picker.PickedObjectFrame = g.Value.Transparent;
              }
            }
          }
        }
      }
    }
    public void BuildAndCull(RenderView rv)
    {
      if (rv.Camera != null && rv.Camera.TryGetTarget(out var cm))
      {
        BuildGrid(cm.Position_World, Info.GenerateDistance);
        View(rv);
        CollectVisibleGlobs(cm);
        CollectVisibleObjects(rv, cm);
      }
    }
    private void TopologizeGlob(Glob g)
    {
      //We should figure out how ot make this more regular,
      //or, split the guy up, to avoid remakign this with each edit

      Gu.Assert(g.BarGrid != null);

      v_v3n3x2t3u1[]? verts = null;

      g.BarGrid.Iterate((grid, x, z) =>
      {
        var bl = grid.Get(x, z);
        if (bl != null)
        {
          foreach (var b in bl)
          {
            vec2[] top = null, side = null, bot = null;
            WorldTile? wt = null;
            if (_blockTiles.TryGetValue(b.Tiles[(int)BeamFaceIndex.Top], out wt))
            {
              top = wt.UV;
            }
            if (_blockTiles.TryGetValue(b.Tiles[(int)BeamFaceIndex.Right], out wt))
            {
              side = wt.UV;
            }
            if (_blockTiles.TryGetValue(b.Tiles[(int)BeamFaceIndex.Right], out wt))
            {
              bot = wt.UV;
            }

            var h = b.TopY - b.BotY;
            var p = new vec3(x * Info.BlockSizeX, b.BotY, z * Info.BlockSizeZ);
            MeshData.GenBoxVerts(ref verts, Info.BlockSizeX, h, Info.BlockSizeZ, top, side, bot, p, true);

          }
        }

        return LambdaBool.Continue;
      });

      if (verts != null && verts.Length > 0)
      {
        ushort[] inds = MeshData.GenerateQuadIndices(verts.Length / 4, false);

        string name = "glob:" + g.Pos.ToString();
        if (g.Opaque == null)
        {
          var pickid = Gu.Context.Renderer.Picker.GenPickId();
          var mat = _worldMaterial_Op;
          mat4 mworld = mat4.getTranslation(Info.GlobI3PosToGlobR3Pos(g.Pos));
          g.Opaque = new SoloMesh(null, mat, mworld, pickid);
        }

        var faces = MeshData.ComputeNormalsAndTangents(verts, inds.AsUIntArray(), true, true);

        g.Opaque.Mesh = new MeshData(name, PrimitiveType.Triangles,
          Gpu.CreateVertexBuffer(name, verts),
          Gpu.CreateIndexBuffer(name, inds.ToArray()),
          Gpu.CreateShaderStorageBuffer(name, faces.ToArray()),
          true
        );

        _renderGlobs.Add(g.Pos, g);
      }
      else
      {
        _renderGlobs.Remove(g.Pos);
      }

      g.State = Glob.GlobState.Done;
    }

    #region Objects

    public WorldObject FindObject(string name)
    {
      WorldObject obj = null;
      _objects.TryGetValue(name, out obj);
      return obj;
    }
    public Camera3D CreateCamera(string name, RenderView rv, vec3 pos)
    {
      Camera3D c = new Camera3D(name, rv);
      c.Position_Local = pos;
      Box3f dummy = Box3f.Zero;
      c.Update(this, 0, ref dummy);
      return c;
    }
    public WorldObject CreateObject(string name, MeshData mesh, Material material, vec3 pos = default(vec3))
    {
      WorldObject ob = new WorldObject(name);
      ob.Name = name;
      ob.Position_Local = pos;
      ob.Mesh = mesh;
      ob.Material = material;
      return ob;
    }
    public WorldObject CreateAndAddObject(string name, MeshData mesh, Material material, vec3 pos = default(vec3))
    {
      return AddObject(CreateObject(name, mesh, material, pos));
    }
    public void DestroyObject(WorldObject wo)
    {
      wo.Unlink();
      wo.OnDestroyed?.Invoke(wo);
      wo.IterateComponentsSafe((cmp) =>
      {
        cmp.OnDestroy(wo);
        return LambdaBool.Continue;
      });
    }
    public void DestroyObject(string name)
    {
      //To destroy you should call the WorldObject's Destroy method
      if (_objects.TryGetValue(name, out WorldObject wo))
      {
        DestroyObject(wo);
        _objects.Remove(name);
      }
      else
      {
        Gu.Log.Error("Object '" + name + "' was not found.");
      }
    }
    public WorldObject AddObject(WorldObject ob)
    {
      if (ob == null)
      {
        Gu.Log.Error("Object was null adding to world.");
        return null;
      }
      //TODO: optimize (hash map or something)
      foreach (var ob2 in _objects.Values)
      {
        if (ob == ob2)
        {
          Gu.Log.Error("Tried to add Object " + ob.Name + " twice. Clone instance if duplicate needed.");
          Gu.DebugBreak();
          return ob;
        }
      }
      //Use a suffix if there is a duplicate object
      int suffix = 0;
      string name_suffix = ob.Name;
      while (FindObject(name_suffix) != null)
      {
        suffix++;
        name_suffix = ob.Name + "-" + suffix.ToString();
      }
      ob.Name = name_suffix;
      _objects.Add(name_suffix, ob);
      SceneRoot.AddChild(ob);

      ob.OnAddedToScene?.Invoke(ob);
      ob.State = WorldObjectState.Active;

      return ob;
    }
    private void UpdateObjects(double dt)
    {
      Box3f dummy = Box3f.Zero;
      dummy.genResetLimits();
      List<string> toRemove = new List<string>();
      foreach (var kvp in _objects)
      {
        var ob = kvp.Value;
        if (ob.State != WorldObjectState.Destroyed)
        {
          //We could drop physics here if we wanted to
          ob.Update(this, dt, ref dummy);

          //This could be a component
          if (ob.HasPhysics)
          {
            UpdateObjectPhysics(ob, (float)dt);
          }
          if (Gu.Context.Renderer.Picker.PickedObjectFrame == null)
          {
            ob.Pick();
          }
        }
        else
        {
          toRemove.Add(kvp.Key);
        }

      }
      foreach (var x in toRemove)
      {
        DestroyObject(x);
      }
      toRemove.Clear();
    }
    private float Gravity = -9.8f * 0.5f; //m/s
    private const float MaxVelocity_Second_Frame = 5 * 2.32f;//max length of vel per second / frame *NOT PER FRAME but by dt*
    private const float MinVelocity = 0.000001f;
    private const float MinVelocity2 = (MinVelocity * MinVelocity);
    private const float MinTimeStep = 0.00001f;
    private void UpdateObjectPhysics(WorldObject ob, float dt)
    {
      if (dt < MinTimeStep)
      {
        dt = MinTimeStep;
      }
      //Assuming we're going to modify object resting state when other objects change state
      float vlen2 = (ob.Velocity * (float)dt).length2();
      if (ob.OnGround && vlen2 > 0)
      {
        ob.OnGround = false;
      }
      if (ob.OnGround)
      {
        return;
      }

      float maxv = MaxVelocity_Second_Frame * dt;
      float maxv2 = (float)Math.Pow(maxv, 2.0f);

      vec3 dbg_initial_v = ob.Velocity;

      //Our final guys in frame time units
      vec3 final_p = ob.Position_Local;
      vec3 final_v = ob.Velocity * dt;

      //Too big
      vlen2 = (final_v * dt).length2();
      if (vlen2 > maxv2)
      {
        final_v = final_v.normalized() * maxv;
      }

      vec3 g_v = vec3.Zero;
      if (ob.HasGravity)
      {
        g_v = new vec3(0, Gravity * dt, 0);
      }

      if (ob.Collides)
      {
        CollideOb(ob, dt, ref final_v, ref final_p, maxv, maxv2, false);
        if (ob.OnGround == false && ob.HasGravity)
        {
          CollideOb(ob, dt, ref g_v, ref final_p, maxv, maxv2, true);
        }
      }
      else
      {
        final_p += final_v + g_v;
      }

      //Dampen world velocity (not frame velocity)
      if (!ob.OnGround && ob.AirFriction > 0.0f)
      {
        float len = final_v.length();
        float newlen = len - len * ob.AirFriction * dt;
        if (newlen <= 0)
        {
          newlen = 0;
        }
        final_v = final_v.normalized() * newlen;
      }

      //Add frame v to p

      ob.Position_Local = final_p;
      //transform v back into world time units instead of frame time units
      ob.Velocity = (final_v + g_v) * (1.0f / (dt == 0 ? 1.0f : dt));

    }
    private void CollideOb(WorldObject ob, float dt, ref vec3 final_v, ref vec3 final_p, float maxvdt, float maxvdt2, bool gravity)
    {
      int max_sim_steps = 32;

      vec3 dbg_initial_v = final_v;
      vec3 dbg_initial_p = final_p;

      final_v = dbg_initial_v;
      final_p = dbg_initial_p;

      bool hit_n_set = false;

      for (int istep = 0; istep < max_sim_steps; istep++)
      {
        if (istep == max_sim_steps - 1)
        {
          int n = 0;
          n++;
        }

        PickRay3D vray = new PickRay3D(final_p, final_v, vec3.Zero);
        vray.IsPlayer = true;
        PickedTri b = RaycastTris(vray);
        if (b.IsHit)
        {
          //we could use the normal function to return the correct point / line too

          vec3 plane_n = b.HitNormal;

          if (gravity && plane_n.dot(new vec3(0, 1, 0)) > 0)
          {
            ob.OnGround = true;
          }

          if (b.RaycastResult == RaycastResult.Inside)
          {
            //use negative t, Back out of ray
            //We will need to add negative t to ray intersect
            //Use Last good position
            int n = 0;
            n++;
          }
          if (vray.Dir.dot(plane_n) <= 0)
          {
            //Move just to the plane
            float cp_len = vray.Length * (b._t1);
            vec3 v_n = final_v.normalized();

            //move away from the plane so we are not intersecting. We do this so that the next raycast won't be inside *this* plane.
            float margin = 0.0001f;
            float margin_adjust = 0;
            float v_dot_n = plane_n.dot(-v_n);//cos theta
            if (v_dot_n != 0) //>0 - so we only move in toward the plane (-v_n)
            {
              margin_adjust = margin / v_dot_n;
            }

            float move_amt = cp_len - margin_adjust;

            vec3 v_cp = v_n * move_amt;
            //Move to where we hit
            vec3 c_p = final_p + v_cp; //next pos

            vec3 v_slide;
            if (move_amt < 0)
            {
              //if vlen < margin - then we ae inside the volume and we need to push out.
              //we are nside margin
              int n = 0;
              n++;
              v_slide = vec3.Zero;
            }
            else
            {
              float friction = 0.77f;// MathUtils.Clamp((-v_n).dot(plane_n) + 0.1f, 0, 1);// World.BlockSizeX * 3 * dt;

              //Use remaining energy to slide
              float remaining_energy = Math.Max(vray.Length * (1.0f - b._t1) * friction, 0);
              vec3 incident = (c_p - final_p).normalized();
              vec3 radiant = incident.reflect(plane_n);
              vec3 v_e_ref = (radiant - incident).normalized();
              v_slide = v_e_ref * remaining_energy;
            }


            final_v = v_slide;
            final_p = c_p;

          }
          else
          {
            int n = 0;
            n++;
          }

          //Zero out small v so we don't simulate forever
          if (final_v.length2() < MinVelocity2)
          {
            final_v = vec3.Zero;
          }
          if (final_v.length2() > maxvdt2)
          {
            final_v = final_v.normalized() * maxvdt;
          }


          ////No bouncing. -- todo
          //if (plane_n.y > 0)
          //{
          //  //ground
          //  ob.OnGround = true;
          //}
        }
        else
        {
          //no hit, break out
          if (final_v.length2() < MinVelocity2)
          {
            final_v = vec3.Zero;
          }
          if ((final_v * dt).length2() > maxvdt2)
          {
            final_v = final_v.normalized() * maxvdt;
          }

          if (gravity && istep == 0)
          {
            ob.OnGround = false;
          }

          //Final add, the velocity does not collide.
          final_p += final_v;

          break;
        }
      }
    }
    private PickedTri RaycastTris(PickRay3D pr)
    {
      PickedTri ret = new PickedTri();

      //TODO:
      Gu.BRThrowNotImplementedException();
      ret._t1 = ret._t2 = 9999;
      return ret;
    }
    private void CollectVisibleObjects(RenderView rv, Camera3D cm)
    {
      //TODO: objects will coincide with collected globs.
      // foreach (var layer in _visible_objects_ordered)
      // {
      //   layer.Value.Clear();
      // }
      _visibleObsFirst_FW.BeginCollectVisibleObjects();
      _visibleObsMid_FW.BeginCollectVisibleObjects();
      _visibleObsLast_FW.BeginCollectVisibleObjects();
      _visibleObsFirst_DF.BeginCollectVisibleObjects();
      _visibleObsMid_DF.BeginCollectVisibleObjects();
      _visibleObsLast_DF.BeginCollectVisibleObjects();
      _visibleObsAll.BeginCollectVisibleObjects();

      NumCulledObjects = 0;

      _worldProps.Reset();

      CollectObjects(rv, cm, SceneRoot);

      //Collect globs
      foreach (var kp in this._visibleRenderGlobs)
      {
        var g = kp.Value;
        if (g.Opaque != null)
        {
          _visibleObsMid_DF.AddVisibleObject(g.Opaque);
          _visibleObsAll.AddVisibleObject(g.Opaque);
        }
        if (g.Transparent != null)
        {
          _visibleObsMid_DF.AddVisibleObject(g.Transparent);
          _visibleObsAll.AddVisibleObject(g.Transparent);
        }
      }

    }
    private void CollectVisibleGlobs(Camera3D cam)
    {
      _visibleRenderGlobs.Clear();
      foreach (var g in _renderGlobs)
      {
        //i think ir emoved glob box due to too much data. probably isn't necessary to do that with new system.
        var b = Info.GetGlobBoxGlobalI3(g.Key);
        if (cam.Frustum.HasBox(b))
        {
          _visibleRenderGlobs.Add(g.Key, g.Value);
        }
      }
    }
    private void AutoSaveWorld(double dt)
    {
      _autoSaveTimeout += dt;
      if (_autoSaveTimeout > _autoSaveTimeoutSeconds)
      {
        _autoSaveTimeout = 0;
        SaveWorld();
      }
    }

    #endregion

    #region Index

    #endregion

    #region Rendering

    public void RenderDeferred(double Delta, RenderView rv)
    {
      _visibleObsFirst_DF.Draw(_worldProps, rv);
      _visibleObsMid_DF.Draw(_worldProps, rv);
      _visibleObsLast_DF.Draw(_worldProps, rv);

    }
    public void RenderForward(double Delta, RenderView rv)
    {
      _visibleObsFirst_FW.Draw(_worldProps, rv);
      _visibleObsMid_FW.Draw(_worldProps, rv);
      _visibleObsLast_FW.Draw(_worldProps, rv);
    }
    public void RenderDebugForward(double Delta, RenderView rv)
    {
      var frame = Gu.Context.FrameStamp;

      //Debug object attribs
      if (_visibleObsAll != null)
      {
        //Bound box
        if (Gu.Context.DebugDraw.DrawBoundBoxes)
        {
          vec4 bbcolor = new vec4(1, 0, 0, 1);
          foreach (var obm in _visibleObsAll.VisibleObjects.Objects)
          {
            foreach (var obi in obm.Value)
            {
              foreach (var ob in obi.Value.Objects)
              {
                Gu.Assert(ob != null);
                Gu.Assert(ob.Material != null);
                if (ob is WorldObject)
                {
                  Gu.Context.DebugDraw.Box((ob as WorldObject).BoundBoxMeshTransform, ob.Material.BaseColor);
                  Gu.Context.DebugDraw.Box((ob as WorldObject).BoundBox, bbcolor);
                }
              }
            }
          }
        }

        //Normals
        if (Gu.Context.DebugDraw.DrawVertexNormals || Gu.Context.DebugDraw.DrawFaceNormals)
        {
          _visibleObsAll.Draw(_worldProps, rv, Material.DebugDraw_VertexNormals_FlatColor);
        }
      }
      // Debug helpers
      if (Gu.Context.DebugDraw.LinePoints.Count > 0)
      {
        GL.LineWidth(1.5f);
        Gpu.CheckGpuErrorsDbg();
        if (_debugDrawLines == null)
        {
          _debugDrawLines = CreateObject("debug_lines", null, new Material("debugLines", Gu.Resources.LoadShader("v_v3c4_debugdraw", false, FileStorage.Embedded)));
        }
        _debugDrawLines.Mesh = new MeshData("debug_lines", PrimitiveType.Lines,
          Gpu.CreateVertexBuffer("debug_lines", Gu.Context.DebugDraw.LinePoints.ToArray()),
          Gpu.CreateIndexBuffer("debug_lines", Gu.Context.DebugDraw.LineInds.ToArray()),
          null,
          false
          );
        DrawCall.Draw(_worldProps, rv, _debugDrawLines);
      }
      if (Gu.Context.DebugDraw.Points.Count > 0)
      {
        GL.PointSize(5);
        Gpu.CheckGpuErrorsDbg();
        if (_debugDrawPoints == null)
        {
          _debugDrawPoints = CreateObject("debug_points", null, new Material("debugPoints", Gu.Resources.LoadShader("v_v3c4_debugdraw", false, FileStorage.Embedded)));
        }
        _debugDrawPoints.Mesh = new MeshData("debug_points", PrimitiveType.Points,
          Gpu.CreateVertexBuffer("debug_points", Gu.Context.DebugDraw.Points.ToArray()),
          null,
          false
          );
        DrawCall.Draw(_worldProps, rv, _debugDrawPoints);
      }

    }
    private void CollectObjects(RenderView rv, Camera3D cam, WorldObject ob)
    {
      Gu.Assert(ob != null);

      if (ob.ExcludeFromRenderView != null && ob.ExcludeFromRenderView.TryGetTarget(out var obrv))
      {
        if (obrv == rv)
        {
          return;
        }
      }

      if (ob.Mesh != null)
      {
        if (cam.Frustum.HasBox(ob.BoundBox))
        {
          if (ob is Light)
          {
            this._worldProps.Lights.Add(ob as Light);
          }

          if (ob.Mesh.DrawMode == DrawMode.Deferred)
          {
            //light objects CAn have meshes too. Cameras too.
            if (ob.Mesh.DrawOrder == DrawOrder.First)
            {
              _visibleObsFirst_DF.AddVisibleObject(ob);
            }
            else if (ob.Mesh.DrawOrder == DrawOrder.Mid)
            {
              _visibleObsMid_DF.AddVisibleObject(ob);
            }
            else if (ob.Mesh.DrawOrder == DrawOrder.Last)
            {
              _visibleObsLast_DF.AddVisibleObject(ob);
            }
          }
          else if (ob.Mesh.DrawMode == DrawMode.Forward)
          {
            if (ob.Mesh.DrawOrder == DrawOrder.First)
            {
              _visibleObsFirst_FW.AddVisibleObject(ob);
            }
            else if (ob.Mesh.DrawOrder == DrawOrder.Mid)
            {
              _visibleObsMid_FW.AddVisibleObject(ob);
            }
            else if (ob.Mesh.DrawOrder == DrawOrder.Last)
            {
              _visibleObsLast_FW.AddVisibleObject(ob);
            }
          }
          _visibleObsAll.AddVisibleObject(ob);
        }
        else
        {
          NumCulledObjects++;
        }
      }

      foreach (var c in ob.Children)
      {
        CollectObjects(rv, cam, c);
      }
    }
    private WorldTile AddWorldTile(ushort code, TileImage img, TileVis vis, float hardness, BlockMeshType meshType, float opacity)
    {
      if (_blockTiles == null)
      {
        _blockTiles = new Dictionary<ushort, WorldTile>();
      }

      var bt = new WorldTile(code, GetTileFile(img), vis, hardness, meshType, opacity);
      _blockTiles.Add(code, bt);
      return bt;
    }
    private void CreateMaterials()
    {
      var maps = CreateAtlas();
      var s = Gu.Resources.LoadShader("v_Glob", false, FileStorage.Embedded);
      _worldMaterial_Op = new Material("worldMaterial_Op", s, maps.AlbedoTexture, maps.NormalTexture);
      _worldMaterial_Tp = new Material("worldMaterial_Tp", s, maps.AlbedoTexture, maps.NormalTexture);
      _worldMaterial_Tp.GpuRenderState.Blend = true;
      _worldMaterial_Tp.GpuRenderState.DepthTest = true;
      _worldMaterial_Tp.GpuRenderState.CullFace = false;

      //Block Material
      _blockObjectMaterial = new Material("BlockObject", Gu.Resources.LoadShader("v_v3n3x2_BlockObject_Instanced", false, FileStorage.Embedded));
    }
    private void DefineWorldTiles()
    {
      //_blockTiles - Manual array that specifies which tiles go on the top, side, bottom
      //The tiles are specified by FileLoc structure which must be a class type.
      //This is used to index into the megatex to find the generated UV coordinates.

      //solid blocks
      AddWorldTile(BlockItemCode.Grass, TileImage.Grass, TileVis.Opaque, HardnessValue.Dirt, BlockMeshType.Block, WorldTile.BlockOpacity_Solid);
      AddWorldTile(BlockItemCode.Dirt, TileImage.Dirt, TileVis.Opaque, HardnessValue.Dirt, BlockMeshType.Block, WorldTile.BlockOpacity_Solid);
    }
    private PBRTextureArray CreateAtlas()
    {
      //Create the atlas.
      //Must be called after context is set.
      _worldMegatex = new MegaTex("world-megatex", true, MegaTex.MtClearColor.DebugRainbow, true, TexFilter.Nearest, true);

      foreach (var resource in WorldStaticData.TileImages)
      {
        MtFile mf = _worldMegatex.AddResource(resource.Value);
      }

      var cmp = _worldMegatex.Compile();

      //From the generated image, set the WorldTile UV coordinates to the generated UV coords.
      cmp.AlbedoTexture.SetFilter(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear);

      foreach (var imgfile in _worldMegatex.Files)
      {
        if (imgfile.Texs.Count > 0)
        {
          MtTex mtt = imgfile.Texs[0];
          if (_blockTiles != null)
          {
            foreach (var block in _blockTiles)
            {
              if (block.Value.Image.RawPath == imgfile.FileLoc.RawPath)
              {
                block.Value.SetTex(mtt);
              }
            }
          }
        }
        else
        {
          Gu.Log.Warn($"Megatex resource {imgfile.FileLoc.RawPath} generated no textures.");
          Gu.DebugBreak();
        }
      }
      foreach (var block in _blockTiles)
      {
        if (block.Value.UV == null)
        {
          Gu.Log.Warn($"block resource {block.Value.Code}was not found in mega tex.");
          Gu.DebugBreak();
        }
      }
      return cmp;
    }
    #endregion

    #region World Edit 

    public void SetGlob(ivec3 gpos, Glob? g)
    {
      //This will not be a problem, this i just for debugging
      bool bhas = _globs.TryGetValue(gpos, out var gexist);
      if (g != null && gexist != null)
      {
        Gu.Log.Warn("Tried to overwrite an existing glob with another glob");
        Gu.DebugBreak();
      }

      //Note: G can be null, in which case, there is no glob yet, we don't generate them anymore
      _globs[gpos] = g;
    }

    public void CreateEntity(vec3 pos, vec3 vel, WorldTile tile)
    {
      var new_ent = tile.Entity.Clone();
      new_ent.Position_Local = pos;
      new_ent.Velocity = vel;
      Gu.World.AddObject(new_ent);
    }

    #endregion
    #region Private: Globs

    private void BuildGrid(vec3 origin, float awareness_radius, bool logprogress = false)
    {
      List<Glob> newGlobs = new List<Glob>();

      Box3f awareness = new Box3f(origin - awareness_radius, origin + awareness_radius);

      Box3i ibox_glob;
      ibox_glob._min = new ivec3(
        (int)Math.Floor(awareness._min.x / Info.GlobWidthX),
        (int)Math.Floor(awareness._min.y / Info.GlobWidthY),
        (int)Math.Floor(awareness._min.z / Info.GlobWidthZ));
      ibox_glob._max = new ivec3(
        (int)Math.Ceiling(awareness._max.x / Info.GlobWidthX),
        (int)Math.Ceiling(awareness._max.y / Info.GlobWidthY),
        (int)Math.Ceiling(awareness._max.z / Info.GlobWidthZ));

      //Limit Y axis ..  Tehnically we need maybe 2-4  up and down
      if (Info.LimitYAxisGeneration > 0)
      {
        int ylimit = Info.LimitYAxisGeneration;
        if (ibox_glob._min.y > ylimit) { ibox_glob._min.y = ylimit; }
        if (ibox_glob._min.y < -ylimit) { ibox_glob._min.y = -ylimit; }
        if (ibox_glob._max.y > ylimit) { ibox_glob._max.y = ylimit; }
        if (ibox_glob._max.y < -ylimit) { ibox_glob._max.y = -ylimit; }
        if (ibox_glob._min.y > ibox_glob._max.y) { ibox_glob._min.y = ibox_glob._max.y; }
      }

      int dbg_current_glob = 0;
      ibox_glob.iterate((x, y, z, count) =>
      {
        dbg_current_glob++;
        if (logprogress)
        {
          Gu.Log.Info("Glob " + dbg_current_glob + "/" + count);
        }

        ivec3 gpos = new ivec3(x, y, z);
        Glob g;

        if (Glob_Can_Generate_Distance(origin, Info.GetGlobBoxGlobalI3(gpos)))
        {
          if (!_globs.TryGetValue(gpos, out g))
          {
            g = LoadGlobOrSetEmpty(gpos);
            //Note: g can be null, null globs mean that it doesn't exist.
          }
        }

        return LambdaBool.Continue;
      });
    }
    private bool Glob_Can_Generate_Distance(vec3 pos, Box3f drome_box)
    {
      return Box_IsWithin_Distance(pos, drome_box, Info.GenerateDistance);
    }
    private bool Glob_Can_Delete_Distance(vec3 pos, Box3f drome_box)
    {
      return Box_IsWithin_Distance(pos, drome_box, Info.GenerateDistance);
    }
    private bool Box_IsWithin_Distance(vec3 pos, Box3f box, float genDistance)
    {
      //return true if the box's dist to camera is less than gen distnace
      float dist_cam2 = box.DistanceToCam2(pos);

      if (dist_cam2 < (genDistance * genDistance))
      {
        return true;
      }
      return false;
    }
    private string GetGlobFileName(ivec3 gpos)
    {
      //[8][8][8]
      Gu.Assert(System.IO.Directory.Exists(_worldSavePath));
      string sx = (gpos.x < 0 ? "" : "+") + gpos.x.ToString("D8");
      string sy = (gpos.y < 0 ? "" : "+") + gpos.y.ToString("D8");
      string sz = (gpos.z < 0 ? "" : "+") + gpos.z.ToString("D8");
      return System.IO.Path.Combine(_worldSavePath, sx + sy + sz + ".drome");
    }
    private string GetWorldFileName()
    {
      Gu.Assert(System.IO.Directory.Exists(_worldSavePath));
      string worldfile = Info.Name + ".world";
      return System.IO.Path.Combine(_worldSavePath, worldfile);
    }
    private Glob LoadGlobOrSetEmpty(ivec3 gpos)
    {
      Glob? g = TryLoadGlob(gpos);
      if (g != null)
      {
        g.State = Glob.GlobState.Loaded;
      }
      SetGlob(gpos, g);

      return g;
    }

    #endregion
    #region Private: Files

    private FileLoc GetTileFile(TileImage img)
    {
      WorldStaticData.TileImages.TryGetValue(img, out var loc);
      Gu.Assert(loc != null);
      return loc;
    }
    private void InitWorldDiskFile(bool delete_world_start_fresh)
    {
      _worldSavePath = System.IO.Path.Combine(Gu.SavePath, Info.Name);

      if (delete_world_start_fresh)
      {
        if (System.IO.Directory.Exists(_worldSavePath))
        {
          Gu.Log.Info("Starting Fresh - Deleting " + _worldSavePath);
          Directory.Delete(_worldSavePath, true);
        }
      }

      // This is the WORLD save file. Player position and stuff
      // This isn't the blocks data
      Gu.Log.Info("Creating world save directory " + _worldSavePath);
      if (!System.IO.Directory.Exists(_worldSavePath))
      {
        System.IO.Directory.CreateDirectory(_worldSavePath);
      }
      if (!TryLoadWorld())
      {
        SaveWorld();
      }
    }
    public void SaveWorld()
    {
      //Serilize all objects .. connect players to window views

      //We can call this if the player moves or something.
      // if (Player == null)
      // {
      //   Gu.BRThrowException("Player must not be null when creating world.");
      // }

      // string worldfn = GetWorldFileName();
      // var enc = Encoding.GetEncoding("iso-8859-1");
      // using (var fs = System.IO.File.OpenWrite(worldfn))
      // using (var br = new System.IO.BinaryWriter(fs, enc))
      // {
      //   br.Write((string)c_strSaveWorldHeader);
      //   br.Write(Player.Position_Local);
      //   br.Write(Player.Rotation_Local);
      // }
    }
    private bool TryLoadWorld()
    {
      return false;
      //Deserilize all objects .. connect players to window views

      // if (Player == null)
      // {
      //   Gu.BRThrowException("Player must not be null when creating world.");
      // }

      // string worldfn = GetWorldFileName();
      // var enc = Encoding.GetEncoding("iso-8859-1");
      // if (!System.IO.File.Exists(worldfn))
      // {
      //   return false;
      // }

      // using (var fs = System.IO.File.OpenRead(worldfn))
      // using (var br = new System.IO.BinaryReader(fs, enc))
      // {
      //   string h = br.ReadString();
      //   if (h != c_strSaveWorldHeader)
      //   {
      //     Gu.BRThrowException("World header '" + h + "' does not match current header version '" + c_strSaveWorldHeader + "'");
      //   }
      //   Player.Position_Local = br.ReadVec3();
      //   Player.Rotation_Local = br.ReadQuat();
      // }
      // return true;
    }
    private string DromeFooter = "EndOfDrome";
    private void SaveGlob(Glob g)
    {
      string globfn = GetGlobFileName(g.Pos);
      var enc = Encoding.GetEncoding("iso-8859-1");
      bool append = false;
      if (System.IO.File.Exists(globfn))
      {
        append = true;
      }

      try
      {
        //     using (var fs = System.IO.File.OpenWrite(globfn))
        //     using (var br = new System.IO.BinaryWriter(fs, enc))
        //     {
        //       br.Write((Int32)DromeFileVersion);
        //       br.Write((Int32)d.Pos.x);
        //       br.Write((Int32)d.Pos.y);
        //       br.Write((Int32)d.Pos.z);
        //       d.BlockStats.Serialize(br);
        //       d.Blocks.Serialize(br);

        //       if (d.GlobRegionStates == null)
        //       {
        //         br.Write((Int32)0);
        //       }
        //       else
        //       {
        //         var byteArr = new byte[Marshal.SizeOf(typeof(RegionBlocks)) * d.GlobRegionStates.Length];
        //         var pinnedHandle = GCHandle.Alloc(d.GlobRegionStates, GCHandleType.Pinned);
        //         Marshal.Copy(pinnedHandle.AddrOfPinnedObject(), byteArr, 0, byteArr.Length);
        //         pinnedHandle.Free();
        //         byte[] compressed = Gu.Compress(byteArr);
        //         br.Write((Int32)compressed.Length);
        //         br.Write(compressed);
        //       }

        //       br.Write(DromeFooter);
        //       br.Close();
        //     }

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
    private Glob TryLoadGlob(ivec3 dpos)
    {
      //Return null if no glob file was found.
      string fn = GetGlobFileName(dpos);
      Glob g = null;
      try
      {
        if (File.Exists(fn))
        {
          //       d = new Drome(this, new ivec3(0, 0, 0), Gu.Context.FrameStamp);

          //       var enc = Encoding.GetEncoding("iso-8859-1");

          //       using (var fs = File.OpenRead(dromefn))
          //       using (var br = new System.IO.BinaryReader(fs, enc))
          //       {
          //         Int32 version = br.ReadInt32();
          //         if (version != DromeFileVersion)
          //         {
          //           Gu.BRThrowException("Glob file verison '" + version + "' does not match required version '" + DromeFileVersion + "'.");
          //         }

          //         //d.DensityState = (Glob.GlobDensityState)br.ReadInt32();
          //         d.Pos.x = br.ReadInt32();
          //         d.Pos.y = br.ReadInt32();
          //         d.Pos.z = br.ReadInt32();
          //         d.BlockStats.Deserialize(br);

          //         d.AllocateBlocks();
          //         d.Blocks.Deserialize(br);

          //         int compressed_count = br.ReadInt32();
          //         if (compressed_count == 0)
          //         {
          //           d.GlobRegionStates = null;
          //         }
          //         else
          //         {
          //           var compressed = br.ReadBytes(compressed_count);

          //           byte[] decompressed = Gu.Decompress(compressed);
          //           var numStructs = decompressed.Length / Marshal.SizeOf(typeof(RegionBlocks));

          //           Gu.Assert(numStructs == Drome.DromeRegionStateCount);

          //           d.GlobRegionStates = new RegionBlocks[numStructs];
          //           var pinnedHandle = GCHandle.Alloc(d.GlobRegionStates, GCHandleType.Pinned);
          //           Marshal.Copy(decompressed, 0, pinnedHandle.AddrOfPinnedObject(), decompressed.Length);
          //           pinnedHandle.Free();
          //         }

          //         string footer = br.ReadString();
          //         if (!footer.Equals(DromeFooter))
          //         {
          //           Gu.BRThrowException("Error: Invalid drome file. Incorrect footer.");
          //         }

          //         br.Close();
          //       }
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Glob " + fn + " had an error loading. ", ex);
        return null;
      }
      return g;
    }

    #endregion


  }//World


  public class WorldLoader
  {
    //Store all the world files in one place, plus global settings.
    private string c_strWorldFileName = "Worlds.dat";//If multple worlds.. will change

    private WindowContext _updateContext = null;
    private FileLoc _worldFileLoc = null;

    private List<string> _worldNames = new List<string>();

    public WorldLoader(WindowContext update)
    {
      _updateContext = update;
      _worldFileLoc = new FileLoc(System.IO.Path.Combine(Gu.SavePath, c_strWorldFileName), FileStorage.Disk);
      if (!LoadWorldsFile(_worldFileLoc))
      {
        SaveWorldsFile(_worldFileLoc);
      }
    }
    public void GoToArea(string areaName)
    {
      //Destroy World.
      //LoadWorldFile
      // WorldInfo wi = new WorldInfo();
      //CurWorld = new WorldArea(inf);
    }
    public World CreateNewWorld(WorldInfo wi)
    {
      var w = Gu.World = new World(_updateContext);
      w.Initialize(wi);
      return w;
    }
    public void CreateFlatArea()
    {
      Gu.Assert(Gu.World != null);
      Box3i b = new Box3i(new ivec3(-1, 0, -1), new ivec3(1, 1, 1));
      b.iterate((x, y, z, dbgcount) =>
      {
        var g = new Glob(Gu.World, new ivec3(x, y, z), Gu.Context.FrameStamp);
        g.BarGrid = new BarGrid(Gu.World.Info);
        g.BarGrid.Edit_GenFlat(0, Gu.World.Info.BlockSizeY);
        Gu.World.SetGlob(g.Pos, g);
        return LambdaBool.Continue;
      }, false);
    }
    public void CreateHillyArea()
    {
      //TRODO:
      // Gu.Assert(Gu.World != null);
      // Box3i b = new Box3i(new ivec3(-1, 0, -1), new ivec3(1, 0, 1));
      // b.iterate((x, y, z, dbgcount) =>
      // {
      //   var g = new Glob(Gu.World, new ivec3(x, y, z), Gu.Context.FrameStamp);
      //   g.BarGrid = new BarGrid(Gu.World.Info);
      //   g.BarGrid.Edit_GenFlat(0, Gu.World.Info.BlockSizeY);
      //   return LambdaBool.Continue;
      // });
    }
    private bool SaveWorldsFile(FileLoc loc)
    {
      try
      {
        var enc = Encoding.GetEncoding("iso-8859-1");
        using (var fs = loc.OpenWrite())
        using (var bw = new System.IO.BinaryWriter(fs, enc))
        {
          bw.Write((Int32)_worldNames.Count);
          foreach (var s in _worldNames)
          {
            bw.Write((string)s);
          }

          bw.Close();
        }
      }
      catch (Exception ex)
      {
        //br.close
        Gu.Log.Error("Error", ex);
        return false;
      }
      return true;
    }
    private bool LoadWorldsFile(FileLoc loc)
    {
      if (!loc.Exists)
      {
        return false;
      }
      try
      {
        var enc = Encoding.GetEncoding("iso-8859-1");
        using (var fs = loc.OpenRead())
        using (var br = new System.IO.BinaryReader(fs, enc))
        {
          Int32 c = br.ReadInt32();
          _worldNames = new List<string>();
          for (var i = 0; i < c; ++i)
          {
            _worldNames.Add(br.ReadString());
          }

          br.Close();
        }
      }
      catch (Exception ex)
      {
        //br.close
        Gu.Log.Error("Error", ex);
        return false;
      }
      return true;
    }

  }//Class




}//NS
