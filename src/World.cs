using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace PirateCraft
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
    private float SanitizeHeight(float f, float max)
    {
      Gu.Assert(f >= 0 && f <= 1);
      return f * max;
    }
    public void Edit_GenHills(float y_base_rel, float min_height, float max_height, bool rnadomJunk)
    {
      //TODO: generate linked topology

      var rimg = Image.RandomImage_R32f(BeamGrid.SizeX + 1, BeamGrid.SizeY + 1, new Minimax<float>(min_height, max_height)).CreateHeightMap(2, 1f, 2).Normalized_R32f();//+1 due to vertex, not beam

      //ResourceManager.SaveImage(System.IO.Path.Combine(Gu.LocalTmpPath, "test-hm1a.png"), rimg.Convert(Img32.ImagePixelFormat.RGBA32ub, true), true);  

      //we move from +x, +z, and do this in the grid too
      BeamGrid.Iterate((g, x, z) =>
      {
        float max = _world.Info.GlobWidthY * 0.4f;
        //height
        ushort tl = _world.Info.ConvertHeight(SanitizeHeight(rimg.GetPixel_R32f(x, z + 1), max));
        ushort tr = _world.Info.ConvertHeight(SanitizeHeight(rimg.GetPixel_R32f(x + 1, z + 1), max));
        ushort bl = _world.Info.ConvertHeight(SanitizeHeight(rimg.GetPixel_R32f(x, z), max));
        ushort br = _world.Info.ConvertHeight(SanitizeHeight(rimg.GetPixel_R32f(x + 1, z), max));
        //base
        ushort bs = 0;

        if (tl == bs) { tl = (ushort)(bs + 1); }
        if (tr == bs) { tr = (ushort)(bs + 1); }
        if (bl == bs) { bl = (ushort)(bs + 1); }
        if (br == bs) { br = (ushort)(bs + 1); }

        if (rnadomJunk && ((x == 8 && z == 8) || (x == 9 && z == 8)))
        {
          //TESTING DEBUG
          //TESTING DEBUG
          //TESTING DEBUG
          tl = tr = bl = br = BeamVert.MaxVal;
        }

        BeamList b = new BeamList();
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
    [DataMember] public string Name { get; private set; } = Library.UnsetName;
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

    //Generation shell
    private int _currentShell = 1;
    private const int _maxShells = 4;//keep this < Min(DromeGlobs) to prevent generating more dromes
    public float GenRadiusShell { get { return GlobWidthX; } }
    public float DeleteMaxDistance { get { return (GenRadiusShell * (float)(_maxShells + 1)); } }//distance beyond which things are deleted, this must be greater than max gen distance to prevent ping pong loading
    public float GenerateDistance { get { return (GenRadiusShell * (float)_currentShell); } } //distance under which things are generated
    public float RenderDistance { get { return (GenRadiusShell) * _maxShells; /* (GlobWidthX * 16) * (GlobWidthX * 16); */ } }

    #endregion

    public WorldInfo(string worldName, bool delete_world_start_fresh, int limit_y_axis = 0, float blockSize = 4.0f, float blockHeightScale = 0.25f, int globBlocksX = 16)
    {
      Name = worldName;
      LimitYAxisGeneration = limit_y_axis;
      DeleteStartFresh = delete_world_start_fresh;
      BlockSizeX = blockSize;
      GlobBlocksX = globBlocksX;
      HeightScale = blockHeightScale;

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
    private int _dbg_added_objects = 0;
    private int _dbg_drawcall_count = 0;
    // massive friggin dict of dict of dict..Dictionary<RenderView, Dictionary<DrawMode, Dictionary<DrawOrder, Dictionary<Material, Dictionary<MeshView, List<Drawable>>>>>>
    private Dictionary<RenderView, Dictionary<DrawMode, SortedDictionary<DrawOrder, DrawCall>>> _dict =
     new Dictionary<RenderView, Dictionary<DrawMode, SortedDictionary<DrawOrder, DrawCall>>>();//View -> stage -> distance/draw order -> instances sorted by material/mesh

    public void Clear(RenderView rv)
    {
      //Clear all collected for JUST the given view
      _dict = _dict.ConstructIfNeeded();
      if (_dict.TryGetValue(rv, out var stageDist))
      {
        stageDist.Clear();
      }

      _dbg_drawcall_count = 0;
      _dbg_added_objects = 0;
    }
    public void AddObject(RenderView rv, Drawable ob, Material? customMaterial = null)
    {
      Gu.Assert(ob != null);
      if (ob.Mesh == null)
      {
        return;
      }

      _dict = _dict.ConstructIfNeeded();

      Dictionary<DrawMode, SortedDictionary<DrawOrder, DrawCall>>? stageDist = null;
      if (!_dict.TryGetValue(rv, out stageDist))
      {
        stageDist = new Dictionary<DrawMode, SortedDictionary<DrawOrder, DrawCall>>();
        _dict.Add(rv, stageDist);
      }

      SortedDictionary<DrawOrder, DrawCall>? distCall = null;
      if (!stageDist.TryGetValue(ob.Mesh.DrawMode, out distCall))
      {
        distCall = new SortedDictionary<DrawOrder, DrawCall>();
        stageDist.Add(ob.Mesh.DrawMode, distCall);
      }

      DrawCall? call = null;
      if (!distCall.TryGetValue(ob.Mesh.DrawOrder, out call))
      {
        call = new DrawCall();
        distCall.Add(ob.Mesh.DrawOrder, call);
      }

      call.AddVisibleObject(ob, customMaterial);
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
    private const string c_strSaveWorldVersion = "0.01";
    private const string c_strSaveWorldHeader = "WorldFilev" + c_strSaveWorldVersion;
    private const int c_intDromeFileVersion = 1;

    #endregion
    #region Public:Members

    public WorldInfo Info { get { return _worldInfo; } }
    public WorldEditor Editor { get { return _worldEditor; } }
    public WorldObject SceneRoot { get { return _sceneRoot; } }
    public int NumGlobs { get { return _globs.Count; } }
    public int NumRenderGlobs { get { return _renderGlobs.Count; } }
    public int NumVisibleRenderGlobs { get { return _visibleRenderGlobs.Count; } }
    public WorldProps WorldProps { get { return _worldProps; } }
    public int NumCulledObjects { get; private set; } = 0;
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
    #region Private:Members

    [DataMember] private GameMode _eGameMode = GameMode.Edit;
    private WorldEditor? _worldEditor = null;
    [DataMember] private WorldInfo? _worldInfo = null;
    [DataMember] private WorldObject _sceneRoot = new WorldObject("Scene_Root");
    private long _lastShellIncrementTimer_ms = 0;
    private long _lastShellIncrementTimer_ms_Max = 500;
    private WorldObject dummy = new WorldObject("dummy_beginrender");
    private WorldObject? _debugDrawLines = null;
    private WorldObject? _debugDrawPoints = null;
    private WorldObject? _debugDrawTris = null;

    //There is no need for ivec3 here.
    //we should sort all objects by distance.
    private VisibleStuff _visibleStuff;
    private Dictionary<ivec3, Glob> _visibleRenderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //globs that must be drawn this frame

    private Dictionary<ivec3, Glob> _globs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //All globs, which may be null if the glob region has been visible, but does not exist
    private Dictionary<ivec3, Glob> _existingGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //globs that are loaded, and exist
    private MultiMap<float, GlobArray> _queuedGlobs = new MultiMap<float, GlobArray>();// queued for topology
    private Dictionary<ivec3, Glob> _renderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //globs that can be drawn this frame. 
    private Dictionary<ushort, WorldTile>? _blockTiles = null;
    private WorldProps? _worldProps = null; //Environment props.
    private Material? _worldMaterial_Op = null;
    private Material? _worldMaterial_Tp = null;
    private MegaTex? _worldMegatex = null;
    private Material? _blockObjectMaterial = null;
    private double _autoSaveTimeoutSeconds = 5;
    private double _autoSaveTimeout = 0;

    #endregion

    public World(WindowContext updateContext)
    {
      UpdateContext = updateContext;
    }
    public void Initialize(WorldInfo info)
    {
      EmbeddedResources.BuildResources();

      _worldInfo = info;
      _worldEditor = new WorldEditor();

      _worldProps = new WorldProps("WorldProps");

      GameMode = Gu.EngineConfig.StartInEditMode ? GameMode.Edit : GameMode.Play;

      DefineWorldTiles();
      CreateMaterials();

      InitWorldDiskFile(info.DeleteStartFresh);

      _worldProps.EnvironmentMap = new Texture("_worldProps.EnvironmentMap", Gu.Lib.LoadImage("envmap", new FileLoc("hilly_terrain_01_2k.hdr", FileStorage.Embedded)), true, TexFilter.Nearest);
      _worldProps.DayNightCycle = new DayNightCycle();
      _worldProps.DayNightCycle.Update(0);

      //Gu.Log.Info("Building initail grid");
      //* BuildDromeGrid(Player.WorldMatrix.extractTranslation(), GenRadiusShell, true);
      //I'm assuming since this is cube voxesl we're going to do physics on the integer grid, we don't need triangle data then.
      //* WaitForAllDromesToGenerate();
      //* UpdateLiterallyEverything_Blockish(Camera); // This will generate the globs
      //* WaitForAllGlobsToGenerate();
    }
    private void IterateObjectsSafe(Func<WorldObject, LambdaBool> f, bool iterateDeleted = false)
    {
      SceneRoot.IterateChildrenSafe(f, iterateDeleted);
    }
    public void UpdateWorld(double dt)
    {
      if (UpdateContext != Gu.Context)
      {
        Gu.Log.Error("Tried to call update twice between two windows. Update must be called once on a single window (or, we could put it on its own thread, unless we do end up with OpenGL stuff.)");
        Gu.DebugBreak();
      }

      Gu.Lib.Update(dt);

      UpdateObjects(dt);
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

    public void BuildAndCull(RenderView rv)
    {
      if (rv.ViewMode != RenderViewMode.UIOnly)
      {
        if (rv.Camera != null && rv.Camera.TryGetTarget(out var cm))
        {
          cm.SanitizeTransform();

          BuildGrid(cm.Position_World, Info.GenerateDistance);

          //Collect visible
          _visibleStuff = _visibleStuff.ConstructIfNeeded();
          _visibleStuff.Clear(rv);
          _worldProps.ClearLights();

          CollectVisibleGlobs(rv, cm);
          CollectVisibleObjects(rv, cm);
        }
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

              if (nbeams.Count > 0)
              {
                Gu.Trap();
              }
              if (nbeams.Count > 1)
              {
                Gu.Trap();
              }
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

        var faces = MeshGen.ComputeNormalsAndTangents(vertsarr, indsarr.AsUIntArray(), true, true);

        g.Opaque.Mesh = new MeshData(name, PrimitiveType.Triangles,
                Gpu.CreateVertexBuffer(name, vertsarr),
                Gpu.CreateIndexBuffer(name, indsarr),
                Gpu.CreateShaderStorageBuffer(name, faces.ToArray()),
                true
              );
        g.Opaque.Mesh.DrawOrder = DrawOrder.Mid;
        g.Opaque.Mesh.DrawMode = DrawMode.Deferred;

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

      //Make sure edges are kosher
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

    #region Objects

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
    public WorldObject? FindObject(string name)
    {
      WorldObject? ret = null;
      IterateObjectsSafe((ob) =>
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
    public WorldObject AddObject(WorldObject ob)
    {
      //Add object to the scene root
      if (ob == null)
      {
        Gu.Log.Error("Object was null adding to world.");
        Gu.DebugBreak();
        return null;
      }

      if (ob.Mesh == null)
      {
        ob.Mesh = MeshData.DefaultBox;
      }
      if (ob.Material == null)
      {
        ob.Material = Material.DefaultObjectMaterial;
      }

      SceneRoot.AddChild(ob);
      ob.OnAddedToScene?.Invoke(ob);
      ob.State = WorldObjectState.Active;
      _worldEditor.Edited = true;

      return ob;
    }
    private void UpdateObjects(double dt)
    {
      Box3f dummy = Box3f.Zero;
      dummy.genResetLimits();
      List<WorldObject> toRemove = new List<WorldObject>();
      IterateObjectsSafe((ob) =>
      {
        if (ob.State != WorldObjectState.Removed)
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
      //Assuming we're going to modify object resting state when other objects change state
      float vlen2 = (ob.Velocity * (float)dt).len2();
      if (ob.OnGround && vlen2 > 0)
      {
        ob.OnGround = false;
      }
      if (ob.OnGround)
      {
        return;
      }

      float maxv = WorldInfo.MaxVelocity_Second_Frame * dt;
      float maxv2 = (float)Math.Pow(maxv, 2.0f);

      vec3 dbg_initial_v = ob.Velocity;

      //Our final guys in frame time units
      vec3 final_p = ob.Position_Local;
      vec3 final_v = ob.Velocity * dt;

      //Too big
      vlen2 = (final_v * dt).len2();
      if (vlen2 > maxv2)
      {
        final_v = final_v.normalized() * maxv;
      }

      vec3 g_v = vec3.Zero;
      if (ob.HasGravity)
      {
        g_v = new vec3(0, _worldInfo.Gravity * dt, 0);
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
      NumCulledObjects = 0;
      CollectVisibleObjects(rv, cm, SceneRoot);
      AddDebugDrawObjects(rv);
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
              PickVisibleGlob(g);
              _visibleRenderGlobs.Add(kvp.Key, g);
              if (g.Opaque != null)
              {
                _visibleStuff.AddObject(rv, g.Opaque);
                DebugDrawObject(rv, g.Opaque);
              }
              if (g.Transparent != null)
              {
                _visibleStuff.AddObject(rv, g.Transparent);
                DebugDrawObject(rv, g.Transparent);
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
    private void CollectVisibleObjects(RenderView rv, Camera3D cam, WorldObject ob)
    {
      Gu.Assert(ob != null);

      //TODO: fix this.
      if (ob.ExcludeFromRenderView != null && ob.ExcludeFromRenderView.TryGetTarget(out var obrv))
      {
        if (obrv == rv)
        {
          return;
        }
      }

      if (cam.Frustum.HasBox(ob.BoundBox))
      {
        if (ob.HasLight)
        {
          _worldProps.Lights.Add(ob);
        }
        if (ob.Mesh != null)
        {
          _visibleStuff.AddObject(rv, ob);
          DebugDrawObject(rv, ob);
          PickVisibleObject(ob);
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

    #endregion

    #region Index

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
    }
    private void DebugDrawObject(RenderView rv, Drawable ob)
    {
      var wo = (ob as WorldObject);
      if (Gu.Context.DebugDraw.DrawBoundBoxes)
      {
        if (wo != null)
        {
          Gu.Assert(ob != null);
          vec4 aabb_color = new vec4(.8194f, .0134f, .2401f, 1);
          vec4 obb_color = new vec4(.9192f, .8793f, .9131f, 1);

          Gu.Context.DebugDraw.Box(wo.BoundBoxMeshTransform, obb_color);
          Gu.Context.DebugDraw.Box(wo.BoundBox, aabb_color);
        }
      }
      if (Gu.Context.DebugDraw.DrawObjectBasis)
      {
        vec3 ob_pos;
        vec3 basisX, basisY, basisZ;
        if (wo == null)
        {
          ob_pos = ob.WorldMatrix.ExtractTranslation();
          basisX = (ob.WorldMatrix * new vec4(1, 0, 0, 0)).xyz().normalized();
          basisY = (ob.WorldMatrix * new vec4(0, 1, 0, 0)).xyz().normalized();
          basisZ = (ob.WorldMatrix * new vec4(0, 0, 1, 0)).xyz().normalized();
        }
        else
        {
          ob_pos = wo.Position_World;
          basisX = wo.BasisX_World;
          basisY = wo.BasisY_World;
          basisZ = wo.BasisZ_World;
        }

        //Basis lines / basis matrix WORLD
        Gu.Context.DebugDraw.Line(ob_pos, ob_pos + basisX, new vec4(1, 0, 0, 1));
        Gu.Context.DebugDraw.Line(ob_pos, ob_pos + basisY, new vec4(0, 1, 0, 1));
        Gu.Context.DebugDraw.Line(ob_pos, ob_pos + basisZ, new vec4(0, 0, 1, 1));
      }
      if (Gu.Context.DebugDraw.DrawVertexAndFaceNormalsAndTangents)
      {
        _visibleStuff.AddObject(rv, ob, Gu.Lib.LoadMaterial(RName.Material_DebugDraw_VertexNormals_FlatColor));
      }
      if (Gu.Context.DebugDraw.DrawWireframeOverlay)
      {
        _visibleStuff.AddObject(rv, ob, Gu.Lib.LoadMaterial(RName.DebugDraw_Wireframe_FlatColor));
      }
    }
    private void AddDebugDrawObjects(RenderView rv)
    {
      if (Gu.Context.DebugDraw.LinePoints.Count > 0)
      {
        //  GL.LineWidth(1.0f);//TODO: - this is now invalid
        Gpu.CheckGpuErrorsDbg();
        if (_debugDrawLines == null)
        {
          _debugDrawLines = CreateObject("debug_lines", null, Gu.Lib.LoadMaterial(RName.Material_DebugDrawMaterial));

          _debugDrawLines.Mesh = new MeshData("debug_lines", PrimitiveType.Lines,
            Gpu.CreateVertexBuffer("debug_lines", Gu.Context.DebugDraw.LinePoints.ToArray()),
            Gpu.CreateIndexBuffer("debug_lines", Gu.Context.DebugDraw.LineInds.ToArray()),
            null, false);
        }
        else
        {
          _debugDrawLines.Mesh.VertexBuffers[0].ExpandBuffer(Gu.Context.DebugDraw.LinePoints.Count);
          _debugDrawLines.Mesh.VertexBuffers[0].CopyToGPU(GpuDataPtr.GetGpuDataPtr(Gu.Context.DebugDraw.LinePoints.ToArray()));
          _debugDrawLines.Mesh.IndexBuffer.ExpandBuffer(Gu.Context.DebugDraw.LineInds.Count);
          _debugDrawLines.Mesh.IndexBuffer.CopyToGPU(GpuDataPtr.GetGpuDataPtr(Gu.Context.DebugDraw.LineInds.ToArray()));
          _debugDrawLines.MeshView.Start = 0;
          _debugDrawLines.MeshView.Count = Gu.Context.DebugDraw.LineInds.Count;
        }
        _visibleStuff.AddObject(rv, _debugDrawLines);
      }
      if (Gu.Context.DebugDraw.Points.Count > 0)
      {
        // GL.PointSize(5);//TODO: - this is now invalid
        Gpu.CheckGpuErrorsDbg();
        if (_debugDrawPoints == null)
        {
          _debugDrawPoints = CreateObject("debug_points", null, Gu.Lib.LoadMaterial(RName.Material_DebugDrawMaterial));
          _debugDrawPoints.Mesh = new MeshData("debug_points", PrimitiveType.Points,
            Gpu.CreateVertexBuffer("debug_points", Gu.Context.DebugDraw.Points.ToArray()),
            null, false);
        }
        else
        {
          _debugDrawPoints.Mesh.VertexBuffers[0].ExpandBuffer(Gu.Context.DebugDraw.Points.Count);
          _debugDrawPoints.Mesh.VertexBuffers[0].CopyToGPU(GpuDataPtr.GetGpuDataPtr(Gu.Context.DebugDraw.Points.ToArray()));
          _debugDrawPoints.MeshView.Start = 0;
          _debugDrawPoints.MeshView.Count = Gu.Context.DebugDraw.Points.Count;
        }
        _visibleStuff.AddObject(rv, _debugDrawPoints);
      }
      if (Gu.Context.DebugDraw.TriPoints.Count > 0)
      {
        //   GL.PointSize(1);//TODO: - this is now invalid
        //  GL.LineWidth(1);
        Gpu.CheckGpuErrorsDbg();
        if (_debugDrawTris == null)
        {
          _debugDrawTris = CreateObject("debug_tris", null, Gu.Lib.LoadMaterial(RName.Material_DebugDrawMaterial));
          _debugDrawTris.Mesh = new MeshData("debug_tris", PrimitiveType.Triangles,
            Gpu.CreateVertexBuffer("debug_tris", Gu.Context.DebugDraw.TriPoints.ToArray()),
            null, false);
        }
        else
        {
          _debugDrawTris.Mesh.VertexBuffers[0].ExpandBuffer(Gu.Context.DebugDraw.TriPoints.Count);
          _debugDrawTris.Mesh.VertexBuffers[0].CopyToGPU(GpuDataPtr.GetGpuDataPtr(Gu.Context.DebugDraw.TriPoints.ToArray()));
          _debugDrawTris.MeshView.Start = 0;
          _debugDrawTris.MeshView.Count = Gu.Context.DebugDraw.TriPoints.Count;          
        }
        _visibleStuff.AddObject(rv, _debugDrawTris);
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
      var shader = Shader.DefaultObjectShader();// Gu.Resources.LoadShader("v_Glob", false, FileStorage.Embedded);
      _worldMaterial_Op = new Material("worldMaterial_Op", shader, maps.Albedo, maps.Normal);
      _worldMaterial_Tp = new Material("worldMaterial_Tp", shader, maps.Albedo, maps.Normal);
      _worldMaterial_Tp.GpuRenderState.Blend = true;
      _worldMaterial_Tp.GpuRenderState.DepthTest = true;
      _worldMaterial_Tp.GpuRenderState.CullFace = false;

      //Block Material
      _blockObjectMaterial = Gu.Lib.LoadMaterial("BlockObjectMaterial", Gu.Lib.LoadShader("v_v3n3x2_BlockObject_Instanced", "v_v3n3x2_BlockObject_Instanced", FileStorage.Embedded));
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
      _worldMegatex = new MegaTex("world-megatex", true, MegaTex.MtClearColor.DebugRainbow, true, TexFilter.Nearest, true);

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

    #region World Edit 

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
    private string GetWorldFileName()
    {
      string worldfile = Info.Name + ".world";
      return System.IO.Path.Combine(Gu.SavePath, worldfile);
    }
    private string GetObjectsFileName()
    {
      return GetWorldFileName() + ".objects";
    }
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
    public bool SaveWorld()
    {
      string worldfn = GetWorldFileName();
      try
      {
        if (!System.IO.Directory.Exists(Gu.SavePath))
        {
          System.IO.Directory.CreateDirectory(Gu.SavePath);
        }

        var enc = Encoding.GetEncoding("iso-8859-1");
        using (var fs = System.IO.File.OpenWrite(worldfn))
        {
          using (var bwFile = new System.IO.BinaryWriter(fs, enc))
          {
            bwFile.Write((string)c_strSaveWorldHeader);

            SerializeTools.SerializeEverything(bwFile, this);

            bwFile.Write((Int32)_globs.Count);//ivec3
            foreach (var g in _globs)
            {
              bwFile.Write(g.Key);//ivec3
              if (g.Value != null)
              {
                bwFile.Write(true);
                SerializeTools.SerializeBlock(g.Value.Name, bwFile, false, (b) =>
                {
                  g.Value.Serialize(b.Writer);
                });
              }
              else
              {
                bwFile.Write(false);
              }
            }

            //   Gu.Log.Warn(("TODO:save obvjects"));
            //   bwFile.Write((Int32)_objects.Count);//ivec3
            //   foreach (var ob in this._objects)
            //   {
            //     SerializeTools.SerializeBlock(ob.Value.Name, bwFile, false, (b) =>
            //     {
            //       ob.Value.Serialize(b.Writer);//get key from ob.Name
            //     });
            //   }
            // }

          }
        }

        //build table with object ids


        //TODO: fix up data sources so that we do not serialize data classes
        //Note: Img32 Data is not being serialized



        // var s = SerializeTools.SerializeJSON(this, true);
        // new FileLoc(GetObjectsFileName(), FileStorage.Disk).WriteAllText(s);
        // var ob = JsonConvert.DeserializeObject(s);


        // using (var fs2 = System.IO.File.OpenWrite())
        // {
        //   JsonSerializer.SerializeToUtf8Bytes(data,
        //             new JsonSerializerOptions { WriteIndented = false, IgnoreNullValues = true });

        //   var bf = new BinaryFormatter();
        //   bf.Serialize(fs2, this);
        // }

      }
      catch (Exception ex)
      {
        Gu.Log.Error($"Failed to load world file{worldfn}", ex);
        return false;
      }
      return true;
    }

    private bool TryLoadWorld()
    {
      return false;

      string worldfn = GetWorldFileName();
      var enc = Encoding.GetEncoding("iso-8859-1");

      try
      {
        if (!System.IO.File.Exists(worldfn))
        {
          return false;
        }
        SerializedFileVersion version = new SerializedFileVersion(1000);
        using (var fs = System.IO.File.OpenRead(worldfn))
        using (var br = new System.IO.BinaryReader(fs, enc))
        {
          string h = br.ReadString();
          if (h != c_strSaveWorldHeader)
          {
            Gu.BRThrowException("World header '" + h + "' does not match current header version '" + c_strSaveWorldHeader + "'");
          }

          SerializeTools.DeserializeBlock(br, (b) => { Info.Deserialize(b.Reader, version); });

          var gcount = br.ReadInt32();//ivec3
          for (int i = 0; i < gcount; i++) //foreach (var g in _globs)
          {
            ivec3 gp = br.ReadIVec3();// bw.Write(g.Key);//ivec3
            bool exist = br.ReadBoolean();
            Glob g = new Glob(this, gp);
            if (exist)
            {
              SerializeTools.DeserializeBlock(br, (b) =>
              {
                g.Deserialize(b.Reader, version);
              });
            }
            SetGlob(gp, g);
          }

          var obcount = br.ReadInt32();//ivec3
          for (int i = 0; i < obcount; ++i)
          {
            WorldObject wo = new WorldObject(Library.UnsetName);
            SerializeTools.DeserializeBlock(br, (b) =>
            {
              wo.Deserialize(b.Reader, version);
            });
            AddObject(wo);
          }

          _worldEditor.Edited = false;//This will be set, unset it.
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error($"Failed to load world file{worldfn}", ex);
        return false;
      }
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
    public void CreateHillsArea()
    {
      Gu.Assert(Gu.World != null);
      Box3i b = new Box3i(new ivec3(-2, 0, -2), new ivec3(2, 1, 2));
      b.iterate((x, y, z, dbgcount) =>
      {
        var g = new Glob(Gu.World, new ivec3(x, y, z));
        g.Edit_GenHills(0, Gu.World.Info.BlockSizeY, Gu.World.Info.BlockSizeY * 5, true);
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
