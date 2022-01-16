
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