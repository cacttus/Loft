
Collision List
WorldObject ob
<typeid, CollisionResponseType>

CollisionResponseType 
  PickupItem






seaweed does not show we're going to have to figure out how to have water at a block at the same time as "stuff"

'
The light alg isn't working yet. X only too. But looks promising


fixed grids of blocks are taking toom uch data
  -> Move grids to globs
  -> For GRay - move the GRay grid to a Dictionrary<ivec3> or something. We don't need gray at every single .. thing.'

bug - regionstates is null but it says it's not WHYYY'

TODO: we need to cull solid globs.

radiosity lighting must come first, because this is how we light the objects and all the clels too
so we need to figurte this out
then we can finish instancing



so waht we're doing
essentially - physics - ellipsoid collision with scenery
    ellipsoid box colllide is probably working, but we need a better raycast so we made new
    but raycast does not work.
    now, there a rea lot of bugs 
    probably in boxes of hierarchy
   we got ellipsoid to collide with scenery, but it's off (program.cs - tests)
   so we are debug drawing the blocks it collides
   we made a static node to collide BVH at the sub-block level
   now there is a problem with block topology
   the topology doesn't add/delete when needed
   this means there is a problem with subdivide() and node destruction
   it DID work when we nuked the whole BVH each time, but that was a bug - we can't keep nuking BVH.'
   

divide drome x 8 
for now, just keep the block data in the whole drome. don't cull empties. 
    'if we change that we'd change the raymarch algorithm and I dont want to yet.
    and basiclly .. all block indexing
    and entire drome save routine ..
    in the future we can move block data to globs
integer bvh - neat

drome load - construct bvh. dont save it

usage of bvh is to collide with frustum to determine visible/invisible regions




mountain

shells work for dromes but not for globs. too many globs I think we have a minimum shell for globs,
//like say 3 glob raius (critical) then we generate what the player sees
///So we march a small radius then the grid of the player.
      
we need a better algorithm for culling out globs than chekcing ivec3 in _globs since 90% of globs have no topology.
1 start from cam pos and walk globs in camera
2 walk known globs neighbors if they're within camera - so all render globs are visible, we already have them, so march their nighbrons
* render globs sorted by distance anyway, so we take the furthest renderglobs and topologize their neighbors if the neighbor is within camera bounds.

globs really have no use besides being handles to mesh data. we can cull 90% of them out and save memory.
dromes will still use awareness but a fixed radius and short one. We only need 8 dromes at a time max.
get rid of most globs
only generate visible globs based on distance from user (not ivec3)

* stitch globs
fix drome / glob intersection in glob generation performance problems.
gix awareness distance

async drome
Lighting + materials

TODO: multiple index buffers (with prim type) per mesh.
/WE are currently rendering 2 meshes opaque and transparent

Async gen -- this is getting annoying
text on screen (debug)

More things -- 
deferred buffers (for picking objects mainly)
shader includes 
d_lighting isn't needed if we're using some kind of integer lighting method.
    plane cast lighting
        use integer grid, but at the points of cubes and blend the points for shadows
           kind of a shadow volume technique I guess
           bounce lighting ugh, I dont know
picking.. sort of.

dir light (sun)
poitn lights[] (visible as light boundaries)
color
position
pick


Things to do before Blobs
* Fix picking and deleting
* Allow for placign torches
* Voxel Lighting
    we have materiasl so we can' tjust blindly light them
    Object lighting need light
    voxel lighting vs ob light.
    Light object - from wall, glance, 
    glance wall.
    Light will come from wall I guess.
    Sun position
    All voxels top level get sun rays I guess


* blob (instead of globs)
16 x 16 x 8 globs = 256 x 128 block
1 gen blob heights
2 structures caves/ erosion
3 grow trees / flowers (biowalkers)

noise - biome - chebyshev voronoi - later
Just blob. - but we have a function that returns the blob per voxel

GetBiome(xyz){
    return if xyz is within blob
    //future
    //blend biomes chebyshev voronoi xyz
}
//Blocks will have their own block generators.
nw List<Block> { .. new BlockGenerator() }
//When we call
class Blob {

    void Update() {
        for all blocks xyz {
            if(block.Type.Generator!=null) {
                block.Type.Generator.Step();
            }
        }
    }
}
//Could be custom class..idk.
class BlockGenerators {
static BlockGenerator Daffodil = new BlockGenerator {..}
static BlockGenerator SmallTree = new BlockGenerator { .. Cedar ..  }
}
//Block Generators need to have initial conditions, like wlakers.
class Biome { 
    List<BlockGenerator> Flora { 
        daffodil, rose, tree
    }
    //?? Fauna {
    //    sheep, cow .. Hmm..
    //}
    public Block GrassBase { grass } // Base block used for top level grass
    List<BlockGenerator> Erosion { //Cave/gorge/plateau//etc 
        new BlockGenerator() { min_radius, max_radius, probability, initial_spawn(0,1), }
    }
}
public class Minimax<T> {
    T Min;
    T Max;
    Minimax<T>(T min, T max) { Min=min, Max=max; }
}
BlockGenerator { 
    Bool Create_or_Destroy (create or destroy blocks)
    
    SimulationStep - simulate this walker every x steps.

    Block Block_To_Create 

    Minimax<vec3> direction // trees - y=1, then xyz=random for leaves

    Minimax<float> SpawnCount = new Minimax(1,1); //Initial number of walkers that exist at this location
    Minimax<float> Radius_blocks = new Minimax(1,4)
    Minimax<float> Radius_decline_per_block //Taper the ends of the cave / feature - in blocks per step

    List<BlockGenerator> Spawn { TreeBranch1 } //walkers to spawn when we die (or not)

    Minimax<int> Death_Radius = 4 Blocks_per_object //Biowalker parameter
    Minimax<float> Spawn_distance //min/max distance to spawn new walkers
}
Blob GenBlob() {
    Blob b = new blob (globs width x * blob_globs x .. )
    for(xyz in blob-size){
        BaseWorld(xyz)
    }
    //Erosion / Caves = CutWalkers
    Walkers
    //flora = Biowalkers
    //Fauna = ?
    for(xyz in blob-size){
        Grow(xyz) /// advance generators
    }


    return b;
}

simple edit blocks, create, destroy
    raycast world -> block node ->block
    render crosshair
    render player hands.
physics - collide w/ground
    player height
destroy /unload globs
world material + shader
    test using geometry shader + dens to create cubes instad of atulal meshs
actual sky 
    sky material for sphere object. 
    clouds
grass/flower/trees
async generation
multiple materials per mesh for multiple block / material
index buffers per material

done
x initial generation of world (like minecraft)
x game/world
x save/load glob
    
* Roadmap
    * Goal is minecraft looking thing - must have
        * voxel world with textures
        * grass, trees, top bot textures
        * islands
        * water
        * be able to move around, jump (physics)
* Steps
    * Debug Mesh Inline (to show globs)
        default debug material
        flat shaded p3 c4
        WorldObject    
    * World generation
        * x Glob grid. 
        * x Voxel generator
        * x Voxel struct { Uint16 value; } - packed
        * x Draw voxels as individual meshes
            * Optimization, some kind of mesh pool.
    * Textures
        * x Load small textures into a megatexture Pack them with the dist.
        * x Use them as textures, top, bot, side.
            * x Voxel shader (top / bot / side) with mega tex.
    * Sky
        * x Render just as background thingymajig
        * x Sky dome no background
        * Environment Map
        * x Equirectangular HDRI
    * Water
        * 2nd mesh.
        * Topology algorithm.

    Improvements
    * Real materials "closure" with PBR materials 
        * Marble. Metal.
    

Next task is to make meshdata accept a byte buffer and make it generic and accept a vertexformat instead.
Then utilmesh so we can creat3e verts

The current issue is the position of camera at -10, 0 -10 is incorrect on the x axis. Need tofigure out why
TODO:
empty object should be axis mesh.





 A Blender Closure is a surface or a volume data from the oject

struct Closure {
#ifdef VOLUMETRICS
  Vec3f absorption;
  Vec3f scatter;
  Vec3f emission;
  float anisotropy;

#else /* SURFACE */
  Vec3f radiance;
  Vec3f transmittance;
  float holdout;
  Vec4f ssr_data;
  Vec2f ssr_normal;
  int flag;
#  ifdef USE_SSS
  Vec3f sss_irradiance;
  Vec3f sss_albedo;
  float sss_radius;
#  endif

#endif
};

Moved system static root that is render context dependent to the game window, and global context dependencies to Gu.

sss subsurface scattering
ssr screen space relfectiojns
 
 Musgrave fBm = fractional brownian motion.
 
Next question is how they actually compile all these shaders together. They actually seem to be using texture gen on the GPU. Weird.
 
 gpu_node_graph.h / .cc - this is the main file for nodes / links / structures
 internal gpu_material.c / h - This is the main materials compile file.
 
    Compile ->  GPU_material_from_nodetree
 
 1/13/22 adding Vec3f and all that
 
 
 Render -> Camera VisibleSet
 
 VisibleInstanceSet
    [Shader, Texture, Distance, Geom]
    
 InstanceSet
    [matrices]
    [geoms]
    
 Geom
    Shader (reference)
 
 
 Materials 
 Blender you must use the nodes now. Images are just inputs into the Principled BSDF
 If there is no input into the Principled BSDF then a color is used
 So for pixel shader we have a limited set of inputs, the principled, then colors, or, images in them.
 Principled BSDF defines all these no need to make our own.


GPU_stack_link takes a shader function as a parameter

gpu_material_library_use_function - searches for a GPU function (glsl)

Output is a single shader pass.
gpu_material_library ok so they literally parse the glsl code. Nice.
BLI - blender library external interface BLI_blenlib.h

eevee_materials.c bind_resources
OSL Open shading language - 

Light probe - a way to sample the environment for indirect lighting in a raster engine
    Reflection cube, reflection plane, irradiance volume
    
/* Shader Virtual Machine
 *
 * A shader is a list of nodes to be executed. These are simply read one after
 * the other and executed, using an node counter. Each node and its associated
 * data is encoded as one or more uint4's in a 1D texture. If the data is larger
 * than an uint4, the node can increase the node counter to compensate for this.
 * Floats are encoded as int and then converted to float again.
 *
 * Nodes write their output into a stack. All stack data in the stack is
 * floats, since it's all factors, colors and vectors. The stack will be stored
 * in local memory on the GPU, as it would take too many register and indexes in
 * ways not known at compile time. This seems the only solution even though it
 * may be slow, with two positive factors. If the same shader is being executed,
 * memory access will be coalesced and cached.
 *
 * The result of shader execution will be a single closure. This means the
 * closure type, associated label, data and weight. Sampling from multiple
 * closures is supported through the mix closure node, the logic for that is
 * mostly taken care of in the SVM compiler.   
 
 
 
 svm_eval_nodes - main entry point to eval all nodes
 
 kernel - gpu kernel or opencl cernek
 black body - absorbs all radiation (i mean, i=t reflects nothing it is black)
 albedo (latin whiteness) - radiosity / irradiance from 0 to 1. 0 is black body 1 is reflective
 
 oren nayar - diffuse lighting model that factors "roughness" in and is hence PBR. It takes microfacet into account. Without microfacet component it is simple lambertian diffuse. 
    sigma - rougness
    rho - albedo (typical lambertian diffuse albedo)
    

ShaderBuilder - One Shader to Rule Them All
 
Input vertex normal color etc
 
output - the shaded pixel
 
More research

surface_frag.glsl
  
a few output fragment shaders
surface / volume / 
 alpha; radiance. transmittance.
 
   ssrNOrmals, ssrData

 Closure cl = nodetree_exec(); This seems to be the big one
 transmittance = alpha
 
 transmittance rendered to a buffer
 radiance rendered to a buffer.
  
 
 Porter Duff blending is a kind of clip blending used by setting index int he layout specifier
 
 we don't need most of this crap for a simple minecraft clone. Just need a generic structure to support multiple vertex formats
 and render diffuse BSDF, etc to the screen.
 
 blinn ggx cook-torrence phong
 GGX is a new model that more closely resembles metal, other than the standard Phong or Blinn-Phong model.
 Difference in these is the way the reflected ray is calculated.
 halfway vector = (eye normal + light normal) / 2 
 smith shadowing model (used with GGX)
 anisotropic distribution when diffuse shading in x!=y
 Normal distribution function - NDF - measures dnesity of a normal orientation on surface
 
 we would have 2 render stages
 first is the material/geom stage for all geoms 
 second is the compositor / post procesor, probably deferred.
 
 
 