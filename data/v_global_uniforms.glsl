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
layout(std140, binding = 0) uniform _ufGpuWorld_Block {
  GpuWorld _ufGpuWorld;
};
uniform sampler2D _ufGpuWorld_s2EnvironmentMap;//Equirectangular ENv map
uniform sampler2D _ufGpuWorld_s2IrradiancaMap; //Equirectangular IRr map.
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
};
layout(std140, binding = 1) uniform _ufGpuCamera_Block {
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
layout(std140, binding = 2) uniform _ufGpuMaterial_Block {
  GpuMaterial _ufGpuMaterial;
};
//TODO: sampler2DArray, and use indexes, we can put them in the shader in cs if we wish
uniform sampler2D _ufGpuMaterial_s2Albedo;     // deferred = Color
uniform sampler2D _ufGpuMaterial_s2Normal;     // deferred = normal
uniform sampler2D _ufGpuMaterial_s2Roughness;  // deferred = position
uniform sampler2D _ufGpuMaterial_s2Metalness;  // deferred = plane
uniform sampler2D _ufGpuMaterial_s2Position;   // 

vec4 getMRT_Color(vec2 tcoord)    { return texture(_ufGpuMaterial_s2Albedo, vec2(tcoord)); }   
vec4 getMRT_Normal(vec2 tcoord)   { return texture(_ufGpuMaterial_s2Normal, vec2(tcoord)); }   
vec4 getMRT_Position(vec2 tcoord) { return texture(_ufGpuMaterial_s2Position, vec2(tcoord)); } 
vec4 getMRT_Material(vec2 tcoord) { return texture(_ufGpuMaterial_s2Metalness, vec2(tcoord)); }

struct GpuPointLight {
  vec3 _pos;
  float _radius;
//    
  vec3 _color;
  float _power;
};
layout(std140,  binding = 3) uniform _ufGpuPointLights_Block {
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
layout(std140, binding = 4) uniform _ufGpuDirLights_Block {
  GpuPointLight _ufGpuDirLights[DEF_MAX_DIR_LIGHTS];
};

uniform samplerCube _ufShadowBoxSamples[DEF_MAX_CUBE_SHADOW_SAMPLES];
uniform samplerCube _ufShadowFrustumSamples[DEF_MAX_FRUS_SHADOW_SAMPLES];

struct GpuInstanceData {
  mat4 _model;
  uvec2 _pickId;
  float _pad0;
  float _pad1;
};

#if defined(DEF_SHADER_STAGE_VERTEX)
#if defined(DEF_INSTANCED)

layout(std140, binding = 5) uniform _ufGpuInstanceData_Block {
  GpuInstanceData _ufGpuInstanceData[DEF_MAX_INSTANCES];
};

mat4 getModelMatrix()
{
  return _ufGpuInstanceData[gl_InstanceID]._model;
}
uint getPick()
{
  return _ufGpuInstanceData[gl_InstanceID]._pickId.x;
}

#else

#error Instancing must always be on for now.

uniform mat4 _ufMatrix_Model;
uniform uint _ufPickId;

mat4 getModelMatrix()
{
  return _ufMatrix_Model;
}
uint getPick()
{
  return _ufPickId;
}

#endif
#endif



