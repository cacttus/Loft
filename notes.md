initial generation of world (like minecraft)
game/world
save/load glob
destroy globs.
world material + shader
    test using geometry shader + dens to create cubes instad of atulal meshs
sky
grass/flowr
tres
async generation
    
    
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
            * Voxel shader (top / bot / side) with mega tex.
    * Sky
        * Render just as background thingymajig
        * Sky dome no background
        * Environment Map
        * Equirectangular HDRI
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
 
 
 