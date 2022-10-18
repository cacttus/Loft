#include "v_global_defines.glsl"

//Globals to all shaders. Include this in every shader.

<GLSL_GLOBAL_STRUCTS>

<GLSL_CONTROL_INPUT_MATERIAL_TEXTURES>

//layout(std430, binding = 0) buffer ssInWeightOffsets { wd_in_st wd_in[]; };
//TODO: "if(hasShadow)" or something, omit .. 
uniform samplerCube _ufShadowBoxSamples[DEF_MAX_CUBE_SHADOW_SAMPLES];
uniform samplerCube _ufShadowFrustumSamples[DEF_MAX_FRUS_SHADOW_SAMPLES];

#if defined(DEF_SHADER_STAGE_VERTEX)
int getInstanceID() {
  return gl_InstanceID;
}
out int _uf_InstanceIDVS;
void setInstanceIDForGS() {
  _uf_InstanceIDVS = gl_InstanceID;
}
#elif defined(DEF_SHADER_STAGE_GEOMETRY)
in int _uf_InstanceIDVS[];
int getInstanceID() {
  return _uf_InstanceIDVS[0];
}
#else
//We can use instance ID in the Frag shader, however, we would need to reformat how we are doing this.
//#error No instance id was set for the given stage (not implemented)
#endif

#if defined(DEF_SHADER_STAGE_VERTEX) || defined(DEF_SHADER_STAGE_GEOMETRY)
#if defined(DEF_INSTANCED)

<GLSL_INSTANCE_DATA_STRUCT>


mat4 getModelMatrix() {
  return _ufGpuInstanceData[getInstanceID()]._model;
}
mat4 getInverseModelMatrix() {
  return _ufGpuInstanceData[getInstanceID()]._model_inverse;
}
uint getPick() {
  return _ufGpuInstanceData[getInstanceID()]._pickId.x;
}

#else

//This is here, on the assumption we'll allow non-instanced rendering (performance)..but there is no reason right now.

#error Instancing must always be on for now.

uniform mat4 _ufMatrix_Model;
uniform mat4 _ufMatrix_Model_Inverse;
uniform uint _ufPickId;

mat4 getModelMatrix() {
  return _ufMatrix_Model;
}
mat4 getInverseModelMatrix() {
  return _ufMatrix_Model_Inverse;
}
uint getPick() {
  return _ufPickId;
}

#endif

#endif



