#include "v_globals.glsl"

in vec3 _vsNormal;
in vec2 _vsTcoords;
in vec3 _vsVertex;
flat in uint _vsPick;
in vec3 _vsTangent;

void main(void) 
{
  vec4 tx_albedo = texture(_ufGpuMaterial_s2Albedo, vec2(_vsTcoords));
  vec4 albedo = tx_albedo * _ufGpuMaterial._vPBR_baseColor;
  
  // We need to set this based on material blend param
  if(albedo.a < 0.001) {
    discard;
  }
  vec3 tx_normal = texture(_ufGpuMaterial_s2Normal, vec2(_vsTcoords)).xyz * 2.0f - 1.0f;
 // float bump_intensity = length(tx_normal);//this is a "special" thing for bump maps.
  //tx_normal = normalize(tx_normal);//normalize in the deferred step
  vec3 bitangent = normalize(cross(_vsTangent, _vsNormal));
  mat3 tbn = mat3(_vsTangent, _vsNormal ,bitangent );
  vec3 final_normal = tbn * tx_normal;//normalize in the deferred step

  setOutput_Color(albedo);  
  setOutput_Pick(_vsPick);  
  setOutput_Normal(vec4(final_normal, 1));  
  setOutput_Position(vec4(_vsVertex, _ufGpuMaterial._flat));   
  setOutput_Plane(vec4(0,0,0, 1));        
}   
