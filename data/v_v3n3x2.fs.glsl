#include "v_globals.glsl"

in vec3 _vsNormal;
in vec2 _vsTcoords;
in vec3 _vsVertex;
flat in uint _vsPick;

void main(void) 
{
  vec4 tx_albedo = texture(_ufGpuMaterial_s2Albedo, vec2(_vsTcoords));
  vec4 albedo = tx_albedo * _ufGpuMaterial._vPBR_baseColor;
  
 // We need tos et this based on material blend param
  if(albedo.a < 0.001) {
    discard;
  }

  vec3 tx_normal = normalize(texture(_ufGpuMaterial_s2Normal, vec2(_vsTcoords)).xyz * 2.0f - 1.0f);
  mat3 mLightMatrix = getLightMatrix(_vsNormal, _vsVertex);
  vec3 surface_normal = normalize(mLightMatrix * tx_normal);  

//  setOutput_Color(vec4(albedo.r *.9,albedo.g * .6,albedo.b * .8,1)); 
  setOutput_Color(albedo);  
  setOutput_Pick(_vsPick);  
  setOutput_Normal(vec4(tx_normal,1));  
  setOutput_Position(vec4(_vsVertex,1));   
  setOutput_Plane(vec4(_vsNormal,_ufGpuMaterial._flat));        
}   
