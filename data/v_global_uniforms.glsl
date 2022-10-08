#include "v_global_defines.glsl"

//Globals to all shaders. Include this in every shader.

//Environment / World
struct GpuWorld {
  float _fFogDamp;
  float _fFogBlend;    
  float _fFogDivisor;
  float _fFocalDepth;
//
  vec3  _vFogColor;
  float _fFocalRange;
//
  int _iPointLightCount; //technically..camera
  int _iDirLightCount;
  float _fHdrSampleExp; //Exponent to use converting input color textures to HDR
  float _fHdrToneMapExp; //Exponent to use when tone mapping back to LDR
//    
  int _iShadowBoxCount;
  float _fTimeSeconds; //Fractional seconds of time. x/1000ms => [0.0,1.0)
  int _pad0;
  int _pad1;
//
  vec3 _vAmbientColor;
  float _fAmbientIntensity;
};
layout(std140, binding = <UBO_BINDING_ID>) uniform _ufGpuWorld_Block {
  GpuWorld _ufGpuWorld;
};
struct GpuCamera { 
  mat4 _m4View;            
  mat4 _m4Projection;
  //
  vec3 _vViewPos;
  float _fWindowWidth;
  //
  vec3 _vViewDir;
  float _fWindowHeight;
//
  vec4 _vWindowViewport;//x,y,w,h
//
  float _fRenderWidth; 
  float _fRenderHeight;
  float _pad0;
  float _pad1;
};
layout(std140, binding = <UBO_BINDING_ID>) uniform _ufGpuCamera_Block {
  GpuCamera _ufGpuCamera;
};
// uniform mat4 _m4View_Debug;           
// uniform mat4 _m4Projection_Debug;
// uniform mat4 _m4Model_Debug;
//Material
struct GpuMaterial { 
  vec4 _vPBR_baseColor;
//
  float _fPBR_roughness;
  float _fPBR_metallic;
  float _fPBR_indexOfRefraction;
  float _fPBR_specular; 
//
  float _flat;
  float _pad0;
  float _pad1;
  float _pad2;
};
layout(std140, binding = <UBO_BINDING_ID>) uniform _ufGpuMaterial_Block {
  GpuMaterial _ufGpuMaterial;
};
<GLSL_CONTROL_INPUT_MATERIAL_TEXTURES>

struct GpuPointLight {
  vec3 _pos;
  float _radius;
//    
  vec3 _color;
  float _power;
};
layout(std140, binding = <UBO_BINDING_ID>) uniform _ufGpuPointLights_Block {
  GpuPointLight _ufGpuPointLights[DEF_MAX_POINT_LIGHTS];
};
struct GpuDirLight {
  vec3 _pos;
  float _radius; // we could use the same data, Dir, Pad, Color, Power.. pos/radius have no sense..unless we want it to
//    
  vec3 _color;
  float _power;
//
  vec3 _dir;
  float _pad;
 };
layout(std140, binding = <UBO_BINDING_ID>) uniform _ufGpuDirLights_Block {
  GpuPointLight _ufGpuDirLights[DEF_MAX_DIR_LIGHTS];
};


uniform samplerCube _ufShadowBoxSamples[DEF_MAX_CUBE_SHADOW_SAMPLES];
uniform samplerCube _ufShadowFrustumSamples[DEF_MAX_FRUS_SHADOW_SAMPLES];

struct GpuDebug {
  vec4 _faceTangentColor;
  vec4 _faceNormalColor;
  vec4 _faceBinormalColor;
  vec4 _vertexTangentColor;
  vec4 _vertexNormalColor;
  vec4 _vertexBinormalColor;
  //
  float _normalLength;
  float _lineWidth;
  float _pad0;
  float _pad1;
 };
layout(std140, binding = <UBO_BINDING_ID>) uniform _ufGpuDebug_Block {
  GpuDebug _ufGpuDebug;
};

struct GpuFaceData {
  vec3 _normal;
  uint _index;//face index
  vec3 _tangent;
  float pad1;
};

//layout(std430, binding = 0) buffer ssInWeightOffsets { wd_in_st wd_in[]; };

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

struct GpuInstanceData {
  mat4 _model;
  uvec2 _pickId;
  float _pad0;
  float _pad1;
  mat4 _model_inverse;
};
layout(std140, binding = <UBO_BINDING_ID>) uniform _ufGpuInstanceData_Block {
  GpuInstanceData _ufGpuInstanceData[DEF_MAX_INSTANCES];
};

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



