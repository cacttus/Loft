
 A Blender Closure is a surface or a volume data from the oject

struct Closure {
#ifdef VOLUMETRICS
  vec3 absorption;
  vec3 scatter;
  vec3 emission;
  float anisotropy;

#else /* SURFACE */
  vec3 radiance;
  vec3 transmittance;
  float holdout;
  vec4 ssr_data;
  vec2 ssr_normal;
  int flag;
#  ifdef USE_SSS
  vec3 sss_irradiance;
  vec3 sss_albedo;
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
 
 1/13/22 adding vec3 and all that
 
 
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
 
GlobCompressor 
{
    //Waaaay later. This should be easy enough thugh.
    public UInt64[] CompressGlob(Uint64[] data
    {
        //Huffman
        //build tree
        //walk.
        //done.
    }
}

using Block = System.UInt32;
 
 class BlockDensity {
    public byte Positive = 0x00;
    public byte Negative = 0x01;
 }
 class BlockItemCode {
    public byte Invalid = 0x00;
    public byte Grass = 0x01;
    public byte Dirt = 0x02;
 }
 Glob
 {
    public Mesh _transparent = null;
    public Mesh _opaque = null;
    public Block[] _blocks = null;
    public ivec3 _size = new ivec3(0,0,0);
    
    Glob() {
    }
        
    void Update()
    {
    
    }
 }
   
 GlobWorld
 {
    public const float RenderRadius = 500.0f;
    public const float BlockSizeX = 1.0f;
    public const float BlockSizeY = 1.0f;
    public const float BlockSizeZ = 1.0f;
    public const int GlobBlocksX = 16;
    public const int GlobBlocksY = 16;
    public const int GlobBlocksZ = 16;
      
    Dictionary<ivec3, Glob> _globs = new D<ivec3,Glob>();
    
    public vec3 GetGlobOriginFromGlobIndex(ivec3 idx)
    {
        //Global space 
    }
    public uint GetGlobRelativeBlockIndex(ivec3 relative_block_idx)
    {
        //Glob space
    }
    public Block PackBlock(BlockItemCode code, BlockDensity density){
        // <-----Unused-------> ItemCode   Density
        // [........][........][........][........]
        //                32 bits 
        Block ret=0;
        ret = code << 8 | density;
        return ret;
    }
    Glob GenerateGlob(ivec3 xyz) 
    {
        //Freedomcraft
    
        vec3 globOrigin = GetGlobOriginFromGlobIndex(xyz);
        
        vec3 halfBlock = new vec3(BlockSizeX*0.5f,BlockSizeY*0.5f,BlockSizeZ*0.5f);
        
        Glob g = new Glob();
        g._blocks = new Block[GlobWorld.GlobBlocksZ * GlobWorld.GlobBlocksZ * size.z + GlobWorld.GlobBlocksY * size.y + size.x];
            
        for(int z = 0; z< GlobBlocksZ; z++)
        {
            for(int y = 0; y< GlobBlocksY; y++) 
            {
                for(int x = 0; x< GlobBlocksX; x++) 
                {
                    uint bidx = GetGlobRelativeBlockIndex(x,y,z);
                
                    //noise in the block center.
                    vec3 noise_pt = globOrigin +  new vec3(x * BlockSizeX, y*BlockSizeY, z*BlockSizeZ) + halfBlock;

                    if(Noise.Noise(noise_pt) > 0)
                    {
                        g._blocks[bidx] = PackBlock(BlockItemCode.Grass, BlockDensity.Positive);
                    }
                    else
                    {
                        g._blocks[bidx] = 0;
                    }
                 }
             }
         }
    }
    Update() 
    {
        Box3i = (player pos - vec3(render radius) ,player pos + vec3(render radius))
    
        //Make globs
        for(int z= ibox.min.z; z< ibox.max.z; z++)
        {
            for(int y= ibox.min.y; y< ibox.max.y; y++)
            {
                for(int x= ibox.min.x; x< ibox.max.x; x++)
                {
                    GenerateGlob(x,y,z)
                }
            }
        }
        
        //Destroy globs
        for all globs in _globs
        {
           if( min point of glob BB is > this.RenderRadius)
           {
              // Destroy abandoned blocks after a certain time.
              if(g.Loaded)
              {
                 g.AbandonTime = Current_Time
                 g.Loaded = false;
              }
           }
        }
        
        for all globs
        {
        if g.Loaded == false
            if g.AbandonTime > 3 seconds or so.
                remove g from globs.
                unload mesh data
                
        }
    }
 }
 
 //Let awareness be the render distance. Camera Z



    collect visible nodes
            
 ob - world - one object
    ob - meshdatas  < loaded cells.

jnot sure

so we need to get a way to make more general meshes first - 
debug / util mesh is super important
debug 
v c 
obj
v t n 
and many maps.
cell
v t n cell id, etc .
ShaderBuilder - kind of like the node thing
so if you add a texutre map, you get a texture attribute

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
    
    
    
 
 
 
 
 
 