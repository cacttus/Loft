using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;
using OpenTK.Graphics.OpenGL4;

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


   //Main glob that holds blocks
   public class Glob
   {
      public enum GlobState
      {
         None, LoadedAndQueued, Topologized
      }
      public enum GlobDensityState
      {
         Partial, SolidBlocksOnly, SolidItems, Empty_AndNoData//Partial = renderable, Solid = fully solid, Empty = empty
      }
      public Int64 GeneratedFrameStamp { get; private set; } = 0;
      public MeshData Transparent = null;
      public MeshData Opaque = null;
      public Block[] Blocks = null;
      public ivec3 Pos = new ivec3(0, 0, 0);
      public GlobDensityState DensityState = GlobDensityState.Partial;
      public GlobState State = GlobState.None;
      public int Solid = 0;
      public int Empty = 0;
      public int Items = 0;
      public vec3 OriginR3
      {
         get
         {
            vec3 r = new vec3(Pos.x * World.GlobWidthX, Pos.y * World.GlobWidthY, Pos.z * World.GlobWidthZ);
            return r;
         }
      }
      public Glob(ivec3 pos, Int64 genframeStamp)
      {
         Pos = pos;
         GeneratedFrameStamp = genframeStamp;
      }
   }
   //GlobWorld
   public class World
   {
      private Dictionary<string, WorldObject> Objects { get; set; } = new Dictionary<string, WorldObject>();

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
      public const int MaxGenerationShells = 20;
      public readonly vec3 BlockRadiusR3 = new vec3(BlockSizeX * 0.5f, BlockSizeY * 0.5f, BlockSizeZ * 0.5f);//Radius from center of glob to the corner.
      public readonly vec3 GlobRadiusR3 = new vec3(GlobWidthX * 0.5f, GlobWidthY * 0.5f, GlobWidthZ * 0.5f);//Radius from center of glob to the corner.
      public vec3 GlobDiameterR3 { get { return GlobRadiusR3 * 2; } }
      public vec3 RenderRadiusShell { get { return GlobDiameterR3; } }
      //  public float MaxRenderDistance { get { return GlobDiameterR3 * 5; } } //Render all the nodes we can see.

      private bool _firstGeneration = true; //Whether this is the initial generation, where we would need to generate everything around the player.
      private int _currentShell = 1;
      private ivec3 playerLastGlob = new ivec3(0, 0, 0);
      private WorldObject dummy = new WorldObject();
      private WorldObject _debugDraw = null;
      private List<WorldObject> _renderObs_Ordered = new List<WorldObject>();
      private Dictionary<ivec3, Glob> _globs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //All globs
      private Dictionary<ivec3, Glob> _renderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
      private Dictionary<ivec3, Glob> _visibleRenderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
      private Dictionary<ivec3, Glob> _globsToUpdate = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.


      public int Dbg_N_OB_Culled = 0;
      public int NumGlobs { get { return _globs.Count; } }
      public int NumRenderGlobs { get { return _renderGlobs.Count; } }
      public int NumVisibleRenderGlobs { get { return _visibleRenderGlobs.Count; } }
      public WorldObject SceneRoot { get; private set; } = new WorldObject();
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
      private const int GlobFileVersion = 1;
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
         Objects.Add(name, c);
         return c;
      }
      public WorldObject CreateObject(string name, MeshData mesh, Material material, vec3 pos = default(vec3))
      {
         WorldObject ob = new WorldObject(pos);
         ob.Name = name;
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
      private WorldObject AddObject(WorldObject ob)
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
         _renderObs_Ordered.Clear();
         Dbg_N_OB_Culled = 0;
         //Add sky as it must come first.
         CollectObjects(camera, Sky);
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
            //Objects
            foreach (var ob in _renderObs_Ordered)
            {
               DrawObMesh(ob,Delta,camera);
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
         camera.EndRender();
      }
      public void RenderDebug(double Delta, Camera3D camera)
      {
         var frame = Gu.Context.FrameStamp;

         vec4 bbcolor = new vec4(1, 0, 0, 1);
         foreach (var ob in Objects.Values)
         {
            Gu.Context.Renderer.DebugDraw.Box(ob.BoundBoxMeshTransform, ob.Color);
            Gu.Context.Renderer.DebugDraw.Box(ob.BoundBox, bbcolor);
         }
         if (_debugDraw == null)
         {
            _debugDraw = CreateAndAddObject("debug_draw", null, new Material(Shader.LoadShader("v_v3c4_debugdraw", false)));
            RemoveObject(_debugDraw.Name);//Doesn't actually destroy it
         }
         if (_debugDraw.Mesh == null)
         {
            _debugDraw.Mesh = new MeshData("Debug", PrimitiveType.Lines, DebugDraw.VertexFormat);
         }
         _debugDraw.Mesh.CreateBuffers(Gpu.GetGpuDataPtr(Gu.Context.Renderer.DebugDraw.Verts.ToArray()), null, false);

         DrawObMesh(_debugDraw, Delta, camera);
      }
      private void CollectObjects(Camera3D cam, WorldObject ob)
      {
         Gu.Assert(ob != null);
         if (ob.Mesh != null)
         {
            if (cam.Frustum.HasBox(ob.BoundBox))
            {
               _renderObs_Ordered.Add(ob);
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
                     Glob g = GenerateAndSaveOrLoadGlob(gpos);
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

            if (g.State == Glob.GlobState.LoadedAndQueued)
            {
               CreateBlocks(g);
            }

            if (g.DensityState != Glob.GlobDensityState.Empty_AndNoData)
            {
               TopologizeGlob(g);
               dbg_nEmptyNoData++;
            }
            else
            {
               //The block is empty, the inside of the block has no topology. No data.
               g.Blocks = null;
            }
            if (g.DensityState != Glob.GlobDensityState.SolidBlocksOnly)
            {
               //No neighboring blocks would be visible, so stitchin gisn't needed
               StitchGlobTopology(g);
               dbg_nSkippedStitch++;
            }
         }
         _globsToUpdate.Clear();

         return newGlobs;
      }
      private void CreateBlocks(Glob g)
      {
         vec3 globOriginR3 = new vec3(GlobWidthX * (float)g.Pos.x, GlobWidthY * (float)g.Pos.y, GlobWidthZ * (float)g.Pos.z);

         for (int z = 0; z < GlobBlocksZ; z++)
         {
            for (int y = 0; y < GlobBlocksY; y++)
            {
               for (int x = 0; x < GlobBlocksX; x++)
               {
                  ivec3 block_index = new ivec3(x, y, z);
                  //Computing density from block center instead of corner
                  vec3 block_world = globOriginR3 + new vec3(
                     x * BlockSizeX + BlockSizeX * 0.5f,
                     y * BlockSizeY + BlockSizeY * 0.5f,
                     z * BlockSizeZ + BlockSizeZ * 0.5f);

                  Block blk = GetBlock(g, block_index);
                  UInt16 bic = CreateBlock(g, blk, block_world, block_index);
                  blk.Value = bic;

                  //Make sure this is just loaded. This function initially generates block values for new worlds.
                  //Don't call manually.
                  Gu.Assert(g.State == Glob.GlobState.LoadedAndQueued);

                  SetBlock(g, block_index, blk, false);
               }
            }
         }

      }
      private UInt16 CreateBlock(Glob g, Block blk, vec3 world_pos, ivec3 block_index)
      {
         //Coming in we have just value/empty blocks now generate the block type.
         UInt16 item = blk.Value;
         if (blk.Value != BlockItemCode.Empty)
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
      private Glob GenerateAndSaveOrLoadGlob(ivec3 gpos)
      {
         Glob g = TryLoadGlob(gpos);
         if (g == null)
         {
            g = GenerateGlob(gpos);
            SaveGlob(g);
         }
         _globsToUpdate.Add(gpos, g);
         g.State = Glob.GlobState.LoadedAndQueued;

         return g;
      }
      private string GetGlobFileName(ivec3 gpos)
      {
         //[8][8][8]
         Gu.Assert(System.IO.Directory.Exists(WorldSavePath));
         string sx = (gpos.x < 0 ? "" : "+") + gpos.x.ToString("D8");
         string sy = (gpos.y < 0 ? "" : "+") + gpos.y.ToString("D8");
         string sz = (gpos.z < 0 ? "" : "+") + gpos.z.ToString("D8");
         return System.IO.Path.Combine(WorldSavePath, sx + sy + sz + ".g");
      }
      private string GetWorldFileName()
      {
         Gu.Assert(System.IO.Directory.Exists(WorldSavePath));
         string worldfile = WorldName + ".world";
         return System.IO.Path.Combine(WorldSavePath, worldfile);
      }
      private Glob GenerateGlob(ivec3 gpos)
      {
         Glob g = new Glob(gpos, Gu.Context.FrameStamp);
         vec3 globOriginR3 = new vec3(GlobWidthX * gpos.x, GlobWidthY * gpos.y, GlobWidthZ * gpos.z);

         g.Solid = g.Empty = g.Items = 0;

         for (int z = 0; z < GlobBlocksZ; z++)
         {
            for (int y = 0; y < GlobBlocksY; y++)
            {
               for (int x = 0; x < GlobBlocksX; x++)
               {
                  //Computing density from block center instead of corner
                  vec3 block_world = globOriginR3 + new vec3(
                     x * BlockSizeX + BlockSizeX * 0.5f,
                     y * BlockSizeY + BlockSizeY * 0.5f,
                     z * BlockSizeZ + BlockSizeZ * 0.5f);
                  var block = new Block(Density(g, block_world));
                  SetBlock(g, new ivec3(x, y, z), block, false);
               }
            }
         }

         return g;
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
      private void TopologizeGlob(Glob g)
      {
         bool globTopologyBefore = g.Opaque != null || g.Transparent != null;

         g.Opaque = null;
         g.Transparent = null;
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
         Block b;
         vec2[] texs = new vec2[4];
         List<MtTex> patches = new List<MtTex>();

         for (int z = 0; z < GlobBlocksZ; z++)
         {
            for (int y = 0; y < GlobBlocksY; y++)
            {
               for (int x = 0; x < GlobBlocksX; x++)
               {
                  b = GetBlock(g, new ivec3(x, y, z));
                  if (b.IsEmpty())
                  {
                     continue;
                  }

                  patches = null;
                  _tileUVs.TryGetValue(b.Value, out patches);

                  for (int face = 0; face < 6; ++face)
                  {
                     //Bottom left corner
                     //This is the exact block center location in R3. It's less efficent but it's easier to use
                     vec3 block_pos_rel_R3 = new vec3(World.BlockSizeX * x, World.BlockSizeY * y, World.BlockSizeZ * z);
                     vec3 block_pos_rel_R3_Center = block_pos_rel_R3 + new vec3(World.BlockSizeX * 0.5f, World.BlockSizeY * 0.5f, World.BlockSizeZ * 0.5f);
                     vec3 block_pos_abs_R3_Center = block_pos_rel_R3_Center + g.OriginR3;
                     vec3 block_pos_abs_R3_Center_Neighbor = block_pos_abs_R3_Center + face_offs[face];
                     ivec3 g_n = R3toI3Glob(block_pos_abs_R3_Center_Neighbor);

                     Block? b_n;
                     if (g_n == g.Pos)
                     {
                        //We are same glob - globs may not be added to the list yet.
                        ivec3 bpos = R3toI3BlockLocal(block_pos_abs_R3_Center_Neighbor);
                        b_n = GetBlock(g, bpos);
                     }
                     else
                     {
                        b_n = GrabBlockR3(block_pos_abs_R3_Center_Neighbor);
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
                        vec3 block_pos_abs_R3 = block_pos_rel_R3 + g.OriginR3;
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
            g.Opaque = new MeshData("", OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles,
               v_v3n3x2.VertexFormat, Gpu.GetGpuDataPtr(verts.ToArray()),
               IndexFormatType.Uint32, Gpu.GetGpuDataPtr(inds.ToArray())
               );
         }

         //Update RenderGlobs
         bool globTopologyAfter = g.Opaque != null || g.Transparent != null;
         if (globTopologyBefore && !globTopologyAfter)
         {
            _renderGlobs.Remove(g.Pos);
         }
         else if (!globTopologyBefore && globTopologyAfter)
         {
            _renderGlobs.Add(g.Pos, g);
         }

         g.State = Glob.GlobState.Topologized;
      }
      private UInt16 Density(Glob g, vec3 world_pos)
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
      private int BlockDataOffset(ivec3 local_pos)
      {
         int ret = local_pos.z * World.GlobBlocksX * World.GlobBlocksY + local_pos.y * World.GlobBlocksX + local_pos.x;
         return ret;
      }
      public void SetBlock(Glob g, ivec3 local_pos, Block block, bool bQueueForUpdate)
      {
         //@DonoTretopologizeyet -- if you're setting a lot of blocks, avoid updating too many times
         //We may be empty, in which case we need to reallocate our data. If the block is empty, though, then setting it to empty does nothing, as we are already empty.
         if (block.IsEmpty() && g.DensityState == Glob.GlobDensityState.Empty_AndNoData)
         {
            return;
         }
         if (g.Blocks == null)
         {
            //We cull blocks from empty globs to save memory.
            g.Blocks = new Block[World.GlobBlocksX * World.GlobBlocksY * World.GlobBlocksZ];
         }

         Block old = GetBlock(g, local_pos);
         g.Blocks[BlockDataOffset(local_pos)] = block;

         if (old.IsSolidBlockNotTransparent())
         {
            if (!block.IsSolidBlockNotTransparent())
            {
               g.Empty++;
               g.Solid--;
            }
         }
         else
         {
            if (block.IsSolidBlockNotTransparent())
            {
               g.Solid++;
               g.Empty--;
            }
         }

         if (old.IsItem())
         {
            if (!block.IsItem())
            {
               g.Items--;
            }
         }
         else
         {
            if (block.IsItem())
            {
               g.Items++;
            }
         }


         //Update the state
         if (g.Solid > 0 && g.Empty > 0)
         {
            g.DensityState = Glob.GlobDensityState.Partial;
         }
         else if (g.Empty == 0)
         {
            g.DensityState = Glob.GlobDensityState.SolidBlocksOnly;
         }
         else if (g.Solid == 0)
         {
            //Later we delete block data to save space for empty globs.
            g.DensityState = Glob.GlobDensityState.Empty_AndNoData;
         }

         //We could add this with each set, but avoid it if wer'e setting lots of density values.
         if (bQueueForUpdate)
         {
            QueueForUpdate(g);
         }
      }
      public void QueueForUpdate(Glob g)
      {
         if (!_globsToUpdate.ContainsKey(g.Pos))
         {
            _globsToUpdate.Add(g.Pos, g);
         }
      }
      public Block GetBlock(Glob g, ivec3 local_pos)
      {
         //If we are empty, then we have deleted our Block[] data to save space. Return an empty block
         if (g.DensityState == Glob.GlobDensityState.Empty_AndNoData)
         {
            return new Block(BlockItemCode.Empty);
         }

         return g.Blocks[BlockDataOffset(local_pos)];
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
      private Block? GrabBlockR3(vec3 R3_pos)
      {
         Glob g = FindGlobR3(R3_pos, GlobCollection.All);
         if (g == null)
         {
            return null;
         }
         ivec3 bpos = R3toI3BlockLocal(R3_pos);
         Block b = GetBlock(g, bpos);

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
      private ivec3 R3toI3BlockLocal(vec3 R3)
      {
         vec3 bpos = new vec3(
            R3toI3BlockComp(R3.x, GlobWidthX, BlockSizeX),
            R3toI3BlockComp(R3.y, GlobWidthY, BlockSizeY),
            R3toI3BlockComp(R3.z, GlobWidthZ, BlockSizeZ));

         if (bpos.x < 0 || bpos.y < 0 || bpos.z < 0 || bpos.x >= World.GlobBlocksX || bpos.y >= World.GlobBlocksY || bpos.z >= World.GlobBlocksZ)
         {
            Gu.DebugBreak();
         }

         return new ivec3((int)bpos.x, (int)bpos.y, (int)bpos.z);
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
      private Box3f GetBlockBoxGlobalR3(vec3 pt)
      {
         //tests
         //var est_a = GetBlockBoxGlobalR3(new vec3(4, 4, 4));//should be 4,8
         //var est_b = GetBlockBoxGlobalR3(new vec3(-4, -4, -4));//should be -4, 0
         //var est_c = GetBlockBoxGlobalR3(new vec3(6.44f, 6.44f, 6.44f));//should be 4,8
         //var est_d = GetBlockBoxGlobalR3(new vec3(-6.44f, -6.44f, -6.44f));//should be -8,-4

         //Snap the point pt to the block grid, and return the bound box of that block
         Box3f box = new Box3f();
         box._min = R3ToI3BlockGlobal(pt).toVec3() * new vec3(BlockSizeX, BlockSizeY, BlockSizeZ);
         box._max.x = box._min.x + BlockSizeX;
         box._max.y = box._min.y + BlockSizeY;
         box._max.z = box._min.z + BlockSizeZ;
         return box;
      }

      #endregion

      public class PickedBlock
      {
         public Glob Glob;
         public Block Block;
         public ivec3 BlockPosLocal;
         public vec3 HitPos;
      }
      public PickedBlock RaycastBlock(PickRay3D pr, GlobCollection collection = GlobCollection.VisibleRender)
      {
         //TODO: much faster if we marched the globs first, then the blocks. We do only blocks
         PickedBlock pb = new PickedBlock();
         //Snap point to block center
         vec3 center = R3ToI3BlockGlobal(pr.Origin).toVec3() *
            new vec3(BlockSizeX, BlockSizeY, BlockSizeZ) +
            new vec3(BlockSizeX * 0.5f, BlockSizeY * 0.5f, BlockSizeZ * 0.5f);
         List<vec3> toCheck = new List<vec3>() { center };
         Glob cur_glob = null;
         ivec3 icur_glob = default(ivec3);
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

            ivec3 iglob = R3toI3Glob(cpos);
            if ((cur_glob == null && already_checked_glob == false) || (icur_glob != iglob))
            {
               cur_glob = FindGlobI3(iglob, collection);
               icur_glob = iglob;
               already_checked_glob = true;
               dbg_nglobcheck++;
            }

            if ((cur_glob != null) && (cur_glob.Blocks != null) && (cur_glob.DensityState != Glob.GlobDensityState.Empty_AndNoData))
            {
               dbg_nblockcheck++;
               ivec3 b3i = R3toI3BlockLocal(cpos);
               Block b = GetBlock(cur_glob, b3i);
               if (!b.IsEmpty())
               {
                  pb.Glob = cur_glob;
                  pb.Block = b;
                  pb.BlockPosLocal = b3i;
                  var blockbox = GetBlockBoxGlobalR3(cpos);
                  if (blockbox.LineOrRayIntersectInclusive_EasyOut(pr, ref bh))
                  {
                     pb.HitPos = pr.Origin + pr.Dir * bh._t;
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

            using (var fs = System.IO.File.OpenWrite(globfn))
            using (var br = new System.IO.BinaryWriter(fs, enc))
            {
               br.Write((Int32)GlobFileVersion);
               br.Write((Int32)g.DensityState);
               br.Write((Int32)g.Pos.x);
               br.Write((Int32)g.Pos.y);
               br.Write((Int32)g.Pos.z);
               br.Write((Int32)g.Solid);
               br.Write((Int32)g.Empty);
               br.Write((Int32)g.Items);
               if (g.Blocks == null)
               {
                  br.Write((Int32)0);
               }
               else
               {

                  var bigAssByteArray = new byte[Marshal.SizeOf(typeof(Block)) * g.Blocks.Length];
                  var pinnedHandle = GCHandle.Alloc(g.Blocks, GCHandleType.Pinned);
                  Marshal.Copy(pinnedHandle.AddrOfPinnedObject(), bigAssByteArray, 0, bigAssByteArray.Length);
                  pinnedHandle.Free();

                  br.Write((Int32)bigAssByteArray.Length);
                  br.Write(bigAssByteArray);
               }
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
      private Glob TryLoadGlob(ivec3 gpos)
      {
         //Return null if no glob file was found.
         string globfn = GetGlobFileName(gpos);
         Glob g = null;
         try
         {
            if (File.Exists(globfn))
            {
               g = new Glob(new ivec3(0, 0, 0), Gu.Context.FrameStamp);

               var enc = Encoding.GetEncoding("iso-8859-1");

               using (var fs = File.OpenRead(globfn))
               using (var br = new System.IO.BinaryReader(fs, enc))
               {
                  Int32 version = br.ReadInt32();
                  if (version != GlobFileVersion)
                  {
                     Gu.BRThrowException("Glob file verison '" + version + "' does not match required version '" + GlobFileVersion + "'.");
                  }

                  g.DensityState = (Glob.GlobDensityState)br.ReadInt32();
                  g.Pos.x = br.ReadInt32();
                  g.Pos.y = br.ReadInt32();
                  g.Pos.z = br.ReadInt32();
                  g.Solid = br.ReadInt32();
                  g.Empty = br.ReadInt32();
                  g.Items = br.ReadInt32();
                  int btecount = br.ReadInt32();
                  if (btecount == 0)
                  {
                     g.Blocks = null;
                  }
                  else
                  {
                     var readBytes = br.ReadBytes(btecount);// File.ReadAllBytes(@"c:\temp\vectors.out");
                     var numStructs = readBytes.Length / Marshal.SizeOf(typeof(Block));
                     g.Blocks = new Block[numStructs];
                     var pinnedHandle = GCHandle.Alloc(g.Blocks, GCHandleType.Pinned);
                     Marshal.Copy(readBytes, 0, pinnedHandle.AddrOfPinnedObject(), readBytes.Length);
                     pinnedHandle.Free();
                  }
               }
            }
         }
         catch (Exception ex)
         {
            Gu.Log.Error("Glob " + globfn + " had an error loading. " + ex.ToString());
            return null;
         }
         return g;
      }

      #endregion


   }
}
