using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;
using OpenTK.Graphics.OpenGL4;
using System.IO.Compression;

namespace PirateCraft
{
  public enum GenState
  {
    Created, Queued, GenStart, GenEnd, Ready, Deleted
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct Block
  {
    //The effigious block
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

      return Value == BlockItemCode.Tussock || Value == BlockItemCode.Dandilion || Value == BlockItemCode.Cedar_Needles;
    }
    public bool IsTransparentOr2Sided()
    {
      //In the future we'll probably keep a list of 2-sided billboards
      return IsItem(); // IsTransparentLiquid() ..
    }
    public bool IsSolidBlockNotTransparent()
    {
      //This should return whether the block is a *solid (non-item) *non-transparent *block (6 solid sides). 
      // Used for face culling.
      //Technically it should index into a LUT to see whether this block-item is solid or not.
      //For now - we are just rendering blocks so we can return HasDensity for this.
      return !IsEmpty() && !IsItem();
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
    public const ushort Cedar_Sapling = 8;
    public const ushort Cedar = 9;
    public const ushort Cedar_Needles = 10;
    public const ushort Feldspar = 11;
    public const ushort Tussock = 12;
    public const ushort Dandilion = 13;
    //Items
    //...
  }
  public enum TileImage
  {
    Grass, GrassSide, Dirt, Plank, Brick, Brick2, Gravel, Sand, Cedar, Cedar_Needles, Cedar_Top, Feldspar, Tussock, Blank, Dandilion
  }
  public class BlockTileUVSide
  {
    public const int Top = 0;
    public const int Side = 1;
    public const int Bottom = 2;
  }
  public class WorldStaticData
  {
    //Unit box for creating mesh cubes, Tiles, Material
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
            { TileImage.Tussock, new FileLoc("tx64_tussock.png", FileStorage.Embedded) },
            { TileImage.Blank, new FileLoc("tx64_blank.png", FileStorage.Embedded) },
            { TileImage.Dandilion, new FileLoc("tx64_dandilion.png", FileStorage.Embedded) },
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
  //Asynchronous generation for globs (mesh topologies within the drome)
  public class QueuedGlobData_WithKernel
  {
    public Glob glob;
    public List<v_v3n3x2> async_verts = null;
    public List<uint> async_inds_op = null;
    public List<uint> async_inds_tp = null;
    public long QueueId = 0;
    public double DistanceToPlayer = 0;//Sort key for generating
    public Block[] CopiedBlocks = null;//Note this is the block kernel of blocks + n
    public static int Kernel_Offset(int dx, int dy, int dz)
    {
      int off = World.GlobBlocksX_Gen_Kernel * World.GlobBlocksY_Gen_Kernel * dz +
                World.GlobBlocksX_Gen_Kernel * dy +
                dx;
      return off;
    }
    public Block GetBlock_Kernel(int dx, int dy, int dz)
    {
      Gu.Assert(dx >= 0 && dx < World.GlobBlocksX_Gen_Kernel);
      Gu.Assert(dy >= 0 && dy < World.GlobBlocksY_Gen_Kernel);
      Gu.Assert(dz >= 0 && dz < World.GlobBlocksZ_Gen_Kernel);
      //If we are empty, then we have deleted our Block[] data to save space. Return an empty block
      int off = Kernel_Offset(dx, dy, dz);

      return CopiedBlocks[off];
    }
  }
  //Asynchronous generation data for dromes (scalar fields e.g. blocks as ushort)
  public class QueuedDromeData
  {
    public Drome drome;
    public long QueueId = 0;
    public double DistanceToPlayer = 0;
  }
  //Topology units
  public class Glob
  {
    public Int64 GeneratedFrameStamp { get; private set; } = 0;
    public MeshData Transparent = null;
    public MeshData Opaque = null;
    public ivec3 Pos = new ivec3(0, 0, 0);
    public GenState State = GenState.Created;
    public Drome Drome = null;
    public object lock_object = new object();
    public long QueueId = 0;
    public long LastVisible_ms = 0; //Last time this glob was visible.
    public static vec3 OriginR3_fn(ivec3 g_pos)
    {
      vec3 r = new vec3(g_pos.x * World.GlobWidthX, g_pos.y * World.GlobWidthY, g_pos.z * World.GlobWidthZ);
      return r;
    }
    public vec3 OriginR3
    {
      get
      {
        vec3 r = new vec3(Pos.x * World.GlobWidthX, Pos.y * World.GlobWidthY, Pos.z * World.GlobWidthZ);
        return r;
      }
    }
    public vec3 CenterR3
    {
      get
      {
        vec3 r = OriginR3 + new vec3(World.GlobWidthX * 0.5f, World.GlobWidthY * 0.5f, World.GlobWidthZ * 0.5f);
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
    public class DromeStats
    {
      public long BaseWorld_ms = 0;
    }
    private Box3f _box;
    public RegionState RegionState = new RegionState();
    public Block[] Blocks = null;
    public RegionState[] RegionStates = null;
    public ivec3 Pos = new ivec3(0, 0, 0);
    public GenState State = GenState.Created;
    public DromeNode Root;
    public DromeStats Stats = new DromeStats();
    public Box3f Box
    {
      get
      {
        return _box;
      }
    }
    public vec3 OriginR3
    {
      get
      {
        vec3 r = new vec3(
          (float)Pos.x * World.DromeWidthX,
          (float)Pos.y * World.DromeWidthY,
          (float)Pos.z * World.DromeWidthZ);
        return r;
      }
    }
    public vec3 CenterR3
    {
      get
      {
        return OriginR3 +
        new vec3(
          (float)World.DromeWidthX * 0.5f,
          (float)World.DromeWidthY * 0.5f,
          (float)World.DromeWidthZ * 0.5f);
      }
    }

    public Drome(ivec3 pos, Int64 genframeStamp)
    {
      Pos = pos;

      _box = new Box3f(
        new vec3(
          Pos.x * World.DromeWidthX,
          Pos.y * World.DromeWidthY,
          Pos.z * World.DromeWidthZ),
        new vec3(
          (Pos.x + 1) * World.DromeWidthX,
          (Pos.y + 1) * World.DromeWidthY,
          (Pos.z + 1) * World.DromeWidthZ
        )
        );
    }

    public static int BlockOffset(ivec3 local_block_pos_in_drome)
    {
      int ret = World.DromeBlocksX * World.DromeBlocksY * local_block_pos_in_drome.z +
        World.DromeBlocksX * local_block_pos_in_drome.y +
        local_block_pos_in_drome.x;
      return ret;
    }
    public static int RegionStateOffset_FromLocalBlockPos(ivec3 local_block_pos_in_drome)
    {
      int dx = local_block_pos_in_drome.x / World.GlobBlocksX;
      int dy = local_block_pos_in_drome.y / World.GlobBlocksY;
      int dz = local_block_pos_in_drome.z / World.GlobBlocksZ;
      int ret = World.DromeGlobsX * World.DromeGlobsY * dz + World.DromeGlobsX * dy + dx;
      return ret;
    }
    public static int RegionStateOffset_FromLocalGlobPos(ivec3 local_glob_pos_in_drome)
    {
      int dx = local_glob_pos_in_drome.x;
      int dy = local_glob_pos_in_drome.y;
      int dz = local_glob_pos_in_drome.z;
      int ret = World.DromeGlobsX * World.DromeGlobsY * dz + World.DromeGlobsX * dy + dx;
      return ret;
    }

  }
  public class DromeNode
  {
    public DromeNode[] Children = null; //octree .. 
    public Box3f Box = new Box3f();
    public Glob Glob = null;
  }
  public abstract class Walker
  {
    public vec3 StartPosR3; //start point from where we walked.
    public ivec3 PosZ3;
    public int MaxSteps;
    public vec3 PosR3Center(vec3 drome_origin)
    {
      vec3 p = World.Z3BlockInDromeLocal_To_R3(drome_origin, PosZ3)
        + new vec3((float)World.BlockSizeX * 0.5f, (float)World.BlockSizeY * 0.5f, (float)World.BlockSizeZ * 0.5f);
      return p;
    }
    public abstract void Move();
    public Walker(Drome d, int max_steps, ivec3 start_pos_local)
    {
      PosZ3 = start_pos_local;
      StartPosR3 = PosR3Center(d.OriginR3);
      MaxSteps = max_steps;
    }
  }
  public class SnakeWalker : Walker
  {
    //This is cool for snake - like movement
    public vec3 Direction;

    public SnakeWalker(Drome d, int max_steps, ivec3 start_pos_local, vec3 direction_normal) : base(d, max_steps, start_pos_local)
    {
      Direction = direction_normal;
    }
    public override void Move()
    {
      int move_blocks = 1;//blocks
                          //Move this guy statistically in the direction of his normal

      float dx = Math.Abs(Random.Next() * Direction.x);
      float dy = Math.Abs(Random.Next() * Direction.y);
      float dz = Math.Abs(Random.Next() * Direction.z);
      if (dx >= dy && dx >= dz)
      {
        PosZ3.x += move_blocks * Math.Sign(Direction.x);
      }
      else if (dy >= dx && dy >= dz)
      {
        PosZ3.y += move_blocks * Math.Sign(Direction.y);
      }
      else
      {
        PosZ3.z += move_blocks * Math.Sign(Direction.z);
      }
    }
  }
  public class RandomWalker : Walker
  {
    public Minimax<vec3> MovementProbability;

    public RandomWalker(Drome d, int max_steps, ivec3 start_pos_local, Minimax<vec3> probability) : base(d, max_steps, start_pos_local)
    {
      MovementProbability = probability;
    }
    public override void Move()
    {
      int move_blocks = 1;//blocks
                          //Move this guy statistically in the direction of his normal

      float rx = (Random.Next(MovementProbability.Min.x, MovementProbability.Max.x));
      float ry = (Random.Next(MovementProbability.Min.y, MovementProbability.Max.y));
      float rz = (Random.Next(MovementProbability.Min.z, MovementProbability.Max.z));

      float dx = Math.Abs(rx);
      float dy = Math.Abs(ry);
      float dz = Math.Abs(rz);

      if (dx >= dy && dx >= dz)
      {
        PosZ3.x += move_blocks * Math.Sign(rx);
      }
      else if (dy >= dx && dy >= dz)
      {
        PosZ3.y += move_blocks * Math.Sign(ry);
      }
      else
      {
        PosZ3.z += move_blocks * Math.Sign(rz);
      }
    }
  }
  public class World
  {
    //These top variables are critical generation control variables.
    public int LimitYAxisGeneration = 0;//0 = off, >0 - limit globs generated along Y axis (faster generation)
    public const float MaxTotalGlobs = 4096 * 2 * 2 * 2;
    public const float MaxRenderGlobs = 4096;
    public const int MaxAwarenessShells = 10;//keep this < Min(DromeGlobs) to prevent generating more dromes
    public int MaxGlobsToGeneratePerFrame_Sync = 32;//number of glob copy operations per render side frame. This can slow down / speed up rendering.
    public const float BlockSizeX = 8.0f;
    public const float BlockSizeY = 8.0f;
    public const float BlockSizeZ = 8.0f;
    public const int GlobBlocksX = 16;
    public const int GlobBlocksY = 16;
    public const int GlobBlocksZ = 16;
    public const int GlobBlocks_Kernel_MarginX = 1;//Extra amount of blocks copied to the generator for neighbor information
    public const int GlobBlocks_Kernel_MarginY = 1;
    public const int GlobBlocks_Kernel_MarginZ = 1;
    public const int GlobBlocksX_Gen_Kernel = GlobBlocksX + GlobBlocks_Kernel_MarginX * 2; //Generation blocks copied from loaded dromes.
    public const int GlobBlocksY_Gen_Kernel = GlobBlocksY + GlobBlocks_Kernel_MarginY * 2;
    public const int GlobBlocksZ_Gen_Kernel = GlobBlocksZ + GlobBlocks_Kernel_MarginZ * 2;
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
    public long GlobAbandon_DeleteTime_ms = 1000 * 5; //X seconds

    public readonly vec3 BlockRadiusR3 = new vec3(BlockSizeX * 0.5f, BlockSizeY * 0.5f, BlockSizeZ * 0.5f);//Radius from center of glob to the corner.
    public readonly vec3 GlobRadiusR3 = new vec3(GlobWidthX * 0.5f, GlobWidthY * 0.5f, GlobWidthZ * 0.5f);//Radius from center of glob to the corner.
    //public vec3 GlobDiameterR3 { get { return GlobRadiusR3 * 2; } }
    public float RenderRadiusShell
    {
      get
      {
        Gu.Assert(Player != null);
        return (float)Player.Far / (float)MaxAwarenessShells;
      }
    }

    private int _currentShell = 1;
    private ivec3 playerLastGlob = new ivec3(0, 0, 0);
    private WorldObject dummy = new WorldObject("dummy_beginrender");
    private WorldObject _debugDrawLines = null;
    private WorldObject _debugDrawPoints = null;
    private Dictionary<DrawOrder, List<WorldObject>> _renderObs_Ordered = null;
    private Dictionary<ivec3, Glob> _globs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //All globs
    private Dictionary<ivec3, Glob> _renderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
    private Dictionary<ivec3, Glob> _visibleRenderGlobs_Frame = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
    private long _queueId = 1;
    private MultiMap<double, QueuedGlobData_WithKernel> _globsToGenerate = new MultiMap<double, QueuedGlobData_WithKernel>(); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
    private MultiMap<double, QueuedGlobData_WithKernel> _globsGenerated = new MultiMap<double, QueuedGlobData_WithKernel>(); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
    private MultiMap<double, QueuedDromeData> _dromesToGenerate = new MultiMap<double, QueuedDromeData>();
    private MultiMap<double, QueuedDromeData> _dromesGenerated = new MultiMap<double, QueuedDromeData>();

    private Dictionary<string, WorldObject> Objects { get; set; } = new Dictionary<string, WorldObject>();
    private Dictionary<ivec3, Drome> _dromes = new Dictionary<ivec3, Drome>(new ivec3.ivec3EqualityComparer()); //All globs

    public int Dbg_N_OB_Culled = 0;
    public int NumGlobs { get { return _globs.Count; } }
    public int NumRenderGlobs { get { return _renderGlobs.Count; } }
    public int NumVisibleRenderGlobs { get { return _visibleRenderGlobs_Frame.Count; } }
    public WorldObject SceneRoot { get; private set; } = new WorldObject("Scene_Root");
    public WorldObject Sky { get; set; } = null;

    public enum GlobCollection
    {
      All, Render, VisibleRender
    }

    private Camera3D _player = null;
    public Camera3D Player
    {
      get
      {
        return _player;
      }
      set
      {
        _player = value;
        playerLastGlob = R3toI3Glob(_player.Position);
      }
    }

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
    public void Initialize(Camera3D player, string worldName, bool delete_world_start_fresh, int limit_y_axis = 0)
    {
      Player = player;
      WorldName = worldName;
      LimitYAxisGeneration = limit_y_axis;

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

      for (int i = 0; i < 1; ++i)
      {
        Task.Factory.StartNew(() =>
        {
          while (true)
          {
            TopologizeGlobs_Async();
            System.Threading.Thread.Sleep(1);
          }
        });
      }

      for (int i = 0; i < 3; ++i)
      {
        Task.Factory.StartNew(() =>
        {
          while (true)
          {
            GenerateDromes_Async();
            System.Threading.Thread.Sleep(1);
          }
        });
      }

      //for (int i = 0; i < 1; ++i)
      //{
      //  Task.Factory.StartNew(() =>
      //  {
      //    while (true)
      //    {
      //      CleanGlobs_Async();
      //    }
      //  });
      //}

      Gu.Log.Info("Building initail grid");
      BuildWorldGrid(player.World.extractTranslation(), RenderRadiusShell * 3, true);
      //I'm assuming since this is cube voxesl we're going to do physics on the integer grid, we don't need triangle data then.
      WaitForAllDromesToGenerate();
      BuildWorldGrid(player.World.extractTranslation(), RenderRadiusShell * 3, true);
      WaitForAllGlobsToGenerate();
    }
    public void Update(double dt, Camera3D cam)
    {
      Gu.Assert(Player != null);

      if (Gu.Context.FrameStamp % 3 == 0)
      {
        BuildWorld();
      }
      UpdateObjects(dt);
      CollectVisibleObjects(cam);
      CollectVisibleGlobs(cam);
      AutoSaveWorld(dt);
    }
    private const int MaxInitialGenerationWaitTime_ms = 1000 * 15;
    private void WaitForAllDromesToGenerate()
    {
      System.Diagnostics.Stopwatch st = new System.Diagnostics.Stopwatch();
      st.Start();
      while (true)
      {
        FinishGeneratingGlobsAndDromes_RenderThread();

        bool genning = false;
        foreach (var d in _dromes.Values)
        {
          if (d.State != GenState.Ready)
          {
            genning = true;
          }
        }

        if (genning == false && _dromesToGenerate.Count == 0 && _dromesGenerated.Count == 0)
        {
          break;
        }

        if (st.ElapsedMilliseconds >= MaxInitialGenerationWaitTime_ms)
        {
          //  Gu.Log.Error("Generation time failed to work in given specified time.");
          // Gu.DebugBreak();
          //  return;
        }
        System.Threading.Thread.Sleep(100);
      }
    }
    private void WaitForAllGlobsToGenerate()
    {
      System.Diagnostics.Stopwatch st = new System.Diagnostics.Stopwatch();
      st.Start();
      while (true)
      {
        FinishGeneratingGlobsAndDromes_RenderThread();

        bool genning = false;
        foreach (var g in _globs.Values)
        {
          if (g.State != GenState.Ready)
          {
            genning = true;
          }
        }

        if (genning == false && _globsToGenerate.Count == 0 && _globsGenerated.Count == 0)
        {
          break;
        }

        if (st.ElapsedMilliseconds >= MaxInitialGenerationWaitTime_ms)
        {
          //   Gu.Log.Error("Generation time failed to work in given specified time.");
          //   Gu.DebugBreak();
          //   return;
        }
        System.Threading.Thread.Sleep(100);
      }
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
      //}, cam.Frustum, MaxRenderDistance);gu

      //Honestly, this isn't too slow. We usually have maybe 500 globs visible at a time.
      _visibleRenderGlobs_Frame.Clear();
      foreach (var g in _renderGlobs)
      {
        if (cam.Frustum.HasBox(GetGlobBoxGlobalI3(g.Key)))
        {
          g.Value.LastVisible_ms = Gu.Milliseconds(); //we're .. uuh uh .uh uh. uh.. visible.
          _visibleRenderGlobs_Frame.Add(g.Key, g.Value);
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
        foreach (var g in _visibleRenderGlobs_Frame)
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
        //HACK: this will change when we do the materials.
        GL.Disable(EnableCap.CullFace);
        foreach (var md in visible_tp)
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
            { BlockItemCode.Tussock , new List<FileLoc>(){ GetTileFile(TileImage.Blank), GetTileFile(TileImage.Tussock), GetTileFile(TileImage.Blank) } },
            { BlockItemCode.Dandilion , new List<FileLoc>(){ GetTileFile(TileImage.Blank), GetTileFile(TileImage.Dandilion), GetTileFile(TileImage.Blank) } },
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
      UpdateGenerationShell();
      BuildWorldGrid(Player.World.extractTranslation(), RenderRadiusShell * (float)_currentShell);
      FinishGeneratingGlobsAndDromes_RenderThread();
      CleanGlobs();
    }
    long _lastShellIncrementTimer = 0;
    private void UpdateGenerationShell()
    {
      ivec3 newPlayerGlob = R3toI3Glob(Player.Position);
      if ((newPlayerGlob != playerLastGlob))
      {
        _currentShell = 1;
        playerLastGlob = newPlayerGlob;
      }
      else if ((_currentShell < MaxAwarenessShells) && (Gu.Milliseconds() - _lastShellIncrementTimer) > 1000)
      {
        //Pretty sure integrals are atomic but on list.. not sure
        if (_globsToGenerate.Count == 0)
        {
          //Only increase shell if we're done generating for this particular shell.
          _currentShell++;
          _lastShellIncrementTimer = Gu.Milliseconds();
        }
      }
    }
    private void FinishGeneratingGlobsAndDromes_RenderThread()
    {
      FinishGeneratingDromes_RenderThread();
      FinishGeneratingGlobs_Sync();
    }
    private void FinishGeneratingDromes_RenderThread()
    {
      lock (_dromesGenerated)
      {
        foreach (var kvp in _dromesGenerated)
        {
          var qdd = kvp.Value;
          qdd.drome.State = GenState.Ready;
          SaveDrome(qdd.drome);
        }
        _dromesGenerated.Clear();
      }
    }
    private void FinishGeneratingGlobs_Sync()
    {
      if (_globsGenerated.Count == 0)
      {
        return;
      }
      //Create the mesh (gpu) and add/remove from renderglobs.
      MultiMap<double, QueuedGlobData_WithKernel> finished;

      lock (_globsGenerated)
      {
        finished = new MultiMap<double, QueuedGlobData_WithKernel>(_globsGenerated);
        _globsGenerated.Clear();
      }

      foreach (var kvp in finished)
      {
        var qgd = kvp.Value;
        var glob = qgd.glob;

        if (qgd.glob.State == GenState.Deleted)
        {
          continue;
        }
        if (!_globs.ContainsKey(qgd.glob.Pos))
        {
          //The glob was deleted. This isn't a thread safe operation (yet).
          continue;
        }

        //if(glob.QueueId != qgd.QueueId)
        //{
        //  //Discard - w
        //  continue;
        //}

        //This only needs to be called for new dromes.
        //  StitchGlobTopology(glob);

        bool globTopologyBefore = glob.Opaque != null || glob.Transparent != null;
        glob.Opaque = null;
        glob.Transparent = null;

        if (qgd.async_inds_op.Count > 0)
        {
          glob.Opaque = new MeshData("", OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles,
             v_v3n3x2.VertexFormat, Gpu.GetGpuDataPtr(qgd.async_verts.ToArray()),
             IndexFormatType.Uint32, Gpu.GetGpuDataPtr(qgd.async_inds_op.ToArray())
             );
        }
        if (qgd.async_inds_tp.Count > 0)
        {
          //This is unnecessary I mean, just a separate index buffer would be ok. For now this is my hack.
          glob.Transparent = new MeshData("", OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles,
             v_v3n3x2.VertexFormat, Gpu.GetGpuDataPtr(qgd.async_verts.ToArray()),
             IndexFormatType.Uint32, Gpu.GetGpuDataPtr(qgd.async_inds_tp.ToArray())
             );
        }
        //Avoid memory leaks
        qgd.async_inds_op = null;
        qgd.async_inds_tp = null;
        qgd.async_verts = null;

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


        glob.State = GenState.Ready;
      }
    }
    private void BuildWorldGrid(vec3 origin, float awareness_radius, bool logprogress = false)
    {
      List<Glob> newGlobs = new List<Glob>();

      Box3f awareness = new Box3f(origin - awareness_radius, origin + awareness_radius);

      Box3i ibox_drome;
      ibox_drome._min = new ivec3(
        (int)Math.Floor(awareness._min.x / DromeWidthX),
        (int)Math.Floor(awareness._min.y / DromeWidthY),
        (int)Math.Floor(awareness._min.z / DromeWidthZ));
      ibox_drome._max = new ivec3(
        (int)Math.Ceiling(awareness._max.x / DromeWidthX),
        (int)Math.Ceiling(awareness._max.y / DromeWidthY),
        (int)Math.Ceiling(awareness._max.z / DromeWidthZ));

      //Limit Y axis ..  Tehnically we need maybe 2-4 dromes up and down
      if (LimitYAxisGeneration > 0)
      {
        int ylimit = LimitYAxisGeneration;
        if (ibox_drome._min.y > ylimit) { ibox_drome._min.y = ylimit; }
        if (ibox_drome._min.y < -ylimit) { ibox_drome._min.y = -ylimit; }
        if (ibox_drome._max.y > ylimit) { ibox_drome._max.y = ylimit; }
        if (ibox_drome._max.y < -ylimit) { ibox_drome._max.y = -ylimit; }
        if (ibox_drome._min.y > ibox_drome._max.y) { ibox_drome._min.y = ibox_drome._max.y; }
      }

      List<Drome> newDromes = new List<Drome>();
      int dbg_current_drome = 0;
      ibox_drome.iterate((x, y, z, count) =>
      {
        dbg_current_drome++;
        if (logprogress)
        {
          if (dbg_current_drome % 1 == 0)
          {
            Gu.Log.Info("Drome " + dbg_current_drome + "/" + count);
          }
        }

        ivec3 dpos = new ivec3(x, y, z);
        Drome d;
        if (!_dromes.TryGetValue(dpos, out d))
        {
          d = GenerateOrLoadDrome(dpos);
        }

        if (d.State == GenState.Ready)
        {
          TopologizeDromeGlobs(d, awareness, logprogress);
        }

        return true;
      });
    }
    private void TopologizeDromeGlobs(Drome d, Box3f awar_r3, bool logprogress = false)
    {
      Gu.Assert(d.State == GenState.Ready);

      //TODO: fix all of this.

      List<Glob> newGlobs = new List<Glob>();
      Box3i ibox_glob;
      //Changed this to be box of globs in drome intersected with awareness
      ibox_glob._min = new ivec3(
        (int)Math.Floor(awar_r3._min.x / GlobWidthX),
        (int)Math.Floor(awar_r3._min.y / GlobWidthY),
        (int)Math.Floor(awar_r3._min.z / GlobWidthZ)
      );
      ibox_glob._max = new ivec3(
        (int)Math.Ceiling(awar_r3._max.x / GlobWidthX),
        (int)Math.Ceiling(awar_r3._max.y / GlobWidthY),
        (int)Math.Ceiling(awar_r3._max.z / GlobWidthZ)
      );

      //  Math.Max((int), (d.Pos.x * DromeGlobsX))
      //  Math.Max((int), (d.Pos.y * DromeGlobsY))
      //  Math.Max((int), (d.Pos.z * DromeGlobsZ))
      //  Math.Min((int), ((d.Pos.x + 1) * DromeGlobsX))
      //  Math.Min((int), ((d.Pos.y + 1) * DromeGlobsY))
      //  Math.Min((int), ((d.Pos.z + 1) * DromeGlobsZ))

      //34 * 34 * 34 = 39304
      int dbg_current = 0;
      ibox_glob.iterate((x, y, z, count) =>
      {
        dbg_current++;
        if (logprogress)
        {
          if (dbg_current % 100 == 0)
          {
            Gu.Log.Info("Glob " + dbg_current + "/" + count);
          }
        }
        //if (_renderGlobs.Count >= MaxRenderGlobs)
        //{
        //  return false;
        //}
        //if (_globs.Count + newGlobs.Count >= MaxTotalGlobs)
        //{
        //  return false;
        //}
        //if (newGlobs.Count >= MaxGlobsToGeneratePerFrame_RenderThread && _firstGeneration == false)
        //{
        //  return false;
        //}

        ivec3 gpos = new ivec3(x, y, z);
        if (!_globs.ContainsKey(gpos))
        {
          //TODO: Look up the density state of this DromeGlob in the drome to determine if it is empty or solidnottransparent
          //TODO: Look up the density state of this DromeGlob in the drome to determine if it is empty or solidnottransparent
          //TODO: Look up the density state of this DromeGlob in the drome to determine if it is empty or solidnottransparent
          //TODO: Look up the density state of this DromeGlob in the drome to determine if it is empty or solidnottransparent
          //TODO: Look up the density state of this DromeGlob in the drome to determine if it is empty or solidnottransparent
          //TODO: Look up the density state of this DromeGlob in the drome to determine if it is empty or solidnottransparent

          //Another check we could do here is collide the glob box with the camera frustum to generate, like Minecraft does.

          vec3 gp = Glob.OriginR3_fn(gpos) + new vec3(GlobWidthX * 0.5f, GlobWidthY * 0.5f, GlobWidthZ * 0.5f);
          if (!d.Box.containsInclusive(gp))
          {
            /// Gu.DebugBreak();

          }
          else
          {
            Box3f gbox = GetGlobBoxGlobalI3(gpos);
            if (Player.Frustum.HasBox(gbox))
            {
              Glob g = new Glob(gpos, Gu.Context.FrameStamp, d);
              QueueForTopo(g);
              _globs.Add(gpos, g);
              newGlobs.Add(g);
            }
          }
        }
        return true;
      });

      int n = 0;
      n++;
    }
    public int dbg_nSkippedStitch = 0;
    public int dbg_nEmptyNoData = 0;
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
        }
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
      d.RegionState.Reset();
      QueuedDromeData qdd = new QueuedDromeData();
      qdd.drome = d;
      qdd.QueueId = _queueId++;
      double dist = (double)(float)(d.CenterR3 - Player.Position).length();
      qdd.DistanceToPlayer = dist;
      _dromes.Add(gpos, d);
      lock (_dromesToGenerate)
      {
        _dromesToGenerate.Add(dist, qdd);
      }

      return d;
    }
    private void GenerateDrome_Async(QueuedDromeData qdd)
    {
      Drome d = qdd.drome;
      d.State = GenState.GenStart;
      {
        GenerateBaseWorld(d);

        //Crust layer.
        var seeds = PlantSeeds(d, 0.07f, new List<ushort>() { BlockItemCode.Grass, BlockItemCode.Dirt });
        GrowTrees(d, seeds);

        seeds = PlantSeeds(d, 0.7f, new List<ushort>() { BlockItemCode.Grass, BlockItemCode.Dirt });
        foreach (var grass in seeds)
        {
          if (IsBlockInsideDromeBounds(grass))
          {
            Block b = GetBlock(d, grass);
            if (b.IsEmpty())
            {
              SetBlock(d, grass, new Block(BlockItemCode.Tussock), true);
            }
          }
        }

        seeds = PlantSeeds(d, 0.18f, new List<ushort>() { BlockItemCode.Grass, BlockItemCode.Dirt });
        foreach (var flower in seeds)
        {
          if (IsBlockInsideDromeBounds(flower))
          {
            Block b = GetBlock(d, flower);
            if (b.IsEmpty())
            {
              SetBlock(d, flower, new Block(BlockItemCode.Dandilion), true);
            }
          }
        }
      }
      d.State = GenState.GenEnd;

      lock (_dromesGenerated)
      {
        _dromesGenerated.Add(qdd.DistanceToPlayer, qdd);
      }

    }
    private void GenerateBaseWorld(Drome d)
    {
      System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
      s.Start();

      vec3 dromeOriginR3 = d.OriginR3;
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
      d.Stats.BaseWorld_ms = s.ElapsedMilliseconds;
    }
    private List<ivec3> PlantSeeds(Drome d, float density01, List<ushort> allowedSoilTypes)
    {
      List<ivec3> planted = new List<ivec3>();
      int seeds = (int)((float)DromeBlocksX * (float)DromeBlocksZ * density01);
      for (int iseed = 0; iseed < seeds; iseed++)
      {
        int rx = (int)Random.Next(0, DromeBlocksX - 1);
        int rz = (int)Random.Next(0, DromeBlocksZ - 1);
        ivec3 v = DropBlock(d, rx, rz, allowedSoilTypes);
        if (v.x >= 0)
        {
          planted.Add(v);
        }
      }
      return planted;
    }
    private ivec3 DropBlock(Drome d, int x, int z, List<ushort> target_types)
    {
      //Doesn't actually set the block. Just drop from the top to the given ground.
      //target block is the allowed block we can drop onto (soil for planting, sand .. )
      for (int dy = DromeBlocksY - 1; dy >= 0; dy--)
      {
        var cpos = new ivec3(x, dy, z);
        var npos = new ivec3(x, dy - 1, z);
        if (IsBlockInsideDromeBounds(cpos) && IsBlockInsideDromeBounds(npos))
        {
          Block cur = GetBlock(d, cpos);
          if (!cur.IsEmpty())
          {
            //First block from top is empty. This is a planting algorithm, so we dont want to go into caves.
            break;
          }
          Block next = GetBlock(d, npos);
          if (cur.IsEmpty() && !next.IsEmpty())
          {
            if (target_types.Contains(next.Value))
            {
              return cpos;
            }
            else
            {
              //Failed - we planted onto something that is not dirt (or taret type)
              break;
            }
          }
        }

      }
      //May have been completely solid
      return new ivec3(-1, -1, -1);
    }
    private void GrowTrees(Drome d, List<ivec3> seeds)
    {
      foreach (var seed in seeds)
      {
        //if seed is tree
        GrowTree(d, seed);
      }
    }
    private void GrowTree(Drome d, ivec3 start_pos)
    {
      ivec3 cp = start_pos;
      vec3 d_origin = d.OriginR3;
      int trunkHeight = Random.NextInt(2, 4);
      ivec3 pos = start_pos;
      for (int y = 0; y <= trunkHeight; y++)
      {
        pos = start_pos;
        pos.y += y;
        if (IsBlockInsideDromeBounds(pos))
        {
          SetBlock(d, pos, new Block(BlockItemCode.Cedar), true);
        }
        else
        {
          break;
        }
      }

      //walk
      vec3 foliage_radius = new vec3(BlockSizeX * 1, BlockSizeY * 1, BlockSizeZ * 1);
      float maxdist2 = (float)Math.Pow(World.BlockSizeX * 3, 2);

      List<Walker> walkers = new List<Walker>();
      var mm = new Minimax<vec3>(new vec3(-1, -0.1f, -1), new vec3(1, 1, 1));
      walkers.Add(new RandomWalker(d, 5, pos, mm));
      walkers.Add(new RandomWalker(d, 5, pos, mm));
      walkers.Add(new RandomWalker(d, 5, pos, mm));
      walkers.Add(new RandomWalker(d, 5, pos, mm));
      walkers.Add(new RandomWalker(d, 5, pos, mm));

      int dbg_nCountWhile = 0;
      while (walkers.Count > 0)
      {
        for (int iwalker = walkers.Count - 1; iwalker >= 0; iwalker--)
        {

          Walker p = walkers[0];
          float len2 = (p.PosR3Center(d_origin) - p.StartPosR3).length2();
          if (len2 >= maxdist2)
          {
            walkers.RemoveAt(0);
            break;
          }
          p.Move();

          if (IsBlockInsideDromeBounds(p.PosZ3))
          {
            SetBlock(d, p.PosZ3, new Block(BlockItemCode.Cedar_Needles), true);
          }

        }

        dbg_nCountWhile++;
      }

    }
    private bool IsBlockInsideDromeBounds(ivec3 block_pos)
    {
      return block_pos.x >= 0 && block_pos.y >= 0 && block_pos.z >= 0 &&
        block_pos.x < DromeBlocksX && block_pos.y < DromeBlocksY && block_pos.z < DromeBlocksZ;
    }
    private Drome FindDromeI3(ivec3 pos, bool null_if_not_gen = true)
    {
      Drome d = null;
      _dromes.TryGetValue(pos, out d);
      if (d != null && null_if_not_gen && d.State != GenState.Ready)
      {
        return null;
      }
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
        _visibleRenderGlobs_Frame.TryGetValue(pos, out g);
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
          QueueForTopo(gn);
        }
      }
    }
    private void GenerateDromes_Async()
    {
      List<QueuedDromeData> tops = GrabFromDromes(1);
      foreach (QueuedDromeData qgd in tops)
      {
        GenerateDrome_Async(qgd);
      }
    }
    private void TopologizeGlobs_Async()
    {
      List<QueuedGlobData_WithKernel> tops = GrabFromTopo(32);

      foreach (QueuedGlobData_WithKernel qgd in tops)
      {
        //TODO: - add this back
        //  Gu.Log.Warn("Commented out density state optimizations until we index into the drome array for states");
        //   if (g.DensityState != Glob.GlobDensityState.Empty_AndNoData)
        {
          TopologizeGlob(qgd);
          //   dbg_nEmptyNoData++;
        }
        //  else
        {
          //The block is empty, the inside of the block has no topology. No data.
          // g.Blocks = null;
        }
        //   if (g.DensityState != Glob.GlobDensityState.SolidBlocksOnly)
        {
          //No neighboring blocks would be visible, so stitchin gisn't needed
          //   dbg_nSkippedStitch++;
        }
      }
      //lock (_globsToTopo_LockObject)
      //{
      // // _globsToTopo.Clear();
      //}
    }
    private void TopologizeGlob(QueuedGlobData_WithKernel qgd)
    {
      //    6    7
      // 2    3
      //    4    5
      // 0    1
      //Mesh
      List<v_v3n3x2> async_verts = new List<v_v3n3x2>();
      List<uint> async_inds_op = new List<uint>();
      List<uint> async_inds_tp = new List<uint>();

      ivec3[] n_off = new ivec3[]
      {
        new ivec3(-1, 0, 0),
        new ivec3( 1, 0, 0),
        new ivec3( 0,-1, 0),
        new ivec3( 0, 1, 0),
        new ivec3( 0, 0,-1),
        new ivec3( 0, 0, 1),
      };

      Block our_block;
      vec2[] texs = new vec2[4];
      List<MtTex> patches = new List<MtTex>();
      Block b_n;
      var glob = qgd.glob;
      ivec3 block_off_glob = new ivec3();

      if (glob.State != GenState.Deleted)
      {
        glob.State = GenState.GenStart;
      }

      //Iterate over a glob topology unit, note the blocks in the queued data have extra padding for neighbor information.
      for (int z = 0; z < GlobBlocksZ; z++)
      {
        for (int y = 0; y < GlobBlocksY; y++)
        {
          for (int x = 0; x < GlobBlocksX; x++)
          {
            block_off_glob.construct(x, y, z);
            our_block = qgd.GetBlock_Kernel(
              x + GlobBlocks_Kernel_MarginX,
              y + GlobBlocks_Kernel_MarginY,
              z + GlobBlocks_Kernel_MarginZ);

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
              //vec3 block_pos_rel_R3_Center = block_pos_rel_R3 + new vec3(World.BlockSizeX * 0.5f, World.BlockSizeY * 0.5f, World.BlockSizeZ * 0.5f);
              //vec3 block_pos_abs_R3_Center = block_pos_rel_R3_Center + glob.OriginR3;
              //vec3 block_pos_abs_R3_Center_Neighbor = block_pos_abs_R3_Center + face_offs[face];

              b_n = qgd.GetBlock_Kernel(
                x + GlobBlocks_Kernel_MarginX + n_off[face].x,
                y + GlobBlocks_Kernel_MarginY + n_off[face].y,
                z + GlobBlocks_Kernel_MarginZ + n_off[face].z
                );

              if (!b_n.IsSolidBlockNotTransparent())
              {
                uint foff = (uint)async_verts.Count;

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
                  async_verts.Add(new v_v3n3x2()
                  {
                    _v = WorldStaticData.bx_verts_face[face, vi]._v + block_pos_abs_R3,
                    _n = WorldStaticData.bx_verts_face[face, vi]._n,
                    _x = texs[vi],
                  });
                }

                if (!our_block.IsTransparentOr2Sided())
                {
                  for (int ii = 0; ii < 6; ++ii)
                  {
                    async_inds_op.Add(foff + WorldStaticData.bx_face_inds[ii]);
                  }
                }
                else
                {
                  for (int ii = 0; ii < 6; ++ii)
                  {
                    async_inds_tp.Add(foff + WorldStaticData.bx_face_inds[ii]);
                  }
                }

              }
            }

          }
        }
      }
      qgd.async_verts = async_verts;
      qgd.async_inds_op = async_inds_op;
      qgd.async_inds_tp = async_inds_tp;
      qgd.CopiedBlocks = null;

      if (glob.State != GenState.Deleted)
      {
        glob.State = GenState.GenEnd;
      }

      lock (_globsGenerated)
      {
        _globsGenerated.Add(qgd.DistanceToPlayer, qgd);
      }

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
    private void CleanGlobs()
    {
      //if a glob is X units from player ... 
      // If a glob has been invisible for Y units ...
      // Delete topology.
      var awar_len2 = Math.Pow(Player.Far, 2); // far render distance is the limit to our fun, here, like minecraft

      List<Glob> toDelete = new List<Glob>();

      var cur_ms = Gu.Milliseconds();
      foreach (var kvp in _globs)
      {
        var ms = kvp.Value.LastVisible_ms - cur_ms;
        if (ms >= GlobAbandon_DeleteTime_ms)
        {
          var g_dist2 = (kvp.Value.CenterR3 - this.Player.Position).length2();
          if (g_dist2 > awar_len2)
          {
            toDelete.Add(kvp.Value);
          }
        }
      }

      foreach (var g in toDelete)
      {
        g.Opaque?.Dispose();
        g.Transparent?.Dispose();
        if (g.State == GenState.Ready)
        {
          g.State = GenState.Deleted;
          _globs.Remove(g.Pos);
        }
      }
      toDelete.Clear();

    }
    //private void SweepGridFrustum(Action<ivec3, Box3f> func, Frustum pf, float fMaxDist)
    //{
    //  vec3 cp = pf.NearCenter;
    //  int iDebugSweepCount = 0;
    //  List<ivec3> toCheck = new List<ivec3>();
    //  HashSet<ivec3> dchecked = new HashSet<ivec3>();

    //  int nPotentialGlobs = (int)((fMaxDist / GlobWidthX) * (fMaxDist / GlobWidthY) * (fMaxDist / GlobWidthZ));
    //  int nMaxPotentialGlobs = 5000;
    //  if (nPotentialGlobs > nMaxPotentialGlobs)
    //  {
    //    //This is technically an error, but we may also just hard limit the sweep routine if we weant.
    //    Gu.Log.WarnCycle("Warning: potential number of globs " + nPotentialGlobs + " exceeds " + nMaxPotentialGlobs);
    //    Gu.DebugBreak();
    //  }

    //  float fMaxDist2 = fMaxDist * fMaxDist;

    //  //Seed
    //  toCheck.Add(R3toI3Glob(cp));

    //  while (toCheck.Count > 0)
    //  {
    //    ivec3 vi = toCheck[0];
    //    toCheck.RemoveAt(0);// erase(toCheck.begin() + 0);
    //    iDebugSweepCount++;

    //    if (!dchecked.Contains(vi))
    //    {
    //      //TODO: fix this because we're getting stack overflows
    //      dchecked.Add(new ivec3(vi.x, vi.y, vi.z));

    //      // if the grid right here intersects the frustum
    //      Box3f box = GetGlobBoxGlobalI3(vi);

    //      vec3 node_center = box.center();

    //      float fDist2 = (pf.NearCenter - node_center).length2();

    //      if (fDist2 < fMaxDist2)
    //      {
    //        if (pf.HasBox(box))
    //        {
    //          func(vi, box);

    //          //Sweep Neighbors
    //          vec3[] n = new vec3[6];
    //          for (int ni = 0; ni < 6; ++ni)
    //          {
    //            n[ni] = node_center + WorldStaticData.GlobNeighborOffsets[ni];

    //          }

    //          toCheck.Add(R3toI3Glob(n[0]));
    //          toCheck.Add(R3toI3Glob(n[1]));
    //          toCheck.Add(R3toI3Glob(n[2]));
    //          toCheck.Add(R3toI3Glob(n[3]));
    //          toCheck.Add(R3toI3Glob(n[4]));
    //          toCheck.Add(R3toI3Glob(n[5]));
    //        }
    //        else
    //        {
    //          int nnn = 0;
    //          nnn++;
    //        }
    //      }
    //      else
    //      {
    //        int nnn = 0;
    //        nnn++;
    //      }
    //    }
    //  }

    //  dchecked.Clear();

    //}
    public Drome GetDromeForGlob(Glob g)
    {
      Drome ret = null;
      ivec3 dvi = GlobPosToDromePos(g.Pos);
      _dromes.TryGetValue(dvi, out ret);
      return ret;
    }
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
        d.RegionStates = new RegionState[World.DromeGlobsX * World.DromeGlobsY * World.DromeGlobsZ];
      }

      Block old = new Block(0);
      if (!bInitialGen_Dont_Queue_For_Update)
      {
        old = GetBlock(d, local_block_pos_in_drome);
      }
      else
      {
        d.RegionState.UpdateInitialGenAddedBlock(block);
        d.RegionStates[Drome.RegionStateOffset_FromLocalBlockPos(local_block_pos_in_drome)].UpdateInitialGenAddedBlock(block);
      }
      d.Blocks[Drome.BlockOffset(local_block_pos_in_drome)] = block;

      if (!bInitialGen_Dont_Queue_For_Update)
      {
        d.RegionState.UpdateBlockModified(old, block);
        d.RegionStates[Drome.RegionStateOffset_FromLocalBlockPos(local_block_pos_in_drome)].UpdateBlockModified(old, block);

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
          QueueForTopo(g);
        }
        else
        {
          Gu.Log.Warn("Glob was null for queue");
        }
      }
    }
    public void QueueForTopo(Glob g)
    {
      //priority is no longer needed when sorting by distance
      QueuedGlobData_WithKernel qgd = new QueuedGlobData_WithKernel();
      qgd.glob = g;
      qgd.QueueId = _queueId++;
      g.QueueId = qgd.QueueId;
      CopyGlobBlocks_Sync(g.Drome, qgd);

      double dist = (qgd.glob.CenterR3 - Player.Position).length();
      qgd.DistanceToPlayer = dist;

      lock (_globsToGenerate)
      {
        _globsToGenerate.Add(dist, qgd);
      }
    }
    public void CopyGlobBlocks_Sync(Drome drome_in, QueuedGlobData_WithKernel qgd)
    {
      ivec3 block_off_drome = GlobOffsetInDrome_LocalZ3(qgd.glob.Pos) * new ivec3(World.GlobBlocksX, World.GlobBlocksY, World.GlobBlocksZ);

      //We don't support for kernels that span > +/-1 neighbor drome (dromes are massive anyway, there'd be no point)
      Gu.Assert(GlobBlocks_Kernel_MarginX < DromeBlocksX); //This would be a massive kernel..
      Gu.Assert(GlobBlocks_Kernel_MarginY < DromeBlocksY);
      Gu.Assert(GlobBlocks_Kernel_MarginZ < DromeBlocksZ);

      //Copy a "kernel" to the given glob data. uigh..
      qgd.CopiedBlocks = new Block[World.GlobBlocksX_Gen_Kernel * World.GlobBlocksY_Gen_Kernel * World.GlobBlocksZ_Gen_Kernel];
      for (int z = 0; z < World.GlobBlocksZ_Gen_Kernel; z++)
      {
        for (int y = 0; y < World.GlobBlocksY_Gen_Kernel; y++)
        {
          for (int x = 0; x < World.GlobBlocksX_Gen_Kernel; x++)
          {
            //Calculate the given drome by the neighbor offset.
            ivec3 d_or_n_block_off = block_off_drome + new ivec3(
              x - GlobBlocks_Kernel_MarginX,
              y - GlobBlocks_Kernel_MarginY,
              z - GlobBlocks_Kernel_MarginZ
              );
            ivec3 d_or_n_off = new ivec3(0, 0, 0);

            if (d_or_n_block_off.x < 0)
            {
              d_or_n_off.x = -1;
              d_or_n_block_off.x += DromeBlocksX;
            }
            else if (d_or_n_block_off.x >= World.DromeBlocksX)
            {
              d_or_n_off.x = 1;
              d_or_n_block_off.x -= DromeBlocksX;
            }
            else
            {
              d_or_n_off.x = 0;
            }

            if (d_or_n_block_off.y < 0)
            {
              d_or_n_off.y = -1;
              d_or_n_block_off.y += DromeBlocksY;
            }
            else if (d_or_n_block_off.y >= World.DromeBlocksY)
            {
              d_or_n_off.y = 1;
              d_or_n_block_off.y -= DromeBlocksY;
            }
            else
            {
              d_or_n_off.y = 0;
            }

            if (d_or_n_block_off.z < 0)
            {
              d_or_n_off.z = -1;
              d_or_n_block_off.z += DromeBlocksZ;
            }
            else if (d_or_n_block_off.z >= World.DromeBlocksZ)
            {
              d_or_n_off.z = 1;
              d_or_n_block_off.z -= DromeBlocksZ;
            }
            else
            {
              d_or_n_off.z = 0;
            }

            Drome drome_n_or_cur = null;
            if (d_or_n_off.x != 0 || d_or_n_off.y != 0 || d_or_n_off.z != 0)
            {
              drome_n_or_cur = FindDromeI3(drome_in.Pos + d_or_n_off);
              if (drome_n_or_cur != null)
              {
                if (drome_n_or_cur.State != GenState.Ready)
                {
                  drome_n_or_cur = null;
                }
              }
            }
            else
            {
              drome_n_or_cur = drome_in;
            }

            int qoff = QueuedGlobData_WithKernel.Kernel_Offset(x, y, z);
            if (drome_n_or_cur != null)
            {
              int doff = Drome.BlockOffset(d_or_n_block_off);
              qgd.CopiedBlocks[qoff] = drome_n_or_cur.Blocks[doff];
            }
            else
            {
              qgd.CopiedBlocks[qoff] = Block.Empty;
            }
          }
        }
      }

    }
    public List<QueuedGlobData_WithKernel> GrabFromTopo(int max_per_step)
    {
      List<QueuedGlobData_WithKernel> ret = new List<QueuedGlobData_WithKernel>();
      lock (_globsToGenerate)
      {
        for (int i = 0; i < max_per_step && i < _globsToGenerate.Count; i++)
        {
          var kvp = _globsToGenerate.First();
          ret.Add(kvp.Value);
          _globsToGenerate.Remove(kvp);
        }
      }
      return ret;
    }
    public List<QueuedDromeData> GrabFromDromes(int max_per_step)
    {
      List<QueuedDromeData> ret = new List<QueuedDromeData>();
      lock (_dromesToGenerate)
      {
        for (int i = 0; i < max_per_step && i < _dromesToGenerate.Count; i++)
        {
          var kvp = _dromesToGenerate.First();
          ret.Add(kvp.Value);
          _dromesToGenerate.Remove(kvp);
        }
      }
      return ret;
    }
    public Block GetBlock(Drome d, ivec3 local_pos_drome)
    {
      //If we are empty, then we have deleted our Block[] data to save space. Return an empty block
      if (d.RegionState.State == RegionState.Empty_AndNoData)
      {
        return new Block(BlockItemCode.Empty);
      }
      //This should not be necessary - if we have blocks we are allocated right?
      //CheckOrAllocateDromeBlocks(d);

      int off = Drome.BlockOffset(local_pos_drome);
      return d.Blocks[off];
    }

    #region Index Functions

    public static vec3 Z3BlockInDromeLocal_To_R3(vec3 drome_r3, ivec3 block_drome_local)
    {
      vec3 ret = new vec3(
        drome_r3.x + block_drome_local.x * (float)World.BlockSizeX,
        drome_r3.y + block_drome_local.y * (float)World.BlockSizeY,
        drome_r3.z + block_drome_local.z * (float)World.BlockSizeZ
        );
      return ret;
    }
    public static vec3 Z3BlockGlobalToR3BlockGlobal(ivec3 z3)
    {
      vec3 ret = new vec3(
        (float)z3.x * BlockSizeX,
        (float)z3.y * BlockSizeY,
        (float)z3.z * BlockSizeZ
        );
      return ret;
    }
    private static ivec3 R3ToI3BlockGlobal(vec3 pt)
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
      if (d.State != GenState.Ready)
      {
        return null;
      }
      ivec3 bpos = R3toI3BlockLocal_Drome(R3_pos);
      Block b = GetBlock(d, bpos);

      return b;
    }
    private static float R3toI3BlockComp(float R3, float BlocksAxis, float BlockWidth)
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
    private static ivec3 R3ToI3BlockLocal_Any(vec3 R3, float cont_w_x, float cont_w_y, float cont_w_z)
    {
      vec3 bpos = new vec3(
       R3toI3BlockComp(R3.x, cont_w_x, BlockSizeX),
       R3toI3BlockComp(R3.y, cont_w_y, BlockSizeY),
       R3toI3BlockComp(R3.z, cont_w_z, BlockSizeZ));

      return new ivec3((int)bpos.x, (int)bpos.y, (int)bpos.z);
    }
    private static ivec3 R3toI3BlockLocal_Glob(vec3 R3)
    {
      ivec3 bpos = R3ToI3BlockLocal_Any(R3, GlobWidthX, GlobWidthY, GlobWidthZ);

      if (bpos.x < 0 || bpos.y < 0 || bpos.z < 0 || bpos.x >= World.GlobBlocksX || bpos.y >= World.GlobBlocksY || bpos.z >= World.GlobBlocksZ)
      {
        Gu.DebugBreak();
      }
      return bpos;
    }
    private static ivec3 R3toI3BlockLocal_Drome(vec3 R3)
    {
      ivec3 bpos = R3ToI3BlockLocal_Any(R3, DromeWidthX, DromeWidthY, DromeWidthZ);
      if (bpos.x < 0 || bpos.y < 0 || bpos.z < 0 || bpos.x >= World.DromeBlocksX || bpos.y >= World.DromeBlocksY || bpos.z >= World.DromeBlocksZ)
      {
        Gu.DebugBreak();
      }
      return bpos;
    }
    private static ivec3 R3toI3Glob(vec3 R3)
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
    private static ivec3 R3toI3Drome(vec3 R3)
    {
      ivec3 dpos = new ivec3(
         (int)Math.Floor(R3.x / World.DromeWidthX),
         (int)Math.Floor(R3.y / World.DromeWidthY),
         (int)Math.Floor(R3.z / World.DromeWidthZ));
      return dpos;
    }
    private static Box3f GetGlobBoxGlobalI3(ivec3 pt)
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
    public static Box3f GetBlockBox(PickedBlock b, float padding)
    {
      return GetBlockBoxLocal(b.Drome, b.BlockPosLocal, padding);
    }
    private static Box3f GetBlockBoxLocal(Drome d, ivec3 local, float padding)
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
    private static Box3f GetBlockBoxGlobalR3(vec3 pt)
    {
      //Snap the point pt to the block grid, and return the bound box of that block
      Box3f box = new Box3f();
      box._min = R3ToI3BlockGlobal(pt).toVec3() * new vec3(BlockSizeX, BlockSizeY, BlockSizeZ);
      box._max.x = box._min.x + BlockSizeX;
      box._max.y = box._min.y + BlockSizeY;
      box._max.z = box._min.z + BlockSizeZ;
      return box;
    }
    private static ivec3 GlobPosToDromePos(ivec3 globPos)
    {
      ivec3 ret = new ivec3((int)Math.Floor((double)globPos.x / (double)DromeGlobsX),
                            (int)Math.Floor((double)globPos.y / (double)DromeGlobsY),
                            (int)Math.Floor((double)globPos.z / (double)DromeGlobsZ));
      return ret;
    }
    private static int GlobOffsetInDrome_LocalZ3_Comp(int Z3, int BlocksAxis)
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
    private static ivec3 GlobOffsetInDrome_LocalZ3(ivec3 pos_glob_global_z3)
    {
      ivec3 r = new ivec3(
        GlobOffsetInDrome_LocalZ3_Comp(pos_glob_global_z3.x, DromeGlobsX),
        GlobOffsetInDrome_LocalZ3_Comp(pos_glob_global_z3.y, DromeGlobsY),
        GlobOffsetInDrome_LocalZ3_Comp(pos_glob_global_z3.z, DromeGlobsZ)
        )
        ;
      return r;
    }
    private static ivec3 LocalGlobBlockPos_To_LocalDromeBlockPos(ivec3 glob_pos_z3_global, ivec3 glob_block_pos_local)
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
      //Pick Block/blocks in the world.
      //  We use an inclusive box test just for the hit, but not for the recursion i.e ray ends up on an edge and we recur to 2 blocks and end up with a lot of duplicates.
      //  This algorithm.. isn't the best we should use a 3d line drawing algorithm to avoid duplicate checks. stepx stepx stepy stepx .. 
      //TODO: much faster if we marched the drome/glob first, then the blocks. We do only blocks
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
              if (!toCheck.Contains(n_pos))
              {
                toCheck.Add(n_pos);
              }
              else
              {
                //Gu.DebugBreak().. 
              }
            }
          }
        };

        //This is technically broken since we traverse >1 block sometimes with exclusive ray-box tests. The Contains() prevents it from blowing up.
        //** Technically speaking we should only go to one one block if we want to fix this algorithm.
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
