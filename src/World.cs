using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using System.Text;

namespace PirateCraft
{
  public struct VisibleBlockFaceData
  {
    public byte faceIdx; // 0 - 6
    public short x, y, z;
  }
  public enum GenState
  {
    Created, Queued, GenStart, GenEnd, Ready, Deleted,
  }
  public enum BlockBits
  {
    Solid,
    Liquid,
    SolidAndLiquid,
    Fog,
    All,
  }
  public static class BlockInfo
  {
    //Honeslty what we need is a BlockOpacity and set to 0 - 1 and tie it to BlockMaterial. 
  }
  public static class Block
  {
    //blocks are stored currently as ushort
    //this class is just a utility methods for block
    //The effigious block
    //11  |  11 11  | 11 1111 1111 
    //fog   liquid           item  = 10
    private static int LiquidBitCount = 4;
    public const int MaxLiquid = 0xF;//1111
    private static int LiquidBitMask = 0x3C00;// 0011110000000000

    private static int SolidBitCount = 10;
    public const int MaxSolid = 0x3FF;//1111
    private static int SolidBitMask = 0x3FF;// 0000001111111111

    public static void SetBlock(ref ushort block, ushort block_solidcode_liquid_or_both, BlockBits bits)
    {
      if (bits == BlockBits.All)
      {
        block = block_solidcode_liquid_or_both;
      }
      else if (bits == BlockBits.SolidAndLiquid)
      {
        //TODO: assuming w'er going to have more bits ..
        block = block_solidcode_liquid_or_both;
      }
      else if (bits == BlockBits.Solid)
      {
        Block.SetSolid(ref block, block_solidcode_liquid_or_both);
      }
      else if (bits == BlockBits.Liquid)
      {
        Block.SetLiquid(ref block, block_solidcode_liquid_or_both);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    public static ushort GetVisibleBlockCode(ushort block)
    {
      //Returns Blockitemcode.missing or blockitemcode.air if this block is not visible.
      // otherwise returns WATER if it is water or, a solid block code (fog, etc need to also return a value)
      var s = GetSolid(block);
      if (s == BlockItemCode.Missing)
      {
        return s;
      }
      else if (s == BlockItemCode.Air)
      {
        if (GetLiquid(block) == 0)
        {
          return BlockItemCode.Air;
        }
        else
        {
          return BlockItemCode.Water; //water is a visible block. This is only returned if there is no solid block.
        }
      }
      else
      {
        return s;
      }
    }
    public static void SetLiquid(ref ushort block, ushort liquid_value)
    {
      Gu.Assert(liquid_value <= MaxLiquid);
      block = (ushort)(((int)block & ~LiquidBitMask) | (((int)liquid_value) << SolidBitCount) & LiquidBitMask);
    }
    public static void AddLiquid(ref ushort block, short liquid_value)
    {
      //Add or remove liquid
      var lqw = GetLiquid(block);
      short newliq = (short)((int)lqw + (int)liquid_value);
      if (newliq > Block.MaxLiquid)
      {
        newliq = Block.MaxLiquid;
      }
      if (newliq < 0)
      {
        newliq = 0;
      }
      ushort newliq_unsigned = (ushort)newliq;

      SetLiquid(ref block, newliq_unsigned);
    }
    public static ushort GetLiquid(ushort block)
    {
      return (ushort)((((int)block & LiquidBitMask) >> SolidBitCount));
    }
    public static void SetSolid(ref ushort block, ushort solid_value)
    {
      //Sanity check
      Gu.Assert(solid_value < BlockItemCode.MaxBlockItemCodes);
      Gu.Assert(solid_value <= MaxSolid);
      block = (ushort)(((int)block & ~SolidBitMask) | (((int)solid_value) << 0) & SolidBitMask);
    }
    public static ushort GetSolid(ushort block)
    {

      var solid_value = (ushort)((((int)block & SolidBitMask) >> 0));
      Gu.Assert(solid_value < BlockItemCode.MaxBlockItemCodes);
      return solid_value;
    }
    public static bool Is(ushort block, ushort blockitemcode)
    {
      return GetSolid(block) == blockitemcode;
    }

    public static bool IsFullOfWater(ushort block)
    {
      return Block.GetLiquid(block) == Block.MaxLiquid;
    }
    public static bool HasWater(ushort block)
    {
      return Block.GetLiquid(block) > 0;
    }
    public static bool HasNoWater(ushort block)
    {
      return Block.GetLiquid(block) == 0;
    }
    public static bool HasNoSolid(ushort b)
    {
      var s = Block.GetSolid(b);

      //Nothing but air
      return (s == BlockItemCode.Air || s == BlockItemCode.Missing || s == BlockItemCode.Water);
    }
    public static bool HasSolid(ushort b)
    {
      return !HasNoSolid(b);
    }
    public static bool IsMeshItem(ushort bwhole)
    {
      //Items are special case cullables since they may not, or may cull entire faces.
      return Block.Is(bwhole, BlockItemCode.Torch);
    }
    public static bool CollidePlayer(ushort bwhole)
    {
      var b = GetSolid(bwhole);
      return !(b == BlockItemCode.Tussock || b == BlockItemCode.Dandilion || b == BlockItemCode.RosePink || b == BlockItemCode.RoseRed || b == BlockItemCode.Seaweed);
    }
    public static bool IsDecalOr2Sided(ushort bwhole)
    {
      var b = GetSolid(bwhole);

      //In the future we'll probably keep a list of 2-sided billboards
      return b == BlockItemCode.Tussock || b == BlockItemCode.Dandilion || b == BlockItemCode.RosePink || b == BlockItemCode.RoseRed ||
        b == BlockItemCode.Cedar_Needles || b == BlockItemCode.Oak_Leaves || b == BlockItemCode.Seaweed;
    }
    public static bool IsSolidBlock(ushort bwhole)
    {
      // THE BLOCK IS SOLID, NOT TRANSPARENT
      //This should return whether the block is a *solid (non-item) *non-transparent *block (6 solid sides). 
      // Used for face culling.
      //Technically it should index into a LUT to see whether this block-item is solid or not.
      //For now - we are just rendering blocks so we can return HasDensity for this.
      return HasSolid(bwhole) && !IsMeshItem(bwhole) && !IsDecalOr2Sided(bwhole); //IsTransparent
    }
    public static bool IsVisibleBlock(ushort bwhole)
    {
      var c = GetVisibleBlockCode(bwhole);
      return c != BlockItemCode.Missing && c != BlockItemCode.Air;
    }
    public static bool IsNotVisible(ushort bwhole)
    {
      return !IsVisibleBlock(bwhole);
    }
  }
  public enum RegionState
  {
    //This is used for culling 99% of the data.
    Partial, VisibleBlocksOnly, Empty_AndNoData // TODO: Solid Only, Transparent, Mixed
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct RegionBlocks
  {
    //This class determines what regions of globs/dromes have data, or are solid, this is used to
    //optimize the topology generator, and, unload empty regions of blocks, as empty areas have no data.

    private const int RegionStateMaxBlocks = World.GlobBlocksX * World.GlobBlocksY * World.GlobBlocksZ;

    private int _value_count = 0;
    private int _empty_count = RegionStateMaxBlocks;

    public static RegionBlocks EmptyRegionState_Glob = new RegionBlocks();
    public bool HasValues
    {
      get
      {
        return (_value_count == World.GlobBlocksX * World.GlobBlocksY * World.GlobBlocksZ) &&
          (_empty_count == 0);
      }
    }
    public bool IsEmpty
    {
      get
      {
        return (_value_count == 0) &&
          (_empty_count == World.GlobBlocksX * World.GlobBlocksY * World.GlobBlocksZ);
      }
    }

    public void Reset()
    {
      _value_count = _empty_count;
    }

    public int ValueCount { get { return _value_count; } }
    public int EmptyCount { get { return _empty_count; } }
    public void Deserialize(BinaryReader br)
    {
      _value_count = br.ReadInt32();
      _empty_count = br.ReadInt32();
    }
    public void Serialize(BinaryWriter br)
    {
      br.Write((Int32)_value_count);
      br.Write((Int32)_empty_count);
    }
    public RegionBlocks()
    {
      //Note: Ctor does not get called.
      Init();
    }
    public void Init()
    {
      if (Block.IsVisibleBlock(Drome.InitialBlockValue))
      {
        _value_count = RegionStateMaxBlocks;
        _empty_count = 0;
      }
      else
      {
        _value_count = 0;
        _empty_count = RegionStateMaxBlocks;
      }
    }
    public RegionState State
    {
      get
      {
        if (ValueCount > 0 && EmptyCount > 0)
        {
          return RegionState.Partial;
        }
        else if (EmptyCount == 0)
        {
          return RegionState.VisibleBlocksOnly;
        }
        else if (ValueCount == 0)
        {
          //Later we delete block data to save space for empty globs.
          return RegionState.Empty_AndNoData;
        }
        return RegionState.Partial;
      }
    }
    public void UpdateBlockModified(ushort old, ushort block)
    {
      //Modified a block, our state may have changed
      if (Block.IsVisibleBlock(old))
      {
        if (!Block.IsVisibleBlock(block))
        {
          _empty_count++;
          _value_count--;
        }
      }
      else
      {
        if (Block.IsVisibleBlock(block))
        {
          _value_count++;
          _empty_count--;
        }
      }
    }
  }
  public class BlockItemCode
  {
    //Blocks
    public const ushort Missing = 0; //block is missing, maybe drome not loaded, not air /land/water
    public const ushort Air = 1; //air = empty 
    public const ushort Land = 2; //catch-all for land
    public const ushort AnyVisible = 4; //unused right now
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
  public class PickedBlock
  {
    //A picked block from a raycast or other

    public bool IsHit { get { return _t1 >= 0.0f && _t1 <= 1.0f; } }
    public Drome Drome = null;
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
  public enum TileImage
  {
    Grass, GrassSide, Dirt, Plank, Brick, Brick2, Gravel, Sand, Cedar,
    Cedar_Needles, Cedar_Top, Feldspar,
    Tussock, Tussock2, Tussock_Stalk_Bot, Tussock_Stalk_Mid, Tussock_Stalk_Top,

    Blank, Dandilion, Cracks1, Cracks2, Cracks3, Cracks4, Feldspar_Coal, Marble_White, Marble_Green, Water, Seaweed, Clay, RedClay, RosePink, RoseRed,
    Oak_Top, Oak, Oak_Leaves,
  }
  public enum BlockSide
  {
    Left = 0, Right = 1, Bottom = 2, Top = 3, Back = 4, Front = 5
  }
  public class BlockTileUVSide
  {
    public const int Top = 0;
    public const int Side = 1;
    public const int Bottom = 2;
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
    public static v_v3n3x2[,] bx_verts_face { get; private set; } = new v_v3n3x2[6, 4];//lrbtaf
    public static uint[] bx_face_inds { get; private set; }

    private static vec3[] bb_planes_Zup = new vec3[8];
    private static vec3[] bb_norms_Zup = new vec3[2];
    public static v_v3n3x2[,] bb_verts_face_zup { get; private set; } = new v_v3n3x2[2, 4];//normals point +x, +z
    public static uint[] bb_face_inds_zup { get; private set; }

    private static void DoBox(float w2, float h2, float d2)
    {
      //Left Righ, Botom top, back front
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
    private static void DoBillboard(float w2, float h2, float d2)
    {
      // 2* 
      //     \ 3* --> +x (normal)
      //
      // 0*  \
      //       1*
      // \ +z
      // float x = World.BlockSizeX, = 1.0f, float y = 1.0f;
      bb_planes_Zup[0] = new vec3(w2 * 0.5f, 0, d2);
      bb_planes_Zup[1] = new vec3(w2 * 0.5f, 0, 0);
      bb_planes_Zup[2] = new vec3(w2 * 0.5f, h2, d2);
      bb_planes_Zup[3] = new vec3(w2 * 0.5f, h2, 0);

      bb_planes_Zup[4] = new vec3(0, 0, d2 * 0.5f);
      bb_planes_Zup[5] = new vec3(w2, 0, d2 * 0.5f);
      bb_planes_Zup[6] = new vec3(0, h2, d2 * 0.5f);
      bb_planes_Zup[7] = new vec3(w2, h2, d2 * 0.5f);

      bb_norms_Zup[0] = new vec3(1, 0, 0);
      bb_norms_Zup[1] = new vec3(0, 0, 1);

      //using box textures here because we won't need it in a normal case anyway.
      bb_verts_face_zup[0, 0] = new v_v3n3x2() { _v = bb_planes_Zup[0], _n = bb_norms_Zup[0], _x = bx_texs[0] };
      bb_verts_face_zup[0, 1] = new v_v3n3x2() { _v = bb_planes_Zup[1], _n = bb_norms_Zup[0], _x = bx_texs[1] };
      bb_verts_face_zup[0, 2] = new v_v3n3x2() { _v = bb_planes_Zup[2], _n = bb_norms_Zup[0], _x = bx_texs[2] };
      bb_verts_face_zup[0, 3] = new v_v3n3x2() { _v = bb_planes_Zup[3], _n = bb_norms_Zup[0], _x = bx_texs[3] };

      bb_verts_face_zup[1, 0] = new v_v3n3x2() { _v = bb_planes_Zup[4], _n = bb_norms_Zup[1], _x = bx_texs[0] };
      bb_verts_face_zup[1, 1] = new v_v3n3x2() { _v = bb_planes_Zup[5], _n = bb_norms_Zup[1], _x = bx_texs[1] };
      bb_verts_face_zup[1, 2] = new v_v3n3x2() { _v = bb_planes_Zup[6], _n = bb_norms_Zup[1], _x = bx_texs[2] };
      bb_verts_face_zup[1, 3] = new v_v3n3x2() { _v = bb_planes_Zup[7], _n = bb_norms_Zup[1], _x = bx_texs[3] };

      bool flip = false;
      bb_face_inds_zup = new uint[6] {
            0,
            (uint)(flip ? 2 : 3),
            (uint)(flip ? 3 : 2),
            0,
            (uint)(flip ? 3 : 1),
            (uint)(flip ? 1 : 3)
         };
    }
    public static void Generate()
    {
      float w2 = World.BlockSizeX, h2 = World.BlockSizeY, d2 = World.BlockSizeZ;
      DoBox(w2, h2, d2);
      DoBillboard(w2, h2, d2);
    }
  }
  public class BlockWorldVisibleStuff
  {
    public MultiMap<float, Glob> visible_globs = new MultiMap<float, Glob>();
    public Dictionary<BlockItem, SortedDictionary<float, vec3>> visible_blockitems = new Dictionary<BlockItem, SortedDictionary<float, vec3>>();
    public void Clear()
    {
      visible_blockitems.Clear();
      visible_globs.Clear();
    }
    public void Collect(Camera3D cam, Glob glob, DromeNode dn)
    {
      //Item collection
      if (visible_blockitems != null || visible_globs != null)
      {
        if (glob != null && glob.CanRender_and_HasRenderData)
        {
          float glob_dist2 = dn.Box.DistanceToCam2(cam);// (cam.PositionWorld - box_center).length2();
          if (visible_globs != null)
          {
            visible_globs.Add(glob_dist2, glob);
          }

          if (glob.BlockItems != null)
          {
            //Note: It is impossible to have duplicates in glob_blockitems
            foreach (var glob_blockitems_kvp in glob.BlockItems)
            {
              BlockItem the_item = glob_blockitems_kvp.Key;
              //Item positions are the block center (right now.. might change to block bottom)
              List<vec3> item_positions_in_glob_r3 = glob_blockitems_kvp.Value;

              //add vec4 (pos.xyz, distance)
              SortedDictionary<float, vec3> items_byDist = null;
              if (!visible_blockitems.TryGetValue(the_item, out items_byDist))
              {
                items_byDist = new SortedDictionary<float, vec3>();
                visible_blockitems.Add(the_item, items_byDist);
              }
              if (item_positions_in_glob_r3 != null)
              {
                foreach (var item_pos in item_positions_in_glob_r3)
                {
                  float dist = the_item.IsVisible(cam, item_pos);
                  if (dist > 0)
                  {
                    items_byDist.Add(dist, item_pos);
                  }
                }
              }
            }

          }
        }
      }
    }
  }
  public class DromeKernel : List<Drome>
  {
    //Asynchronous generation for globs (mesh topologies within the drome)

    private const int C27_Count = 27;
    public DromeKernel()
    {
      for (int i = 0; i < C27_Count; ++i)
      {
        Add(null);
      }
    }
    public void Set(int ddx, int ddy, int ddz, Drome d)
    {
      this[9 * ddz + 3 * ddy + ddx] = d;
    }
    public Drome Get(int ddx, int ddy, int ddz)
    {
      return this[9 * ddz + 3 * ddy + ddx];
    }
    public Drome Get(ivec3 idx)
    {
      return Get(idx.x, idx.y, idx.z);
    }
    public void Lock()
    {
      //Unlock our boys
      foreach (var d in this)
      {
        if (d != null)
        {
          d.Lock();
        }
      }
    }
    public void Unlock()
    {
      //Unlock our boys
      foreach (var d in this)
      {
        if (d != null)
        {
          d.Unlock();
        }
      }
    }
  }
  public class QueuedGlobData_WithKernel
  {
    //public Drome drome;//keep this here to prevent drome from disappearing while iterating its blocks ** this willb e in locked droems
    public Glob MyGlob;
    //C27
    //                  24 25 26
    //         15 16 17 21 22 23
    //06 07 08 12 13 14 18 19 20
    //03 04 05 09 10 11 
    //00 01 02
    // x-->  ^y /z  Center (our drome) = 13

    public DromeKernel ScalarFields = new DromeKernel();
    public List<v_v3n3x2u1> async_verts = null;
    public List<VisibleBlockFaceData> async_face_data = null;
    public List<ushort> async_inds_op = null;
    public List<ushort> async_inds_tp = null;
    public Dictionary<BlockItem, List<vec3>> async_block_items = null;
    public List<vec3> async_colors = null;

    public double DistanceToPlayer = 0;//Sort key for generating
    public ushort[] CopiedBlocks = null;//Note this is the block kernel of blocks + n

    public void CreateBuffers()
    {
      if (async_verts == null)
      {
        async_verts = new List<v_v3n3x2u1>();
      }
      if (async_inds_op == null)
      {
        async_inds_op = new List<ushort>();
      }
      if (async_inds_tp == null)
      {
        async_inds_tp = new List<ushort>();
      }
      if (async_face_data == null)
      {
        async_face_data = new List<VisibleBlockFaceData>();
      }
      if (async_colors == null)
      {
        async_colors = new List<vec3>();
      }
    }
    public void ReleaseBuffers()
    {
      async_verts?.Clear();
      async_verts = null;
      async_inds_op?.Clear();
      async_inds_op = null;
      async_inds_tp?.Clear();
      async_inds_tp = null;
      //async_face_data?.Clear(); //Note: Since we directly copy this stuff, it's not wise to clear it .. just in case we didn't set it to null
      //async_face_data = null;
      //async_block_items?.Clear();
      //async_block_items = null;
      async_colors?.Clear();
      async_colors = null;
    }
    public static int Kernel_Offset(int dx, int dy, int dz)
    {
      int off = World.GlobBlocksX_Gen_Kernel * World.GlobBlocksY_Gen_Kernel * dz +
                World.GlobBlocksX_Gen_Kernel * dy +
                dx;
      return off;
    }
    public ushort GetBlock_Kernel(int dx, int dy, int dz)
    {
      Gu.Assert(dx >= 0 && dx < World.GlobBlocksX_Gen_Kernel);
      Gu.Assert(dy >= 0 && dy < World.GlobBlocksY_Gen_Kernel);
      Gu.Assert(dz >= 0 && dz < World.GlobBlocksZ_Gen_Kernel);
      //If we are empty, then we have deleted our Block[] data to save space. Return an empty block
      int off = Kernel_Offset(dx, dy, dz);

      return CopiedBlocks[off];
    }
    public ushort GetBlock_Glob_Drome(int dx, int dy, int dz)
    {
      //I am no longer using "Missing" in the assumption this will work.
      ivec3 glocal = World.GlobGlobal_Z3_To_DromeLocal_Z3(MyGlob.Pos);

      ivec3 c27_drome_idx = new ivec3(1, 1, 1);

      int x = glocal.x * World.GlobBlocksX + dx;
      int y = glocal.y * World.GlobBlocksY + dy;
      int z = glocal.z * World.GlobBlocksZ + dz;

      //Wrap
      if (x < 0)
      {
        x = (x % World.DromeBlocksX) + World.DromeBlocksX;
        c27_drome_idx.x -= 1;
      }
      else if (x >= World.DromeBlocksX)
      {
        x = (x % World.DromeBlocksX);
        c27_drome_idx.x += 1;
      }
      if (y < 0)
      {
        y = (y % World.DromeBlocksY) + World.DromeBlocksY;
        c27_drome_idx.y -= 1;
      }
      else if (y >= World.DromeBlocksY)
      {
        y = (y % World.DromeBlocksY);
        c27_drome_idx.y += 1;
      }
      if (z < 0)
      {
        z = (z % World.DromeBlocksZ) + World.DromeBlocksZ;
        c27_drome_idx.z -= 1;
      }
      else if (z >= World.DromeBlocksZ)
      {
        z = (z % World.DromeBlocksZ);
        c27_drome_idx.z += 1;
      }

      var d = ScalarFields.Get(c27_drome_idx);

      Gu.Assert(d != null);

      if (d.Blocks.Grid == null)
      {
        return BlockItemCode.Air;
      }

      return d.Blocks.Get_Direct_Unsafe_But_Fast(x, y, z);
    }
  }
  public class QueuedDromeData
  {
    //Asynchronous generation data for dromes (scalar fields e.g. blocks as ushort)
    public Drome drome = null;
    public ivec3 gpos;
    public double DistanceToPlayer = 0;
  }
  public class Glob
  {
    //Topology units

    public Dictionary<BlockItem, List<vec3>> BlockItems = null;
    public MeshData Transparent { get; set; } = null;
    public MeshData Opaque { get; set; } = null;

    public WeakReference<Drome> Drome { get; private set; } = null;
    public ivec3 Pos { get; private set; } = new ivec3(0, 0, 0);
    public Int64 GeneratedFrameStamp { get; private set; } = 0;
    public GenState State { get; set; } = GenState.Created;
    public List<VisibleBlockFaceData> VisibleFaceData = null;
    public long GpuFaceColors_UpdateStamp = 0;

    public static int dbg_ncalc = 0;

    public object lock_object = new object();
    public bool CanRender_and_HasRenderData
    {
      get
      {
        //Don't check for genstate here. If you check against genstate then you will get flickering as the glob is being updated.
        //meshes are updated synchronously anyway, so it's alright if this glob data is being generated.
        bool canrender = (Transparent != null) || (Opaque != null);
        return canrender;
      }
    }
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
    public ivec3 DromeLocalZ3
    {
      get
      {
        return World.GlobGlobal_Z3_To_DromeLocal_Z3(Pos);
      }
    }
    public Glob(ivec3 pos, Int64 genframeStamp, Drome drom)
    {
      Pos = pos;
      GeneratedFrameStamp = genframeStamp;
      Drome = new WeakReference<Drome>(drom);
    }
  }
  public class DromeNode
  {
    public DromeNode[] Children = null; //octree .. 
    private bool[] ChildWasProcessed = new bool[8] { false, false, false, false, false, false, false, false }; //this could be just a byte but man it sucks doing bit shit in c number
    protected Box3f _box;
    public bool IsLeaf = false;
    public bool IsStaticHierarchy = false; //Static leaf box block
    public Glob Glob = null;
    public bool IsDirty = true;
    public long LastVisible_ms = 0; //Last time this glob was visible.
    public static bool IsVisible(World w, Camera3D cam, Box3f box)
    {
      float dist_cam2 = box.DistanceToCam2(cam);
      bool vis = cam.Frustum.HasBox(box) && (dist_cam2 < (w.RenderDistance * w.RenderDistance));
      return vis;
    }
    public Box3f Box
    {
      get
      {
        return _box;
      }
    }
    public vec3 OriginR3 { get { return _box._min; } }
    public void Subdivide_Static_Hierarchy(bool isroot)
    {
      IsStaticHierarchy = true;

      //treat this as a static glob that we subdivide until we hit blocks. This is used for raycasting.
      if (isroot)
      {
        _box = new Box3f(new vec3(0, 0, 0), new vec3(World.GlobWidthX, World.GlobWidthY, World.GlobWidthZ));
      }

      if ((Box.Width() < World.BlockSizeX + 0.01f) &&
        (Box.Height() < World.BlockSizeY + 0.01f) &&
        (Box.Depth() < World.BlockSizeZ + 0.01f))
      {
        IsLeaf = true;
      }
      else
      {
        Children = new DromeNode[8];
        Box3f[] cbox = Box.Divide();
        for (int ci = 0; ci < 8; ci++)
        {
          Children[ci] = new DromeNode();
          Children[ci]._box = cbox[ci];
          Children[ci].Subdivide_Static_Hierarchy(false);
        }
      }
    }
    private int CheckLeaf(Drome root)
    {
      //check if leaf
      //also check region state if we are. 
      if (
          (Box.Width() < World.GlobWidthX + 0.01f) &&
          (Box.Height() < World.GlobWidthY + 0.01f) &&
          (Box.Depth() < World.GlobWidthZ + 0.01f))
      {
        //So, we either did, or didn't divide the child.

        var state = root.GetRegionStateForDromeNodeLeaf(this);
        if (state.IsEmpty /*|| rs.IsSolid*/)//.. ?Makes no sesne for solid as cubers generate their own topology.
        {
          root.dbg_nCount_Empty_Leaves++;

          return 8; //Parent culls any nodes that return 8.
        }
        IsLeaf = true;
        root.dbg_nCountLeaves++;
      }
      return 0;
    }

    public int DoLiterallyEverything(World w, Drome root, Camera3D cam, BlockWorldVisibleStuff stuff = null, ModifiedBlock mb = null)
    {
      //What this does:
      // subdivide drome into node hierarchy
      // create globs (world meshes)
      // edit globs
      // delete globs
      // delete node hierarchy
      // collect visible objects visible globs for render

      Gu.Assert((w != null) && (root != null) && (cam != null));

      int nculled = 0;

      //return the number of children that are empty or, have no topology
      Gu.Assert(root != null);

      LastVisible_ms = Gu.Milliseconds();

      if (IsLeaf)
      {
        //Note - We already checked for region state when CREATING this leaf - so if we get here
        //it means we have a non-empty region state and this leaf does have visible data. (however we dont' check for world objects here)
        if ((mb != null) || (Glob == null))
        {
          vec3 box_center = _box.center();
          ivec3 gpos = World.R3toI3Glob(box_center);
          Glob = w.QueueGlob(Glob, root, gpos);
        }
        else if ((Glob != null) && (Glob.State == GenState.Ready) && (stuff != null))
        {
          //We have a glob and we are collecting stuff to render
          stuff.Collect(cam, Glob, this);
        }

        return 0;
      }
      else
      {
        //Create or recreate missing children
        for (int ci = 0; ci < 8; ci++)
        {
          var box = ((Children != null) && (Children[ci] != null)) ? Children[ci].Box : Box.GetDivisionChild(ci);

          bool must_process = false;
          if (mb == null)
          {

            //visible
            if (DromeNode.IsVisible(w, cam, box))
            {
              must_process = true;
            }
          }
          else if (mb != null)
          {
            //dividing by a modified block / point. Node may not be visible but we still divide.
            if (Children != null && Children[ci] != null)
            {
              must_process = box.containsPointBottomLeftInclusive(mb.Pos);
            }
            else
            {
              must_process = box.containsPointBottomLeftInclusive(mb.Pos);
            }
          }

          if (must_process)
          {
            ChildWasProcessed[ci] = true;

            if (Children == null)
            {
              Children = new DromeNode[8] { null, null, null, null, null, null, null, null };
            }

            int num_culled_child = 0;

            if (Children[ci] == null)
            {
              Children[ci] = new DromeNode();
              Children[ci]._box = Box.GetDivisionChild(ci);
              //** Check the region state, exit if it is empty
              num_culled_child = Children[ci].CheckLeaf(root);
              if (num_culled_child == 8)
              {
                Children[ci] = null;
              }
            }

            if (Children[ci] != null)
            {
              num_culled_child = Children[ci].DoLiterallyEverything(w, root, cam, stuff, mb);
            }

            //cull empty child
            if (num_culled_child == 8)
            {
              Children[ci] = null;
              root.dbg_nCountCulled++;
            }
          }

          if (Children == null || Children[ci] == null)
          {
            nculled++;
          }
        }

        //We culled all the kids - set children to  null. and get ourselves culled.
        if (nculled == 8)
        {
          Children = null;
        }
      }

      //Delete check.. Make sure the Gen  < delete distance or we gen/delete over and over..
      if (w.Drome_or_Node_Can_Delete_Distance(_box) && ((Gu.Milliseconds() - LastVisible_ms) > World.Abandon_DeleteTime_DromeNode_ms))
      {
        //Delete if we are invisible
        nculled = 8;
      }
      if (this == root)
      {
        //Do not cull dromes here. We cull them outside here
        nculled = 0;
      }

      return nculled;
    }
    private static int dbg_int_ncalls = 0;
    public void RaycastBlockBVH(Drome root, PickRay3D pr, ref PickedBlock pb, vec3? pg_origin = null)
    {
      //Note this also works if you create a point ray.
      if (this == root)
      {
        dbg_int_ncalls = 0;
      }
      dbg_int_ncalls++;
      if (pb.AddPickedBlockBoxes_Debug)
      {
        //  Gu.Log.WarnCycle("Picked boxces debug is enabled. disable / delete this later.");
        if (pb.PickedBlockBoxes_Debug == null)
        {
          pb.PickedBlockBoxes_Debug = new List<Box3f>();
        }
        pb.PickedBlockBoxes_Debug.Add(Box);
      }
      if (IsStaticHierarchy == true)
      {
        if (IsLeaf == true)
        {
          //we are a block
          Gu.Assert(pg_origin != null);

          var cpos_r3 = pg_origin.Value + _box.center();
          ivec3 b3i = Drome.R3toI3BlockLocal_Drome(cpos_r3);
          ushort b = root.GetBlock(b3i);

          if (pb.AddPickedBlockBoxes_Debug)
          {
            //  Gu.Log.WarnCycle("Picked boxces debug is enabled. disable / delete this later.");
            if (pb.PickedBlockBoxes_Debug == null)
            {
              pb.PickedBlockBoxes_Debug = new List<Box3f>();
            }
            pb.PickedBlockBoxes_Debug.Add(World.GetBlockBoxGlobalR3(cpos_r3));
          }

          if (!Block.HasNoSolid(b) && (!pr.IsPlayer || (pr.IsPlayer && Block.CollidePlayer(b))))
          {
            BoxAAHit bh = new BoxAAHit();
            //note pr is currently translated into static glob space
            if (_box.LineOrRayIntersectInclusive_EasyOut(pr, ref bh))
            {
              if (bh.IsHit && (bh._t1 < pb._t1))
              {
                pb._t1 = (float)bh._t1;
                pb._t2 = (float)bh._t2;

                pb.Drome = root;
                pb.Block = b;
                pb.BlockPosLocalZ3 = b3i;
                vec3 hp_static_glob = pr.Origin + pr.Dir * (float)bh._t1;
                //This is the static glob at 0,0,0, add the minimum of the parent to translate the box into the actual coordinates.
                pb.HitPosR3 = pg_origin.Value + hp_static_glob;
                pb.BlockCenterR3 = cpos_r3;
                //pb.HitNormal_Ray = -pr.Dir;
                //Get normal for transformed point
                //pb.HitNormal = _box.Normal_PlaneOnly(hp_static_glob, pr.Dir);
                //  var test_r3 = new Box3f(_box._min + pg_origin.Value, _box._max + pg_origin.Value);
                //  var test_normal_r3 = test_r3.Normal_PlaneOnly(pb.HitPosR3, pr.Dir);
                pb.RaycastResult = bh.RaycastResult;
                return;
              }
            }
          }

        }
      }
      else
      {
        if (IsLeaf == true)
        {
          //Translate the ray relative to the static "block box" that has a hierarchy
          //of box nodes at the sub-glob level. Then raycast into this box until we reach a block.
          PickRay3D pr_translated = new PickRay3D(pr.Origin - _box._min, pr.Dir, pr.Radius);

          Drome.Static_Hierarchy.RaycastBlockBVH(root, pr_translated, ref pb, _box._min);
        }
      }

      if (Children != null)
      {
        BoxAAHit bh = new BoxAAHit();
        foreach (var c in this.Children)
        {
          if (c != null)
          {
            if (c.Box.LineOrRayIntersectInclusive_EasyOut(pr, ref bh))
            {
              if (bh.IsHit)
              {
                //Don't set t here, the ray is inside the boxes most of hte time.
                c.RaycastBlockBVH(root, pr, ref pb, pg_origin);
              }
            }
          }
        }
      }



    }//RaycastBlockBVH

  }
  public class ModifiedBlock
  {
    public vec3 Pos { get; }
    public ivec3 IPos { get; }
    public ushort OldBlock { get; } //BlockItemcode //these arent used
    public ushort NewBlock { get; } //BlockItemcode
    public ModifiedBlock(vec3 p, ushort oldblock, ushort newBLock)
    {
      Pos = p;
      OldBlock = oldblock;
      NewBlock = newBLock;
    }
  }
  public class BlockStats
  {
    //A more sophisticated RegionState for dromes.
    //We need to know via the generator how many plagio, wood, etc exist in the block, ot generate in it

    public Dictionary<ushort, int> BlockCounts = new Dictionary<ushort, int>();
    public BlockStats()
    {
      //All blocks are air initially.
      BlockCounts.Add(Drome.InitialBlockValue, Drome.DromeBlockCount);
    }
    public RegionState RegionState
    {
      get
      {
        //Gets whether we are solid / partial / empty
        RegionState ret = RegionState.Partial;
        Gu.Assert(BlockCounts.Keys.Count > 0);//This should be impossible.
        if (BlockCounts.Keys.Count > 1)
        {
          ret = RegionState.Partial;
        }
        else
        {
          ushort k = BlockCounts.Keys.First();
          if (Block.IsVisibleBlock(k))
          {
            ret = RegionState.VisibleBlocksOnly;
          }
          else
          {
            ret = RegionState.Empty_AndNoData;
          }
        }
        return ret;
      }
    }
    public void UpdateBlockModified(ushort oldb, ushort newb)
    {
      ushort old_state = Block.GetVisibleBlockCode(oldb);
      ushort new_state = Block.GetVisibleBlockCode(newb);

      if (!BlockCounts.ContainsKey(new_state))
      {
        BlockCounts.Add(new_state, 0);
      }
      BlockCounts[new_state] += 1;
      BlockCounts[old_state] -= 1;
      if (BlockCounts[old_state] <= 0)
      {
        BlockCounts.Remove(old_state);
      }

    }
    public void Serialize(BinaryWriter br)
    {
      br.Write((int)BlockCounts.Keys.Count);
      foreach (var c in BlockCounts)
      {
        br.Write((UInt16)c.Key);
        br.Write((int)c.Value);
      }
    }
    public void Deserialize(BinaryReader br)
    {
      BlockCounts.Clear();
      int bcount = br.ReadInt32();
      for (int ci = 0; ci < bcount; ci++)
      {
        ushort code = br.ReadUInt16();
        int count = br.ReadInt32();

        BlockCounts.Add(code, count);
      }
    }
  }
  public class Drome : DromeNode
  {
    //Density / Block units / BVH Root

    //public BeamGrid2 BeamGrid2 = null;
    //public Grid3D<GRay> LightGrid = null;
    public Grid3D<ushort> Blocks = new Grid3D<ushort>(World.DromeBlocksX, World.DromeBlocksY, World.DromeBlocksZ);

    public int _lock = 0;
    public bool Locked { get { return _lock > 0; } }
    public void Lock() { _lock++; }
    public void Unlock() { _lock--; }

    private WeakReference<World> _world = null;
    public BlockStats BlockStats = new BlockStats();
    public RegionBlocks[] GlobRegionStates = null;
    public ivec3 Pos = new ivec3(0, 0, 0);
    public GenState GenState = GenState.Created; //Note C# integral types are atomic.
    public static DromeNode Static_Hierarchy = null;

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
        return OriginR3 + new vec3(World.DromeWidthX * 0.5f,
                                   World.DromeWidthY * 0.5f,
                                   World.DromeWidthZ * 0.5f);
      }
    }
    public int dbg_nCount_Empty_Leaves = 0;
    public int dbg_nCountLeaves = 0;
    public int dbg_nCountProcessed_Children = 0;
    public int dbg_nCountCulled = 0;
    public int dbg_nCountHave = 0;

    public static Box3f GetDromeBox(ivec3 pos)
    {
      return new Box3f(
        new vec3(
          pos.x * World.DromeWidthX,
          pos.y * World.DromeWidthY,
          pos.z * World.DromeWidthZ),
        new vec3(
          (pos.x + 1) * World.DromeWidthX,
          (pos.y + 1) * World.DromeWidthY,
          (pos.z + 1) * World.DromeWidthZ
        )
        );
    }
    public Drome(World w, ivec3 pos, Int64 genframeStamp)
    {
      Pos = pos;
      _world = new WeakReference<World>(w);

      _box = GetDromeBox(pos);

      if (Static_Hierarchy == null)
      {
        Static_Hierarchy = new DromeNode();
        Static_Hierarchy.Subdivide_Static_Hierarchy(true);
      }
    }
    public RegionBlocks GetRegionStateForDromeNodeLeaf(DromeNode d)
    {
      //We do not have any region states if we are an empty Drome.
      //REturn the default "none" to signal to the system that we can cull everything.
      if (GlobRegionStates == null)
      {
        return RegionBlocks.EmptyRegionState_Glob;
      }
      vec3 c = d.Box._min + (d.Box._max - d.Box._min) * 0.5f;
      ivec3 gpos_global = World.R3toI3Glob(c);
      ivec3 gpos_local = World.GlobGlobal_Z3_To_DromeLocal_Z3(gpos_global);
      //we are a glob
      var off = Drome.RegionStateOffset_FromLocalGlobPos(gpos_local);
      Gu.Assert(off < Drome.DromeRegionStateCount);
      var rs = GlobRegionStates[off];
      return rs;
    }
    public static int BlockOffset(ivec3 local_block_pos_in_drome)
    {
      int ret = World.DromeBlocksX * World.DromeBlocksY * local_block_pos_in_drome.z +
        World.DromeBlocksX * local_block_pos_in_drome.y +
        local_block_pos_in_drome.x;
      return ret;
    }
    public static int BlockOffset(int local_x, int local_y, int local_z)
    {
      int ret = World.DromeBlocksX * World.DromeBlocksY * local_z +
        World.DromeBlocksX * local_y +
        local_x;
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
    public static ivec3 R3toI3BlockLocal_Drome(vec3 R3)
    {
      ivec3 bpos = World.R3ToI3BlockLocal_Any(R3, World.DromeWidthX, World.DromeWidthY, World.DromeWidthZ);
      if (bpos.x < 0 || bpos.y < 0 || bpos.z < 0 || bpos.x >= World.DromeBlocksX || bpos.y >= World.DromeBlocksY || bpos.z >= World.DromeBlocksZ)
      {
        Gu.DebugBreak();
      }
      return bpos;
    }
    public void Create_or_UpdateGlobForModifiedBlock(ModifiedBlock mb)
    {
      if (_world.TryGetTarget(out var w))
      {
        //Basically what this does is we drill down to the glob that was modified and create it.
        //Then, the visibility update should catch the new glob and generate it.
        DoLiterallyEverything(w, this, w.Camera, null, mb);
        //**TODO: subdivide neighbors as as well when this block borders other globs.
      }
    }
    public void DeleteGlobForModifiedBlock(ModifiedBlock mb)
    {
      if (_world.TryGetTarget(out var w))
      {
        //Same as above - we do a subdivision again. When we drill down to the given point, we'll determine if the glob is visible, or not.
        //if it's not, it will get deleted automatically.
        DoLiterallyEverything(w, this, w.Camera, null, mb);
        //**TODO: subdivide neighbors as as well when this block borders other globs.
      }
    }
    public const ushort InitialBlockValue = BlockItemCode.Air;
    public const int DromeBlockCount = World.DromeBlocksX * World.DromeBlocksY * World.DromeBlocksZ;
    public const int DromeRegionStateCount = World.DromeGlobsX * World.DromeGlobsY * World.DromeGlobsZ;
    public void AllocateBlocks()
    {
      Blocks.Allocate(Drome.InitialBlockValue);
    }
    public void AllocateRegionStates()
    {
      GlobRegionStates = new RegionBlocks[Drome.DromeRegionStateCount];
      //foreach(var g in GlobRegionStates)
      for (int i = 0; i < GlobRegionStates.Length; i++)
      {
        GlobRegionStates[i].Init();
      }
    }
    public bool HasBlockData()
    {
      return Blocks.Grid != null;
    }
    public void SetBlock(ivec3 local_block_pos_in_drome, ushort block_solidcode_liquid_or_both, bool bInitialGen_Dont_DivideGlob, BlockBits bits)
    {
      //Sets the actual block data code - does not account for what is in the block i.e. solid/liquid.. sets the whole code
      //dontdivideglob - if this is false then we won't run the division routine to create a BVH, and queue affected nodes for update, set InitialGen to true when mining blocks 

      //We may be empty, in which case we need to reallocate our data.
      if (!HasBlockData())
      {
        //We cull blocks from empty globs to save memory.
        AllocateBlocks();
        AllocateRegionStates();
      }

      ushort old = GetBlock(local_block_pos_in_drome);
      // ushort old_solid = Block.GetSolid(old);
      // ushort old_liquid = Block.GetLiquid(old);

      ushort block = old;
      Block.SetBlock(ref block, block_solidcode_liquid_or_both, bits);

      if (bits == BlockBits.Liquid)
      {
        int n = 0;
        n++;
      }

      Blocks.Set(local_block_pos_in_drome, block);

      //See comments on drome.
      BlockStats.UpdateBlockModified(old, block);
      GlobRegionStates[Drome.RegionStateOffset_FromLocalBlockPos(local_block_pos_in_drome)].UpdateBlockModified(old, block);

      if (!bInitialGen_Dont_DivideGlob)
      {

        int region_off = Drome.RegionStateOffset_FromLocalBlockPos(local_block_pos_in_drome);

        bool solid_before = GlobRegionStates[region_off].HasValues;
        bool empty_before = GlobRegionStates[region_off].IsEmpty;

        GlobRegionStates[region_off].UpdateBlockModified(old, block);

        bool solid_after = GlobRegionStates[region_off].HasValues;
        bool empty_after = GlobRegionStates[region_off].IsEmpty;

        vec3 block_pos_r3 = OriginR3 + new vec3(
            (float)local_block_pos_in_drome.x * World.BlockSizeX,
            (float)local_block_pos_in_drome.y * World.BlockSizeY,
            (float)local_block_pos_in_drome.z * World.BlockSizeZ
            );

        ModifiedBlock mb = new ModifiedBlock(block_pos_r3, old, block);
        if (!empty_before && empty_after)
        {
          DeleteGlobForModifiedBlock(mb);
        }
        else
        {
          //we are solid
          Create_or_UpdateGlobForModifiedBlock(mb);
        }

      }
    }
    public ushort GetBlock(ivec3 local_pos_drome, IndexMode im = IndexMode.Throw)
    {
      //If we are empty, then we have deleted our Block[] data to save space. Return an empty block
      if (BlockStats.RegionState == RegionState.Empty_AndNoData)
      {
        return BlockItemCode.Air;
      }
      var b = Blocks.Get(local_pos_drome, im);
      return b;
    }
    public ushort GetBlock(int local_x, int local_y, int local_z)
    {
      //If we are empty, then we have deleted our Block[] data to save space. Return an empty block
      if (BlockStats.RegionState == RegionState.Empty_AndNoData)
      {
        return BlockItemCode.Air;
      }
      return Blocks.Get(local_x, local_y, local_z, IndexMode.Throw);
    }
    public void SetBlock(vec3 pos_r3, ushort solid_liquid_both, BlockBits bits)
    {
      ivec3 b_pos = R3toI3BlockLocal_Drome(pos_r3);
      SetBlock(b_pos, solid_liquid_both, false, bits);
    }


  }//class Drome
  public abstract class Walker
  {
    public vec3 StartPosR3; //start point from where we walked.
                            // public vec3 MovementR3; //How much we move
    float MaxDist2_R3 = float.MaxValue; // Maximum distance in R3
    public ivec3 PosZ3;
    public int MaxSteps;
    public int CurStep = 0;
    public ushort BlockCode;
    public bool Dead = false;
    public List<ushort> ExistingBlocksOnly;//Missing = Any block, Air = air only, Value = Any non-air block, else - only the given block
                                           //Minimax<int> Steps;
    private Minimax<int> Size; //size of hole

    private Drome drome;
    public vec3 PosR3Center()
    {
      vec3 p = World.Z3BlockInDromeLocal_To_R3(drome.OriginR3, PosZ3)
        + new vec3((float)World.BlockSizeX * 0.5f, (float)World.BlockSizeY * 0.5f, (float)World.BlockSizeZ * 0.5f);
      return p;
    }
    public virtual bool Move()
    {
      if (!Dead)
      {
        float dist = (PosR3Center() - StartPosR3).length2();
        if (dist > MaxDist2_R3)
        {
          Dead = true;
        }
        else
        {
          CurStep++;
          if (CurStep >= MaxSteps)
          {
            Dead = true;
          }
        }
      }
      return Dead;
    }
    public virtual void Carve(World w, Drome d)
    {
      //int siz = Random.Next(Size);
      for (int zi = Size.Min; zi < Size.Max; zi++)
      {
        for (int yi = Size.Min; yi < Size.Max; yi++)
        {
          for (int xi = Size.Min; xi < Size.Max; xi++)
          {
            ivec3 vp = PosZ3 + new ivec3(xi, yi, zi);
            if (w.IsBlockInsideDromeBounds(vp))
            {
              ushort cur_block = d.GetBlock(vp);
              bool yes_we_can =
               (ExistingBlocksOnly.Contains(BlockItemCode.Missing)) || // any block
               ((ExistingBlocksOnly.Contains(BlockItemCode.Land)) && (cur_block != BlockItemCode.Air && (Block.GetLiquid(cur_block) == 0))) || //Only solid blocks
               ((ExistingBlocksOnly.Contains(BlockItemCode.Water)) && (cur_block != BlockItemCode.Air && cur_block != BlockItemCode.Land)) || //Only water blocks
               (ExistingBlocksOnly.Contains(cur_block)); // only the given blocks.


              if (yes_we_can)
              {
                d.SetBlock(vp, BlockCode, true, BlockBits.Solid);
              }
            }

          }
        }
      }

    }
    public Walker(Drome d, Minimax<int> steps, ivec3 start_pos_local, ushort blockcode, float max_dist2_r3, List<ushort> existing, Minimax<int> size)
    {
      Gu.Assert(d != null);
      drome = d;
      //Steps = steps;
      MaxSteps = Random.Next(steps);
      BlockCode = blockcode;
      PosZ3 = start_pos_local;
      StartPosR3 = PosR3Center();
      MaxDist2_R3 = max_dist2_r3;
      ExistingBlocksOnly = existing;
      Size = size;
    }
  }
  public class SnakeWalker : Walker
  {
    //This is cool for snake - like movement
    public vec3 Direction;
    public SnakeWalker(Drome d, Minimax<int> steps, ivec3 start_pos_local, vec3 direction_normal, ushort block, float max_dist2_r3, List<ushort> existing, Minimax<int> size) :
      base(d, steps, start_pos_local, block, max_dist2_r3, existing, size)
    {
      Direction = direction_normal;
    }
    public override bool Move()
    {
      int move_blocks = 1;//blocks
                          //Move this guy statistically in the direction of his normal

      float dx = Math.Abs(Random.NextF() * Direction.x);
      float dy = Math.Abs(Random.NextF() * Direction.y);
      float dz = Math.Abs(Random.NextF() * Direction.z);
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
      return base.Move();
    }
  }
  public class RandomWalker : Walker
  {
    public Minimax<vec3> MovementProbability;

    public RandomWalker(Drome d, Minimax<int> steps, ivec3 start_pos_local, Minimax<vec3> probability, ushort blockcode, float max_dist2_r3, List<ushort> existing, Minimax<int> size) :
      base(d, steps, start_pos_local, blockcode, max_dist2_r3, existing, size)
    {
      //max_dist2_r3 = the maximum distance SQUARED, in R3. Maxvalue means the walker can go forever (based on step count)
      MovementProbability = probability;
    }
    public override bool Move()
    {
      int move_blocks = 1;//blocks
                          //Move this guy statistically in the direction of his normal

      vec3 rxyz = Random.Next(MovementProbability);

      float dx = Math.Abs(rxyz.x);
      float dy = Math.Abs(rxyz.y);
      float dz = Math.Abs(rxyz.z);

      if (dx >= dy && dx >= dz)
      {
        PosZ3.x += move_blocks * Math.Sign(rxyz.x);
      }
      else if (dy >= dx && dy >= dz)
      {
        PosZ3.y += move_blocks * Math.Sign(rxyz.y);
      }
      else
      {
        PosZ3.z += move_blocks * Math.Sign(rxyz.z);
      }
      return base.Move();
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
  public class BlockFaceInfo
  {
    public FileLoc Image { get; private set; }
    public MtTex UV { get; set; }
    public TileVis Visibility { get; private set; }
    public BlockFaceInfo(FileLoc loc, TileVis vis)
    {
      Image = loc;
      Visibility = vis;
    }
  }
  public class BlockTile
  {
    //Provides the visible information for a block. Images. Mesh type. Visibility.
    public const float BlockOpacity_Solid = 1.0f;
    public const float BlockOpacity_Billboard = 0.5f;
    public const float BlockOpacity_Liquid = 0.07f;
    public const float BlockOpacity_Transparent = 0.0f;
    public ushort Code { get; private set; } = 0;
    public BlockFaceInfo[] FaceInfos { get; private set; } = new BlockFaceInfo[3];//top / side / bot
                                                                                  //TODO: variations
    public bool IsVisible() { return Opacity > 0 && Opacity < 1; }
    //For now we just have tile for this block, in the futre we can add dictionary<ushort, BFI[]> for changing block image on any type of block 
    public BlockFaceInfo[] Growth_Infos_Side { get; set; } = null;//Growth info for growing plants. <block type, top/side/bot faces> tpChanges to the Mid face based on whether this block is on, top of the same, on side of same, or on bot of same
    public Minimax<int> GrowthHeight = new Minimax<int>(1, 1);
    public float MineTime_Pickaxe { get; private set; } = 4;
    public BlockMeshType MeshType { get; private set; } = BlockMeshType.Block;
    public WorldObject Entity { get; private set; } = null;
    public bool IsChainedPlant { get; private set; } = false;
    public float Opacity { get; private set; } = BlockOpacity_Transparent;
    public BlockTile(ushort code, BlockFaceInfo[] faces, float hardness_pickaxe, BlockMeshType meshType, bool is_chained, float opacity)
    {
      Gu.Assert(faces.Length == 3);
      Code = code;
      FaceInfos = faces;
      MineTime_Pickaxe = hardness_pickaxe;
      MeshType = meshType;
      IsChainedPlant = is_chained; //TODO: make this into an enum or more general structure for block destroy/create
      Opacity = opacity;
    }
    public MtTex[] GetUVPatch(BlockSide faceIdx, ushort b_above, ushort b_below)
    {
      //Above / Below = this is used to grow blocks like grass -- Growth_Infos_Side
      //int he future, of course this would be a kernel.
      if (FaceInfos == null)
      {
        return null;
      }
      Gu.Assert(FaceInfos.Length == 3);

      MtTex side = FaceInfos[1].UV;

      if ((faceIdx == BlockSide.Left || faceIdx == BlockSide.Right || faceIdx == BlockSide.Back || faceIdx == BlockSide.Front) && (Growth_Infos_Side != null))
      {
        Gu.Assert(Growth_Infos_Side.Length == 3);
        //lrbt
        if (b_above == Code && b_below != Code)
        {
          //bot
          side = Growth_Infos_Side[0].UV;
        }
        else if (b_above == Code && b_below == Code)
        {
          //mid
          side = Growth_Infos_Side[1].UV;
        }
        else if (b_above != Code && b_below == Code)
        {
          //top
          side = Growth_Infos_Side[2].UV;
        }
        else
        {
          //Single block, now growth info - fall through to default block
        }
      }

      Gu.Assert(side != null);

      return new MtTex[]
      {
        FaceInfos[0].UV,
        side,
        FaceInfos[2].UV
      };
    }
    static Material EntityMaterial = null;
    public void DefineEntity(Texture2D albedo, Texture2D normal)
    {
      //Only create entity when we have defined the textures
      Gu.Assert(FaceInfos != null);
      Entity = new WorldObject("entity");
      MeshData md = null;

      if (MeshType == BlockMeshType.Block)
      {
        float size = 0.25142f;
        Gu.Assert(FaceInfos[BlockTileUVSide.Top].UV != null);
        Gu.Assert(FaceInfos[BlockTileUVSide.Side].UV != null);
        Gu.Assert(FaceInfos[BlockTileUVSide.Bottom].UV != null);

        var t0 = FaceInfos[BlockTileUVSide.Top].UV.GetQuadTexs();
        var t1 = FaceInfos[BlockTileUVSide.Side].UV.GetQuadTexs();
        var t2 = FaceInfos[BlockTileUVSide.Bottom].UV.GetQuadTexs();

        md = MeshData.GenBox(World.BlockSizeX, World.BlockSizeY, World.BlockSizeZ, t0, t1, t2);
        Entity.Scale_Local = new vec3(size, size, size);
      }
      else if (MeshType == BlockMeshType.Billboard)
      {
        float size = 0.39142f;
        Gu.Assert(FaceInfos[BlockTileUVSide.Side].UV != null);
        var t1 = FaceInfos[BlockTileUVSide.Side].UV.GetQuadTexs();
        md = MeshData.GenPlane(World.BlockSizeX, World.BlockSizeY, t1);
        Entity.Rotation_Local = quat.fromAxisAngle(new vec3(1, 0, 0), -MathUtils.M_PI_2, true);//rotate quad so it is upright
        Entity.Scale_Local = new vec3(size, size, size);
      }
      else if (MeshType == BlockMeshType.Liquid)
      {
      }
      else
      {
        // Do nothing
        Gu.BRThrowNotImplementedException();
      }
      if (EntityMaterial == null)
      {
        EntityMaterial = Material.DefaultDiffuse().Clone();
        EntityMaterial.Textures.Clear();
        EntityMaterial.Textures.Add(Shader.TextureInput.Albedo, albedo);
        EntityMaterial.Textures.Add(Shader.TextureInput.Normal, normal);
        EntityMaterial.GpuRenderState.CullFace = false;
      }
      Entity.Mesh = md;
      Entity.Material = EntityMaterial;
      Entity.Collides = true;
      Entity.HasPhysics = true;
      Entity.HasGravity = true;
      vec3 axis = new vec3(0, 1, 0);
      Entity.OnAddedToScene = (self) =>
      {
        var ec = new EventComponent((self) =>
        {
          self.Destroy();
        }, World.DropDestroyTime_Seconds, false);
        ec.Start();
        self.Components.Add(ec);
      };
      float animationTime = 5.0f; //seconds

      float h = World.BlockSizeY * 0.1f;
      List<Keyframe> keys = new List<Keyframe>();
      keys.Add(new Keyframe(0.0f / 4.0f * animationTime, quat.fromAxisAngle(axis, 0), KeyframeInterpolation.Ease, new vec3(0, h * 0.0f, 0), KeyframeInterpolation.Ease));
      keys.Add(new Keyframe(2.0f / 4.0f * animationTime, quat.fromAxisAngle(axis, MathUtils.M_PI - 0.001f), KeyframeInterpolation.Ease, new vec3(0, h * 1.0f, 0), KeyframeInterpolation.Ease));
      keys.Add(new Keyframe(4.0f / 4.0f * animationTime, quat.fromAxisAngle(axis, MathUtils.M_PI * 2.0f - 0.001f), KeyframeInterpolation.Ease, new vec3(0, h * 0.0f, 0), KeyframeInterpolation.Ease));

      var ac = new AnimationComponent(keys, true);
      Entity.Components.Add(ac);
      ac.Play();
    }

  }
  public class BlockItem
  {
    //This is a mesh object for blocks. 
    public FileLoc FileLoc { get; private set; }
    public WorldObject WorldObject { get; private set; }
    public float IsVisible(Camera3D cam, vec3 instance_pos)
    {
      //Check if block item bound box is within frust.
      //NOTE - its within frustum and not within .. 
      //Returns >=0 if the bound box is within the frustum, as the distance to the center of the bb.
      //return <0 if bound box is outside frustum

      //OK so .. blockobject instances need to also be updated .. 
      // .. in that sense .. technikcally .. we need to have instances be worldobjects and share the mesh data.
      //however .. no i don't want to do that. We have one object that we udated .. 

      Box3f bi_box = new Box3f(instance_pos + WorldObject.BoundBox._min, instance_pos + WorldObject.BoundBox._max);
      if (cam.Frustum.HasBox(bi_box))
      {
        //**TODO: instead of BB center we can return BB min value.
        float fDist2 = bi_box.DistanceToCam2(cam);
        return fDist2;
      }
      return -1.0f;
    }
    public BlockItem(ushort code, FileLoc loc, vec3 scale)
    {
      FileLoc = loc;
      var objs = Gu.Resources.LoadObjects(loc);
      WorldObject = objs[0];

      WorldObject.Scale_Local = scale;
      WorldObject.Position_Local = new vec3(0, -World.BlockSizeY * 0.5f, 0); // Move the object to the base of the block.

      WorldObject.Iterate((WorldObject o) =>
      {
        o.Material = World.BlockObjectMaterial;
      });
    }
  }
  public class DayNightCycle
  {
    //                  + Noon
    // 
    // Dusk +                        + Dawn    < sun direction
    //
    //                  + Midngiht

    private double DayLengthSeconds = 60;// 60.0f * 5.0f;
    private double NightLengthSeconds = 60;//60.0f * 5.0f;

    public double StarOrCloud_Blend { get; private set; } = 0; //1 = day,  -1 = night

    public double DayTime_Seconds { get; private set; } = 0;// Time in seconds
    public double DayLength_Seconds { get { return DayLengthSeconds + NightLengthSeconds; } }
    public dvec3 MoonDir { get; private set; } = new dvec3(0, 0, 1);//Direction of moon towards earth or inverse of sun
    public dvec3 ActiveLightDir { get; private set; } = new dvec3(0, 0, 1);//Depending on whether it is night, or not this is the direction of sun / moon towards earth
    public const float SkyRadius = 400.0f;

    public float DayQuad0 { get; private set; } = 0;
    public float DayQuad1 { get; private set; } = 1;

    public dvec3 SkyColor;
    public dvec3 LightColor;
    //https://yorktown.cbe.wwu.edu/sandvig/shared/NetColors.aspx
    private dvec3 Sky_NoonColor = new dvec3(vec4.FromHex("#FAFAD200").xyz());//Goldenrod
    private dvec3 Sky_DuskColor = new dvec3(vec4.FromHex("#FF450000").xyz());//Gold FFD70000
    private dvec3 Sky_DawnColor = new dvec3(vec4.FromHex("#FF450000").xyz());//Gold
    private dvec3 Sky_MidnightColor = new dvec3(vec4.FromHex("#16163000").xyz());//Midnightblue

    private dvec3 Light_NoonColor = new dvec3(vec4.FromHex("#FAFAD200").xyz());//light goldenrod
    private dvec3 Light_MidnightColor = new dvec3(vec4.FromHex("#E6E6FA00").xyz()); //a very faint lavender blue
    private dvec3 Light_DuskColor { get { return (Light_NoonColor + Light_MidnightColor) * 0.5; } }//Gold FFD70000
    private dvec3 Light_DawnColor { get { return Light_DuskColor; } }//orangered

    public bool IsDay
    {
      get
      {
        return DayTime_Seconds < DayLengthSeconds;
      }
    }

    public void Update(double dt)
    {
      DayTime_Seconds = (DayTime_Seconds + dt) % DayLength_Seconds;

      double time01 = DayTime_Seconds / DayLength_Seconds;
      MoonDir = new dvec3(Math.Cos(time01 * MathUtils.M_2PI), Math.Sin(time01 * MathUtils.M_2PI), 0);

      ActiveLightDir = IsDay ? (MoonDir) : MoonDir * -1.0f;

      double d2 = DayLengthSeconds * 0.5;
      double n2 = NightLengthSeconds * 0.5;

      double a = 0;
      double b = d2;
      double c = d2 + d2;
      double d = d2 + d2 + n2;
      double e = d2 + d2 + n2 + n2;

      dvec3 c_sky_a = dvec3.Zero, c_sky_b = dvec3.Zero;
      dvec3 c_light_a = dvec3.Zero, c_light_b = dvec3.Zero;
      double t = 0;

      double dawndusk_duration_power = 4;

      if (DayTime_Seconds >= a && DayTime_Seconds < b)
      {
        c_sky_a = Sky_DawnColor;
        c_sky_b = Sky_NoonColor;
        c_light_a = Light_DawnColor;
        c_light_b = Light_NoonColor;
        t = (DayTime_Seconds - a) / (b - a);
        t = 1 - (Math.Pow(1 - t, dawndusk_duration_power));
        DayQuad0 = 0;
        DayQuad1 = 1;
      }
      else if (DayTime_Seconds >= b && DayTime_Seconds < c)
      {
        c_sky_a = Sky_NoonColor;
        c_sky_b = Sky_DuskColor;
        c_light_a = Light_NoonColor;
        c_light_b = Light_DuskColor;
        t = (DayTime_Seconds - b) / (c - b);
        t = (Math.Pow(t, dawndusk_duration_power));
        DayQuad0 = 1;
        DayQuad1 = 0;
      }
      else if (DayTime_Seconds >= c && DayTime_Seconds < d)
      {
        c_sky_a = Sky_DuskColor;
        c_sky_b = Sky_MidnightColor;
        c_light_a = Light_DuskColor;
        c_light_b = Light_MidnightColor;
        t = (DayTime_Seconds - c) / (d - c);
        t = 1 - (Math.Pow(1 - t, dawndusk_duration_power));
        DayQuad0 = 0;
        DayQuad1 = 1;
      }
      else if (DayTime_Seconds >= d && DayTime_Seconds < e)
      {
        c_sky_a = Sky_MidnightColor;
        c_sky_b = Sky_DawnColor;
        c_light_a = Light_MidnightColor;
        c_light_b = Light_DawnColor;
        t = (DayTime_Seconds - d) / (e - d);
        t = (Math.Pow(t, dawndusk_duration_power));
        DayQuad0 = 1;
        DayQuad1 = 0;
      }

      SkyColor = dvec3.CosineInterpolate(c_sky_a, c_sky_b, t);
      LightColor = dvec3.CosineInterpolate(c_light_a, c_light_a, t);

      //0 star 1 cloud
      double daynight_tex_blendspd = 4;
      double dot = MoonDir.dot(new dvec3(0, 1, 0));
      dot = Math.Pow(Math.Abs(dot), daynight_tex_blendspd) * Math.Sign(dot);
      if (dot < 0)
      {
        StarOrCloud_Blend = MathUtils.Ease(0, -1, dot) * 0.5 + 0.5;
      }
      else
      {
        StarOrCloud_Blend = MathUtils.Ease(0, 1, dot) * 0.5 + 0.5;
      }
    }
  }
  public class World
  {
    #region Constants

    public enum GlobCollection
    {
      Render, VisibleRender
    }
    public static Material BlockObjectMaterial = null;
    //These top variables are critical generation control variables.
    public int LimitYAxisGeneration = 0;//0 = off, >0 - limit globs generated along Y axis (faster generation)
    public const float MaxTotalGlobs = 4096 * 2 * 2 * 2;
    public const float MaxRenderGlobs = 4096;
    public int MaxGlobsToGeneratePerFrame_Sync = 32;//number of glob copy operations per render side frame. This can slow down / speed up rendering.
    public const float BlockSizeX = 32.0f;
    public const float BlockSizeY = 32.0f;
    public const float BlockSizeZ = 32.0f;
    public const int GlobBlocksX = 8;
    public const int GlobBlocksY = 8;//Note2: change the GlobSHader Uniform buffer to match this^cubed //Note: now this must be <=8 since we are using ushort
    public const int GlobBlocksZ = 8;
    public const int GlobBlocks_Kernel_MarginX = 1;//Extra amount of blocks copied to the generator for neighbor information
    public const int GlobBlocks_Kernel_MarginY = 1;
    public const int GlobBlocks_Kernel_MarginZ = 1;
    public const int GlobBlocksX_Gen_Kernel = GlobBlocksX + GlobBlocks_Kernel_MarginX * 2; //Generation blocks copied from loaded dromes.
    public const int GlobBlocksY_Gen_Kernel = GlobBlocksY + GlobBlocks_Kernel_MarginY * 2;
    public const int GlobBlocksZ_Gen_Kernel = GlobBlocksZ + GlobBlocks_Kernel_MarginZ * 2;
    public const float GlobWidthX = GlobBlocksX * BlockSizeX;
    public const float GlobWidthY = GlobBlocksY * BlockSizeY;
    public const float GlobWidthZ = GlobBlocksZ * BlockSizeZ;
    public const int DromeGlobsX = 8;
    public const int DromeGlobsY = 8;
    public const int DromeGlobsZ = 8;
    public const int DromeBlocksX = GlobBlocksX * DromeGlobsX;
    public const int DromeBlocksY = GlobBlocksY * DromeGlobsY;
    public const int DromeBlocksZ = GlobBlocksZ * DromeGlobsZ;
    public const float DromeWidthX = GlobWidthX * DromeGlobsX;
    public const float DromeWidthY = GlobWidthY * DromeGlobsY;
    public const float DromeWidthZ = GlobWidthZ * DromeGlobsZ;
    public const float CrustHeightDromes = 2;
    public const float CrustHeight = CrustHeightDromes * DromeWidthY;
    public const float DropDestroyTime_Seconds = (60) * 3; // x minutes
    private const int MaxInitialGenerationWaitTime_ms = 1000 * 15;

    public readonly vec3 BlockRadiusR3 = new vec3(BlockSizeX * 0.5f, BlockSizeY * 0.5f, BlockSizeZ * 0.5f);//Radius from center of glob to the corner.
    public readonly vec3 GlobRadiusR3 = new vec3(GlobWidthX * 0.5f, GlobWidthY * 0.5f, GlobWidthZ * 0.5f);//Radius from center of glob to the corner.
                                                                                                          //public vec3 GlobDiameterR3 { get { return GlobRadiusR3 * 2; } }
    public float GenRadiusShell
    {
      get
      {
        return GlobRadiusR3.length();// * (float)DromeGlobsX * 0.5f;
      }
    }
    public const long Abandon_DeleteTime_DromeNode_ms = 1000 * 5; // * X seconds
    public const long Abandon_DeleteTime_Drome_ms = 1000 * 10; // Dromes stay in memory longer than their nodes. We need the scalar field data more often. When they are fully generated they can be discarded.
    public float DeleteMaxDistance { get { return (GenRadiusShell * (float)(_maxShells + 1)); } }//distance beyond which things are deleted, this must be greater than max gen distance to prevent ping pong loading
    public float GenerateDistance { get { return (GenRadiusShell * (float)_currentShell); } } //distance under which things are generated
    public float RenderDistance { get { return (GenRadiusShell) * _maxShells; /* (GlobWidthX * 16) * (GlobWidthX * 16); */ } }

    #endregion

    #region Members
    public DayNightCycle DayNightCycle = new DayNightCycle();
    private int _currentShell = 1;
    private const int _maxShells = 4;//keep this < Min(DromeGlobs) to prevent generating more dromes
    private long _lastShellIncrementTimer_ms = 0;
    private long _lastShellIncrementTimer_ms_Max = 500;
    private ivec3 playerLastGlob = new ivec3(0, 0, 0);
    private WorldObject dummy = new WorldObject("dummy_beginrender");
    private WorldObject _debugDrawLines = null;
    private WorldObject _debugDrawPoints = null;
    private Dictionary<DrawOrder, List<WorldObject>> _renderObs_Ordered = null;
    //private MultiMap<float, Glob> _visibleRenderGlobs_Frame = new MultiMap<float, Glob>(); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
    //private Dictionary<BlockItem, SortedDictionary<float, vec3>> _visible_BlockObjects_Frame = new Dictionary<BlockItem, SortedDictionary<float, vec3>>();
    private BlockWorldVisibleStuff _stuff = new BlockWorldVisibleStuff();
    private int _globsGenerating = 0;
    private int _globsGenerating_Max = 32;
    private int _dromesGenerating = 0;
    private int _dromesGenerating_Max = 16;

    private MultiMap<float, QueuedGlobData_WithKernel> _queuedGlobs = new MultiMap<float, QueuedGlobData_WithKernel>(); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
    private MultiMap<float, QueuedDromeData> _queuedDromes = new MultiMap<float, QueuedDromeData>(); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.

    private Dictionary<string, WorldObject> Objects { get; set; } = new Dictionary<string, WorldObject>();//Flat list of all objects
    private Dictionary<ivec3, Drome> _dromes = new Dictionary<ivec3, Drome>(new ivec3.ivec3EqualityComparer()); //All globs

    public int Dbg_N_OB_Culled = 0;
    public int NumGenGlobs { get { return _globsGenerating; } }
    public int NumGenDromes { get { return _dromesGenerating; } }
    public int NumVisibleRenderGlobs { get { return _stuff.visible_globs.Count; } }
    public WorldObject SceneRoot { get; private set; } = new WorldObject("Scene_Root");
    public Dictionary<ushort, BlockTile> BlockTiles { get; private set; } = null;
    public Dictionary<ushort, BlockItem> BlockItems { get; private set; } = null;

    private WorldObject _player = null;
    public WorldObject Player
    {
      get
      {
        return _player;
      }
      set
      {
        _player = value;
        playerLastGlob = R3toI3Glob(_player.Position_World);
      }
    }
    public Camera3D Camera
    {
      get
      {
        Gu.Assert(_player != null);
        return (Camera3D)_player.Children.First();
      }
    }
    private Material _worldMaterial_Op = null;
    private Material _worldMaterial_Tp = null;
    private MegaTex _worldMegatex = new MegaTex("tex", true, 0);
    private const string SaveWorldVersion = "0.01";
    private const string SaveWorldHeader = "WorldFilev" + SaveWorldVersion;
    private const int DromeFileVersion = 1;
    private double AutoSaveTimeoutSeconds = 2;
    private double AutoSaveTimeout = 0;

    public string WorldSavePath = "";
    public string WorldName = "";

    #endregion

    public World()
    {
    }

    public void Initialize(WorldObject player, string worldName, bool delete_world_start_fresh, int limit_y_axis = 0)
    {
      Player = player;
      WorldName = worldName;
      LimitYAxisGeneration = limit_y_axis;

      if (!MathUtils.IsPowerOfTwo(GlobBlocksX) || !MathUtils.IsPowerOfTwo(GlobBlocksY) || !MathUtils.IsPowerOfTwo(GlobBlocksZ))
      {
        Gu.BRThrowException("Glob blocks x,y,z must be a power of 2.");
      }

      //This would actually be incorrect world OBs should be instanced
      //Init draw array.
      _renderObs_Ordered = new Dictionary<DrawOrder, List<WorldObject>>();
      for (int do_i = 0; do_i < (int)DrawOrder.MaxDrawOrders; do_i++)
      {
        _renderObs_Ordered.Add((DrawOrder)do_i, new List<WorldObject>());
      }

      DefineBlockTiles();
      CreateMaterials();
      CreateBlockItems();

      //Generate the mesh data we use to create cubess
      WorldStaticData.Generate();

      InitWorldDiskFile(delete_world_start_fresh);

      DayNightCycle.Update(0);

      Gu.Log.Info("Building initail grid");

      //* BuildDromeGrid(Player.WorldMatrix.extractTranslation(), GenRadiusShell, true);
      //I'm assuming since this is cube voxesl we're going to do physics on the integer grid, we don't need triangle data then.
      //* WaitForAllDromesToGenerate();
      //* UpdateLiterallyEverything_Blockish(Camera); // This will generate the globs
      //* WaitForAllGlobsToGenerate();
    }
    private void CreateBlockItems()
    {

      //mesh objs
      AddBlockItem(BlockItemCode.Torch, new FileLoc("torch.glb", FileStorage.Embedded), new vec3(1, 1, 1));
    }
    public void Update(double dt, Camera3D cam)
    {
      Gu.Assert(Player != null);

      BuildWorld();
      UpdateObjects(dt);
      CollectVisibleObjects(cam);
      UpdateLiterallyEverything_Blockish(cam);
      LaunchGlobAndDromeQueues();
      AutoSaveWorld(dt);

      DayNightCycle.Update(dt);
    }
    int dbg_nLit_Frame = 0;
    int dbg_nullBG_Frame = 0;

    private void WaitForAllDromesToGenerate()
    {
      System.Diagnostics.Stopwatch st = new System.Diagnostics.Stopwatch();
      st.Start();
      while (true)
      {
        LaunchGlobAndDromeQueues();
        Gu.Context.Gpu.ExecuteCallbacks_RenderThread(Gu.Context);

        bool genning = false;
        foreach (var d in _dromes.Values)
        {
          if (d.GenState != GenState.Ready)
          {
            genning = true;
          }
        }

        if (genning == false)
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
        LaunchGlobAndDromeQueues();
        Gu.Context.Gpu.ExecuteCallbacks_RenderThread(Gu.Context);

        if (_globsGenerating == 0)
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
      c.Position_Local = pos;
      Box3f dummy = Box3f.Zero;
      c.Update(this, 0, ref dummy);
      //   AddObject(c);
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
    //public void RemoveObject(string name)
    //{
    //  if (Objects.TryGetValue(name, out WorldObject wo))
    //  {
    //    SceneRoot.RemoveChild(wo);
    //    Objects.Remove(name);
    //  }
    //  else
    //  {
    //    Gu.Log.Error("Object '" + name + "' was not found.");
    //  }
    //}
    private void DestroyObject(string name)
    {
      //To destroy you should call the WorldObject's Destroy method
      if (Objects.TryGetValue(name, out WorldObject wo))
      {
        wo.Unlink();
        Objects.Remove(name);
        wo.OnDestroyed?.Invoke(wo);
        foreach (var cmp in wo.Components)
        {
          cmp.OnDestroy(wo);
        }
        wo = null;
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

      ob.OnAddedToScene?.Invoke(ob);
      ob.State = WorldObjectState.Active;

      return ob;
    }
    private void UpdateObjects(double dt)
    {
      Box3f dummy = Box3f.Zero;
      dummy.genResetLimits();
      List<string> toRemove = new List<string>();
      foreach (var kvp in Objects)
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
    private const float MaxVelocity_Second_Frame = World.BlockSizeY * 2.32f;//max length of vel per second / frame *NOT PER FRAME but by dt*
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
        PickedBlock b = RaycastBlock_2(vray);
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
    private void CollectVisibleObjects(Camera3D camera)
    {
      foreach (var layer in _renderObs_Ordered)
      {
        layer.Value.Clear();
      }
      Dbg_N_OB_Culled = 0;

      CollectObjects(camera, SceneRoot);
    }
    private void UpdateLiterallyEverything_Blockish(Camera3D cam)
    {
      _stuff.Clear();
      List<ivec3> to_unload = new List<ivec3>();
      //foreach (var kvp in _dromes)
      foreach (var kvp in _dromes)
      {
        var the_drome = kvp.Value;
        //This is assuming we only gen/load dromes once. if we do some other funky stuff we need a lock.
        if (the_drome.GenState == GenState.Ready)
        {
          if (DromeNode.IsVisible(this, cam, the_drome.Box))
          {
            the_drome.DoLiterallyEverything(this, the_drome, cam, _stuff, null);
          }
          else if (Drome_or_Node_Can_Delete_Distance(the_drome.Box) && ((Gu.Milliseconds() - the_drome.LastVisible_ms) > World.Abandon_DeleteTime_Drome_ms))
          {
            //Delete if we are invisible & not referenced
            if (the_drome.Locked == false)
            {
              to_unload.Add(kvp.Key);
            }
          }

        }
      }

      //Dromes we generated within the glob generator (since we are iterating dromes above, we can't modify the collection)
      foreach (var scalar_field_z3 in _scalarFieldsNeeded)
      {
        QueueDrome(scalar_field_z3);
      }
      _scalarFieldsNeeded.Clear();

      //Unload / or / remove dromes.. here we go...
      foreach (var key in to_unload)
      {
        // ** Dromes should rarely get unloaded. We do need them to stay resident in memory often now that we have made generation seamless (so long as tehy are there).
        Gu.Log.Info("Unloading drome .. " + key.ToString());
        _dromes.Remove(key);
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
      DrawCall_UniformData ud = new DrawCall_UniformData()
      {
        dt = Delta,
        cam = camera,
        ob = dummy,
        dnc = DayNightCycle,
      };

      //Render to this camera.
      camera.BeginRender();
      {
        //Draw First World Objects (sky)
        if (_renderObs_Ordered.Keys.Contains(DrawOrder.First))
        {
          _renderObs_Ordered[DrawOrder.First].Sort((x, y) => x.UniqueID.CompareTo(y.UniqueID));
          foreach (var ob in _renderObs_Ordered[DrawOrder.First])
          {
            DrawObMesh(ob, ud);
          }
        }
        //Second World Objects
        if (_renderObs_Ordered.Keys.Contains(DrawOrder.Mid))
        {
          _renderObs_Ordered[DrawOrder.Mid].Sort((x, y) => x.UniqueID.CompareTo(y.UniqueID));
          foreach (var ob in _renderObs_Ordered[DrawOrder.Mid])
          {
            DrawObMesh(ob, ud);
            _renderObs_Ordered[DrawOrder.First].Sort((x, y) => x.UniqueID.CompareTo(y.UniqueID));
          }
        }

        ud.ob = dummy;
        //Globs
        Glob.dbg_ncalc = 0;
        List<MeshData> visible_op = new List<MeshData>();
        List<MeshData> visible_tp = new List<MeshData>();
        foreach (var g in _stuff.visible_globs)
        {
          bool gvisible = false;
          //No PVS, render all at first
          if (g.Value.Opaque != null)
          {
            visible_op.Add(g.Value.Opaque);
            gvisible = true;
          }
          if (g.Value.Transparent != null)
          {
            visible_tp.Add(g.Value.Transparent);
            gvisible = true;
          }
          if (gvisible)
          {
            //g.Value.CalculateLightsIfNeeded(); //TODO: this should probably be async and launched for all globs via thread pool
          }
        }

        //TESTING Disable fog when under water -- not really but if the b
        //int he futruer player block (camer visible)= water then diable fog
        ud.shaderData._fFogBlend = 0.56361f;
        if (Player.Position_World.y < 0)
        {
          ud.shaderData._fFogBlend = 0.0f;
        }

        _worldMaterial_Op.Draw(visible_op.ToArray(), ud);
        float min = 0;
        float max = 1;
        int steps = 4;
        {
          //need 2 zbuffers. we won't getinto MRT until later
          // GL.DepthRange(0.75f, 1.0f);
          // _worldMaterial_Tp.Draw(Delta, visible_tp.ToArray(), camera, dummy);
          //  GL.DepthRange(0.50f, 0.75f);
          // _worldMaterial_Tp.Draw(Delta, visible_tp.ToArray(), camera, dummy);
          // GL.DepthRange(0.25f, 0.50f);
          //_worldMaterial_Tp.Draw(Delta, visible_tp.ToArray(), camera, dummy);
          // GL.DepthRange(0.0f, 0.25f);
          _worldMaterial_Tp.Draw(visible_tp.ToArray(), ud);

        }

        //Block Objects DrawBlockObjects
        foreach (var ite in _stuff.visible_blockitems)
        {
          Box3f dummy = Box3f.Zero;
          dummy.genResetLimits();
          //Update the base object, blockobjects share a single object
          if (ite.Value.Count > 0)
          {
            ite.Key.WorldObject.Update(this, Delta, ref dummy);
            BlockItem bi = ite.Key;
            ud.instanceData = new mat4[ite.Value.Count];
            int i_inst = 0;
            foreach (var kvp in ite.Value)
            {
              //we are iterating by distance here so we are automatically sorted
              ud.instanceData[i_inst] = mat4.getTranslation(kvp.Value);
              i_inst++;
            }
            DrawObMesh(bi.WorldObject, ud);
          }
        }
        ud.instanceData = null;//null this so we dont f up

      }

      //Draw Last order World Objects
      if (_renderObs_Ordered.Keys.Contains(DrawOrder.Last))
      {
        _renderObs_Ordered[DrawOrder.Last].Sort((x, y) => x.UniqueID.CompareTo(y.UniqueID));
        foreach (var ob in _renderObs_Ordered[DrawOrder.Last])
        {
          DrawObMesh(ob, ud);
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

      if (Gu.Context.DebugDraw.LinePoints.Count > 0)
      {
        GL.LineWidth(1.5f);
        Gpu.CheckGpuErrorsDbg();
        if (_debugDrawLines == null)
        {
          _debugDrawLines = CreateObject("debug_lines", null, new Material("debugLines",Gu.Resources.LoadShader("v_v3c4_debugdraw", false, FileStorage.Embedded)));
        }
        _debugDrawLines.Mesh = new MeshData("Debugasfd", PrimitiveType.Lines,
          Gpu.CreateVertexBuffer(Gu.Context.DebugDraw.LinePoints.ToArray()),
          Gpu.CreateIndexBuffer(Gu.Context.DebugDraw.LineInds.ToArray()),
          false
          );
        DrawCall_UniformData ud = new DrawCall_UniformData()
        {
          dt = Delta,
          cam = camera
        };
        DrawObMesh(_debugDrawLines, ud);
      }
      if (Gu.Context.DebugDraw.Points.Count > 0)
      {
        GL.PointSize(5);
        Gpu.CheckGpuErrorsDbg();
        if (_debugDrawPoints == null)
        {
          _debugDrawPoints = CreateObject("debug_points", null, new Material("debugPoints",Gu.Resources.LoadShader("v_v3c4_debugdraw", false, FileStorage.Embedded)));
        }
        _debugDrawPoints.Mesh = new MeshData("Debugds", PrimitiveType.Points,
          Gpu.CreateVertexBuffer(Gu.Context.DebugDraw.Points.ToArray()),
          false
          );
        DrawCall_UniformData ud = new DrawCall_UniformData()
        {
          dt = Delta,
          cam = camera
        };
        DrawObMesh(_debugDrawPoints, ud);
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
    private void DrawObMesh(WorldObject ob, DrawCall_UniformData ud)
    {
      ud.ob = ob;
      if (ob.Mesh != null)
      {
        ob.Material.Draw(ob.Mesh, ud);
      }
      else
      {
        //this is technically an error
      }
      foreach (var c in ob.Children)
      {
        DrawObMesh(c, ud);
      }
    }
    private BlockTile AddBlockTile(ushort code, BlockFaceInfo[] faces, float hardness_pickaxe, BlockMeshType meshType, bool is_chained, float opacity)
    {
      if (BlockTiles == null)
      {
        BlockTiles = new Dictionary<ushort, BlockTile>();
      }
      var bt = new BlockTile(code, faces, hardness_pickaxe, meshType, is_chained, opacity);
      BlockTiles.Add(code, bt);
      return bt;
    }
    private void AddBlockItem(ushort code, FileLoc file, vec3 scale)
    {
      if (BlockItems == null)
      {
        BlockItems = new Dictionary<ushort, BlockItem>();
      }
      BlockItems.Add(code, new BlockItem(code, file, scale));
    }
    private class HardnessValue
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
    public BlockFaceInfo[] MakeFaces_x3(TileImage topsidebot_img, TileVis topsidebot_vis = TileVis.Opaque)
    {
      //Makes a block with 3 faces having all the same texture
      return new BlockFaceInfo[]
      {
        new BlockFaceInfo(GetTileFile(topsidebot_img), topsidebot_vis),
        new BlockFaceInfo(GetTileFile(topsidebot_img), topsidebot_vis),
        new BlockFaceInfo(GetTileFile(topsidebot_img), topsidebot_vis)
      };
    }
    public BlockFaceInfo[] MakeFaces(TileImage top_img, TileVis top_vis, TileImage sid_img, TileVis sid_vis, TileImage bot_img, TileVis bot_vis)
    {
      //make a block with 3 texture faces
      return new BlockFaceInfo[]
      {
        new BlockFaceInfo(GetTileFile(top_img), top_vis),
        new BlockFaceInfo(GetTileFile(sid_img), sid_vis),
        new BlockFaceInfo(GetTileFile(bot_img), bot_vis)
      };
    }
    private void CreateMaterials()
    {
      var maps = CreateAtlas();
      var s = Gu.Resources.LoadShader("v_Glob", false, FileStorage.Embedded);
      _worldMaterial_Op = new Material("worldMaterial_Op",s, maps.Albedo, maps.Normal);
      _worldMaterial_Tp = new Material("worldMaterial_Tp",s, maps.Albedo, maps.Normal);
      _worldMaterial_Tp.GpuRenderState.Blend = true;
      _worldMaterial_Tp.GpuRenderState.DepthTest = true;
      _worldMaterial_Tp.GpuRenderState.CullFace = false;

      //Create block entities
      foreach (var bt in BlockTiles)
      {
        bt.Value.DefineEntity(maps.Albedo, maps.Normal);
      }

      //Block Material
      BlockObjectMaterial = new Material("BlockObject",Gu.Resources.LoadShader("v_v3n3x2_BlockObject_Instanced", false, FileStorage.Embedded));
    }
    private void DefineBlockTiles()
    {
      //CreateBlockTiles
      //_blockTiles - Manual array that specifies which tiles go on the top, side, bottom
      //The tiles are specified by FileLoc structure which must be a class type.
      //This is used to index into the megatex to find the generated UV coordinates.

      //solid blocks
      AddBlockTile(BlockItemCode.Grass, MakeFaces(TileImage.Grass, TileVis.Opaque, TileImage.GrassSide, TileVis.Opaque, TileImage.Dirt, TileVis.Opaque), HardnessValue.Dirt, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.Dirt, MakeFaces_x3(TileImage.Dirt), HardnessValue.Dirt, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.Brick, MakeFaces_x3(TileImage.Brick), HardnessValue.Gravel, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.Brick2, MakeFaces_x3(TileImage.Brick2), HardnessValue.Gravel, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.Gravel, MakeFaces_x3(TileImage.Gravel), HardnessValue.Gravel, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.Sand, MakeFaces_x3(TileImage.Sand), HardnessValue.Dirt, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.Cedar_Needles, MakeFaces_x3(TileImage.Cedar_Needles, TileVis.Decal), HardnessValue.Leaf, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.Cedar, MakeFaces(TileImage.Cedar_Top, TileVis.Opaque, TileImage.Cedar, TileVis.Opaque, TileImage.Cedar_Top, TileVis.Opaque), HardnessValue.Wood, BlockMeshType.Block, false, BlockTile.BlockOpacity_Billboard);
      AddBlockTile(BlockItemCode.Feldspar, MakeFaces_x3(TileImage.Feldspar), HardnessValue.Rock, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.Feldspar_Coal, MakeFaces_x3(TileImage.Feldspar_Coal), HardnessValue.Rock, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.Marble_Green, MakeFaces_x3(TileImage.Marble_Green), HardnessValue.DeepRock, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.Marble_White, MakeFaces_x3(TileImage.Marble_White), HardnessValue.DeepRock, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.Clay, MakeFaces_x3(TileImage.Clay), HardnessValue.Wood, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.RedClay, MakeFaces_x3(TileImage.RedClay), HardnessValue.Wood, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);
      AddBlockTile(BlockItemCode.Oak_Leaves, MakeFaces_x3(TileImage.Oak_Leaves, TileVis.Decal), HardnessValue.Leaf, BlockMeshType.Block, false, BlockTile.BlockOpacity_Billboard);
      AddBlockTile(BlockItemCode.Oak, MakeFaces(TileImage.Cedar_Top, TileVis.Opaque, TileImage.Oak, TileVis.Opaque, TileImage.Cedar_Top, TileVis.Opaque), HardnessValue.Leaf, BlockMeshType.Block, false, BlockTile.BlockOpacity_Solid);

      //liquid
      //**This is no longer used
      AddBlockTile(BlockItemCode.Water, MakeFaces_x3(TileImage.Water, TileVis.Transparent), HardnessValue.Water, BlockMeshType.Liquid, false, BlockTile.BlockOpacity_Liquid);

      //billboard
      var t = AddBlockTile(BlockItemCode.Tussock, MakeFaces_x3(TileImage.Tussock), HardnessValue.Leaf, BlockMeshType.Billboard, true, BlockTile.BlockOpacity_Billboard);
      t.Growth_Infos_Side = new BlockFaceInfo[3] {
        new BlockFaceInfo(GetTileFile(TileImage.Tussock_Stalk_Bot), TileVis.Decal),
        new BlockFaceInfo(GetTileFile(TileImage.Tussock_Stalk_Mid), TileVis.Decal),
        new BlockFaceInfo(GetTileFile(TileImage.Tussock_Stalk_Top), TileVis.Decal)
      };
      t.GrowthHeight = new Minimax<int>(1, 3);
      AddBlockTile(BlockItemCode.Dandilion, MakeFaces_x3(TileImage.Dandilion), HardnessValue.Leaf, BlockMeshType.Billboard, false, BlockTile.BlockOpacity_Billboard);
      AddBlockTile(BlockItemCode.Seaweed, MakeFaces_x3(TileImage.Seaweed), HardnessValue.Leaf, BlockMeshType.Billboard, false, BlockTile.BlockOpacity_Billboard);
      AddBlockTile(BlockItemCode.RosePink, MakeFaces_x3(TileImage.RosePink), HardnessValue.Leaf, BlockMeshType.Billboard, false, BlockTile.BlockOpacity_Billboard);
      AddBlockTile(BlockItemCode.RoseRed, MakeFaces_x3(TileImage.RoseRed), HardnessValue.Leaf, BlockMeshType.Billboard, false, BlockTile.BlockOpacity_Billboard);
    }
    private MegaTex.CompiledTextures CreateAtlas()
    {
      //Create the atlas.
      //Must be called after context is set.
      foreach (var resource in WorldStaticData.TileImages)
      {
        MtTexPatch p = _worldMegatex.GetTex(resource.Value);

        if (p == null)
        {
          Gu.Log.Error("Tex patch " + resource.Value.QualifiedPath + " was not found in the megatex. Check the filename, and make sure it's embedded (or on disk).");
          Gu.DebugBreak();
        }
        else if (p.GetTexs().Count > 0)
        {
          MtTex mtt = p.GetTexs()[0];
          foreach (var block in BlockTiles)
          {
            //Block Faces
            Gu.Assert(block.Value.FaceInfos != null && block.Value.FaceInfos.Length == 3);
            foreach (var fi in block.Value.FaceInfos)
            {
              //This is a special comparision with a qualified path.
              if (fi.Image == resource.Value)
              {
                fi.UV = mtt;
              }
            }

            //Growth Infos - changes to block faces for growing plants.
            if (block.Value.Growth_Infos_Side != null)
            {
              foreach (var fi in block.Value.Growth_Infos_Side)
              {
                //This is a special comparision with a qualified path.
                if (fi.Image == resource.Value)
                {
                  fi.UV = mtt;
                }
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

      _worldMegatex.GetFont(new FileLoc("EmilysCandy-Regular.ttf", FileStorage.Embedded));

      _worldMegatex.LoadImages();
      var cmp = _worldMegatex.Compile(MegaTex.MtClearColor.BlackNoAlpha, true, TexFilter.Nearest, true);

      cmp.Albedo.SetFilter(TextureMinFilter.NearestMipmapLinear, TextureMagFilter.Nearest);

      return cmp;
    }
    #endregion

    #region World Edit 
    public void PlayDropSound()
    {
      Gu.Context.Audio.Play(new FileLoc("wood_1.ogg", FileStorage.Embedded));
    }
    public void PlayPickSound(ushort bc)
    {
      string embedded_file = "";
      int num = 0;
      if (Block.Is(bc, BlockItemCode.Brick) ||
          Block.Is(bc, BlockItemCode.Brick2) ||
          Block.Is(bc, BlockItemCode.Feldspar) ||
          Block.Is(bc, BlockItemCode.Gravel)
               )
      {
        embedded_file = "rock";
        num = 5;
      }
      else if (Block.Is(bc, BlockItemCode.Dirt) ||
               Block.Is(bc, BlockItemCode.Grass) ||
               Block.Is(bc, BlockItemCode.Cedar) ||
               Block.Is(bc, BlockItemCode.Oak_Leaves) ||
               Block.Is(bc, BlockItemCode.Cedar_Needles)
               )
      {
        embedded_file = "wood";
        num = 4;
      }
      else if (Block.Is(bc, BlockItemCode.Sand)
               )
      {
        embedded_file = "glass";
        num = 5;
      }
      else
      {
        embedded_file = "rock";
        num = 5;
      }
      int r = Random.NextInt(1, num);
      embedded_file += "_" + r.ToString() + ".ogg";

      var x = Gu.Context.Audio.Play(new FileLoc(embedded_file, FileStorage.Embedded));
    }

    private void PlayMinedSound()
    {
      Gu.Context.Audio.Play(new FileLoc("mined.ogg", FileStorage.Embedded));
    }
    public void CreateEntity(vec3 pos, vec3 vel, BlockTile tile)
    {
      var new_ent = tile.Entity.Clone();
      new_ent.Position_Local = pos;
      new_ent.Velocity = vel;
      Gu.World.AddObject(new_ent);
    }
    public void DestroyBlock(vec3 block_pos_global, bool create_entity, bool play_sound, int max_chain = 64)
    {
      //Better, more general destroyed block that allows us to create entities and destroy chains.
      vec3 cur_pos = block_pos_global;
      ushort base_block = 0;

      //this isn't a bounded sequence, technically it could be infinite.
      for (int iblock = 0; iblock < max_chain; iblock++)
      {
        PickRay3D pointRay = new PickRay3D(cur_pos);
        PickedBlock b = RaycastBlock_2(pointRay);

        if (b.IsHit)
        {
          //If we are the same block type (for chained destroy) or we haven't mined the block yet
          if (base_block == 0 || b.Block == base_block)
          {
            base_block = b.Block;

            if (b.Drome != null)
            {
              b.Drome.SetBlock(b.BlockPosLocalZ3, BlockItemCode.Air, false, BlockBits.Solid);
            }
            else
            {
              Gu.BRThrowException("Drome for picked block was null");
            }

            PlayMinedSound();

            if (Gu.World.BlockTiles.TryGetValue(b.Block, out var tile))
            {
              if (create_entity)
              {
                CreateEntity(b.HitPosR3, Random.RandomVelocity(new vec3(-0.2f, 1, -0.2f), new vec3(0.2f, 1, 0.2f), World.BlockSizeY * 12.0f), tile);
              }

              //Recur

              if (tile.IsChainedPlant)
              {
                cur_pos += WorldStaticData.BlockNeighborOffsets[(int)BlockSide.Top];
              }
            }
          }
          else
          {
            break;
          }
        }
        else
        {
          break;
        }
      }
    }

    #endregion

    #region Private: Globs & Dromes

    private void BuildWorld()
    {
      UpdateGenerationShell();
      BuildDromeGrid(Player.WorldMatrix.extractTranslation(), GenerateDistance);
    }
    private void UpdateGenerationShell()
    {
      ivec3 newPlayerGlob = R3toI3Glob(Player.Position_World);
      if ((newPlayerGlob != playerLastGlob))
      {
        _currentShell = 1;
        playerLastGlob = newPlayerGlob;
      }
      else if ((_currentShell < _maxShells) && ((Gu.Milliseconds() - _lastShellIncrementTimer_ms) >= _lastShellIncrementTimer_ms_Max))
      {
        //Pretty sure integrals are atomic but on list.. not sure
        if (_globsGenerating == 0 && _dromesGenerating == 0)
        {
          //Only increase shell if we're done generating for this particular shell.
          _currentShell++;
          _lastShellIncrementTimer_ms = Gu.Milliseconds();
        }
      }
    }
    private void FinishGeneratingGlob_Sync(QueuedGlobData_WithKernel qgd)
    {
      var glob = qgd.MyGlob;
      var id = System.Threading.Thread.CurrentThread.ManagedThreadId;

      if (qgd.MyGlob.State == GenState.Deleted)
      {
        return;
      }

      //Copy everything

      glob.Opaque = null;
      glob.Transparent = null;
      glob.VisibleFaceData = null;
      glob.BlockItems = null;

      if (qgd.async_inds_op != null && qgd.async_inds_op.Count > 0)
      {
        glob.Opaque = new MeshData("", OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles,
          new List<GPUBuffer>{
            Gpu.CreateVertexBuffer(qgd.async_verts.ToArray()),
            Gpu.CreateVertexBuffer(qgd.async_colors.ToArray())
          },
          Gpu.CreateIndexBuffer(qgd.async_inds_op.ToArray()),
          false
           );
        qgd.async_inds_op.Clear();
        qgd.async_inds_op = null;
      }
      if (qgd.async_inds_tp != null && qgd.async_inds_tp.Count > 0)
      {
        //This is unnecessary I mean, just a separate index buffer would be ok. For now this is my hack.
        glob.Transparent = new MeshData("", OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles,
          new List<GPUBuffer>{
            Gpu.CreateVertexBuffer(qgd.async_verts.ToArray()),
            Gpu.CreateVertexBuffer(qgd.async_colors.ToArray())
          },
          Gpu.CreateIndexBuffer(qgd.async_inds_tp.ToArray()),
          false
           );
        qgd.async_inds_tp.Clear();
        qgd.async_inds_tp = null;
      }
      if (qgd.async_face_data != null)
      {
        glob.VisibleFaceData = qgd.async_face_data;
        qgd.async_face_data = null;
      }
      if (qgd.async_block_items != null && qgd.async_block_items.Count > 0)
      {
        glob.BlockItems = qgd.async_block_items;
        qgd.async_block_items = null;
      }

      qgd.ScalarFields.Unlock();


      //Avoid memory leaks

      qgd.ReleaseBuffers();
      qgd = null;

      glob.State = GenState.Ready;
    }

    private void BuildDromeGrid(vec3 origin, float awareness_radius, bool logprogress = false)
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
          Gu.Log.Info("Drome " + dbg_current_drome + "/" + count);
        }

        ivec3 dpos = new ivec3(x, y, z);
        Drome d;

        if (Drome_Can_Generate_Distance(Drome.GetDromeBox(dpos)))
        {
          if (!_dromes.TryGetValue(dpos, out d))
          {
            d = QueueDrome(dpos);
          }
        }

        return true;
      });
    }
    public bool Drome_or_Node_Can_Delete_Distance(Box3f drome_box)
    {
      return !Box_IsWithin_Distance(drome_box, DeleteMaxDistance);
    }
    public bool Drome_Can_Generate_Distance(Box3f drome_box)
    {
      return Box_IsWithin_Distance(drome_box, GenerateDistance);
    }
    public bool Box_IsWithin_Distance(Box3f box, float genDistance)
    {
      //return true if the box's dist to camera is less than gen distnace
      float dist_cam2 = box.DistanceToCam2(Camera);

      if (dist_cam2 < (genDistance * genDistance))
      {
        return true;
      }
      return false;
    }
    private ushort CreateBlock(float solid, vec3 world_pos, ivec3 local_pos)
    {
      ushort item = BlockItemCode.Air;
      float yz3 = world_pos.y / BlockSizeY;
      if (solid > 0)
      {
        if (yz3 < -10)
        {
          item = BlockItemCode.Feldspar;
        }
        else if (yz3 < -4)
        {
          item = BlockItemCode.Gravel;
        }
        else if (yz3 < 0)
        {
          item = BlockItemCode.Sand;
        }
        else
        {
          item = BlockItemCode.Dirt;
        }
      }

      Block.SetSolid(ref item, item);//redundant, but safer

      if (yz3 < -2 && item == BlockItemCode.Air)
      {
        Block.SetLiquid(ref item, Block.MaxLiquid);// = BlockItemCode.Water;

        ushort test = Block.GetLiquid(item);
        int n = 0;
        n++;
      }

      return item;
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

    #region Drome Generation

    private void GenerateDrome_Async(QueuedDromeData qdd)
    {
      Drome d = qdd.drome;
      d.GenState = GenState.GenStart;
      {
        GenerateBaseLand(d);

        HeightGrid_Stuff_Grass_Stoneshell(d);

        Minimax<int> smallHole = new Minimax<int>(0, 1);
        Minimax<int> bigHole = new Minimax<int>(-1, 1);
        Minimax<int> hugeHole = new Minimax<int>(-2, 1);

        Minimax<int> any_height_drome = new Minimax<int>(0, (int)((float)(World.DromeBlocksY - 1)));

        //Instead of D, now we should pass the kernel
        MakeOre(d, any_height_drome, .006f, new Minimax<int>(16, 64), BlockItemCode.Feldspar_Coal, new List<ushort> { BlockItemCode.Feldspar }, bigHole);
        MakeOre(d, any_height_drome, .0001f, new Minimax<int>(4, 10), BlockItemCode.Marble_White, new List<ushort> { BlockItemCode.Feldspar }, smallHole);
        MakeOre(d, any_height_drome, .0001f, new Minimax<int>(6, 32), BlockItemCode.Marble_Green, new List<ushort> { BlockItemCode.Feldspar }, smallHole);
        MakeOre(d, any_height_drome, .001f, new Minimax<int>(32, 128), BlockItemCode.RedClay, new List<ushort> { BlockItemCode.Grass, BlockItemCode.Dirt, BlockItemCode.Sand }, bigHole);

        //Rocks in dirt / dirt in rocks
        MakeOre(d, any_height_drome, .001f, new Minimax<int>(32, 128), BlockItemCode.Feldspar, new List<ushort> { BlockItemCode.Grass, BlockItemCode.Dirt, BlockItemCode.Sand }, bigHole);
        MakeOre(d, any_height_drome, .001f, new Minimax<int>(32, 128), BlockItemCode.Dirt, new List<ushort> { BlockItemCode.Grass, BlockItemCode.Sand, BlockItemCode.Feldspar }, bigHole);

        //Under Water
        MakeOre(d, any_height_drome, .0001f, new Minimax<int>(32, 64), BlockItemCode.Gravel, new List<ushort> { BlockItemCode.Sand }, hugeHole);
        MakeOre(d, any_height_drome, .0001f, new Minimax<int>(32, 128), BlockItemCode.Clay, new List<ushort> { BlockItemCode.Sand }, hugeHole);

        //MakeOre(d, new Minimax<int>(0, (int)((float)(World.DromeBlocksY - 1) /* (2.0f / 3.0f) */)), .001f, new Minimax<int>(128, 512), BlockItemCode.Brick, new List<ushort> { BlockItemCode.Air });
        //MakeOre(d, new Minimax<int>(0, (int)((float)(World.DromeBlocksY - 1) /* (2.0f / 3.0f) */)), .0001f, new Minimax<int>(511, 512), BlockItemCode.Brick2, new List<ushort> { BlockItemCode.Air });

        var surface_drop = new List<ushort> { BlockItemCode.Air, BlockItemCode.Cedar_Needles, BlockItemCode.Oak_Leaves };
        var surface_or_seafloor_drop = new List<ushort> { BlockItemCode.Water, BlockItemCode.Air, BlockItemCode.Cedar_Needles, BlockItemCode.Oak_Leaves };

        var grass_or_dirt_soil = new List<ushort>() { BlockItemCode.Grass, BlockItemCode.Dirt };
        var any_block_soil = new List<ushort>() { BlockItemCode.AnyVisible };
        var sand_soil = new List<ushort>() { BlockItemCode.Sand };

        List<ivec3> seeds;
        //Idk, just trying out carving the WOOORRRLLLLDDD
        seeds = PlantSurfaceSeeds(d, .0009f, any_block_soil, surface_drop);
        seeds.AddRange(PlantDeepSeeds(d, any_height_drome, .0009f, any_block_soil));
        MakeCaves(d, seeds, new Minimax<int>(128, 1024), new List<ushort> { BlockItemCode.Land }, new Minimax<int>(-2, 2));

        //Water / waterfalls
        seeds = PlantSurfaceSeeds(d, Random.NextInt(0, 3), any_block_soil, surface_drop);
        DropLiquid(d, seeds, Block.MaxLiquid);

        seeds = PlantSurfaceSeeds(d, 0.01f, grass_or_dirt_soil, surface_drop);
        GrowTrees(d, seeds, BlockItemCode.Cedar, BlockItemCode.Cedar_Needles, 9, new Minimax<vec3>(new vec3(-.4f, -1, -.4f), new vec3(.4f, 1.5f, .4f)), new Minimax<int>(1, 2), new Minimax<int>(-1, 1));

        seeds = PlantSurfaceSeeds(d, 0.064f, grass_or_dirt_soil, surface_drop);
        GrowTrees(d, seeds, BlockItemCode.Oak, BlockItemCode.Oak_Leaves, 16, new Minimax<vec3>(new vec3(-1, -0.1f, -1), new vec3(1, 1, 1)), new Minimax<int>(3, 8), new Minimax<int>(-1, 1));

        seeds = PlantSurfaceSeeds(d, 0.7f, grass_or_dirt_soil, surface_drop);
        GrowSeeds(d, seeds, BlockItemCode.Tussock);

        seeds = PlantSurfaceSeeds(d, 0.06f, grass_or_dirt_soil, surface_drop);
        GrowSeeds(d, seeds, BlockItemCode.Dandilion);

        seeds = PlantSurfaceSeeds(d, 0.04f, grass_or_dirt_soil, surface_drop);
        GrowSeeds(d, seeds, BlockItemCode.RoseRed);

        seeds = PlantSurfaceSeeds(d, 0.04f, grass_or_dirt_soil, surface_drop);
        GrowSeeds(d, seeds, BlockItemCode.RosePink);

        seeds = PlantSurfaceSeeds(d, 0.4f, sand_soil, surface_or_seafloor_drop);
        GrowSeeds(d, seeds, BlockItemCode.Seaweed);

        if (d.BlockStats.RegionState == RegionState.Empty_AndNoData)
        {
          //Free empty regions.
          d.Blocks.Grid = null;
        }

        SettleLiquids(qdd, d);

      }
      d.GenState = GenState.GenEnd;

    }
    private void SettleLiquids(QueuedDromeData qdd, Drome d)
    {
      ivec3[] water_neighbors = new ivec3[]
      {
        new ivec3(-1, 0, 0),
        new ivec3( 1, 0, 0),
        new ivec3( 0,-1, 0),
        new ivec3( 0, 0,-1),
        new ivec3( 0, 0, 1),
      };
      //we need to grab water from upper and mid-level dromes as well
      for (int iy = DromeBlocksY - 2; iy >= 0; iy--)
      {
        for (int iset = 0; iset < 20; iset++)
        {
          bool set = false;
          //For each layer, simulate water, if we find it.
          for (int iz = 0; iz < DromeBlocksZ; iz++)
          {
            for (int ix = 0; ix < DromeBlocksX; ix++)
            {
              //Rule: blockes don't lose liquid when water travels down
              //if a block below has less liquid, it gets added teh liquid above, but above does not change
              //horizontally, blocks lose 1 liquid for every spread.
              //step 1 - pull down water from above
              //step 2.1 - spread water every 20 blocks or so .. 
              //step 2.2 - spread water if ground is below, or on side

              ivec3 b_cur_pos = new ivec3(ix, iy, iz);

              ushort b_cur_liq_new = 0;
              // 1 pull down from abpve
              //TODO: get liquid .. uniform for all dromes and neighbors
              ushort b_above = d.GetBlock(ix, iy + 1, iz);
              ushort b_above_liq = Block.GetLiquid(b_above);
              ushort b_cur = d.GetBlock(ix, iy, iz);
              if (b_above_liq > 0)
              {
                if (Block.IsSolidBlock(b_cur))
                {
                  //Only add liquid to block below if the liquid above is greater. This way the simulation doesn't poop
                  ushort b_cur_liq = Block.GetLiquid(b_cur);
                  if (b_above_liq > b_cur_liq)
                  {
                    Block.AddLiquid(ref b_cur, (short)b_above_liq);

                    b_cur_liq_new = Block.GetLiquid(b_cur);

                    d.SetBlock(b_cur_pos, b_cur_liq_new, true, BlockBits.Liquid);
                    set = true;

                  }
                }
              }

              b_cur_liq_new = Block.GetLiquid(b_cur);

              // Spread around
              if (iy - 1 >= 0)//**TODO: simulate other dromes
              {
                ushort b_below = d.GetBlock(ix, iy - 1, iz);
                if (Block.IsSolidBlock(b_below))
                {
                  //spread to neighbors
                  foreach (var wn in water_neighbors)
                  {
                    ivec3 b_npos = b_cur_pos + wn;
                    ushort b_nblk = d.GetBlock(b_npos, IndexMode.Clamp); //*** TODO: Simulate other dromes
                    ushort b_n_liq = Block.GetLiquid(b_nblk);
                    ushort b_n_sol = Block.GetSolid(b_nblk);
                    //Spread the liquid if the neighbor has greater liquid
                    if (b_cur_liq_new > 0 && ((int)b_cur_liq_new - (int)b_n_liq > 1))//so we dont flip flop >1 - 1
                    {
                      if (!Block.IsSolidBlock(b_n_sol))
                      {
                        //sub 1 for each neighbor
                        short new_m1 = (short)((int)b_cur_liq_new - 1);// hmm.. maybe 1, 2, 3 who knows
                        if (new_m1 < 0)
                        {
                          new_m1 = 0;
                        }
                        Block.SetLiquid(ref b_nblk, (ushort)new_m1);
                        ushort b_n_liquid_new = Block.GetLiquid(b_nblk);
                        d.SetBlock(b_npos, b_n_liquid_new, true, BlockBits.Liquid);
                        set = true;
                      }
                    }
                  }
                }
              }

            }//for x
          }//for z

          if (set == false)
          {
            break;
          }


        }//for..iset

      }

    }
    private void HeightGrid_Stuff_Grass_Stoneshell(Drome d)
    {
      //Do stuff that iterates the blocks on the Y "drop" axis
      //Grow grass blocks
      // Change deep blocks to be stone, not grass

      //Block x/z
      float bx_z3 = d.OriginR3.x / BlockSizeX;
      float bz_z3 = d.OriginR3.z / BlockSizeZ;
      float by_z3 = d.OriginR3.y / BlockSizeY;

      // int[] heights = new int[DromeBlocksX * DromeBlocksZ];

      //Sprout grass on the top layers only.
      //Iterate whole drome and x, y, z will get grass
      for (int iz = 0; iz < DromeBlocksZ; iz++)
      {
        for (int ix = 0; ix < DromeBlocksX; ix++)
        {
          bool setGrass = false;
          int grassdistance = 0;
          for (int iy = DromeBlocksY - 1; iy >= 0; iy--)
          {
            ushort thisone = d.GetBlock(ix, iy, iz);

            int y_z3 = (int)by_z3 + (int)iy;

            //Find the first top, and set to grass.
            if (setGrass == false)
            {
              // * Grass blocks
              ushort above = BlockItemCode.Missing;
              if (iy < DromeBlocksY - 1)
              {
                above = d.GetBlock(ix, iy + 1, iz);
              }

              if (((Block.Is(thisone, BlockItemCode.Dirt) && (Block.Is(above, BlockItemCode.Air))) || (Block.IsSolidBlock(thisone) && (y_z3 < 0) && Block.IsFullOfWater(above))))
              {
                ivec3 v = new ivec3(ix, iy, iz);
                //trying to set sand on ocean floor
                d.SetBlock(v, (y_z3 < 0) ? BlockItemCode.Sand : BlockItemCode.Grass, true, BlockBits.Solid);
                setGrass = true;
              }
            }
            else
            {
              if (Block.HasNoSolid(thisone))
              {
                break;
              }

              grassdistance += 1;
            }


            //If we are a distance from the grass, set the block to be plagio .. the random makes grass crust look less uniform
            //the random kind of sucks it's too fuzzy
            float randoid = RandoCrust(ix, iy, iz, bx_z3, by_z3, bz_z3, -4, 4);
            if (grassdistance > (4 + randoid))
            {
              ivec3 v = new ivec3(ix, iy, iz);
              d.SetBlock(v, BlockItemCode.Feldspar, true, BlockBits.Solid);
            }


          }
        }
      }
    }
    void DropLiquid(Drome d, List<ivec3> water_seeds, ushort liquid_level)
    {
      foreach (var seed_pos in water_seeds)
      {
        if (IsBlockInsideDromeBounds(seed_pos))
        {
          ushort b = d.GetBlock(seed_pos);
          if (!Block.IsSolidBlock(b)) // TODO: we can override blocks if some are more "dominant"
          {
            d.SetBlock(seed_pos, liquid_level, true, BlockBits.Liquid);
          }
        }
      }
    }
    float RandoCrust(int ix, int iy, int iz, float bx_z3, float by_z3, float bz_z3, int min, int max, float freq = 1, float amp = 1)
    {
      //TODO: some rnadom sin / cosin thing
      float randoid_x = bx_z3 + (float)ix;
      float randoid_z = bz_z3 + (float)iz;
      float randoid_y = by_z3 + (float)iy;
      float randoid = (float)(
          Math.Sin(((randoid_x * 0.745742748) % (Math.PI * 2.0)) * freq) * 1.911
        * Math.Cos(((randoid_z * 0.724555791) % (Math.PI * 2.0)) * freq) * 1.891
        * Math.Sin(((randoid_z * 0.342534578) % (Math.PI * 2.0)) * freq) * 1.911
        * Math.Cos(((randoid_z * 0.308536891) % (Math.PI * 2.0)) * freq) * 1.891
        * Math.Sin(((randoid_x * 0.464667758) % (Math.PI * 2.0)) * freq) * 1.911
        * Math.Cos(((randoid_x * 0.497174241) % (Math.PI * 2.0)) * freq) * 1.891
        ) * amp;
      randoid = Math.Clamp(randoid, min, max);
      return randoid;
    }
    private void GrowSeeds(Drome d, List<ivec3> seeds, ushort item)
    {
      int growthHeight = 1;

      ivec3 growth_pos;
      foreach (var seed_pos in seeds)
      {
        if (BlockTiles.TryGetValue(item, out BlockTile bt))
        {
          growthHeight = Random.Next(bt.GrowthHeight);
          if (growthHeight == 1)
          {
            int n = 0;
            n++;
          }
          if (growthHeight > 1)
          {
            int n = 0;
            n++;
          }
          Gu.Assert(growthHeight > 0);
        }

        for (int gy = 0; gy < growthHeight; gy++)
        {
          growth_pos = seed_pos;
          growth_pos.y += gy; //start at 1, because the growth height will be between [1, n]. Seed Pos is zero. So we want to add 0, 1, 2, .. 

          if (IsBlockInsideDromeBounds(growth_pos))
          {
            ushort b = d.GetBlock(growth_pos);
            if (Block.HasNoSolid(b)) // TODO: we can override blocks if some are more "dominant"
            {
              d.SetBlock(growth_pos, item, true, BlockBits.Solid);
            }

          }

        }
      }
    }
    private void GenerateBaseLand(Drome d)
    {
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
            float dens = BaseLandDensity(block_world);
            ivec3 z3 = new ivec3(x, y, z);
            ushort created_value = CreateBlock(dens, block_world, z3);
            d.SetBlock(new ivec3(x, y, z), created_value, true, BlockBits.All);
          }
        }

      }
    }
    private List<ivec3> PlantSurfaceSeeds(Drome d, float density01, List<ushort> allowedSoilTypes, List<ushort> skipDrop = null)
    {
      int seeds = (int)((float)DromeBlocksX * (float)DromeBlocksZ * density01);
      return PlantSurfaceSeeds(d, seeds, allowedSoilTypes, skipDrop);
    }
    private List<ivec3> PlantSurfaceSeeds(Drome d, int count, List<ushort> allowedSoilTypes, List<ushort> skipDrop = null)
    {
      List<ivec3> planted = new List<ivec3>();
      for (int iseed = 0; iseed < count; iseed++)
      {
        int rx = (int)Random.Next(0, DromeBlocksX - 1);
        int rz = (int)Random.Next(0, DromeBlocksZ - 1);
        ivec3 v = DropBlock(d, rx, rz, allowedSoilTypes, skipDrop);
        if (v.x >= 0)
        {
          planted.Add(v);
        }
      }
      return planted;
    }
    private void MakeOre(Drome d, Minimax<int> ore_height, float density01, Minimax<int> steps, ushort ore_block_code, List<ushort> existingBlocks, Minimax<int> size)
    {
      List<Walker> walkers = new List<Walker>();
      List<ivec3> ore_seeds = new List<ivec3>();
      int seeds = (int)((float)DromeBlocksX * (float)DromeBlocksY * (float)DromeBlocksZ * density01);
      ivec3 ore_pos;
      var probabilitye = new Minimax<vec3>(new vec3(-1, -1, -1), new vec3(1, 1, 1));

      for (int iseed = 0; iseed < seeds; iseed++)
      {
        ore_pos.x = (int)Random.Next(0, DromeBlocksX - 1);
        ore_pos.z = (int)Random.Next(0, DromeBlocksZ - 1);
        ore_pos.y = Random.Next(ore_height);

        if (!Block.HasNoSolid(d.GetBlock(ore_pos)))
        {
          walkers.Add(new RandomWalker(d, steps, ore_pos, probabilitye, ore_block_code, (World.GlobWidthX * World.GlobWidthX), existingBlocks, size));
          ore_seeds.Add(ore_pos);
        }
      }
      SimulateWalkers(d, walkers);

    }
    private List<ivec3> PlantDeepSeeds(Drome d, Minimax<int> height, float density01, List<ushort> target_soil_types)
    {
      //Plant seeds within the land on target soils.
      List<ivec3> seedList = new List<ivec3>();
      bool any = target_soil_types.Contains(BlockItemCode.Missing);
      int max_tries = 16;//try this many times to randomly place seed, else, give up
      ivec3 seed_pos;
      int seeds = (int)((float)DromeBlocksX * (float)DromeBlocksY * (float)DromeBlocksZ * density01);
      for (int iseed = 0; iseed < seeds; iseed++)
      {
        for (int itry = 0; itry < max_tries; itry++)
        {
          seed_pos.x = (int)Random.Next(0, DromeBlocksX - 1);
          seed_pos.z = (int)Random.Next(0, DromeBlocksZ - 1);
          seed_pos.y = Random.Next(height);

          ushort this_blk = d.GetBlock(seed_pos);
          ushort liquid = Block.GetLiquid(this_blk) > 0 ? BlockItemCode.Water : BlockItemCode.Missing;
          ushort solid = Block.GetSolid(this_blk);

          if (any || target_soil_types.Contains(solid) || target_soil_types.Contains(liquid))
          {
            seedList.Add(seed_pos);
            break;
          }

        }

      }
      return seedList;
    }
    private void MakeCaves(Drome d, List<ivec3> seeds, Minimax<int> steps, List<ushort> existingBlocks, Minimax<int> size)
    {
      List<Walker> walkers = new List<Walker>();
      List<ivec3> ore_seeds = new List<ivec3>();

      var probabilitye = new Minimax<vec3>(new vec3(-1, -4, -1), new vec3(1, 0, 1));

      foreach (var seed in seeds)
      {
        if (!Block.HasNoSolid(d.GetBlock(seed)))
        {
          vec3 prob = Random.Next(probabilitye);
          walkers.Add(new SnakeWalker(d, steps, seed, prob, BlockItemCode.Air, (World.GlobWidthX * World.GlobWidthX), existingBlocks, size));
          ore_seeds.Add(seed);
        }
      }
      SimulateWalkers(d, walkers);
    }
    private ivec3 DropBlock(Drome d, int x, int z, List<ushort> target_soil_types, List<ushort> ignore_as_air)
    {
      //Set TargetSoilTypes to Missing
      //Doesn't actually set the block. Just drop from the top to the given ground.
      //target block is the allowed block we can drop onto (soil for planting, sand .. )

      //Don't put missing in here.
      Gu.Assert(!target_soil_types.Contains(BlockItemCode.Missing));
      Gu.Assert(!ignore_as_air.Contains(BlockItemCode.Missing));

      for (int dy = DromeBlocksY - 1; dy >= 0; dy--)
      {
        var this_one = new ivec3(x, dy, z);
        var below_us = new ivec3(x, dy - 1, z);

        if (IsBlockInsideDromeBounds(this_one) && IsBlockInsideDromeBounds(below_us))
        {
          ushort this_blk = d.GetBlock(this_one);
          ushort this_liquid = Block.GetLiquid(this_blk) > 0 ? BlockItemCode.Water : BlockItemCode.Missing;
          ushort this_solid = Block.GetSolid(this_blk);

          //Ignore dropping on these blocks int his array
          if ((ignore_as_air != null) && (ignore_as_air.Contains(this_solid) || ignore_as_air.Contains(this_liquid)))
          {
          }
          else if (this_solid > 0 || this_liquid > 0)
          {
            //This block isn't in "ignore" Invalid block - don't plant.
            break;
          }

          var block_below = d.GetBlock(below_us);
          ushort below_liquid = Block.GetLiquid(block_below) > 0 ? BlockItemCode.Water : BlockItemCode.Missing;
          ushort below_solid = Block.GetSolid(block_below);

          if (target_soil_types.Contains(below_liquid) ||
            target_soil_types.Contains(below_solid) ||
            (target_soil_types.Contains(BlockItemCode.AnyVisible) && Block.IsVisibleBlock(block_below)))
          {
            return this_one;
          }


        }

      }
      //May have been completely solid
      return new ivec3(-1, -1, -1);
    }
    private void GrowTrees(Drome d, List<ivec3> seeds, ushort bark_block, ushort leaf_block, int leafWalker_Count, Minimax<vec3> foliage_shape, Minimax<int> trunkHeight, Minimax<int> size)
    {
      foreach (var seed in seeds)
      {
        //if seed is tree
        GrowTree(d, seed, bark_block, leaf_block, leafWalker_Count, foliage_shape, trunkHeight, size);
      }
    }
    private void GrowTree(Drome d, ivec3 start_pos, ushort bark_block, ushort leaf_block, int leafWalker_Count, Minimax<vec3> foliage_shape, Minimax<int> trunk_Height, Minimax<int> size)
    {
      ivec3 cp = start_pos;
      vec3 d_origin = d.OriginR3;
      int trunkHeight = Random.Next(trunk_Height);
      ivec3 pos = start_pos;
      for (int y = 0; y <= trunkHeight; y++)
      {
        pos = start_pos;
        pos.y += y;
        if (IsBlockInsideDromeBounds(pos))
        {
          d.SetBlock(pos, bark_block, true, BlockBits.Solid);
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
      Minimax<int> steps = new Minimax<int>(5, 10);
      for (int iw = 0; iw < leafWalker_Count; iw++)
      {
        walkers.Add(new RandomWalker(d, steps, pos, foliage_shape, leaf_block, maxdist2, new List<ushort> { BlockItemCode.Air }, size));
      }
      SimulateWalkers(d, walkers);
    }
    private void SimulateWalkers(Drome d, List<Walker> walkers)
    {
      //simulate walkers until they reach maxdist2
      vec3 d_origin = d.OriginR3;
      int dbg_nCountWhile = 0;
      while (walkers.Count > 0)
      {
        for (int iwalker = walkers.Count - 1; iwalker >= 0; iwalker--)
        {
          Walker p = walkers[0];
          if (p.Move())
          {
            walkers.RemoveAt(0);
            break;
          }

          p.Carve(this, d);

        }

        dbg_nCountWhile++;
      }
    }

    //private void LightDrome_Async(Drome d)
    //{
    //  ///do this only once we've set up the glob shader

    //  vec3 sun_color = vec4.FromHex("#FFD70000").xyz();
    //  vec3 sun_dir = new vec3(-1, -1, -1).normalized();
    //  vec3 sun_dir_n = sun_dir.normalized();

    //  //Technically this should be vertex lighting, but for now we are doing just faces.
    //  d.LightGrid = new Grid3D<GRay>(DromeBlocksX, DromeBlocksY, DromeBlocksZ);
    //  d.LightGrid.Allocate(default(GRay));

    //  LightX(d, this.DayNightCycle.SunDirInv.x * -1, World.DromeBlocksX);

    //  //occlusion value per face
    //  //0 1 2 3 4 5
    //  // 111
    //  //gauss function with bvh
    //}
    //void LightX(Drome d, double dir, int axis_siz)
    //{
    //  //this wont work. it will light up underground shit, and caves
    //  int xstart = dir > 0 ? 0 : axis_siz - 1;
    //  int xend = axis_siz - xstart - 1;
    //  int xinc = xstart > xend ? -1 : 1;
    //  //int ystart = sun_dir.y > 0 ? World.DromeBlocksY - 1 : 0;
    //  //int zstart = sun_dir.z > 0 ? World.DromeBlocksZ - 1 : 0;

    //  // ** This is just for X axis -- Y and Z need to be done... But its' not workign yet
    //  GRay cur_ray;
    //  float div = 0;
    //  for (int yi = 0; yi < d.LightGrid.SizeY; yi++)
    //  {
    //    for (var zi = 0; zi < d.LightGrid.SizeZ; zi++)
    //    {
    //      //multiply by normal based on the percentage of light in each direction
    //      d.LightGrid.Set(xstart, yi, zi, new GRay(sun_color, sun_dir));

    //      for (int xi = xstart + xinc; xi != (xend + xinc); xi += xinc) //xstart+xinc = 1 - we skip the first block since we just set it
    //      {
    //        div = 1.0f; // full light

    //        //get previous block ray
    //        cur_ray = d.LightGrid.Get(xi - xinc, yi, zi, IndexMode.Throw);

    //        if (d.HasBlockData())
    //        {
    //          ushort b0 = d.Blocks.Get(xi, yi, zi, IndexMode.Clamp);

    //          //ushort b_next = d.Blocks.Get(xi+xinc, yi, zi, IndexMode.Clamp).Value;
    //          //if (Block.IsSolidBlockNotTransparent(b_next))
    //          //{
    //          //  //reflect the ray
    //          //}
    //          if (Block.IsSolidBlockNotDecal_OrWater(b0))
    //          {
    //            div = 0;
    //          }
    //          else
    //          {
    //            ushort b1 = d.Blocks.Get(xi, yi - 1, zi, IndexMode.Clamp);
    //            ushort b2 = d.Blocks.Get(xi, yi + 1, zi, IndexMode.Clamp);
    //            ushort b3 = d.Blocks.Get(xi, yi, zi - 1, IndexMode.Clamp);
    //            ushort b4 = d.Blocks.Get(xi, yi, zi + 1, IndexMode.Clamp);

    //            int bn = 0;
    //            if (Block.IsSolidBlockNotDecal_OrWater(b1))
    //            {
    //              bn += 1;
    //            }
    //            if (Block.IsSolidBlockNotDecal_OrWater(b2))
    //            {
    //              bn += 1;
    //            }
    //            if (Block.IsSolidBlockNotDecal_OrWater(b3))
    //            {
    //              bn += 1;
    //            }
    //            if (Block.IsSolidBlockNotDecal_OrWater(b4))
    //            {
    //              bn += 1;
    //            }
    //            //God my brain .. not wroking
    //            // bn = 0 -> sub = 0
    //            // bn = 1 -> sub = 0
    //            // bn = 2 -> sub = 1/32 * 3 //greater value = less flloff
    //            // bn = 3 -> sub = 1/32 * 2
    //            // bn = 4 -> sub = 1/32 * 1
    //            // We do not sub for 1 block, only for 2-4
    //            if (bn == 0 || bn == 1)
    //            {
    //            }
    //            else if (bn == 2)
    //            {
    //              div = 1.0f - 1.0f / 32.0f * 3;
    //            }
    //            else if (bn == 3)
    //            {
    //              div = 1.0f - 1.0f / 32.0f * 2;
    //            }
    //            else if (bn == 4)
    //            {
    //              div = 1.0f - 1.0f / 32.0f * 1;
    //            }

    //          }
    //        }

    //        cur_ray.Dir *= div;

    //        //alter ray intensity
    //        float min_ambient = 0.0125f;
    //        if (cur_ray.Dir.length2() < (min_ambient * min_ambient))
    //        {
    //          //Ambient lighting.
    //          cur_ray.Dir = cur_ray.Dir.normalized() * min_ambient;
    //        }
    //        d.LightGrid.Set(xi, yi, zi, cur_ray, IndexMode.Throw);

    //      }
    //    }
    //  }

    //}
    #endregion

    public bool IsBlockInsideDromeBounds(ivec3 block_pos)
    {
      return block_pos.x >= 0 && block_pos.y >= 0 && block_pos.z >= 0 &&
        block_pos.x < DromeBlocksX && block_pos.y < DromeBlocksY && block_pos.z < DromeBlocksZ;
    }
    public Drome FindDromeI3(ivec3 pos, bool null_if_not_gen = true)
    {
      Drome d = null;
      _dromes.TryGetValue(pos, out d);
      if (d != null && null_if_not_gen && d.GenState != GenState.Ready)
      {
        return null;
      }
      return d;
    }
    private void StitchGlobTopology(Glob g)
    {
      //TODO: Reimplement this with new glob bvh data structure
      //for (int ni = 0; ni < 6; ++ni)
      //{
      //  Glob gn = GetNeighborGlob(g, ni, GlobCollection.All);
      //  if (gn != null)
      //  {
      //    QueueForTopo(gn);
      //  }
      //}
    }
    private void TopologizeGlob(QueuedGlobData_WithKernel qgd)
    {
      var glob = qgd.MyGlob;
      ivec3 block_off_glob = new ivec3();
      Drome drome = null;

      if (!glob.Drome.TryGetTarget(out drome))
      {
        return;
      }
      if (drome.BlockStats.RegionState == RegionState.Empty_AndNoData)
      {
        return;
      }
      if (drome.Blocks.Grid == null)
      {
        //This shouldn't happen. Only nullify grids when our region state is empty.
        Gu.DebugBreak();
        return;
      }

      if (glob.State != GenState.Deleted)
      {
        glob.State = GenState.GenStart;
      }

      //Iterate over a glob topology unit, note the blocks in the queued data have extra padding for neighbor information.
      for (int gblock_z = 0; gblock_z < GlobBlocksZ; gblock_z++)
      {
        for (int gblock_y = 0; gblock_y < GlobBlocksY; gblock_y++)
        {
          for (int gblock_x = 0; gblock_x < GlobBlocksX; gblock_x++)
          {
            block_off_glob.construct(gblock_x, gblock_y, gblock_z);

            //we really need neighbor information.
            ushort our_block = qgd.GetBlock_Glob_Drome(
              gblock_x,
              gblock_y,
              gblock_z);

            ivec3 gblock_xyz = new ivec3(gblock_x, gblock_y, gblock_z);

            ivec3 dblock_xyz = World.LocalGlobBlockPos_To_LocalDromeBlockPos(glob.Pos, gblock_xyz);

            ushort solid_block = Block.GetSolid(our_block);
            ushort liquid_block = Block.GetLiquid(our_block);

            bool hassolid = Block.HasSolid(our_block);
            bool haswater = Block.HasWater(our_block);

            if (!hassolid && !haswater)
            {
              continue;
            }

            vec3 block_pos_rel_R3 = new vec3(World.BlockSizeX * gblock_x, World.BlockSizeY * gblock_y, World.BlockSizeZ * gblock_z);
            vec3 block_pos_rel_R3_Center = block_pos_rel_R3 + new vec3(World.BlockSizeX * 0.5f, World.BlockSizeY * 0.5f, World.BlockSizeZ * 0.5f);
            vec3 block_pos_abs_R3_Center = block_pos_rel_R3_Center + glob.OriginR3;

            if (hassolid)
            {
              if (Block.IsMeshItem(our_block))
              {
                BlockItem bi = null;
                if (BlockItems.TryGetValue(solid_block, out bi))
                {
                  List<vec3> vecs = null;
                  if (qgd.async_block_items == null)
                  {
                    qgd.async_block_items = new Dictionary<BlockItem, List<vec3>>();
                  }
                  if (!qgd.async_block_items.TryGetValue(bi, out vecs))
                  {
                    vecs = new List<vec3>();
                    qgd.async_block_items.Add(bi, vecs);
                  }
                  vecs.Add(block_pos_abs_R3_Center);
                }
                else
                {
                  Gu.Log.Error("The block item for code '" + solid_block + "' was not found ");
                }
              }
              else if (BlockTiles.TryGetValue(solid_block, out BlockTile bt))
              {
                qgd.CreateBuffers();

                ushort b_above = qgd.GetBlock_Glob_Drome(gblock_x, gblock_y + 1, gblock_z);
                ushort b_below = qgd.GetBlock_Glob_Drome(gblock_x, gblock_y - 1, gblock_z);

                b_above = Block.GetSolid(b_above);
                b_below = Block.GetSolid(b_above);

                if (bt.MeshType == BlockMeshType.Billboard)
                {
                  TopologizeGlob_Billboard(drome, glob, bt, block_pos_rel_R3, our_block, b_above, b_below, qgd, gblock_xyz, dblock_xyz);
                }
                else if (bt.MeshType == BlockMeshType.Block)
                {
                  TopologizeGlob_Block(drome, glob, bt, block_pos_rel_R3, our_block, b_above, b_below, qgd, gblock_xyz, false, dblock_xyz);
                }

              }
              else
              {
                //Could not find block tile
                Gu.Log.Error("Could not find block tile for code '" + solid_block + "' ");
                Gu.DebugBreak();
              }

            }

            if (haswater)
            {
              qgd.CreateBuffers();

              //ushort[] b_n = new ushort[]
              //{
              //  qgd.GetBlock_Glob_Drome(gblock_x-1, gblock_y, gblock_z),
              //  qgd.GetBlock_Glob_Drome(gblock_x+1, gblock_y, gblock_z),
              //  qgd.GetBlock_Glob_Drome(gblock_x, gblock_y-1, gblock_z),
              //  qgd.GetBlock_Glob_Drome(gblock_x, gblock_y+1, gblock_z),
              //  qgd.GetBlock_Glob_Drome(gblock_x, gblock_y, gblock_z-1),
              //  qgd.GetBlock_Glob_Drome(gblock_x, gblock_y, gblock_z+1),
              //};
              //for(int bi=0; bi<6; bi++)
              //{
              //  b_n[bi] = Block.GetLiquid(b_n[bi]);
              //}

              if (BlockTiles.TryGetValue(BlockItemCode.Water, out BlockTile bt))
              {
                ushort b_above = qgd.GetBlock_Glob_Drome(gblock_x, gblock_y + 1, gblock_z);
                ushort b_below = qgd.GetBlock_Glob_Drome(gblock_x, gblock_y - 1, gblock_z);

                b_above = Block.GetLiquid(b_above);
                b_below = Block.GetLiquid(b_above);

                //We can expand the growthinfos if we want to use different trextures (may not be oto mu0udsofuoifujh
                TopologizeGlob_Block(drome, glob, bt, block_pos_rel_R3, our_block, b_above, b_below, qgd, gblock_xyz, true, dblock_xyz);
                //TopologizeGlob_Liquid(drome, glob, bt, block_pos_rel_R3, our_block, b_above, b_below, qgd, gblock_x, gblock_y, gblock_z);
              }
            }

          }//for x
        }//for y
      }//for z
      qgd.CopiedBlocks = null;

      if (glob.State != GenState.Deleted)
      {
        glob.State = GenState.GenEnd;
      }

    }
    private void TopologizeGlob_Billboard(Drome drome, Glob glob, BlockTile bt, vec3 block_pos_rel_R3,
      ushort our_block, ushort b_above, ushort b_below,
      QueuedGlobData_WithKernel qgd,
      ivec3 gblock_xyz,
      ivec3 dblock_xyz)
    {
      MtTex[] patches = bt.GetUVPatch(BlockSide.Left, b_above, b_below); //Just pass Zero here, because all the side faces for billboards are the same for now

      for (int iface = 0; iface < 2; ++iface)
      {
        //Texs are the same for billboards regardless of which face. But it should be side.
        uint foff = (uint)qgd.async_verts.Count;
        vec2[] texs = new vec2[4];
        texs = patches[BlockTileUVSide.Side].GetQuadTexs();

        Light_And_AddBlockFaceV4I6(iface,
          gblock_xyz,
          drome, glob, qgd,
          our_block, b_above, b_below,
          block_pos_rel_R3, WorldStaticData.bb_verts_face_zup, WorldStaticData.bb_face_inds_zup, texs, false, dblock_xyz);
      }
    }

    private void TopologizeGlob_Block(Drome drome, Glob glob, BlockTile bt, vec3 block_pos_rel_R3,
      ushort our_block, ushort b_above, ushort b_below,
      QueuedGlobData_WithKernel qgd, ivec3 gblock_xyz, bool liquid, ivec3 dblock_xyz)
    {
      //    6    7
      // 2    3
      //    4    5
      // 0    1

      for (int iface = 0; iface < 6; ++iface)
      {
        //TODO: remove the "default" condition in grid. very slow. Replace with "global drome" code
        ushort b_n = qgd.GetBlock_Glob_Drome(
          gblock_xyz.x + WorldStaticData.n_off[iface].x,
          gblock_xyz.y + WorldStaticData.n_off[iface].y,
          gblock_xyz.z + WorldStaticData.n_off[iface].z);

        //Neighbor cull check
        if (
          //  bt.IsVisible()

          (!liquid && (
          (Block.HasNoSolid(b_n)) || //Solid blocks - don't gen missing, but if it's air, or water, gen.
          (Block.IsDecalOr2Sided(b_n)) || //always topo next to decals (for now)
          Block.IsMeshItem(b_n)  //always topo next to meshes (for now)
          ))
          //Hack liquid
          ||
          (liquid && ((!Block.HasWater(b_n) && (Block.Is(b_n, BlockItemCode.Air) || (!Block.IsSolidBlock(our_block) && (iface == 3)))))) // face 3 = top (because water does not ever each top) only cull if the cur is solid

          )
        {
          MtTex[] patches = bt.GetUVPatch((BlockSide)iface, b_above, b_below);
          vec2[] texs = new vec2[4];

          if (bt.FaceInfos != null && bt.FaceInfos.Length == 3)
          {
            if ((iface == 0) || (iface == 1) || (iface == 4) || (iface == 5))
            {
              //LRAF
              texs = patches[BlockTileUVSide.Side].GetQuadTexs();
            }
            else if (iface == 2)
            {
              //B
              texs = patches[BlockTileUVSide.Bottom].GetQuadTexs();
            }
            else if (iface == 3)
            {
              //T
              texs = patches[BlockTileUVSide.Top].GetQuadTexs();
            }
          }
          else
          {
            //The Top/Side/Bot tile images could not be found (were not created) - default to the whole megatexture [0,1]
            texs[0] = WorldStaticData.bx_verts_face[iface, 0]._x;
            texs[1] = WorldStaticData.bx_verts_face[iface, 1]._x;
            texs[2] = WorldStaticData.bx_verts_face[iface, 2]._x;
            texs[3] = WorldStaticData.bx_verts_face[iface, 3]._x;
          }

          Light_And_AddBlockFaceV4I6(iface,
            gblock_xyz,
            drome, glob, qgd,
            our_block, b_above, b_below,
            block_pos_rel_R3, WorldStaticData.bx_verts_face, WorldStaticData.bx_face_inds, texs, liquid,
            dblock_xyz);

        }
      }
    }
    private void TopologizeGlob_LiquidBlock(Drome drome, Glob glob, BlockTile bt, vec3 block_pos_rel_R3, ushort our_block, ushort b_above, ushort b_below, QueuedGlobData_WithKernel qgd, int gblock_x, int gblock_y, int gblock_z)
    {
      //get neighbor kernel
      ushort[] nblock = new ushort[6];
      for (int ni = 0; ni < 6; ++ni)
      {
        nblock[ni] = qgd.GetBlock_Glob_Drome(
          gblock_x + WorldStaticData.n_off[ni].x,
          gblock_y + WorldStaticData.n_off[ni].y,
          gblock_z + WorldStaticData.n_off[ni].z);
      }

      for (int iface = 0; iface < 6; ++iface)
      {
        //TODO: remove the "default" condition in grid. very slow. Replace with "global drome" code



      }
    }
    private void Light_And_AddBlockFaceV4I6(int iface, ivec3 gblock_xyz,
    Drome drome, Glob glob, QueuedGlobData_WithKernel qgd, ushort our_block, ushort b_above, ushort b_below, vec3 block_pos_rel_R3,
    v_v3n3x2[,] verts_face, uint[] inds_face, vec2[] texs_face, bool liquid,
    ivec3 dblock_xyz)
    {
      uint foff = (uint)qgd.async_verts.Count;

      Gu.Assert(iface < 6);
      Gu.Assert(iface < verts_face.Length); //I think the lenght of md arrays is the rank index.
      Gu.Assert(inds_face.Length == 6);
      Gu.Assert(texs_face.Length == 4);


      //Verts + Indexes   
      vec3 block_pos_abs_R3 = block_pos_rel_R3 + glob.OriginR3;
      for (int vi = 0; vi < 4; ++vi)
      {
        float liquid_height = 0;
        //subtract some height from liquid
        //Test / Hack - subtract some height from the water to make it more cooler
        //in the future we will use this to calculate actual water height.
        if (liquid && (
          (iface == 0 && (vi == 2 || vi == 3)) ||
          (iface == 1 && (vi == 2 || vi == 3)) ||
          (iface == 3) ||
          (iface == 4 && (vi == 2 || vi == 3)) ||
          (iface == 5 && (vi == 2 || vi == 3))
          ))
        {
          if (b_above > 0)
          {
            //We are attaching to liquid above
            liquid_height = 0;
          }
          else
          {
            var liquuuud = Block.GetLiquid(our_block);
            liquid_height = BlockSizeY * ((float)liquuuud / ((float)(Block.MaxLiquid)));
          }

        }

        //Points (blocks)
        qgd.async_verts.Add(new v_v3n3x2u1()
        {
          _v = verts_face[iface, vi]._v + block_pos_abs_R3 - new vec3(0, liquid_height, 0),
          _n = verts_face[iface, vi]._n,
          _x = texs_face[vi],
          _u = 0//TODO: material id
        });

        //Colors for points
        qgd.async_colors.Add(new vec3(1, 1, 1));

      }
      qgd.async_face_data.Add(new VisibleBlockFaceData()
      {
        faceIdx = (byte)iface,
        x = (short)dblock_xyz.x,
        y = (short)dblock_xyz.y,
        z = (short)dblock_xyz.z
      });

      if (!Block.IsDecalOr2Sided(our_block))
      {
        for (int ii = 0; ii < 6; ++ii)
        {
          qgd.async_inds_op.Add(Convert.ToUInt16(foff + inds_face[ii]));
        }
      }
      else
      {
        for (int ii = 0; ii < 6; ++ii)
        {
          qgd.async_inds_tp.Add(Convert.ToUInt16(foff + inds_face[ii]));
        }
      }
    }
    private double[] rnd = {
      Random.NextD(0.1, Math.PI * 2.0),
      Random.NextD(0.1, Math.PI * 2.0),
      Random.NextD(0.1, Math.PI * 2.0),
      Random.NextD(0.1, Math.PI * 2.0),
      Random.NextD(0.1, Math.PI * 2.0),
      Random.NextD(0.1, Math.PI * 2.0),
      Random.NextD(0.1, Math.PI * 2.0)
    };
    private float BaseLandDensity(vec3 world_pos)
    {
      float numCrustDromes = 2;
      float crustHeight = DromeWidthY * numCrustDromes;

      double d = -world_pos.y;

      double wx = Math.Pow(world_pos.x / World.BlockSizeX, 1) + 200.0;// * Math.Pow(world_pos.x,1.2);
      double wy = (world_pos.y / World.BlockSizeY);
      double wz = Math.Pow(world_pos.z / World.BlockSizeZ, 1) + 200.0;// * Math.Pow(world_pos.z,1.2);

      //basic hilly thingy
      for (int ia = 1; ia <= 7; ++ia)
      {
        double a = (float)ia;
        double f = 1.0 / a;

        double sign = ia % 2 == 0 ? -1.0f : 1.0f;//prevent huge hils

        double by = world_pos.y / World.BlockSizeY;

        d = d + (

          ((sign) * Math.Cos((wx * 0.2 * f) + rnd[0]) * 2.0 * a +
          (sign) * Math.Sin((wz * 0.2 * f) + rnd[1]) * 2.0 * a) *

         //Some variation to the landtopology
         ((sign) * Math.Cos((wx * 0.1 * f) + rnd[2]) * 1.0 * a +
         (sign) * Math.Sin((wz * 0.1 * f) + rnd[3]) * 0.03 * a) *

         ((sign) * Math.Cos(Math.Sin((wx * 0.74 * f) + rnd[4])) * -0.4 * a +
         (sign) * Math.Sin((wz * 0.074 * f) + rnd[5]) * 1.0 * a +
         (sign) * Math.Cos((wy * 0.74 * f) + rnd[6]) * -1.0 * a)

         +

         ((sign) * Math.Cos((wy * -0.14 * f) + rnd[4]) * -0.14834581 * a -
          (sign) * Math.Sin((wx * -0.17 * f) + rnd[5]) * 0.23488988 * a -
          (sign) * Math.Cos((wz * -0.01 * f) + rnd[6]) * 0.3413484584 * a)

          *
                   ((sign) * Math.Cos((wy * 0.4474744 * f) + rnd[1]) * 0.04134813 * a *
          (sign) * Math.Sin((wx * 0.9775432 * f) + rnd[2]) * 0.231481 * a -
          (sign) * Math.Cos((wz * 0.947547 * f) + rnd[3]) * -0.0413481 * a)

                    *
                   ((sign) * Math.Sin((wx * 0.04227 * f) + rnd[3]) * 1.147775457 * a +
          (sign) * Math.Sin((wz * 0.0977542 * f) + rnd[1]) * -1.223033045 * a *
          (sign) * Math.Sin((wx * 0.09432475 * f) + rnd[1]) * 1.4477546456 * a)


          *
                   ((sign) * Math.Cos((wx * 0.74 * f) + rnd[1]) * 0.04 * a *
          (sign) * Math.Cos((wy * 0.978254682 * f) + rnd[2]) * 0.0680565486 * a -
          (sign) * Math.Cos((wz * 0.94245672 * f) + rnd[3]) * 0.04134 * a)


                              *
                   ((sign) * Math.Sin((wx * 0.004 * f) + rnd[3]) * 0.143814658 * a +
          (sign) * Math.Cos((wy * 0.0752345 * f) + rnd[1]) * -0.228304185 * a *
          (sign) * Math.Sin((wz * 0.04632 * f) + rnd[1]) * 0.441348348 * a)


          *
                   ((sign) * Math.Cos((wx * 0.0474257 * f) + rnd[1]) * 0.144240456 * a *
          (sign) * Math.Sin((wy * 0.77547245 * f) + rnd[2]) * 0.204573685638 * a +
          (sign) * Math.Cos((wz * 0.0474572457 * f) + rnd[3]) * -0.140134863486 * a)

          *

          ((sign) * Math.Cos((wy * 3.3474257 * f) + rnd[6]) * -0.044424456 * a *
          (sign) * Math.Sin((wx * -2.77547245 * f) + rnd[5]) * 0.02004573685638 * a *
          (sign) * Math.Cos((wz * 2.0474572457 * f) + rnd[6]) * -0.09134863486 * a)


         ) * (double)BlockSizeY * (0.053745724);  //global amplitude
      }
      ;

      return (float)d;
    }
    public Drome GetDromeForGlob(Glob g)
    {
      Drome ret = null;
      ivec3 dvi = GlobPosToDromePos(g.Pos);
      _dromes.TryGetValue(dvi, out ret);
      return ret;
    }
    public void SetBlock(vec3 pos_r3, ushort b, BlockBits bits)
    {
      //ivec3 block_i3 = R3ToI3BlockGlobal(pt);
      foreach (var d in _dromes)
      {
        if (d.Value.Box.containsPointBottomLeftInclusive(pos_r3))
        {
          d.Value.SetBlock(pos_r3, b, bits);
        }
      }
    }
    private Drome QueueDrome(ivec3 gpos)
    {
      //Queue drome for scalar field generation
      Drome drome = null;
      _dromes.TryGetValue(gpos, out drome);
      if (drome == null)
      {
        drome = new Drome(this, gpos, Gu.Context.FrameStamp);

        QueuedDromeData qdd = new QueuedDromeData();
        qdd.drome = drome;
        qdd.gpos = gpos;
        double dist = (double)(float)(drome.CenterR3 - Player.Position_World).length();
        qdd.DistanceToPlayer = dist;
        _dromes.Add(gpos, drome);

        _queuedDromes.Add((float)dist, qdd);
      }
      return drome;
    }
    public Glob QueueGlob(Glob oldGlob, Drome d, ivec3 gpos_glob_global_z3)
    {
      //Queue glob for triangle topology.
      //Create a new glob if we are ready to generate it (everything is locked).
      //Returns null if the required dromes (scalar fields) could not be locked
      Gu.Assert(d != null);

      var needed_dromes = GetRequiredScalarFields_or_GenerateThem(d, gpos_glob_global_z3);
      if (needed_dromes == null)
      {
        //Return null so that the requested Glob is not yet set, and we will generate it later.
        return null;
      }
      else
      {
        needed_dromes.Lock();
      }

      Glob g = new Glob(gpos_glob_global_z3, Gu.Context.FrameStamp, d);

      if (oldGlob != null)
      {
        //Save the mesh data, or else we get a flicker while the glob is generating.
        g.Opaque = oldGlob.Opaque;
        g.Transparent = oldGlob.Transparent;
        g.BlockItems = oldGlob.BlockItems;
        oldGlob = null;
      }

      //priority is no longer needed when sorting by distance
      QueuedGlobData_WithKernel qgd = new QueuedGlobData_WithKernel();
      qgd.ScalarFields = needed_dromes;
      qgd.MyGlob = g;
      d.Lock();
      //    CopyGlobBlocks_Sync(d, qgd);
      qgd.async_block_items = new Dictionary<BlockItem, List<vec3>>();//completely new. do not copy .. we generate it.

      double dist = (qgd.MyGlob.CenterR3 - Player.Position_World).length();
      qgd.DistanceToPlayer = dist;

      _queuedGlobs.Add((float)dist, qgd);

      return g;
    }
    private void LaunchGlobAndDromeQueues()
    {
      WindowContext wc = Gu.Context;
      while (_queuedGlobs.Count > 0)
      {
        if (_globsGenerating >= _globsGenerating_Max)
        {
          break;
        }
        var kvp = _queuedGlobs.First();
        _queuedGlobs.Remove(kvp);
        _globsGenerating++;

        var qgd = kvp.Value;

        ThreadPool.QueueUserWorkItem(stateinfo =>
        {
          TopologizeGlob(qgd);
          Gu.Context.Gpu.Post_To_RenderThread(wc, x =>
          {
            FinishGeneratingGlob_Sync(qgd);
            _globsGenerating--;
          });
        });
      }


      while (_queuedDromes.Count > 0)
      {
        if (_dromesGenerating >= _dromesGenerating_Max)
        {
          break;
        }
        var kvp = _queuedDromes.First();
        _queuedDromes.Remove(kvp);

        _dromesGenerating++;

        var qdd = kvp.Value;

        ThreadPool.QueueUserWorkItem(info =>
        {
          //We're doing file operations on another thread eeww
          var drome = TryLoadDrome(qdd.gpos);

          if (drome == null)
          {
            GenerateDrome_Async(qdd);
            SaveDrome(qdd.drome);
            drome = qdd.drome;
          }

          drome.LastVisible_ms = Gu.Milliseconds();

          Gu.Context.Gpu.Post_To_RenderThread(wc, x =>
          {
            _dromesGenerating--;
            qdd.drome.GenState = GenState.Ready;
          });
        });

      }

    }
    private void CopyGlobBlocks_Sync(Drome drome_in, QueuedGlobData_WithKernel qgd)
    {
      ivec3 block_off_drome = GlobGlobal_Z3_To_DromeLocal_Z3(qgd.MyGlob.Pos) * new ivec3(World.GlobBlocksX, World.GlobBlocksY, World.GlobBlocksZ);

      //We don't support for kernels that span > +/-1 neighbor drome (dromes are massive anyway, there'd be no point)
      Gu.Assert(GlobBlocks_Kernel_MarginX < DromeBlocksX); //This would be a massive kernel..
      Gu.Assert(GlobBlocks_Kernel_MarginY < DromeBlocksY);
      Gu.Assert(GlobBlocks_Kernel_MarginZ < DromeBlocksZ);

      //Copy a "kernel" to the given glob data. uigh..
      qgd.CopiedBlocks = new ushort[World.GlobBlocksX_Gen_Kernel * World.GlobBlocksY_Gen_Kernel * World.GlobBlocksZ_Gen_Kernel];
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
                if (drome_n_or_cur.GenState != GenState.Ready)
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
            if (drome_n_or_cur != null && drome_n_or_cur.HasBlockData())
            {
              //int doff = Drome.BlockOffset(d_or_n_block_off);
              //qgd.CopiedBlocks[qoff] = drome_n_or_cur.Blocks[doff];

              qgd.CopiedBlocks[qoff] = drome_n_or_cur.Blocks.Get(d_or_n_block_off);

            }
            else
            {
              qgd.CopiedBlocks[qoff] = BlockItemCode.Missing;
            }
          }
        }
      }

    }
    List<ivec3> _scalarFieldsNeeded = new List<ivec3>();
    private DromeKernel GetRequiredScalarFields_or_GenerateThem(Drome d, ivec3 gpos_glob_global_z3)
    {
      //Get a kernel of up to 8 dromes required to seamlessly iterate a scalar field.
      //  This returns 27, however we only ever get 8 max (1=center, 2=face, 4=edge, 8=corner)
      DromeKernel dk = new DromeKernel();
      ivec3 my_pos = d.Pos;
      ivec3 glob_local_z3 = GlobGlobal_Z3_To_DromeLocal_Z3(gpos_glob_global_z3);

      //Add into the C27

      for (int z = -1; z <= 1; z++)
      {
        for (int y = -1; y <= 1; y++)
        {
          for (int x = -1; x <= 1; x++)
          {
            //drome neighbor offset -1, 0, 1 along each axis
            int dx = 0, dy = 0, dz = 0;
            if ((x == -1) && (glob_local_z3.x == 0))
            {
              dx = x;
            }
            else if ((x == 1) && (glob_local_z3.x == World.DromeGlobsX - 1))
            {
              dx = x;
            }

            if ((y == -1) && (glob_local_z3.y == 0))
            {
              dy = y;
            }
            else if ((y == 1) && (glob_local_z3.y == World.DromeGlobsY - 1))
            {
              dy = y;
            }

            if ((z == -1) && (glob_local_z3.z == 0))
            {
              dz = z;
            }
            else if ((z == 1) && (glob_local_z3.z == World.DromeGlobsZ - 1))
            {
              dz = z;
            }

            if (dk.Get(dx + 1, dy + 1, dz + 1) == null)
            {
              ivec3 n_off = my_pos + new ivec3(dx, dy, dz);
              Drome d_n = FindDromeI3(n_off, true);
              if (d_n == null)
              {
                //The drome was not loaded. Queue it so we have it, and generate this glob later.
                _scalarFieldsNeeded.Add(n_off);
                return null;
              }
              else
              {
                dk.Set(dx + 1, dy + 1, dz + 1, d_n);
              }
            }

          }
        }
      }

      return dk;
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
    private ushort? FindBlockR3_Drome(vec3 R3_pos)
    {
      Drome d = FindDromeR3(R3_pos);
      if (d == null)
      {
        return null;
      }
      if (d.GenState != GenState.Ready)
      {
        return null;
      }
      ivec3 bpos = Drome.R3toI3BlockLocal_Drome(R3_pos);
      ushort b = d.GetBlock(bpos);

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
    public static ivec3 R3ToI3BlockLocal_Any(vec3 R3, float cont_w_x, float cont_w_y, float cont_w_z)
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
    public static ivec3 R3toI3Glob(vec3 R3)
    {
      //v3toi3Node
      ivec3 gpos = new ivec3(
         (int)Math.Floor(R3.x / World.GlobWidthX),
         (int)Math.Floor(R3.y / World.GlobWidthY),
         (int)Math.Floor(R3.z / World.GlobWidthZ));
      return gpos;
    }
    //private Glob FindGlobR3(vec3 R3_pos, GlobCollection c)
    //{
    //  ivec3 gpos = R3toI3Glob(R3_pos);

    //  return FindGlobI3(gpos, c);
    //}
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
      return GetBlockBoxLocal(b.Drome, b.BlockPosLocalZ3, padding);
    }
    private static Box3f GetBlockBoxLocal(Drome d, ivec3 local, float padding = 0)
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
    public static Box3f GetBlockBoxGlobalR3(vec3 pt, float padding = 0)
    {
      //Snap the point pt to the block grid, and return the bound box of that block
      Box3f box = new Box3f();
      box._min = R3ToI3BlockGlobal(pt).toVec3() * new vec3(BlockSizeX, BlockSizeY, BlockSizeZ);
      box._max.x = box._min.x + BlockSizeX;
      box._max.y = box._min.y + BlockSizeY;
      box._max.z = box._min.z + BlockSizeZ;
      box._max += padding;
      box._min += padding;
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
    public static ivec3 GlobGlobal_Z3_To_DromeLocal_Z3(ivec3 pos_glob_global_z3)
    {
      //Convert 
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
      ivec3 glob_off_local = GlobGlobal_Z3_To_DromeLocal_Z3(glob_pos_z3_global);

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

    public PickedBlock RaycastBlock_2(PickRay3D pr)
    {
      //Less buggy raycasting using BVH. Also, faster. Usually ~20 checks until we hit a block.
      //This works for rays and e-beams
      PickedBlock pb = new PickedBlock();
      pb.AddPickedBlockBoxes_Debug = true;

      //create list of dromes sorted by distance from the ray.
      MultiMap<float, Drome> dlist = new MultiMap<float, Drome>();
      foreach (var d in _dromes)
      {
        BoxAAHit bh = new BoxAAHit();
        if (d.Value.Box.LineOrRayIntersectInclusive_EasyOut(pr, ref bh))
        {
          dlist.Add((float)bh._t1, d.Value);
        }
      }

      foreach (var d in dlist)
      {
        d.Value.RaycastBlockBVH(d.Value, pr, ref pb);
      }

      return pb;
    }
    public PickedBlock RaycastBlock(PickRay3D pr)
    {
      //This algorithm sucks. We need to make a new one.
      //@param radius - if we are raycasting a sphere. Otherwise 0 for points.
      //Pick Block/blocks in the world.
      //  We use an inclusive box test just for the hit, but not for the recursion i.e ray ends up on an edge and we recur to 2 blocks and end up with a lot of duplicates.
      //  This algorithm.. isn't the best we should use a 3d line drawing algorithm to avoid duplicate checks. stepx stepx stepy stepx .. 
      //TODO: much faster if we marched the drome/glob first, then the blocks. We do only blocks
      PickedBlock pb = new PickedBlock();
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

        vec3 cpos_r3 = toCheck[0]; //center of current block as we march along this ray, Bresenham-style
        toCheck.RemoveAt(0);

        vec3 cpos_proj = pr.Project(cpos_r3);
        float cpos_proj_len2 = (cpos_proj - pr.Origin).length2();
        float cpos_ortho_len2 = (cpos_r3 - cpos_proj).length2();
        //TODO: Error: radius . radius is not r^2 here - ellipsoid radius is not r dot r for ortho_len_2, it chage
        if (cpos_proj_len2 > pr.Length * pr.Length)
        {
          //We are beyond the ray, or,
          // we checked a block that was further along the ray than the current minimum hit distance.
          break;
        }

        ivec3 iglob = R3toI3Drome(cpos_r3);
        if ((cur_drome == null && already_checked_glob == false) || (icur_drome != iglob))
        {
          cur_drome = FindDromeI3(iglob);
          icur_drome = iglob;
          already_checked_glob = true;
          dbg_nglobcheck++;
        }

        if ((cur_drome != null) && (cur_drome.Blocks != null) && (cur_drome.BlockStats.RegionState != RegionState.Empty_AndNoData))
        {
          dbg_nblockcheck++;
          ivec3 b3i = Drome.R3toI3BlockLocal_Drome(cpos_r3);
          ushort b = cur_drome.GetBlock(b3i);
          if (!Block.HasNoSolid(b))
          {
            //b.IsEmpty()==false is sufficient for a hit. (drome,i3) is sufficient for the information too.
            //You don't need the other data than for more information.
            var blockbox = GetBlockBoxGlobalR3(cpos_r3);
            if (blockbox.LineOrRayIntersectInclusive_EasyOut(pr, ref bh))
            {
              pb.Drome = cur_drome;
              pb.Block = b;
              pb.BlockPosLocalZ3 = b3i;
              pb._t1 = (float)bh._t1;
              pb._t2 = (float)bh._t2;
              pb.HitPosR3 = pr.Origin + pr.Dir * (float)bh._t1;
              pb.BlockCenterR3 = cpos_r3;
              pb.HitNormal = blockbox.Normal(pb.HitPosR3);
              break;
            }
            else
            {
              //Error - we did in fact collilde with this block but the box says otherwise
              Gu.DebugBreak();
              int n = 0;
              n++;
            }
          }
        }

        Action<bool, vec3> recur_neighbor_by_dir = (dir_test, n_offset) =>
        {
          if (dir_test)
          {
            vec3 n_pos = cpos_r3 + n_offset;
            Box3f b = GetBlockBoxGlobalR3(n_pos);
            if (b.LineOrRayIntersectInclusive_EasyOut(pr, ref bh))
            {
              if (!toCheck.Contains(n_pos))
              {
                toCheck.Add(n_pos);
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

    private FileLoc GetTileFile(TileImage img)
    {
      WorldStaticData.TileImages.TryGetValue(img, out var loc);
      Gu.Assert(loc != null);
      return loc;
    }

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
        br.Write(Player.Position_Local);
        br.Write(Player.Rotation_Local);
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
        Player.Position_Local = br.ReadVec3();
        Player.Rotation_Local = br.ReadQuat();
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
          d.BlockStats.Serialize(br);
          d.Blocks.Serialize(br);

          if (d.GlobRegionStates == null)
          {
            br.Write((Int32)0);
          }
          else
          {
            var byteArr = new byte[Marshal.SizeOf(typeof(RegionBlocks)) * d.GlobRegionStates.Length];
            var pinnedHandle = GCHandle.Alloc(d.GlobRegionStates, GCHandleType.Pinned);
            Marshal.Copy(pinnedHandle.AddrOfPinnedObject(), byteArr, 0, byteArr.Length);
            pinnedHandle.Free();
            byte[] compressed = Gu.Compress(byteArr);
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
          d = new Drome(this, new ivec3(0, 0, 0), Gu.Context.FrameStamp);

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
            d.BlockStats.Deserialize(br);

            d.AllocateBlocks();
            d.Blocks.Deserialize(br);

            int compressed_count = br.ReadInt32();
            if (compressed_count == 0)
            {
              d.GlobRegionStates = null;
            }
            else
            {
              var compressed = br.ReadBytes(compressed_count);

              byte[] decompressed = Gu.Decompress(compressed);
              var numStructs = decompressed.Length / Marshal.SizeOf(typeof(RegionBlocks));

              Gu.Assert(numStructs == Drome.DromeRegionStateCount);

              d.GlobRegionStates = new RegionBlocks[numStructs];
              var pinnedHandle = GCHandle.Alloc(d.GlobRegionStates, GCHandleType.Pinned);
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

    #endregion


  }
}
