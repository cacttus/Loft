#include "v_global_version.glsl"


#define M_PI 3.1415926535897932384626433832795
#define length2(x) dot(x,x)

//These defines control the shaders and their parameters. They are set by the shader compiler.
//Do not remove the <> tag. This gets populated with varibles.
<GLSL_CONTROL_DEFINES_HERE>

#if defined(DEF_PIPELINE_STAGE_FORWARD) && (defined(DEF_PIPELINE_STAGE_DEFERRED) || defined(DEF_PIPELINE_STAGE_SHADOW_DEPTH) || defined(DEF_PIPELINE_STAGE_DEFERRED_BLIT) || defined(DEF_PIPELINE_STAGE_FORWARD_BLIT))
#error Shader outputs are ambiguous.
#elif defined(DEF_PIPELINE_STAGE_DEFERRED) && (defined(DEF_PIPELINE_STAGE_SHADOW_DEPTH) || defined(DEF_PIPELINE_STAGE_DEFERRED_BLIT) || defined(DEF_PIPELINE_STAGE_FORWARD_BLIT))
#error Shader outputs are ambiguous.
#elif defined(DEF_PIPELINE_STAGE_SHADOW_DEPTH) && (defined(DEF_PIPELINE_STAGE_DEFERRED_BLIT) || defined(DEF_PIPELINE_STAGE_FORWARD_BLIT))
#error Shader outputs are ambiguous.
#elif defined(DEF_PIPELINE_STAGE_DEFERRED_BLIT) && defined(DEF_PIPELINE_STAGE_FORWARD_BLIT)
#error Shader outputs are ambiguous.
#endif

#if !defined(DEF_PIPELINE_STAGE_FORWARD) && !defined(DEF_PIPELINE_STAGE_DEFERRED) && !defined(DEF_PIPELINE_STAGE_SHADOW_DEPTH) && !defined(DEF_PIPELINE_STAGE_DEFERRED_BLIT) && !defined(DEF_PIPELINE_STAGE_FORWARD_BLIT)
#error Shader had no outputs defined.
#endif

#ifdef DEF_SHADER_STAGE_VERTEX
#ifdef DEF_SHADER_STAGE_GEOMETRY
#error Shader was defined as both vertex and geometry.
#endif
#ifdef DEF_SHADER_STAGE_FRAGMENT
#error Shader was defined as both vertex and fragment.
#endif
#endif

#ifdef DEF_SHADER_STAGE_GEOMETRY
#ifdef DEF_SHADER_STAGE_FRAGMENT
#error Shader was defined as both geometry and fragment.
#endif
#endif

#ifndef DEF_SHADER_STAGE_VERTEX
#ifndef DEF_SHADER_STAGE_GEOMETRY
#ifndef DEF_SHADER_STAGE_FRAGMENT
#error Shader had no type defined.
#endif
#endif
#endif

