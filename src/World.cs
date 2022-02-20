using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;

namespace PirateCraft
{
   public class BlockItemCode
   {
      //Blocks
      public const ushort Empty = 0x00;
      public const ushort Grass = 0x01;
      public const ushort Dirt = 0x02;
      public const ushort Brick = 0x03;
      public const ushort Brick2 = 0x04;
      public const ushort Gravel = 0x05;
      public const ushort Sand = 0x06;
      //Items
      //...
   }
   //The effigious block
   [StructLayout(LayoutKind.Sequential)]
   public struct Block
   {
      public Block(UInt16 val) { Value = val; }
      public UInt16 Value;
      public bool HasDensity()
      {
         return Value != 0;
      }
      public bool IsItem()
      {
         //Items are special case cullables since they may not, or may cull entire faces.
         return false;
      }
      public bool IsSolidBlockNotTransparent()
      {
         //This should return whether the block is a solid (non-item) non-transparent block. 
         // Used for face culling.
         //Technically it should index into a LUT to see whether this block-item is solid or not.
         //For now - we are just rendering blocks so we can return HasDensity for this.
         return HasDensity();
      }
   }
   //Unit box for creating mesh cubes
   class UnitBoxMeshData
   {
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
   //   GlobCompressor 
   //{
   //    //Waaaay later. This should be easy enough thugh.
   //    public UInt64[] CompressGlob(Uint64[] data
   //    {
   //      //Huffman
   //      //build tree
   //      //walk.
   //      //done.
   //   }
   //}
   public enum GlobDensityState
   {
      Partial, SolidBlocksOnly, SolidItems, Empty_AndNoData//Partial = renderable, Solid = fully solid, Empty = empty
   }
   public class Glob
   {
      public UInt64 GeneratedFrameStamp { get; private set; } = 0;
      public MeshData Transparent = null;
      public MeshData Opaque = null;
      public Block[] Blocks = null;
      public ivec3 Pos { get; private set; }
      public GlobDensityState GlobDensityState = GlobDensityState.Partial;
      private void AllocateBlocksIfEmpty()
      {
         if (Blocks == null)
         {
            Blocks = new Block[World.GlobBlocksX * World.GlobBlocksY * World.GlobBlocksZ];
         }
      }
      public vec3 OriginR3
      {
         get
         {
            vec3 r = new vec3(Pos.x * World.GlobWidthX, Pos.y * World.GlobWidthY, Pos.z * World.GlobWidthZ);
            return r;
         }
      }
      public Glob(ivec3 pos, UInt64 genframeStamp)
      {
         Pos = pos;
         GeneratedFrameStamp = genframeStamp;
      }

      public void SetBlock(int x, int y, int z, Block b)
      {
         //We may be empty, in which case we need to reallocate our data. If the block is empty, though, then setting it to empty does nothing, as we are already empty.
         if (!b.HasDensity() && GlobDensityState == GlobDensityState.Empty_AndNoData)
         {
            return;
         }
         AllocateBlocksIfEmpty();
         Blocks[z * World.GlobBlocksX * World.GlobBlocksY + y * World.GlobBlocksX + x] = b;
      }
      public Block GetBlock(int x, int y, int z)
      {
         //If we are empty, then we have deleted our Block[] data to save space. Return an empty block
         if (this.GlobDensityState == GlobDensityState.Empty_AndNoData)
         {
            return new Block(BlockItemCode.Empty);
         }

         return Blocks[z * World.GlobBlocksX * World.GlobBlocksY + y * World.GlobBlocksX + x];
      }

      public void Update()
      {
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
      public readonly float GlobDiameter = (float)Math.Sqrt(GlobWidthX * GlobWidthX + GlobWidthY * GlobWidthY + GlobWidthZ * GlobWidthZ);
      public float RenderRadiusShell { get { return GlobDiameter; } }
      public float MaxRenderDistance { get { return GlobDiameter * 5; } } //Render all the nodes we can see.

      private bool _firstGeneration = true; //Whether this is the initial generation, where we would need to generate everything around the player.
      private int _currentShell = 1;
      private ivec3 playerLastGlob = new ivec3(0, 0, 0);

      public int NumGlobs { get { return _globs.Count; } }
      public int NumRenderGlobs { get { return _renderGlobs.Count; } }
      public int NumVisibleRenderGlobs { get { return _visibleRenderGlobs.Count; } }

      Dictionary<ivec3, Glob> _globs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //All globs
      Dictionary<ivec3, Glob> _renderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.
      Dictionary<ivec3, Glob> _visibleRenderGlobs = new Dictionary<ivec3, Glob>(new ivec3.ivec3EqualityComparer()); //Just globs that get drawn. This has a dual function so we know also hwo much topology we're drawing.

      //TODO:players
      public WorldObject Player = null;

      //Thread GlobGenerator;
      //object GlobMutex;

      Material _worldMaterial = null;
      //Texture2D _worldTexture = null;
      //Texture2D _worldBump = null;
      MegaTex _worldMegatex = new MegaTex("tex", true);

      public World()
      {
      }

      #region Objects

      Dictionary<ushort, List<FileLoc>> _blockTiles;
      Dictionary<ushort, List<MtTex>> _tileUVs;
      Dictionary<TileImage, FileLoc> _tile_resources = new Dictionary<TileImage, FileLoc>() {
            { TileImage.Grass, new FileLoc("tx64_grass.png", FileStorage.Embedded) },
            { TileImage.GrassSide, new FileLoc("tx64_grass_side.png", FileStorage.Embedded) },
            { TileImage.Dirt, new FileLoc("tx64_dirt.png", FileStorage.Embedded) },
            { TileImage.Plank, new FileLoc("tx64_plank.png", FileStorage.Embedded) },
            { TileImage.Brick, new FileLoc("tx64_brick.png", FileStorage.Embedded) },
            { TileImage.Brick2, new FileLoc("tx64_brick2.png", FileStorage.Embedded) },
            { TileImage.Gravel, new FileLoc("tx64_gravel.png", FileStorage.Embedded) },
            { TileImage.Sand, new FileLoc("tx64_sand.png", FileStorage.Embedded) },
         };
      private enum TileImage
      {
         Grass, GrassSide, Dirt, Plank, Brick, Brick2, Gravel, Sand
      }
      private class BlockUVSide
      {
         public const int Top = 0;
         public const int Side = 1;
         public const int Bottom = 2;
      }
      private FileLoc GetTileFile(TileImage img)
      {
         _tile_resources.TryGetValue(img, out var loc);
         Gu.Assert(loc != null);
         return loc;
      }
      public void Initialize(WorldObject player)
      {
         Player = player;
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
         foreach (var resource in _tile_resources)
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

         //Generate the mesh data we use to create cubess
         UnitBoxMeshData.Generate();

         //Asynchronous generator .. (TODO)
         // Task.Factory.StartNew(() => {
         //});

         Gu.Log.Info("Building initail grid");
         BuildGlobGridAndTopologize(player.World.extractTranslation(), RenderRadiusShell * 5, true);
      }

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
         c.Update(0);
         Objects.Add(name, c);
         return c;
      }
      private void AddObject(string name, WorldObject ob)
      {
         //Use a suffix if there is a duplicate object
         int suffix = 0;
         string name_suffix = name;
         while (FindObject(name_suffix) != null)
         {
            suffix++;
            name_suffix = name + "-" + suffix.ToString();
         }
         ob.Name = name_suffix;
         Objects.Add(name_suffix, ob);
      }
      public WorldObject CreateObject(string name, MeshData mesh, Material material, vec3 pos = default(vec3))
      {
         WorldObject ob = new WorldObject(pos);
         ob.Name = name;
         ob.Mesh = mesh;
         ob.Material = material;
         AddObject(name, ob);
         return ob;
      }
      public void DestroyObject(string name)
      {
         if (Objects.TryGetValue(name, out WorldObject wo))
         {
            Objects.Remove(name);
         }
         else
         {
            Gu.Log.Error("Object '" + name + "' was not found.");
         }
      }
      public void Update(double dt, Camera3D cam)
      {
         BuildWorld();
         UpdateObjects(dt);

         //Honestly, this isn't too slow. We usually have maybe 500 globs visible at a time.
         _visibleRenderGlobs.Clear();
         foreach(var g in _renderGlobs)
         {
            if (cam.Frustum.HasBox(GetNodeBoxForGridPos(g.Key)))
            {
               _visibleRenderGlobs.Add(g.Key,g.Value);
            }
         }

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
      }
      private void UpdateObjects(double dt)
      {
         foreach (var ob in Objects.Values)
         {
            ob.Update(dt);
         }
      }
      public void Render(double Delta, Camera3D camera)
      {
         //Render to this camera.
         camera.BeginRender();
         {
            //Objects
            //TODO: PVS of course we're going to use a bucket collection algorithm. This is in the future.
            foreach (var ob in Objects.Values)
            {
               DrawOb(ob, Delta, camera);
            }

            //PVS for globs
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

            WorldObject dummy = new WorldObject();

            _worldMaterial.BeginRender(Delta, camera, dummy);
            foreach (var md in visible_op)
            {
               md.Draw();
            }
            _worldMaterial.EndRender();
         }
         camera.EndRender();
      }
      private void DrawOb(WorldObject ob, double Delta, Camera3D camera)
      {
         if (ob.Mesh != null)
         {
            //Material mat = ob.Material;
            //if (ob.Material == null)
            //{
            //   mat = Material.DefaultFlatColor();
            //}
            ob.Material.BeginRender(Delta, camera, ob);
            ob.Mesh.Draw();
            //Renderer.Render(camera, ob, mat);
            ob.Material.EndRender();
         }
         foreach (var c in ob.Children)
         {
            DrawOb(c, Delta, camera);
         }
      }

      #endregion

      #region Globs

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

         if (Gu.CurrentWindowContext.FrameStamp % 3 == 0)
         {
            //Quick-n-dirty "don't kill me"
            ivec3 newPlayerGlob = R3toI3Glob(Player.Position);

            float awareness_radius = RenderRadiusShell * _currentShell;

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
      private List<Glob> BuildGlobGrid(vec3 origin, float awareness_radius, bool logprogress=false)
      {
         //Build a grid of globs in the volume specified by origin/radius
         List<Glob> newGlobs = new List<Glob>();

         
         //TODO: we use a cube here, we should check against an actual sphere below. It looks nicer.
         vec3 awareness = new vec3(awareness_radius, awareness_radius, awareness_radius);
         Box3f bf = new Box3f(origin - awareness, origin + awareness);

         Box3i ibox;
         ibox._min = new ivec3((int)(bf._min.x / GlobWidthX), (int)(bf._min.y / GlobWidthY), (int)(bf._min.z / GlobWidthZ));
         ibox._max = new ivec3((int)(bf._max.x / GlobWidthX), (int)(bf._max.y / GlobWidthY), (int)(bf._max.z / GlobWidthZ));

         //Limit Y axis ..  DEBUG ONLY
         Gu.Log.WarnCycle("Limiting debug Y axis for testing");
         int ylimit = 3;
         if(ibox._min.y > ylimit) {  ibox._min.y = ylimit; }
         if(ibox._min.y < -ylimit) {  ibox._min.y = -ylimit; }
         if (ibox._max.y > ylimit) { ibox._max.y = ylimit; }
         if (ibox._max.y < -ylimit) { ibox._max.y = -ylimit; }
         if(ibox._min.y > ibox._max.y) { ibox._min.y = ibox._max.y; }

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
                     Glob g = GenerateGlob(gpos);
                     _globs.Add(gpos, g);
                     newGlobs.Add(g);
                  }

               }
            }
         }
         return newGlobs;
      }
      private List<Glob> BuildGlobGridAndTopologize(vec3 origin, float awareness_radius, bool logprogress = false)
      {
         List<Glob> newGlobs = BuildGlobGrid(origin, awareness_radius, logprogress);

         if (logprogress)
         {
            Gu.Log.Info("Topologizing " + newGlobs.Count);
         }
         //Prevent unnecessary stitching.
         foreach (Glob g in newGlobs)
         {
            if (g.GlobDensityState != GlobDensityState.Empty_AndNoData)
            {
               //The block is empty, the inside of the block has no topology.
               TopologizeGlob(g);
            }
            if (g.GlobDensityState != GlobDensityState.SolidBlocksOnly)
            {
               //No neighboring blocks would be visible, so stitchin gisn't needed
               StitchGlob(g);
            }
         }
         return newGlobs;
      }
      private Glob GenerateGlob(ivec3 gpos)
      {
         //Density and all that.
         Glob g = new Glob(gpos, Gu.CurrentWindowContext.FrameStamp);
         vec3 globOriginR3 = new vec3(GlobWidthX * gpos.x, GlobWidthY * gpos.y, GlobWidthZ * gpos.z);

         bool hasSolid = false;
         bool hasEmpty = false;
         bool hasItems = false;//Item blocks would cull based on which sides are culled. This is a special case.

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
                  var block = new Block(Density(block_world));
                  if (block.IsSolidBlockNotTransparent())
                  {
                     hasSolid = true;
                  }
                  else if (block.IsItem())
                  {
                     hasItems = true;
                  }
                  else
                  {
                     hasEmpty = true;
                  }
                  g.SetBlock(x, y, z, block);
               }
            }
         }

         if (hasSolid && hasEmpty)
         {
            g.GlobDensityState = GlobDensityState.Partial;
         }
         else if (hasSolid)
         {
            g.GlobDensityState = GlobDensityState.SolidBlocksOnly;
         }
         else if (hasEmpty)
         {
            //Delete block data to save space for empty globs.
            g.GlobDensityState = GlobDensityState.Empty_AndNoData;
            g.Blocks = null;
         }

         return g;
      }
      private Glob GetGlobAtPos(ivec3 pos)
      {
         _globs.TryGetValue(pos, out var g);
         return g;
      }
      private Glob GetNeighborGlob(Glob g, int i)
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
         Glob ret = FindGlobR3(neighbor_center_R3);
         return ret;
      }
      private void StitchGlob(Glob g)
      {
         for (int ni = 0; ni < 6; ++ni)
         {
            Glob gn = GetNeighborGlob(g, ni);
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
                  b = g.GetBlock(x, y, z);
                  if (!b.HasDensity())
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
                           b_n = g.GetBlock(bpos.x, bpos.y, bpos.z);
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
                              texs[0] = new vec2(patches[BlockUVSide.Side].uv0.x, patches[BlockUVSide.Side].uv0.y);
                              texs[1] = new vec2(patches[BlockUVSide.Side].uv1.x, patches[BlockUVSide.Side].uv0.y);
                              texs[2] = new vec2(patches[BlockUVSide.Side].uv0.x, patches[BlockUVSide.Side].uv1.y);
                              texs[3] = new vec2(patches[BlockUVSide.Side].uv1.x, patches[BlockUVSide.Side].uv1.y);
                           }
                           else if (face == 2)
                           {
                              //B
                              texs[0] = new vec2(patches[BlockUVSide.Bottom].uv0.x, patches[BlockUVSide.Bottom].uv0.y);
                              texs[1] = new vec2(patches[BlockUVSide.Bottom].uv1.x, patches[BlockUVSide.Bottom].uv0.y);
                              texs[2] = new vec2(patches[BlockUVSide.Bottom].uv0.x, patches[BlockUVSide.Bottom].uv1.y);
                              texs[3] = new vec2(patches[BlockUVSide.Bottom].uv1.x, patches[BlockUVSide.Bottom].uv1.y);
                           }
                           else if (face == 3)
                           {
                              //T
                              texs[0] = new vec2(patches[BlockUVSide.Top].uv0.x, patches[BlockUVSide.Top].uv0.y);
                              texs[1] = new vec2(patches[BlockUVSide.Top].uv1.x, patches[BlockUVSide.Top].uv0.y);
                              texs[2] = new vec2(patches[BlockUVSide.Top].uv0.x, patches[BlockUVSide.Top].uv1.y);
                              texs[3] = new vec2(patches[BlockUVSide.Top].uv1.x, patches[BlockUVSide.Top].uv1.y);
                           }

                        }
                        else
                        {
                           //The Top/Side/Bot tile images could not be found (were not created) - default to the whole megatexture [0,1]
                           texs[0] = UnitBoxMeshData.bx_verts_face[face, 0]._x;
                           texs[1] = UnitBoxMeshData.bx_verts_face[face, 1]._x;
                           texs[2] = UnitBoxMeshData.bx_verts_face[face, 2]._x;
                           texs[3] = UnitBoxMeshData.bx_verts_face[face, 3]._x;
                        }

                        //Verts + Indexes
                        vec3 block_pos_abs_R3 = block_pos_rel_R3 + g.OriginR3;
                        for (int vi = 0; vi < 4; ++vi)
                        {
                           verts.Add(new v_v3n3x2()
                           {
                              _v = UnitBoxMeshData.bx_verts_face[face, vi]._v + block_pos_abs_R3,
                              _n = UnitBoxMeshData.bx_verts_face[face, vi]._n,
                              _x = texs[vi], 
                           });
                        }

                        for (int ii = 0; ii < 6; ++ii)
                        {
                           inds.Add(foff + UnitBoxMeshData.bx_face_inds[ii]);
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
      }
      private Block? GrabBlockR3(vec3 R3_pos)
      {
         Glob g = FindGlobR3(R3_pos);
         if (g == null)
         {
            return null;
         }
         ivec3 bpos = R3toI3BlockLocal(R3_pos);
         Block b = g.GetBlock(bpos.x, bpos.y, bpos.z);

         return b;
      }
      private float R3toI3BlockComp(float R3, float BlocksAxis, float BlockWidth)
      {
         float bpos;
         if (R3 < 0)
         {
            bpos = (float)Math.Floor((R3 % BlocksAxis + BlocksAxis)/BlockWidth);
         }
         else
         {
            bpos = (float) Math.Floor((R3 % BlocksAxis)/BlockWidth);
         }
         return bpos;
      }
      private ivec3 R3toI3BlockLocal(vec3 R3)
      {
         vec3 bpos = new vec3(
            R3toI3BlockComp(R3.x, GlobWidthX, BlockSizeX),
            R3toI3BlockComp(R3.y, GlobWidthY, BlockSizeY),
            R3toI3BlockComp(R3.z, GlobWidthZ, BlockSizeZ) );

         if (bpos.x < 0 || bpos.y < 0 || bpos.z < 0 ||bpos.x >= World.GlobBlocksX || bpos.y >= World.GlobBlocksY || bpos.z >= World.GlobBlocksZ)
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
      private Glob FindGlobR3(vec3 R3_pos)
      {
         ivec3 gpos = R3toI3Glob(R3_pos);

         return GetGlobAtPos(gpos);
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

            d = d + (sign) * MathUtils.cosf(world_pos.x/World.BlockSizeX * 0.1f * f) * 3 * a + (sign) * MathUtils.sinf(world_pos.z / World.BlockSizeZ * 0.1f * f) * 3 * a * BlockSizeY;
         }

         ushort item = BlockItemCode.Empty;
         if (d > 0)
         {
            //Testing..
            //We have stuff. Default to grassy grass.
            if (world_pos.y < BlockSizeY * -4)
            {
               item = BlockItemCode.Gravel;

            }
            else if (world_pos.y < 0)
            {
               item = BlockItemCode.Sand;
            }
            else
            {
               item = BlockItemCode.Grass;
            }
            //  Random.Next() > 0.3f ?
            //BlockItemCode.Grass :
            //(Random.Next() > 0.6f ? BlockItemCode.Brick2 : BlockItemCode.Brick);
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
               Box3f box = GetNodeBoxForGridPos(vi);

               vec3 node_center = box.center();

               float fDist2 = (pf.NearCenter - node_center).length2();

               if (fDist2 < fMaxDist2)
               {
                  if (pf.HasBox(box))
                  {
                     func(vi, box);

                     //Sweep Neighbors
                     vec3[] n = new vec3[6];
                     n[0] = node_center + new vec3(-World.GlobWidthX, 0, 0);
                     n[1] = node_center + new vec3(World.GlobWidthX, 0, 0);
                     n[2] = node_center + new vec3(0, -World.GlobWidthY, 0);
                     n[3] = node_center + new vec3(0, World.GlobWidthY, 0);
                     n[4] = node_center + new vec3(0, 0, -World.GlobWidthZ);
                     n[5] = node_center + new vec3(0, 0, World.GlobWidthZ);
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
      private Box3f GetNodeBoxForGridPos(ivec3 pt)
      {
         Box3f box = new Box3f();
         box._min.x = (float)(pt.x + 0) * GlobWidthX;
         box._min.y = (float)(pt.y + 0) * GlobWidthY;
         box._min.z = (float)(pt.z + 0) * GlobWidthZ;
         box._max.x = (float)(pt.x + 1) * GlobWidthX;
         box._max.y = (float)(pt.y + 1) * GlobWidthY;
         box._max.z = (float)(pt.z + 1) * GlobWidthZ;
         return box;
      }

      #endregion

   }
}
