using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Loft
{

  #region Enums

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
  public class BeamSideIndex
  {
    public const int Left = 0;
    public const int Right = 1;
    public const int Back = 2;
    public const int Front = 3;
  }
  public class BeamFaceIndex
  {
    public const int Left = 0;
    public const int Right = 1;
    public const int Bottom = 2;
    public const int Top = 3;
    public const int Back = 4;
    public const int Front = 5;
  }
  public class BeamEdgeIndex
  {
    public const int BL = 0;
    public const int BR = 1;
    public const int TL = 2;
    public const int TR = 3;
  }
  public enum QuadNeighborIndex
  {
    BL, BR, TL, TR
  }
  public enum BeamNeighbor
  {
    Left, Right, Bot, Top, Back, Front,
  }
  public enum BeamCap
  {
    Bottom, Top
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
  public class TileCode
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

  #endregion

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

  public class WorldStaticData
  {
    //side - 1 of 4 sides of the column
    //face - 1 of 6 faces
    public static int[,] side_to_edge = new int[4, 2]{
     {2,0},
     {1,3},
     {0,1},
     {3,2}
    };
    public static int[] side_to_opposite_side = new int[4]{
      1, 0, 3, 2
    };
    public static int[] sideidx_to_faceidx = new int[4] {
      0, 1, 4, 5//lr,,af
    };
    public static int[] faceidx_to_sideidx = new int[6] {
      0, 1, -1, -1, 2, 3//lr,,af
    };
    public static vec3[] face_idx_to_normal = new vec3[] {
      new vec3(-1, 0, 0),
      new vec3( 1, 0, 0),
      new vec3( 0,-1, 0),
      new vec3( 0, 1, 0),
      new vec3( 0, 0,-1),
      new vec3( 0, 0, 1),
    };
    public static vec3[] side_direction = new vec3[] {
      new vec3( 0, 0,-1),
      new vec3( 0, 0, 1),
      new vec3( 0,-1, 0),
      new vec3( 0, 1, 0),
      new vec3( 0, 0,-1),
      new vec3( 0, 0, 1),
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
            { TileImage.Grass, new FileLoc("tx64_grass.png", EmbeddedFolder.Image) },
            { TileImage.GrassSide, new FileLoc("tx64_grass_side.png", EmbeddedFolder.Image) },
            { TileImage.Dirt, new FileLoc("tx64_dirt.png", EmbeddedFolder.Image) },
            { TileImage.Plank, new FileLoc("tx64_plank.png", EmbeddedFolder.Image) },
            { TileImage.Brick, new FileLoc("tx64_brick.png", EmbeddedFolder.Image) },
            { TileImage.Brick2, new FileLoc("tx64_brick2.png", EmbeddedFolder.Image) },
            { TileImage.Gravel, new FileLoc("tx64_gravel.png", EmbeddedFolder.Image) },
            { TileImage.Sand, new FileLoc("tx64_sand.png", EmbeddedFolder.Image) },
            { TileImage.Cedar, new FileLoc("tx64_cedar.png", EmbeddedFolder.Image) },
            { TileImage.Cedar_Needles, new FileLoc("tx64_cedar_needles.png", EmbeddedFolder.Image) },
            { TileImage.Cedar_Top, new FileLoc("tx64_cedar_top.png", EmbeddedFolder.Image) },
            { TileImage.Feldspar, new FileLoc("tx64_plagioclase_feldspar.png", EmbeddedFolder.Image) },
            { TileImage.Tussock, new FileLoc("tx64_tussock.png", EmbeddedFolder.Image) },
            { TileImage.Tussock2, new FileLoc("tx64_tussock2.png", EmbeddedFolder.Image) },
            { TileImage.Tussock_Stalk_Bot, new FileLoc("tx64_tussock_stalk_bot.png", EmbeddedFolder.Image) },
            { TileImage.Tussock_Stalk_Mid, new FileLoc("tx64_tussock_stalk_mid.png", EmbeddedFolder.Image) },
            { TileImage.Tussock_Stalk_Top, new FileLoc("tx64_tussock_stalk_top.png", EmbeddedFolder.Image) },
            { TileImage.Blank, new FileLoc("tx64_blank.png", EmbeddedFolder.Image) },
            { TileImage.Dandilion, new FileLoc("tx64_dandilion.png", EmbeddedFolder.Image) },
            { TileImage.Cracks1, new FileLoc("tx64_cracks1.png", EmbeddedFolder.Image) },
            { TileImage.Cracks2, new FileLoc("tx64_cracks2.png", EmbeddedFolder.Image) },
            { TileImage.Cracks3, new FileLoc("tx64_cracks3.png", EmbeddedFolder.Image) },
            { TileImage.Cracks4, new FileLoc("tx64_cracks4.png", EmbeddedFolder.Image) },
            { TileImage.Feldspar_Coal, new FileLoc("tx64_plagioclase_coal.png", EmbeddedFolder.Image) },
            { TileImage.Marble_Green, new FileLoc("tx64_marble_green.png", EmbeddedFolder.Image) },
            { TileImage.Marble_White, new FileLoc("tx64_marble_white.png", EmbeddedFolder.Image) },
            { TileImage.Water, new FileLoc("tx64_water.png", EmbeddedFolder.Image) },
            { TileImage.Seaweed, new FileLoc("tx64_seaweed.png", EmbeddedFolder.Image) },
            { TileImage.Clay, new FileLoc("tx64_clay.png", EmbeddedFolder.Image) },
            { TileImage.RedClay, new FileLoc("tx64_red_clay.png", EmbeddedFolder.Image) },
            { TileImage.RosePink, new FileLoc("tx64_rose_pink.png", EmbeddedFolder.Image) },
            { TileImage.RoseRed, new FileLoc("tx64_rose_red.png", EmbeddedFolder.Image) },
            { TileImage.Oak, new FileLoc("tx64_oak_side.png", EmbeddedFolder.Image) },
            { TileImage.Oak_Leaves, new FileLoc("tx64_oak_leaves.png", EmbeddedFolder.Image) },
         };

    // private static vec3[] bx_box = new vec3[8];
    // public static vec3[] bx_norms = new vec3[6];//lrbtaf
    // private static vec2[] bx_texs = new vec2[4];
    // public static v_v3n3x2t3u1[,] bx_verts_face { get; private set; } = new v_v3n3x2t3u1[6, 4];//lrbtaf
    // public static uint[] bx_face_inds { get; private set; }

    // private static vec3[] bb_planes_Zup = new vec3[8];
    // private static vec3[] bb_norms_Zup = new vec3[2];
    // public static v_v3n3x2t3u1[,] bb_verts_face_zup { get; private set; } = new v_v3n3x2t3u1[2, 4];//normals point +x, +z
    // public static uint[] bb_face_inds_zup { get; private set; }
  }
  public enum VertFlags
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
  public enum EdgeFlags
  {
    HasBeam = 0x01 //true if this edge has a beam attached.
  }
  public enum BeamFlags
  {
    TopConfigIsLeftConfig = 0x01, // the 2 triangles that form the top point to the right |/| vs, left |\|
    BotConfigIsLeftConfig = 0x02
  }
  public class BeamVert
  {
    public const ushort MaxVal = ushort.MaxValue;
    public const ushort MinVal = 1;
    //Beam edges can't be zero, in case we do use the RangeList (eventually)
    // however they must have a "zero height" because we allow for "pyramids"
  }
  public interface IRangeItem
  {
    public ushort Min { get; }
    public ushort Max { get; }
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct BeamEdge : IRangeItem
  {
    //The reason using short instead of float, is that it makes it very fast to index the vertex grabbing the monad, 
    //space, yes also space, but that's not a huge concern with beams
    //In Marshal.SizeOf can't serialize if it has sub-structs so BeamVert must be contained
    public ByteFlags Flags = new ByteFlags();
    public byte pad = 0;
    public ushort _min = 0;
    public ushort _max = 1;
    //Beam edges can't be zero, in case we do use the RangeList (eventually)
    // however they must have a "zero height" because we allow for "single pyramids"

    public ushort Min { get { return _min; } }
    public ushort Max { get { return _max; } }
    public ushort Top { get { return _max; } set { _max = value; } }
    public ushort Bot { get { return _min; } set { _min = value; } }

    public ushort Height
    {
      get
      {
        return (ushort)(_max - _min);
      }
    }
    public BeamEdge() { }
    public BeamEdge(ushort b, ushort t, byte eflags = 0, byte v0flags = 0, byte v1flags = 0)
    {
      Bot = b;
      Top = t;
      Flags.Set((int)eflags);
    }
    public bool HasHeight(ushort h)
    {
      return h >= _min && h <= _max;
    }
    public bool ContainsOrEqual(ushort h)
    {
      return h >= Min && h <= Max;
    }
    public void Sanitize()
    {
      if (_max <= _min)
      {
        _max = (ushort)(_min + 1);
      }
    }
  }
  public class Beam : ISerializeBinary
  {
    //we could store BeamEdge verts in a grid and have "on/off" for the voxel, however, that is inefficient given what we're trying to achieve.
    public const int c_iEdgeCount = 4;
    public const int c_iSideCount = 6;

    public BeamEdge[] Edges = new BeamEdge[4]; //6 bytes * 4 = 24 bytes;
    public ushort[] Tiles = new ushort[c_iSideCount] {  // 6 * 2 = 12 bytes
      TileCode.Dirt , TileCode.Dirt, TileCode.Dirt,
      TileCode.Grass, TileCode.Dirt, TileCode.Dirt  };
    ShortFlags Flags = new ShortFlags(); // 2 bytes

    public static bool ContainsHeightInclusive_WithCaps(ushort val, ushort min, ushort max)
    {
      return ((val >= min) && (val <= max));
    }
    public static int FaceIdxToSideIdx(int face)
    {
      return face == 3 ? 2 : face == 4 ? 3 : face;
    }
    public BeamEdge?[] GetEdge(int side)
    {
      BeamEdge?[] ret = new BeamEdge?[2];
      ret[0] = Edges[WorldStaticData.side_to_edge[side, 0]]; ret[1] = Edges[WorldStaticData.side_to_edge[side, 1]];
      return ret;
    }
    public ushort MinHeight(int face)
    {
      if (face == 2)
      {
        return MinHeight();
      }
      if (face == 3)
      {
        return CaplessMax;
      }
      BeamEdge?[] e = GetEdge(FaceIdxToSideIdx(face));
      ushort miny = (ushort)Math.Min(e[0].Value.Min, e[1].Value.Min);
      return miny;
    }
    public ushort MaxHeight(int face)
    {
      if (face == 2)
      {
        return CaplessMin;
      }
      if (face == 3)
      {
        return MaxHeight();
      }
      BeamEdge?[] e = GetEdge(FaceIdxToSideIdx(face));
      ushort miny = (ushort)Math.Max(e[0].Value.Max, e[1].Value.Max);
      return miny;
    }
    public ushort MaxHeight()
    {
      ushort y = 0;
      foreach (var e in Edges)
      {
        y = Math.Max(e._max, y);
      }
      return y;
    }
    public ushort MinHeight()
    {
      ushort y = BeamVert.MaxVal;
      foreach (var e in Edges)
      {
        y = Math.Min(e._min, y);
      }
      return y;
    }
    public ushort CaplessMax
    {
      //returns the height without the cap (e.g. non-homogeneous, pyrimidal, geometry) .
      get
      {
        ushort y = 0;
        foreach (var e in Edges)
        {
          y = Math.Min(e._max, y);
        }
        return y;
      }
    }
    public ushort CaplessMin
    {
      //returns the height without the cap (e.g. non-homogeneous, pyrimidal, geometry) .
      get
      {
        ushort y = BeamVert.MaxVal;
        foreach (var e in Edges)
        {
          y = Math.Max(e._min, y);
        }
        return y;
      }
    }
    public Beam() { }
    public Beam(ushort t, ushort b) : this(new ushort[] { t, b, t, b, t, b, t, b })
    {
    }
    public Beam(ushort[] heights, byte[]? vflags = null, byte[]? eflags = null)
    {
      //BeamEdgeIndex
      Gu.Assert(heights.Length == 8);
      for (int i = 0; i < 8; i += 2)
      {
        if (heights[i + 0] > heights[i + 1])
        {
          Gu.Log.Warn($"beam height {heights[i + 0]},{heights[i + 1]} was invalid.");
          heights[i + 1] = heights[i + 0];
          Gu.DebugBreak();
        }
        Edges[i / 2] = new BeamEdge(
          heights[i + 0],
          heights[i + 1],
          (byte)(eflags != null ? eflags[i / 2] : 0),
          (byte)(vflags != null ? vflags[i] : 0)
        );

      }
    }
    public bool TopConfigIsLeftConfig()
    {
      return Flags.Test((int)BeamFlags.TopConfigIsLeftConfig);
    }
    public bool BotConfigIsLeftConfig()
    {
      return Flags.Test((int)BeamFlags.BotConfigIsLeftConfig);
    }
    public void Serialize(BinaryWriter bw)
    {
      bw.Write(Edges);
      bw.Write(Tiles);
      Flags.Serialize(bw);
    }
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      Edges = br.Read<BeamEdge>();
      Tiles = br.Read<ushort>();
      Flags.Deserialize(br, version);
    }
  }
  // public class VertexMonad
  // {
  //   BeamVert[] Verts = null;
  //   public int EdgeIndex = BeamEdgeIndex.BL;
  //   public VertexMonad()
  //   {
  //   }
  // }
  public class BeamList : ISerializeBinary
  {
    public List<Beam> _beams = new List<Beam>();
    public List<Beam> Beams { get { return _beams; } }
    public BeamList() { }
    public void AddBeam(Beam b)
    {
      _beams.Add(b);
      _beams.Sort((x, y) => x.MinHeight() - y.MinHeight());
    }
    public List<Beam> GetBeamsForHeight(ushort h)
    {
      //Return an ordered list of beams by range - bottom to top
      List<Beam> beams = new List<Beam>();
      foreach (var beam in _beams)
      {
        if (beam.MinHeight() >= h && beam.MaxHeight() <= h)
        {
          beams.Add(beam);
        }
      }
      return beams;
    }
    public List<Beam> GetBeamsForRange(ushort min, ushort max)
    {
      //Return an ordered list of beams - bottom to top
      List<Beam> beams = new List<Beam>();
      foreach (var beam in _beams)
      {
        var bmin = beam.MinHeight();
        var bmax = beam.MaxHeight();

        if (Beam.ContainsHeightInclusive_WithCaps(min, bmin, bmax) || Beam.ContainsHeightInclusive_WithCaps(max, bmin, bmax))
        {
          beams.Add(beam);
        }
      }
      return beams;
    }
    public void Serialize(BinaryWriter bw)
    {
      bw.Write((Int32)this.Beams.Count);
      foreach (var b in this.Beams)
      {
        b.Serialize(bw);
      }
    }
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      var nbeams = br.ReadInt32();
      for (int n = 0; n < nbeams; n++)
      {
        Beam b = new Beam();
        b.Deserialize(br, version);
        Beams.Add(b);
      }
    }
  }
  public class Glob : ISerializeBinary
  {
    public enum GlobState
    {
      None,
      CreatedOrLoaded, // user added a new one or we just loaded from disk
      Queued,  //we are going to topo the glob
      Topologized, //can render
      Edited, //user edited , needs to be queued
      //deleted
    }
    public Int64 GeneratedFrameStamp { get; private set; } = 0;
    public Drawable Transparent = null;
    public Drawable Opaque = null;
    public ivec3 Pos = new ivec3(0, 0, 0);
    private World _world = null;
    public Grid2D<BeamList> BeamGrid = null;
    public GlobState State = GlobState.CreatedOrLoaded;
    private bool _locked = false;
    public string Name
    {
      get
      {
        return "glob:" + Pos.ToString();
      }
    }
    public void Lock() { _locked = true; }
    public void Unlock() { _locked = false; }


    public bool HasMeshData()
    {
      return Opaque != null || Transparent != null;
    }

    public vec3 OriginR3
    {
      get
      {
        vec3 r = new vec3(Pos.x * _world.Info.GlobWidthX, Pos.y * _world.Info.GlobWidthY, Pos.z * _world.Info.GlobWidthZ);
        return r;
      }
    }
    public Glob(World w, ivec3 pos)
    {
      BeamGrid = new Grid2D<BeamList>(w.Info.GlobBlocksX, w.Info.GlobBlocksZ);
      _world = w;
      Pos = pos;
      GeneratedFrameStamp = Gu.Context.FrameStamp;
    }
    public void DestroyGlob()
    {
      _world = null;
    }
    public void Iterate(Func<Grid2D<BeamList>, int, int, LambdaBool> f, bool inclusive = false)
    {
      BeamGrid.Iterate(f, inclusive);
    }
    public void Edit_GenFlat(float y_base_rel, float y_height_rel)
    {

      BeamGrid.Iterate((g, x, z) =>
      {
        var bl = new BeamList();
        bl.AddBeam(new Beam(_world.Info.ConvertHeight(y_base_rel), _world.Info.ConvertHeight(y_height_rel)));
        BeamGrid.Set(new ivec2(x, z), bl);
        return LambdaBool.Continue;
      });
    }
    private float SanitizeHeight(float f, float min, float max)
    {
      Gu.Assert(min <= max);
      Gu.Assert(f >= 0 && f <= 1);
      return min + f * (max - min);
    }
    public void Edit_GenHills(float min_height, float max_height, bool rnadomJunk)
    {
      //TODO: generate linked topology
      var rimg = Image.RandomImage_R32f(BeamGrid.SizeX + 1, BeamGrid.SizeY + 1, new Minimax<float>(0, 1)); //+1 due to vertex, not beam
      rimg = rimg.CreateHeightMap(2, 1f, 2);
      rimg = rimg.Normalized_R32f();

      Lib.SaveImage(System.IO.Path.Combine(Gu.LocalTmpPath, "test-hm1a.png"), rimg.Convert(Image.ImagePixelFormat.RGBA32ub, true), true);

      //we move from +x, +z, and do this in the grid too
      BeamGrid.Iterate((g, x, z) =>
      {
        float max = max_height; // _world.Info.GlobWidthY * 0.9f;
        float min = min_height;

        //height
        var test = rimg.GetPixel_R32f(x, z + 1);
        ushort tl = _world.Info.ConvertHeight(SanitizeHeight(rimg.GetPixel_R32f(x, z + 1), min, max));
        ushort tr = _world.Info.ConvertHeight(SanitizeHeight(rimg.GetPixel_R32f(x + 1, z + 1), min, max));
        ushort bl = _world.Info.ConvertHeight(SanitizeHeight(rimg.GetPixel_R32f(x, z), min, max));
        ushort br = _world.Info.ConvertHeight(SanitizeHeight(rimg.GetPixel_R32f(x + 1, z), min, max));
        //base
        ushort bs = 0;

        if (tl == bs) { tl = (ushort)(bs + 1); }
        if (tr == bs) { tr = (ushort)(bs + 1); }
        if (bl == bs) { bl = (ushort)(bs + 1); }
        if (br == bs) { br = (ushort)(bs + 1); }

        //TESTING DEBUG
        //Testing random "cliff" beams
        if (rnadomJunk && ((x == 8 && z == 8) || (x == 9 && z == 8)))
        {
          tl = tr = bl = br = BeamVert.MaxVal;
        }

        BeamList b = new BeamList();
        // b.AddBeam(new Beam(new ushort[] { (ushort)(bl/2), bl, (ushort)(br/2), br, (ushort)(tl/2), tl, (ushort)(tr/2), tr }));
        b.AddBeam(new Beam(new ushort[] { bs, bl, bs, br, bs, tl, bs, tr }));
        BeamGrid.Set(new ivec2(x, z), b);

        var bbbtest = BeamGrid.Get(new ivec2(x, z));

        return LambdaBool.Continue;
      }, false);
    }
    public void Serialize(BinaryWriter bw)
    {
      bw.Write((ivec3)Pos);
      this.BeamGrid.Serialize(bw);
    }
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      Pos = br.ReadIVec3();
      this.BeamGrid.Deserialize(br, version);
    }
  }
  public class GlobArray
  {
    //GlobManifold, QueuedGlob
    public const int c_iCount = 27;
    public const int c_iCenterGlob = 13;
    public const int c_iLeftGlob = 12;
    public const int c_iRightGlob = 14;
    public const int c_iBotGlob = 10;
    public const int c_iTopGlob = 16;
    public const int c_iBackGlob = 4;
    public const int c_iFrontGlob = 22;

    public Glob[] GlobsC27 = new Glob[c_iCount];//C27
    public static int Index13Rel(int x, int y, int z)
    {
      Gu.Assert(x >= -1 && x <= 1 && y >= -1 && y <= 1 && z >= -1 && z <= 1);
      return (z + 1) * 3 * 3 + (y + 1) * 3 + (x + 1);
    }
    public Glob NeighborRel(int x, int y, int z)
    {
      return GlobsC27[Index13Rel(x, y, z)];
    }
    public Glob NeighborRel_Beam(int x, int y, int z, ref int bx, ref int bz, ref int bmin, ref int bmax, int maxval, int gblocksx, int gblocksz)
    {
      //Get the neighbor glob, and his beam xy beam based on an input neighbor glob/beamxy
      int gx = 0, gy = 0, gz = 0;
      if (bx == 0 && x == -1) { gx = -1; bx = maxval; }
      else if (bx == gblocksx - 1 && x == 1) { gx = +1; bx = 0; }
      else { bx += x; }

      if (bz == 0 && z == -1) { gz = -1; bz = maxval; }
      else if (bz == gblocksz - 1 && z == 1) { gz = +1; bz = 0; }
      else { bz += z; }

      if (bmin == 0 && y == -1) { gy = -1; bmin = BeamVert.MaxVal; bmax = bmin; } //Ok so this max may be wrong ..-1.. TODO: check
      else if (bmax == maxval && y == 1) { gy = +1; bmax = 0; bmax = bmin; }

      return GlobsC27[Index13Rel(gx, gy, gz)];
    }
    public void GetNeigborGlob_AndBeamOffset_ForGlobBeam(WorldInfo wi, int bx, int bz, BeamNeighbor gni, out Glob g, out int nx, out int nz)
    {
      g = GlobsC27[c_iCenterGlob];
      nx = bx;
      nz = bz;
      if (gni == BeamNeighbor.Left && bx == 0)
      {
        g = GlobsC27[c_iLeftGlob];
        bx = wi.GlobBlocksX + bx;
      }
      if (gni == BeamNeighbor.Back && bz == 0)
      {
        g = GlobsC27[c_iBackGlob];
        bz = wi.GlobBlocksZ + bz;
      }
      if (gni == BeamNeighbor.Right && bx == wi.GlobBlocksX)
      {
        g = GlobsC27[c_iRightGlob];
        bz = wi.GlobBlocksZ - bz;
      }
      if (gni == BeamNeighbor.Front && bz == wi.GlobBlocksZ)
      {
        g = GlobsC27[c_iFrontGlob];
        bx = wi.GlobBlocksX - bx;
      }
    }
    public GlobArray()
    {
    }
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
      // UV = new vec2[] {
      //   new vec2(_mtTex.uv0.x, _mtTex.uv1.y),
      //   _mtTex.uv1,
      //   _mtTex.uv0,
      //   new vec2(_mtTex.uv1.x, _mtTex.uv0.y)
      //  };

      //New UV - 00BL -> 11TR
      UV = new vec2[] { _mtTex.uv0, _mtTex.uv1 };
    }
  }

  [DataContract]
  public class WorldInfo : ISerializeBinary
  {
    //Contains base metrics for creating a world, size, voxels .. 

    #region Public:Members

    public const float c_EarthGravity = -9.8f;//m/s
    public const float MaxVelocity_Second_Frame = 5 * 2.32f;//max length of vel per second / frame *NOT PER FRAME but by dt*
    public const float MinVelocity = 0.000001f;
    public const float MinVelocity2 = (MinVelocity * MinVelocity);
    public const float MinTimeStep = 0.00001f;
    public const float DropDestroyTime_Seconds = (60) * 3; // x minutes
    public const int MaxGlobsToGeneratePerFrame_Sync = 32;//number of glob copy operations per render side frame. This can slow down / speed up rendering.
                                                          //public const float PlayerHeight  = 2.0f;
                                                          //public const float PlayerWidth  = 0.5f;
                                                          //public const float PlayerDepth  = 0.1f;

    //Temp variables
    public bool DeleteStartFresh = false;
    [DataMember] public int LimitYAxisGeneration = 0;//0 = off, >0 - limit globs generated along Y axis (faster generation)

    //Serialized
    [DataMember] public string Name { get; private set; } = Lib.UnsetName;
    [DataMember] public float HeightScale { get; private set; } = 0; // Height of a block relative to BlockSize
    [DataMember] public float WallXFactor { get; private set; } = 0.1f; // Width of a wall / [0,1] = % of BlockSize
    [DataMember] public float WallYFactor { get; private set; } = 0.1f; // 
    [DataMember] public float WallZFactor { get; private set; } = 0.1f; // 
    [DataMember] public float BlockSizeX { get; private set; } = 0;
    [DataMember] public float BlockSizeY { get; private set; } = 0; // BlockSizeX * HeightScale;
    [DataMember] public float BlockSizeZ { get; private set; } = 0; // BlockSizeX;
    [DataMember] public int GlobBlocksX { get; private set; } = 0;
    [DataMember] public int GlobBlocksY { get; private set; } = 0; //No y, but we can have a Y Snap
    [DataMember] public int GlobBlocksZ { get; private set; } = 0;
    [DataMember] public float GlobWidthX { get; private set; } = 0;
    [DataMember] public float GlobWidthY { get; private set; } = 0;
    [DataMember] public float GlobWidthZ { get; private set; } = 0;
    [DataMember] public float Gravity { get; private set; } = c_EarthGravity * 0.5f; //m/s
    [DataMember] public FileLoc? WorldScriptLoc = null;

    //Generation shell
    private int _currentShell = 1;
    private const int _maxShells = 4;//keep this < Min(DromeGlobs) to prevent generating more dromes
    public float GenRadiusShell { get { return GlobWidthX; } }
    public float DeleteMaxDistance { get { return (GenRadiusShell * (float)(_maxShells + 1)); } }//distance beyond which things are deleted, this must be greater than max gen distance to prevent ping pong loading
    public float GenerateDistance { get { return (GenRadiusShell * (float)_currentShell); } } //distance under which things are generated
    public float RenderDistance { get { return (GenRadiusShell) * _maxShells; /* (GlobWidthX * 16) * (GlobWidthX * 16); */ } }

    #endregion

    public WorldInfo(string worldName, FileLoc? worldScriptLoc, bool delete_world_start_fresh, int limit_y_axis = 0, float blockSize = 4.0f, float blockHeightScale = 0.25f, int globBlocksX = 16)
    {
      Name = worldName;
      LimitYAxisGeneration = limit_y_axis;
      DeleteStartFresh = delete_world_start_fresh;
      BlockSizeX = blockSize;
      GlobBlocksX = globBlocksX;
      HeightScale = blockHeightScale;
      WorldScriptLoc = worldScriptLoc;

      Compute();
    }
    public void Compute()
    {
      BlockSizeY = BlockSizeX * HeightScale;
      BlockSizeZ = BlockSizeX;
      GlobBlocksZ = GlobBlocksY = GlobBlocksX;
      GlobWidthX = (float)GlobBlocksX * BlockSizeX;
      GlobWidthY = (float)GlobBlocksY * BlockSizeY;
      GlobWidthZ = (float)GlobBlocksZ * BlockSizeZ;

      if (!MathUtils.IsPowerOfTwo(GlobBlocksX) || !MathUtils.IsPowerOfTwo(GlobBlocksY) || !MathUtils.IsPowerOfTwo(GlobBlocksZ))
      {
        Gu.BRThrowException("Glob blocks x,y,z must be a power of 2.");
      }
    }

    #region Indexing Stuff

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

      if (bpos.x < 0 || bpos.y < 0 || bpos.z < 0 || bpos.x >= GlobBlocksX || bpos.y >= GlobBlocksY || bpos.z >= GlobBlocksZ)
      {
        Gu.DebugBreak();
      }
      return bpos;
    }
    public float HeightGlobalToHeightGlob(float h)
    {
      //Global height -> glob relative height
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
    public ushort ConvertHeight(float height, bool glob_relative = true)
    {
      if (!glob_relative)
      {
        height = HeightGlobalToHeightGlob(height);
      }
      return Gu.QuantitizeUShortFloat(height, 0, this.GlobWidthY, BeamVert.MaxVal);
    }
    public float ConvertHeight(ushort height)
    {
      return Gu.UnQuantitizeUShortFloat(height, 0, this.GlobWidthY, BeamVert.MaxVal);
    }
    public Box3f GetBeamBox_Relative_NOCaps(Beam beam)
    {
      Box3f b = new Box3f(new vec3(0, ConvertHeight(beam.CaplessMin), 0), new vec3(this.BlockSizeX, ConvertHeight(beam.CaplessMax), this.BlockSizeZ));
      return b;
    }
    public Box3f GetBeamBox_Relative_WITHCaps(Beam beam)
    {
      Box3f b = new Box3f(new vec3(0, ConvertHeight(beam.MinHeight()), 0), new vec3(this.BlockSizeX, ConvertHeight(beam.MaxHeight()), this.BlockSizeZ));
      return b;
    }
    public TriPlane? MakeTriFrom4Heights(float v0, float v1, float v2, float v3, int tid, vec3 beam_origin)
    {
      TriPlane? tp = null;
      float w = this.BlockSizeX;
      float d = this.BlockSizeZ;
      vec3[] quad = new vec3[4] { new vec3(0, v0, 0), new vec3(w, v1, 0), new vec3(0, v2, d), new vec3(w, v3, d) };
      if (tid == 0)
      {
        tp = new TriPlane(quad[0], quad[2], quad[3]);
      }
      if (tid == 1)
      {
        tp = new TriPlane(quad[0], quad[3], quad[1]);
      }
      if (tid == 2)
      {
        tp = new TriPlane(quad[0], quad[3], quad[1]);
      }
      if (tid == 3)
      {
        tp = new TriPlane(quad[1], quad[0], quad[2]);
      }
      Gu.Assert(tp != null);
      tp._p1 += beam_origin;
      tp._p2 += beam_origin;
      tp._p3 += beam_origin;
      return tp;
    }
    public TriPlane[] GetConfigTriangles(Beam beam, BeamCap cap, vec3 beam_origin)
    {
      /*
          right 
          v2   v3
           0 / 1  < tid
          v0   v1 < vbase
          1 2       1
          0       0 2  
          left 
          v2   v3
           2 \ 3  < tid
          v0   v1 < vbase
          2    1 2
          1 0    0        
      */
      //default = top / right
      int tid0 = 0, tid1 = 0;
      float h0 = 0, h1 = 0, h2 = 0, h3 = 0;
      if (cap == BeamCap.Bottom)
      {
        h0 = ConvertHeight(beam.Edges[0].Top);
        h1 = ConvertHeight(beam.Edges[1].Top);
        h2 = ConvertHeight(beam.Edges[2].Top);
        h3 = ConvertHeight(beam.Edges[3].Top);

        if (!beam.BotConfigIsLeftConfig())
        {
          tid0 = 0;
          tid1 = 1;
        }
        else
        {
          tid0 = 2;
          tid1 = 3;
        }
      }
      else
      {
        h0 = ConvertHeight(beam.Edges[0].Bot);
        h1 = ConvertHeight(beam.Edges[1].Bot);
        h2 = ConvertHeight(beam.Edges[2].Bot);
        h3 = ConvertHeight(beam.Edges[3].Bot);
        if (!beam.TopConfigIsLeftConfig())
        {
          tid0 = 0;
          tid1 = 1;
        }
        else
        {
          tid0 = 2;
          tid1 = 3;
        }
      }

      TriPlane[] ret = new TriPlane[2];

      ret[0] = MakeTriFrom4Heights(h0, h1, h2, h3, tid0, beam_origin);
      ret[1] = MakeTriFrom4Heights(h0, h1, h2, h3, tid1, beam_origin);

      return ret;
    }
    public vec3 GetBeamOriginR3(int x, int z, vec3 glob_originR3)
    {
      return glob_originR3 + new vec3(x * this.BlockSizeX, 0, z * BlockSizeZ);
    }
    public bool Beam_Contains_Point_Bottom_Left_Inclusive_BEAM_BOX_ONLY_WITH_CAPS(int x, int z, vec3 pt, Beam beam, vec3 glob_originR3)
    {
      //check whole box boundary
      var b = GetBeamBox_Relative_WITHCaps(beam);
      b.Validate(true, true);
      return b.containsPointBottomLeftInclusive(pt);
    }
    public bool Beam_Contains_Point_Global_Bottom_Left_Inclusive_WITHCaps(int x, int z, vec3 pt, Beam beam, vec3 glob_originR3)
    {
      ushort topi = beam.MaxHeight();
      ushort boti = beam.MinHeight();

      Gu.Assert(topi != boti);//Busines rule no zero hieght,s top != bottom - this would blow up the whole system

      ushort h = ConvertHeight(pt.y);

      //check underneath beam triangles.
      vec3 beam_origin = GetBeamOriginR3(x, z, glob_originR3);
      TriPlane[] t = null;
      if (h < boti)
      {
        t = GetConfigTriangles(beam, BeamCap.Bottom, beam_origin);
      }
      else if (h > topi)
      {
        t = GetConfigTriangles(beam, BeamCap.Top, beam_origin);
      }
      else
      {
        Gu.Log.Error("Beam height is acting squirrelly.");
        Gu.DebugBreak();
      }
      if (t[0].Distance(pt) < 0 && t[1].Distance(pt) < 0)
      {
        return true;
      }
      return false;
    }
    public vec3 E2OffFace(int iface)
    {
      vec3 e2off = new vec3(0, 0, 0);
      if (iface == BeamFaceIndex.Left) { e2off.z -= this.BlockSizeZ; }
      else if (iface == BeamFaceIndex.Right) { e2off.z += this.BlockSizeZ; }
      else if (iface == BeamFaceIndex.Top) { }
      else if (iface == BeamFaceIndex.Bottom) { }
      else if (iface == BeamFaceIndex.Back) { e2off.x += this.BlockSizeX; }
      else if (iface == BeamFaceIndex.Front) { e2off.x -= this.BlockSizeX; }
      else { Gu.BRThrowNotImplementedException(); }
      return e2off;
    }
    public vec3 FaceOrigin(int iface)
    {
      //origin = BL, 
      vec3 side_origin = new vec3(0, 0, 0);
      if (iface == BeamFaceIndex.Left) { side_origin.z += BlockSizeZ; } //TL
      else if (iface == BeamFaceIndex.Right) { side_origin.x += BlockSizeX; } //BR
      else if (iface == BeamFaceIndex.Top) { }
      else if (iface == BeamFaceIndex.Bottom) { }
      else if (iface == BeamFaceIndex.Back) { }//BL
      else if (iface == BeamFaceIndex.Front) { side_origin.x += BlockSizeX; side_origin.z += BlockSizeZ; }//TR
      else { Gu.BRThrowNotImplementedException(); }
      return side_origin;
    }
    #endregion

    public void Serialize(BinaryWriter bw)
    {
      bw.Write((String)Name);
      bw.Write((Single)HeightScale);
      bw.Write((Single)WallXFactor);
      bw.Write((Single)WallYFactor);
      bw.Write((Single)WallZFactor);
      bw.Write((Single)BlockSizeX);
      bw.Write((Single)BlockSizeY);
      bw.Write((Single)BlockSizeZ);
      bw.Write((Int32)GlobBlocksX);
      bw.Write((Int32)GlobBlocksY);
      bw.Write((Int32)GlobBlocksZ);
      bw.Write((Single)GlobWidthX);
      bw.Write((Single)GlobWidthY);
      bw.Write((Single)GlobWidthZ);

    }
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      Name = br.ReadString();
      HeightScale = br.ReadSingle();
      WallXFactor = br.ReadSingle();
      WallYFactor = br.ReadSingle();
      WallZFactor = br.ReadSingle();
      BlockSizeX = br.ReadSingle();
      BlockSizeY = br.ReadSingle();
      BlockSizeZ = br.ReadSingle();
      GlobBlocksX = br.ReadInt32();
      GlobBlocksY = br.ReadInt32();
      GlobBlocksZ = br.ReadInt32();
      GlobWidthX = br.ReadSingle();
      GlobWidthY = br.ReadSingle();
      GlobWidthZ = br.ReadSingle();
    }
  }

  public class VisibleStuff
  {
    public Dictionary<RenderView, Dictionary<DrawMode, SortedDictionary<DrawOrder, DrawCall>>> Dict { get { return _dict; } }

    private int _dbg_added_objects = 0;
    private int _dbg_drawcall_count = 0;
    // massive friggin dict of dict of dict..Dictionary<RenderView, Dictionary<DrawMode, Dictionary<DrawOrder, Dictionary<Material, Dictionary<MeshView, List<Drawable>>>>>>
    private Dictionary<RenderView, Dictionary<DrawMode, SortedDictionary<DrawOrder, DrawCall>>> _dict =
     new Dictionary<RenderView, Dictionary<DrawMode, SortedDictionary<DrawOrder, DrawCall>>>();//View -> stage -> distance/draw order -> instances sorted by material/mesh

    //easier than iterating that massive thing above
    public Dictionary<RenderView, HashSet<Glob>> VisibleGlobs { get; private set; } = new Dictionary<RenderView, HashSet<Glob>>();
    public Dictionary<RenderView, HashSet<WorldObject>> VisibleObjects { get; private set; } = new Dictionary<RenderView, HashSet<WorldObject>>();

    public void Clear(RenderView rv)
    {
      //Clear all collected for JUST the given view
      _dict = _dict.ConstructIfNeeded();
      if (_dict.TryGetValue(rv, out var stageDist))
      {
        stageDist.Clear();
      }
      if (VisibleGlobs.TryGetValue(rv, out var glist))
      {
        glist.Clear();
      }
      if (VisibleObjects.TryGetValue(rv, out var oblist))
      {
        oblist.Clear();
      }


      _dbg_drawcall_count = 0;
      _dbg_added_objects = 0;
    }
    public void AddObject(RenderView rv, Drawable ob, Material? customMaterial = null, Glob? isGlob = null)
    {
      Gu.Assert(ob != null);
      if (ob.Mesh == null)
      {
        return;
      }

      var the_mat = customMaterial != null ? customMaterial : ob.Material;

      _dict = _dict.ConstructIfNeeded();

      Dictionary<DrawMode, SortedDictionary<DrawOrder, DrawCall>>? stageDist = null;
      if (!_dict.TryGetValue(rv, out stageDist))
      {
        stageDist = new Dictionary<DrawMode, SortedDictionary<DrawOrder, DrawCall>>();
        _dict.Add(rv, stageDist);
      }

      SortedDictionary<DrawOrder, DrawCall>? distCall = null;
      if (!stageDist.TryGetValue(the_mat.DrawMode, out distCall))
      {
        distCall = new SortedDictionary<DrawOrder, DrawCall>();
        stageDist.Add(the_mat.DrawMode, distCall);
      }

      DrawCall? call = null;
      if (!distCall.TryGetValue(the_mat.DrawOrder, out call))
      {
        call = new DrawCall();
        distCall.Add(the_mat.DrawOrder, call);
      }

      call.AddVisibleObject(ob, customMaterial);

      //add to the flat list as well.
      if (isGlob != null)
      {
        HashSet<Glob>? hs = new HashSet<Glob>();
        if (!VisibleGlobs.TryGetValue(rv, out hs))
        {
          hs = new HashSet<Glob>();
          VisibleGlobs.Add(rv, hs);
        }
        hs.Add(isGlob);
      }
      else if (ob is WorldObject)
      {
        var obb = ob as WorldObject;
        HashSet<WorldObject>? hs = new HashSet<WorldObject>();
        if (!VisibleObjects.TryGetValue(rv, out hs))
        {
          hs = new HashSet<WorldObject>();
          VisibleObjects.Add(rv, hs);
        }
        hs.Add(obb);
      }
      else
      {
        //the object is a drawable, probably a glob or debug object, etc
      }

      _dbg_added_objects++;
    }
    public void Draw(RenderView rv, DrawMode dm, WorldProps wp)
    {
      Gu.Assert(rv != null);
      Gu.Assert(dm != null);
      Gu.Assert(wp != null);

      if (_dict.TryGetValue(rv, out var modes))
      {
        if (modes.TryGetValue(dm, out var orders))
        {
          foreach (var order_call in orders)
          {
            var call = order_call.Value;

            call.Draw(wp, rv);

            _dbg_drawcall_count++;
          }
        }
      }
    }

  }//stuff

  [DataContract]
  public class World
  {
    #region Private:Constants

    private const long c_lngAbandon_DeleteTime_DromeNode_ms = 1000 * 5; // * X seconds
    private const long c_lngAbandon_DeleteTime_Drome_ms = 1000 * 10; // Dromes stay in memory longer than their nodes. We need the scalar field data more often. When they are fully generated they can be discarded.
    private const int c_intMaxInitialGenerationWaitTime_ms = 1000 * 15;

    private const int c_intDromeFileVersion = 1;

    #endregion
    #region Public:Members

    public Dictionary<ivec3, Glob> Globs { get { return _globs; } }
    public WorldInfo Info { get { return _worldInfo; } }
    public WorldEditor Editor { get { return _worldEditor; } }
    public WorldObject SceneRoot { get { return _sceneRoot; } }
    public int NumGlobs { get { return _globs.Count; } }
    public int NumRenderGlobs { get { return _renderGlobs.Count; } }
    public int NumVisibleRenderGlobs { get { return _visibleRenderGlobs.Count; } }
    public WorldProps WorldProps { get { return _worldProps; } }
    public int NumCulledObjects { get; private set; } = 0;
    public int NumVisibleObjects { get; private set; } = 0;
    public WindowContext UpdateContext { get; private set; } = null;
    public GameMode GameMode
    {
      get { return _eGameMode; }
      set
      {
        _eGameMode = value;
      }
    }

    #endregion
    #region Private: Members

    [DataMember] private GameMode _eGameMode = GameMode.Edit;
    [DataMember] private WorldInfo? _worldInfo = null;
    [DataMember] private WorldObject _sceneRoot;
    private WorldEditor? _worldEditor = null;

    //There is no need for ivec3 here.
    //we should sort all objects by distance.
    private VisibleStuff _visibleStuff;
    private Dictionary<ivec3, Glob> _visibleRenderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //globs that must be drawn this frame
    private Dictionary<ivec3, Glob> _globs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //All globs, which may be null if the glob region has been visible, but does not exist
    private Dictionary<ivec3, Glob> _existingGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //globs that are loaded, and exist
    private SortedList<float, GlobArray> _queuedGlobs = new SortedList<float, GlobArray>(new FloatSort());// queued for topology
    private Dictionary<ivec3, Glob> _renderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //globs that can be drawn this frame. 
    private Dictionary<ushort, WorldTile>? _blockTiles = null;
    private WorldProps? _worldProps = null; //Environment props.
    private Material? _worldMaterial_Op = null;
    private Material? _worldMaterial_Tp = null;
    private MegaTex? _worldMegatex = null;
    private Material? _blockObjectMaterial = null;
    private double _autoSaveTimeoutSeconds = 30;
    private double _autoSaveTimeout = 0;
    private WorldScript? _worldScript = null;

    #endregion
    #region Enter/Exit/Update

    public World(WindowContext updateContext)
    {
      UpdateContext = updateContext;
    }
    public void Initialize(WorldInfo info)
    {
      _sceneRoot = new WorldObject("Scene_Root");

      //Gu.Lib.Add(_sceneRoot);//??

      EmbeddedResources.BuildResources();

      _worldInfo = info;
      _worldEditor = new WorldEditor();

      CreateWorldProps();

      GameMode = Gu.EngineConfig.StartInEditMode ? GameMode.Edit : GameMode.Play;

      DefineWorldTiles();
      CreateMaterials();

      //Load the terrain and existing objects
      InitWorldDiskFile(info.DeleteStartFresh);

      //Call an update to refresh everything
      UpdateWorld(0);

      LoadWorldScript();
    }
    public void IterateRootObjectsSafe(Func<WorldObject, LambdaBool> f, bool iterateDeleted = false)
    {
      SceneRoot.IterateChildrenSafe(f, iterateDeleted);
    }
    private void CreateWorldProps()
    {
      _worldProps = new WorldProps("WorldProps");

      //this should be set via the script
      _worldProps.EnvironmentMap = new Texture("_worldProps.EnvironmentMap",
        Gu.Lib.GetOrLoadImage(new FileLoc("hilly_terrain_01_2k.hdr", EmbeddedFolder.Image)), true, TexFilter.Nearest);
      _worldProps.DayNightCycle = new DayNightCycle();
      _worldProps.DayNightCycle.Update(0);
    }
    private void LoadWorldScript()
    {
      //Load world script for objects.
      Gu.Assert(_worldInfo != null);
      if (_worldInfo.WorldScriptLoc != null)
      {
        _worldScript = new WorldScript(_worldInfo.WorldScriptLoc);
        if (_worldScript.Compile())
        {
          if (_worldScript != null)
          {
            _worldScript.OnLoad(this);
          }
          else
          {
            Gu.Log.Error("World script object was not defined!.");
            Gu.DebugBreak();
          }
        }
        else
        {
          //We could possibly load an old assembly .. later
          Gu.Log.Error("World script did not compile.");
          Gu.DebugBreak();
        }
      }
      else
      {
        Gu.Log.Error("No world script specified.");
        Gu.DebugBreak();
      }
    }
    public void UpdateWorld(double dt)
    {
      if (UpdateContext != Gu.Context)
      {
        Gu.Log.Error("Tried to call update twice between two windows. Update must be called once on a single window (or, we could put it on its own thread, unless we do end up with OpenGL stuff.)");
        Gu.DebugBreak();
      }

      _worldScript?.OnUpdate(this, dt);

      Gu.Lib.Update(dt);

      UpdateRootObjects(dt);
      CheckSaveWorld(dt);

      TopologizeGlobs();

      _worldProps.DayNightCycle.Update(dt);
    }
    public void UpdateWorldEditor(double dt)
    {
      //Update editor after picking
      if (UpdateContext != Gu.Context)
      {
        Gu.Log.Error("Tried to call update twice between two windows. Update must be called once on a single window (or, we could put it on its own thread, unless we do end up with OpenGL stuff.)");
        Gu.DebugBreak();
      }

      Gu.Assert(_worldEditor != null);
      Gu.TryGetSelectedView(out var selview);
      //update after picking, view can be null
      _worldEditor.Update(selview);
    }

    public void ExitWorld()
    {
      _worldScript?.OnExit(this);
    }

    #endregion
    #region Topology

    public void Pick()
    {
      //Pick objects at start of frame, *relying on the previous culled collection 
      if (Gu.TryGetSelectedView(out var rv))
      {
        if (Gu.Context.Renderer.Picker.PickedObjectFrame == null)
        {
          if (_visibleStuff.VisibleGlobs.TryGetValue(rv, out var gs))
          {
            foreach (var g in gs)
            {
              PickVisibleGlob(g);
              if (Gu.Context.Renderer.Picker.PickedObjectFrame != null)
              {
                break;
              }
            }
          }
        }
        if (Gu.Context.Renderer.Picker.PickedObjectFrame == null)
        {
          if (_visibleStuff.VisibleObjects.TryGetValue(rv, out var obs))
          {
            foreach (var ob in obs)
            {
              PickVisibleObject(ob);
              if (Gu.Context.Renderer.Picker.PickedObjectFrame != null)
              {
                break;
              }
            }
          }
        }
      }

    }

    public void BuildAndCull(RenderView rv)
    {
      Gu.Assert(rv != null);//must have RV

      if (rv.ViewMode != RenderViewMode.UIOnly)
      {
        //Collect visible
        _visibleStuff = _visibleStuff.ConstructIfNeeded();
        _visibleStuff.Clear(rv);
        _worldProps.Reset();

        rv.Camera.SanitizeTransform();
        BuildGrid(rv.Camera.Position_World, Info.GenerateDistance);
        CollectVisibleGlobs(rv, rv.Camera);
        CollectVisibleObjects(rv, rv.Camera);
        Editor.CollectVisibleObjects(rv);

        //Must come after editor it modifies this
        rv.Overlay.BuildDebugDrawMeshes(_visibleStuff);
      }
      if (rv.ViewMode != RenderViewMode.WorldOnly && rv.Gui != null)
      {
        _visibleStuff.AddObject(rv, rv.Gui.GetDrawable());
      }
    }
    private void TopologizeGlobs()
    {
      foreach (var qg in _queuedGlobs)
      {
        TopologizeGlob(qg.Value);
      }
      _queuedGlobs.Clear();
    }
    private void QueueGlob(Glob g, vec3 campos)
    {
      Gu.Assert(g != null);

      GlobArray qg = new GlobArray();

      for (int z = -1; z <= 1; z++)
      {
        for (int y = -1; y <= 1; y++)
        {
          for (int x = -1; x <= 1; x++)
          {
            int idx = GlobArray.Index13Rel(x, y, z);
            var npos = g.Pos + new ivec3(x, y, z);
            if (_existingGlobs.TryGetValue(npos, out var neighbor))
            {
              Gu.Assert(neighbor != null);//Existing globs cannot be null
              Gu.Assert(neighbor.BeamGrid != null);//Existing globs cannot be null

              neighbor.Lock();

              qg.GlobsC27[idx] = neighbor;
              if (idx == GlobArray.c_iCenterGlob)
              {
                neighbor.State = Glob.GlobState.Queued;
              }
            }
            else
            {
              qg.GlobsC27[idx] = null;
            }
          }
        }
      }
      float dist = Info.GetGlobBoxGlobalI3(g.Pos).DistanceToCam2(campos);
      _queuedGlobs.Add(dist, qg);
    }
    private void TopologizeGlob(GlobArray ga)
    {
      //We should figure out how ot make this more regular,
      //or, split the guy up, to avoid remakign this with each edit

      Gu.Assert(ga != null);
      Gu.Assert(ga.GlobsC27 != null);
      var g = ga.GlobsC27[GlobArray.c_iCenterGlob];
      Gu.Assert(g != null);
      Gu.Assert(g.BeamGrid != null);

      List<v_v3n3x2t3u1> verts = new List<v_v3n3x2t3u1>();
      List<ushort> inds = new List<ushort>();

      //beam patches are calculating quads individually, which is causing incorrect normals.
      g.BeamGrid.Iterate((grid, x, z) =>
      {
        var beamlist = grid.Get(x, z);
        if (beamlist != null)
        {
          foreach (var beam in beamlist.Beams)
          {
            vec2[] sideuv = null;
            WorldTile? wt = null;
            var beam_origin = g.OriginR3 + new vec3((float)x * Info.BlockSizeX, 0, (float)z * Info.BlockSizeZ);

            //Do sides
            for (int iside = 0; iside < 4; iside++)
            {
              int iface = WorldStaticData.sideidx_to_faceidx[iside];

              if (_blockTiles.TryGetValue(beam.Tiles[iface], out wt))
              {
                sideuv = wt.UV;
              }

              var nbeams = GetBeamsForFace(ga, x, z, beam, iface);
              Gu.Assert(nbeams != null);
              ushort ce0_min, ce0_max;
              ushort ce1_min, ce1_max;
              var ei0 = WorldStaticData.side_to_edge[iside, 0];
              var ei1 = WorldStaticData.side_to_edge[iside, 1];
              ce0_min = ce0_max = beam.Edges[ei0].Min;
              ce1_min = ce1_max = beam.Edges[ei1].Min;

              var iside_opp = WorldStaticData.side_to_opposite_side[iside];
              var ei_opp0 = WorldStaticData.side_to_edge[iside_opp, 1];//reverse 1/0 for opposing side
              var ei_opp1 = WorldStaticData.side_to_edge[iside_opp, 0];

              //build faces from the bottom up -y->+y
              for (int bni = 0; bni < nbeams.Count; bni++)
              {
                var ne0 = nbeams[bni].Edges[ei_opp0];
                if (ne0.ContainsOrEqual(ce0_min))
                {
                  ce0_min = ne0.Max;
                  if (bni < nbeams.Count - 1)
                  {
                    ce0_max = nbeams[bni + 1].Edges[ei_opp0].Min;
                  }
                }
                var ne1 = nbeams[bni].Edges[ei_opp1];
                if (ne1.ContainsOrEqual(ce1_min))
                {
                  ce1_min = ne1.Max;
                  if (bni < nbeams.Count - 1)
                  {
                    ce1_max = nbeams[bni + 1].Edges[ei_opp1].Min;
                  }
                }
                DoSideOrCap(iface, ce0_min, ce0_max, ce1_min, ce1_max, verts, inds, beam_origin, sideuv);

                ce0_min = ne0.Max;//done with this one, go past it
                ce1_min = ne1.Max;
              }

              //Do left over face
              ce0_max = beam.Edges[ei0].Max;
              ce1_max = beam.Edges[ei1].Max;
              DoSideOrCap(iface, ce0_min, ce0_max, ce1_min, ce1_max, verts, inds, beam_origin, sideuv);
            }//do sides

            //Do top
            vec2[] top_botuv = new vec2[] { new vec2(0, 0), new vec2(1, 1) };
            if (_blockTiles.TryGetValue(beam.Tiles[BeamFaceIndex.Top], out wt))
            {
              top_botuv = wt.UV;
            }
            //2 3 = edges
            //0 1
            DoSideOrCap(BeamFaceIndex.Top, beam.Edges[0].Max, beam.Edges[2].Max, beam.Edges[1].Max, beam.Edges[3].Max, verts, inds, beam_origin, top_botuv);

            if (_blockTiles.TryGetValue(beam.Tiles[BeamFaceIndex.Bottom], out wt))
            {
              top_botuv = wt.UV;
            }
            DoSideOrCap(BeamFaceIndex.Bottom, beam.Edges[2].Min, beam.Edges[0].Min, beam.Edges[3].Min, beam.Edges[1].Min, verts, inds, beam_origin, top_botuv);

          }
        }

        return LambdaBool.Continue;
      });

      if (verts != null && verts.Count > 0)
      {
        //ushort[] inds = MeshData.GenerateQuadIndices(verts.Length / 4, false);

        string name = g.Name;
        if (g.Opaque == null)
        {
          g.Opaque = new Drawable("glob-" + g.Pos.ToString(), null, _worldMaterial_Op, mat4.Identity);
        }
        var vertsarr = verts.ToArray();
        var indsarr = inds.ToArray();

        //TODO: supply adjacent tile info for adjacent normals.. or something
        //normals are not correct because they are in quads - adjacent verts are not constructed in patches.
        var faces = MeshGen.ComputeNormalsAndTangents(vertsarr, indsarr.AsUIntArray(), true, true);

        g.Opaque.Mesh = new MeshData(name, PrimitiveType.Triangles,
                Gpu.CreateVertexBuffer(name, vertsarr),
                Gpu.CreateIndexBuffer(name, indsarr),
                Gpu.CreateShaderStorageBuffer(name, faces.ToArray()),
                true
              );


        _renderGlobs.Add(g.Pos, g);
      }
      else
      {
        _renderGlobs.Remove(g.Pos);
      }

      foreach (var ng in ga.GlobsC27)
      {
        ng?.Unlock();
      }

      g.State = Glob.GlobState.Topologized;
    }
    private void DoSideOrCap(int iface, ushort ce0_min, ushort ce0_max, ushort ce1_min, ushort ce1_max, List<v_v3n3x2t3u1> verts, List<ushort> inds, vec3 beam_origin, vec2[] uvs)
    {
      //value input edge id = 0,2,1,3 for top
      /*
          right 
          v2   v3
          0 / 1  < tid
          v0   v1 < vbase
          2 3       3
          0       0 1  
          left 
          v2   v3
          2 \ 3  < tid
          v0   v1 < vbase
          2    2 3
          0 1    1        
      */
      //e0 is on the left, normal pointing towards you, origin bot left

      Gu.Assert(iface >= 0 && iface <= 6);

      bool doe0 = ce0_min < ce0_max;
      bool doe1 = ce1_min < ce1_max;
      bool isCap = ((iface == BeamFaceIndex.Top) || (iface == BeamFaceIndex.Bottom));

      if (doe0)
      {
        if (ce0_min >= ce0_max)
        {
          ce0_max = (ushort)(ce0_min + 1);
          Gu.Log.WarnCycle("Edge1 was invalid");
        }
      }
      if (doe1)
      {
        if (ce1_min >= ce1_max)
        {
          ce1_max = (ushort)(ce1_min + 1);
          Gu.Log.WarnCycle("Edge2 was invalid");
        }
      }

      if (isCap || doe0 || doe1)
      {
        //make some data for whatever face we need
        float he0_min = Info.ConvertHeight(ce0_min);
        float he0_max = Info.ConvertHeight(ce0_max);
        float he1_min = Info.ConvertHeight(ce1_min);
        float he1_max = Info.ConvertHeight(ce1_max);
        int voff = verts.Count;
        vec3 n = WorldStaticData.face_idx_to_normal[iface]; //this is the default, compute_normalsandtangents can compute the correct face normals.
        vec3 e0_orig = beam_origin + Info.FaceOrigin(iface);
        vec3 e1_orig = beam_origin + Info.FaceOrigin(iface) + Info.E2OffFace(iface);

        var qverts = new v_v3n3x2t3u1[4] {
          new v_v3n3x2t3u1(){_v = e0_orig + new vec3(0, he0_min, 0) , _n = n, _x = new vec2(uvs[0].x,uvs[0].y) },//uvs are wrong, some kind of tiling will need
          new v_v3n3x2t3u1(){_v = e1_orig + new vec3(0, he1_min, 0) , _n = n, _x = new vec2(uvs[1].x,uvs[0].y) },
          new v_v3n3x2t3u1(){_v = e0_orig + new vec3(0, he0_max, 0) , _n = n, _x = new vec2(uvs[0].x,uvs[1].y) },
          new v_v3n3x2t3u1(){_v = e1_orig + new vec3(0, he1_max, 0) , _n = n, _x = new vec2(uvs[1].x,uvs[1].y) },
        };

        //This is temporary eventually we'll do blending like in dmc
        if (iface == BeamFaceIndex.Top || iface == BeamFaceIndex.Bottom)
        {
        }
        else
        {
          //We need to go into the DMC shader and see how we did this.
          //1 11 < megatex uvs
          //0 1
          //2 3 < ordinals
          //0 1
          float uvw = uvs[1].x - uvs[0].x;
          float uvh = uvs[1].y - uvs[0].y;

          qverts[0]._x = new vec2(uvs[0].x, uvs[0].y + (qverts[0]._v.y % Info.BlockSizeY));
          qverts[1]._x = new vec2(uvs[1].x, uvs[0].y + (qverts[1]._v.y % Info.BlockSizeY));
          qverts[2]._x = qverts[0]._x + new vec2(0, ((qverts[2]._v.y - qverts[0]._v.y) / Info.BlockSizeY) / uvh);
          qverts[3]._x = qverts[1]._x + new vec2(0, ((qverts[3]._v.y - qverts[1]._v.y) / Info.BlockSizeY) / uvh);
        }

        //*** TODO SPLIT TRIANGLES FOR HALF SIDES

        //do face
        if (iface == BeamFaceIndex.Top || iface == BeamFaceIndex.Bottom)
        {
          qverts[1]._v += new vec3(Info.BlockSizeX, 0, 0);
          qverts[2]._v += new vec3(0, 0, Info.BlockSizeZ);
          qverts[3]._v += new vec3(Info.BlockSizeX, 0, Info.BlockSizeZ);

          //*** TODO: CULL TOP / BOT FACES 
          verts.AddRange(qverts);
          if (iface == BeamFaceIndex.Top)
          {
            inds.AddRange(new ushort[6] {
            (ushort) (voff + 0), (ushort) (voff + 3), (ushort) (voff + 2),
            (ushort) (voff + 0), (ushort) (voff + 1), (ushort) (voff + 3) });
          }
          else
          {
            //hacky here, just guessing .. this is all temporary until culling gets made.
            inds.AddRange(new ushort[6] {
            (ushort)(voff + 0), (ushort)(voff + 2), (ushort)(voff + 3),
            (ushort)(voff + 0), (ushort)(voff + 3), (ushort)(voff + 1) });
          }
        }
        else if (doe0 && doe1)
        {
          //quad
          verts.AddRange(qverts);
          inds.AddRange(new ushort[6] {
            (ushort)(voff + 0), (ushort)(voff + 3), (ushort)(voff + 2),
            (ushort)(voff + 0), (ushort)(voff + 1), (ushort)(voff + 3) });
        }
        else if (doe0)
        {
          //tri
          verts.AddRange(new v_v3n3x2t3u1[3] { qverts[0], qverts[1], qverts[2] });
          inds.AddRange(new ushort[3] { (ushort)(voff + 0), (ushort)(voff + 1), (ushort)(voff + 2) });
        }
        else if (doe1)
        {
          //tri
          verts.AddRange(new v_v3n3x2t3u1[3] { qverts[0], qverts[1], qverts[3] });
          inds.AddRange(new ushort[3] { (ushort)(voff + 0), (ushort)(voff + 1), (ushort)(voff + 2) });
        }
      }


    }
    public List<Beam> GetBeamsForFace(GlobArray ga, int bx, int bz, Beam beam, int face)
    {
      List<Beam> ret = new List<Beam>();
      int dx = 0, dy = 0, dz = 0;
      if (face == BeamFaceIndex.Left) { dx = -1; }
      else if (face == BeamFaceIndex.Right) { dx = +1; }
      else if (face == BeamFaceIndex.Bottom) { dy = -1; }
      else if (face == BeamFaceIndex.Top) { dy = +1; }
      else if (face == BeamFaceIndex.Back) { dz = -1; }
      else if (face == BeamFaceIndex.Front) { dz = +1; }

      var bmin = (int)beam.MinHeight();
      var bmax = (int)beam.MaxHeight();

      Glob gn = ga.NeighborRel_Beam(dx, dy, dz,
                                    ref bx, ref bz,
                                    ref bmin, ref bmax,
                                    BeamVert.MaxVal,
                                    Info.GlobBlocksX, Info.GlobBlocksZ);
      if (gn != null)
      {
        var bl = gn.BeamGrid.Get(bx, bz);
        if (bl != null)
        {
          ret = bl.GetBeamsForRange((ushort)bmin, (ushort)bmax);
        }
      }
      return ret;
    }

    #endregion
    #region Objects

    public WorldObject? FindObject(string name)
    {
      WorldObject? ret = null;
      IterateRootObjectsSafe((ob) =>
      {
        if (ob.Name == name)
        {
          ret = ob;
          return LambdaBool.Break;
        }
        return LambdaBool.Continue;
      });
      return ret;
    }
    public WorldObject CreateObject(string name, MeshData mesh, Material material, vec3 pos = default(vec3))
    {
      WorldObject ob = new WorldObject(name);
      ob.Position_Local = pos;
      ob.Mesh = mesh;
      ob.Material = material;
      return ob;
    }
    public WorldObject CreateAndAddObject(string name, MeshData mesh, Material material, vec3 pos = default(vec3))
    {
      return AddObject(CreateObject(name, mesh, material, pos));
    }
    public void RemoveObject(WorldObject ob)
    {
      RemoveObjectInternal(ob);
    }
    private void RemoveObjectInternal(WorldObject wo)
    {
      wo.UnlinkFromParent();
      wo.State = WorldObjectState.Removed;
      _worldEditor.Edited = true;
    }
    public WorldObject AddObject(WorldObject ob, bool defaultBox = true)
    {
      //Add object to the scene root
      if (ob == null)
      {
        Gu.Log.Error("Object was null adding to world.");
        Gu.DebugBreak();
        return null;
      }

      if (!(ob is Armature))
      {
        if (defaultBox)
        {
          if (ob.Mesh == null)
          {
            ob.Mesh = Gu.Lib.GetMesh(Rs.Mesh.DefaultBox);
          }
          if (ob.Material == null)
          {
            ob.Material = Gu.Lib.GetMaterial(Rs.Material.DefaultObjectMaterial);
          }
        }
      }

      SceneRoot.AddChild(ob);
      //  ob.OnAddedToScene?.Invoke(ob);
      ob.State = WorldObjectState.Active;
      _worldEditor.Edited = true;

      return ob;
    }
    private void UpdateRootObjects(double dt)
    {
      Box3f dummy = Box3f.Zero;
      dummy.genResetLimits();
      List<WorldObject> toRemove = new List<WorldObject>();
      IterateRootObjectsSafe((ob) =>
      {
        if (ob.State != WorldObjectState.Removed)
        {
          if (ob.Visible)
          {
            //We could drop physics here if we wanted to
            ob.Update(dt, ref dummy);

            //This could be a component
            if (ob.HasPhysics)
            {
              UpdateObjectPhysics(ob, (float)dt);
            }
          }
        }
        else
        {
          toRemove.Add(ob);
        }
        return LambdaBool.Continue;
      });
      foreach (var x in toRemove)
      {
        RemoveObjectInternal(x);
      }
      toRemove.Clear();

    }
    private void UpdateObjectPhysics(WorldObject ob, float dt)
    {
      if (dt < WorldInfo.MinTimeStep)
      {
        dt = WorldInfo.MinTimeStep;
      }
      var phy = ob.PhysicsData;
      if (phy == null || phy.Enabled == false)
      {
        return;
      }

      //Assuming we're going to modify object resting state when other objects change state
      float vlen2 = (phy.Velocity * (float)dt).len2();
      if (phy.OnGround && vlen2 > 0)
      {
        phy.OnGround = false;
      }
      if (phy.OnGround)
      {
        return;
      }

      float maxv = WorldInfo.MaxVelocity_Second_Frame * dt;
      float maxv2 = (float)Math.Pow(maxv, 2.0f);

      vec3 dbg_initial_v = phy.Velocity;

      //Our final guys in frame time units
      vec3 final_p = ob.Position_Local;
      vec3 final_v = phy.Velocity * dt;

      //Too big
      vlen2 = (final_v * dt).len2();
      if (vlen2 > maxv2)
      {
        final_v = final_v.normalized() * maxv;
      }

      vec3 g_v = vec3.Zero;
      if (phy.HasGravity)
      {
        g_v = new vec3(0, _worldInfo.Gravity * dt, 0);
      }

      if (phy.Collides)
      {
        CollideOb(ob, phy, dt, ref final_v, ref final_p, maxv, maxv2, false);
        if (phy.OnGround == false && phy.HasGravity)
        {
          CollideOb(ob, phy, dt, ref g_v, ref final_p, maxv, maxv2, true);
        }
      }
      else
      {
        final_p += final_v + g_v;
      }

      //Dampen world velocity (not frame velocity)
      if (!phy.OnGround && phy.AirFriction > 0.0f)
      {
        float len = final_v.length();
        float newlen = len - len * phy.AirFriction * dt;
        if (newlen <= 0)
        {
          newlen = 0;
        }
        final_v = final_v.normalized() * newlen;
      }

      //Add frame v to p

      ob.Position_Local = final_p;
      //transform v back into world time units instead of frame time units
      phy.Velocity = (final_v + g_v) * (1.0f / (dt == 0 ? 1.0f : dt));
    }
    private void CollideOb(WorldObject ob, PhysicsData phy, float dt, ref vec3 final_v, ref vec3 final_p, float maxvdt, float maxvdt2, bool gravity)
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
            phy.OnGround = true;
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
          if (final_v.len2() < WorldInfo.MinVelocity2)
          {
            final_v = vec3.Zero;
          }
          if (final_v.len2() > maxvdt2)
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
          if (final_v.len2() < WorldInfo.MinVelocity2)
          {
            final_v = vec3.Zero;
          }
          if ((final_v * dt).len2() > maxvdt2)
          {
            final_v = final_v.normalized() * maxvdt;
          }

          if (gravity && istep == 0)
          {
            phy.OnGround = false;
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

    #endregion
    #region Culling / View

    private void CollectVisibleObjects(RenderView rv, Camera3D cm)
    {
      NumCulledObjects = 0;
      NumVisibleObjects = 0;
      CollectVisibleObjects(rv, cm, SceneRoot);
    }
    private void CollectVisibleGlobs(RenderView rv, Camera3D cam)
    {
      Gu.Assert(cam != null);
      _visibleRenderGlobs.Clear();

      //TODO: Optimize: there can be thousands of these
      //We could walk the globs (as usual) if we assert they are interconnected (problem last time, unlinked glob neighbors)
      foreach (var kvp in _existingGlobs)
      {
        var g = kvp.Value;
        //i think ir emoved glob box due to too much data. probably isn't necessary to do that with new system.
        var b = Info.GetGlobBoxGlobalI3(kvp.Key);
        if (cam.Frustum.HasBox(b))
        {
          if (g.State == Glob.GlobState.Topologized)
          {
            if (g.HasMeshData())
            {
              _visibleRenderGlobs.Add(kvp.Key, g);
              if (g.Opaque != null)
              {
                AddDrawable(rv, g.Opaque, g);
              }
              if (g.Transparent != null)
              {
                AddDrawable(rv, g.Transparent, g);
              }
            }
          }
          else if (g.State == Glob.GlobState.CreatedOrLoaded)
          {
            QueueGlob(g, cam.Position_World);
          }
        }
      }
    }
    private void AddDrawable(RenderView rv, Drawable ob, Glob? g = null)
    {
      Gu.Assert(ob != null);

      //draw for render mode
      Material mat = null;
      if (rv.Overlay.ObjectRenderMode == ObjectRenderMode.Solid)
      {
        mat = Gu.Lib.GetMaterial(Rs.Material.DebugDraw_Solid_FlatColor);
      }
      else if (rv.Overlay.ObjectRenderMode == ObjectRenderMode.Wire)
      {
        mat = Gu.Lib.GetMaterial(Rs.Material.DebugDraw_Wireframe_FlatColor);
        _worldProps.WireframeColor = new vec4(.193f, .179f, .183f, 1);
      }
      else if (rv.Overlay.ObjectRenderMode == ObjectRenderMode.Textured)
      {
        mat = ob.Material;
        mat.Flat = true;
      }
      else if (rv.Overlay.ObjectRenderMode == ObjectRenderMode.Rendered)
      {
        mat = ob.Material;
        mat.Flat = false;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      _visibleStuff.AddObject(rv, ob, mat, g);

      //wireframe overlay
      if (rv.Overlay.DrawWireframeOverlay && rv.Overlay.ObjectRenderMode != ObjectRenderMode.Wire)
      {
        if (rv.Overlay.ObjectRenderMode == ObjectRenderMode.Solid)
        {
          _worldProps.WireframeColor = new vec4(.193f, .179f, .183f, 1);
        }
        else
        {
          _worldProps.WireframeColor = new vec4(.793f, .779f, .783f, 1);
        }
        _visibleStuff.AddObject(rv, ob, Gu.Lib.GetMaterial(Rs.Material.DebugDraw_Wireframe_FlatColor), g);
      }

      //Bound boxes
      if (rv.Overlay.DrawBoundBoxesAndGizmos)
      {
        if ((ob as WorldObject) != null && (ob as WorldObject).BoundBoxMeshTransform != null)
        {
          var wo = (ob as WorldObject);
          vec4 obb_color = new vec4(.9192f, .8793f, .9131f, 1);
          rv.Overlay.Box(wo.BoundBoxMeshTransform, obb_color);
        }

        vec4 aabb_color = new vec4(.8194f, .0134f, .2401f, 1);
        rv.Overlay.Box(ob.BoundBox, aabb_color);
      }
      //Basis
      if (rv.Overlay.DrawObjectBasis)
      {
        vec3 ob_pos;
        vec3 basisX, basisY, basisZ;

        ob_pos = ob.WorldMatrix.ExtractTranslation();
        basisX = (ob.WorldMatrix * new vec4(1, 0, 0, 0)).xyz.normalized();
        basisY = (ob.WorldMatrix * new vec4(0, 1, 0, 0)).xyz.normalized();
        basisZ = (ob.WorldMatrix * new vec4(0, 0, 1, 0)).xyz.normalized();

        //Basis lines / basis matrix WORLD
        rv.Overlay.Line(ob_pos, ob_pos + basisX, new vec4(1, 0, 0, 1));
        rv.Overlay.Line(ob_pos, ob_pos + basisY, new vec4(0, 1, 0, 1));
        rv.Overlay.Line(ob_pos, ob_pos + basisZ, new vec4(0, 0, 1, 1));
      }
      if (rv.Overlay.DrawVertexAndFaceNormalsAndTangents)
      {
        _visibleStuff.AddObject(rv, ob, Gu.Lib.GetMaterial(Rs.Material.DebugDraw_VertexNormals_FlatColor));
      }

    }
    private void CollectVisibleObjects(RenderView rv, Camera3D cam, WorldObject ob)
    {
      Gu.Assert(ob != null);

      //TODO: fix this.
      bool excluded = (ob.ExcludeFromRenderView != null) && ob.ExcludeFromRenderView.TryGetTarget(out var obrv) && (obrv == rv);

      if (ob.Visible && !excluded)
      {
        if (cam == ob || cam.Frustum.HasBox(ob.BoundBox))
        {
          if (ob is Light)
          {
            _worldProps.Lights.Add(ob as Light);
          }
          //if (rv.Overlay.DrawBoundBoxesAndGizmos)
          {
            //dont draw frustum fro the active camera veiwport.
            if (ob is Camera3D)
            {
              if (rv.Camera != ob)
              {
                var c = ob as Camera3D;
                rv.Overlay.DrawFrustum(c.Frustum, 2);
              }
            }
          }
          if (ob.Mesh != null)
          {
            AddDrawable(rv, ob);
            NumVisibleObjects++;
          }
          else
          {
            NumCulledObjects++;
          }
        }

        ob.IterateChildrenSafe((c) =>
        {
          CollectVisibleObjects(rv, cam, c);
          return LambdaBool.Continue;
        });
      }
    }
    private void PickVisibleObject(WorldObject ob)
    {
      if (Gu.Context.Renderer.Picker.PickedObjectFrame == null)
      {
        ob.Pick();
      }
    }
    private void PickVisibleGlob(Glob g)
    {
      //TODO: picking code is copied from worldobject  we can share this..sloppy fn

      if (Gu.Context.Renderer.Picker.PickedObjectFrame != null)
      {
        //Picking is pixel perfect, so the first picked object is the exact object.
        //However objects may have children, and components which can also be picked, and may not be in the global list.
        //Obviously, a list of pickid->obj would be the fastest.
        return;
      }
      if (g.Opaque != null)
      {
        if (g.Opaque.PickId != Picker.c_iInvalidPickId)
        {
          var pixid = Gu.Context.Renderer.Picker.SelectedPixelId;
          if (pixid != 0)
          {
            if (pixid == g.Opaque.PickId)
            {
              Gu.Context.Renderer.Picker.PickedObjectFrame = g.Opaque;
            }
          }
        }
      }
      if (g.Transparent != null)
      {
        if (g.Transparent.PickId != Picker.c_iInvalidPickId)
        {
          var pixid = Gu.Context.Renderer.Picker.SelectedPixelId;
          if (pixid != 0)
          {
            if (pixid == g.Transparent.PickId)
            {
              Gu.Context.Renderer.Picker.PickedObjectFrame = g.Transparent;
            }
          }
        }
      }

    }

    #endregion
    #region Rendering

    public void RenderPipeStage(RenderView rv, PipelineStageEnum stage)
    {
      if (stage == PipelineStageEnum.Deferred)
      {
        _visibleStuff.Draw(rv, DrawMode.Deferred, _worldProps);
      }
      else if (stage == PipelineStageEnum.Forward)
      {
        _visibleStuff.Draw(rv, DrawMode.Forward, _worldProps);
      }
      else if (stage == PipelineStageEnum.Debug)
      {
        _visibleStuff.Draw(rv, DrawMode.Debug, _worldProps);
      }
    }

    public List<WorldObject> GetAllVisibleRootObjects()
    {

      var r = new List<WorldObject>();
      this.SceneRoot.IterateChildrenSafe((o) =>
      {
        Gu.Assert(o is WorldObject);
        r.Add(o);
        return LambdaBool.Continue;
      });
      return r;
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
      _worldMaterial_Op = new Material("worldMaterial_Op", maps.Albedo, maps.Normal);
      _worldMaterial_Op._gpuMaterial._vBlinnPhong_Spec = new vec4(0.59f, 0.61f, 0.61f, 170.0f);
      _worldMaterial_Op.DrawOrder = DrawOrder.Mid;
      _worldMaterial_Op.DrawMode = DrawMode.Deferred;
      _worldMaterial_Op.GpuRenderState.Blend = false;
      _worldMaterial_Op.GpuRenderState.DepthTest = true;
      _worldMaterial_Op.GpuRenderState.CullFace = true;

      _worldMaterial_Tp = new Material("worldMaterial_Tp", maps.Albedo, maps.Normal);
      _worldMaterial_Tp.GpuRenderState.Blend = true;
      _worldMaterial_Tp.GpuRenderState.DepthTest = true;
      _worldMaterial_Tp.GpuRenderState.CullFace = false;
      _worldMaterial_Tp.DrawOrder = DrawOrder.Mid;
      _worldMaterial_Tp.DrawMode = DrawMode.Deferred;

      //Block Material
      _blockObjectMaterial = new Material("BlockObjectMaterial", new Shader("v_v3n3x2_BlockObject_Instanced", "v_v3n3x2_BlockObject_Instanced"));
    }
    private void DefineWorldTiles()
    {
      //_blockTiles - Manual array that specifies which tiles go on the top, side, bottom
      //The tiles are specified by FileLoc structure which must be a class type.
      //This is used to index into the megatex to find the generated UV coordinates.

      //solid blocks
      AddWorldTile(TileCode.Grass, TileImage.Grass, TileVis.Opaque, HardnessValue.Dirt, BlockMeshType.Block, WorldTile.BlockOpacity_Solid);
      AddWorldTile(TileCode.Dirt, TileImage.Dirt, TileVis.Opaque, HardnessValue.Dirt, BlockMeshType.Block, WorldTile.BlockOpacity_Solid);
    }
    private PBRTextureArray CreateAtlas()
    {
      //Create the atlas.
      //Must be called after context is set.
      _worldMegatex = new MegaTex("world-megatex", true, MegaTex.MtClearColor.DebugRainbow, true, TexFilter.Nearest, 0.45f);

      foreach (var resource in WorldStaticData.TileImages)
      {
        MtFile mf = _worldMegatex.AddResource(resource.Value);
      }

      var cmp = _worldMegatex.Compile();

      //From the generated image, set the WorldTile UV coordinates to the generated UV coords.
      cmp.Albedo.SetFilter(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear);
      cmp.Normal.SetFilter(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear);
      //note:this doesn't mtter for tiles, since megatex is packed we must repeat uv's manually
      cmp.Albedo.SetWrap(TextureWrapMode.Repeat, TextureWrapMode.Repeat);
      cmp.Normal.SetWrap(TextureWrapMode.Repeat, TextureWrapMode.Repeat);

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
    #region Editing

    public void SetGlob(ivec3 gpos, Glob? g)
    {
      //Note: G can be null, in which case, there is no glob yet
      bool bhas = _globs.TryGetValue(gpos, out var gexist);
      if (g != null && gexist != null)
      {
        //This will not be a problem, this i just for debugging
        Gu.Log.Warn("Tried to overwrite an existing glob with another glob");
        Gu.DebugBreak();
      }
      if (g != null)
      {
        _existingGlobs.Add(gpos, g);
      }
      _globs.Add(gpos, g);
      _worldEditor.Edited = true;
    }

    #endregion
    #region Globs

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

        //Sanitize Glob Pos


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
    public Glob GetGlobForPoint(vec3 pt, GlobArray? ga = null)
    {
      if (ga != null)
      {
        foreach (var ng in ga.GlobsC27)
        {
          if (ng != null)
          {
            if (Info.GetGlobBoxGlobalI3(ng.Pos).containsPointBottomLeftInclusive(pt))
            {
              return ng;
            }
          }
        }
      }
      else
      {
        var i3 = Info.R3GlobaltoI3Glob(pt);
        if (_globs.TryGetValue(i3, out var outg))
        {
          return outg;
        }
      }
      return null;
    }

    #endregion
    #region Files / Saving

    private void CheckSaveWorld(double dt)
    {
      _autoSaveTimeout += dt;
      if (_autoSaveTimeout > _autoSaveTimeoutSeconds || _worldEditor.Edited)
      {
        SaveWorld();

        _autoSaveTimeout = 0;
        _worldEditor.Edited = false;
      }
    }
    private FileLoc GetTileFile(TileImage img)
    {
      WorldStaticData.TileImages.TryGetValue(img, out var loc);
      Gu.Assert(loc != null);
      return loc;
    }

    // private string GetObjectsFileName()
    // {
    //   return GetWorldFileName() + ".objects";
    // }
    private Glob LoadGlobOrSetEmpty(ivec3 gpos)
    {
      //Basically this is designed to load globs from the file where needed however that is
      //a future features "dynamic glob loading" for now we have them all loaded  BR_DYNAMIC_GLOB_LOADING
      Glob? g = null;//TryLoadGlob(gpos);
      if (g != null)
      {
        g.State = Glob.GlobState.CreatedOrLoaded;
      }
      SetGlob(gpos, g);

      return g;
    }
    private void InitWorldDiskFile(bool delete_world_start_fresh)
    {
      if (delete_world_start_fresh)
      {
        if (System.IO.Directory.Exists(Gu.SavePath))
        {
          Gu.Log.Info("Starting Fresh - Deleting " + Gu.SavePath);
          Directory.Delete(Gu.SavePath, true);
        }
      }
      Gu.Log.Info("Creating world save directory " + Gu.SavePath);
      if (!System.IO.Directory.Exists(Gu.SavePath))
      {
        System.IO.Directory.CreateDirectory(Gu.SavePath);
      }
      if (!TryLoadWorld())
      {
        SaveWorld();
      }
    }
    private void SaveWorld()
    {
      WorldFile w = new WorldFile();
      w.SaveWorld(this);
    }
    private bool TryLoadWorld()
    {
      return false;
      WorldFile w = new WorldFile();
      w.LoadWorld(this);
      _worldEditor.Edited = false;//This will be set, unset it.
      return true;
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
    public World GoToWorld(WorldInfo wi)
    {
      if (Gu.World != null)
      {
        Gu.World.ExitWorld();
        Gu.World = null;
        //GC.Collect();
      }
      var w = Gu.World = new World(_updateContext);
      w.Initialize(wi);
      return w;
    }
    public void CreateHillsArea()
    {
      //this will be moved to the editor or script
      Gu.Assert(Gu.World != null);
      Box3i b = new Box3i(new ivec3(-2, 0, -2), new ivec3(2, 1, 2));
      b.iterate((x, y, z, dbgcount) =>
      {
        //_world.Info.GlobWidthY * 0.9f
        var g = new Glob(Gu.World, new ivec3(x, y, z));
        g.Edit_GenHills(0, Gu.World.Info.GlobWidthY * 0.15f, true);
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
        if (!loc.ExistsOnDisk())
        {
          loc.Create();
        }
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
