#include "v_globals.glsl"

in vec3 _vsNormal;
in vec3 _vsVertex;
flat in uint _vsPick;

void main(void) 
{
  //vec4 tx_albedo = texture(_ufGpuMaterial_s2Albedo, vec2(_vsTcoords));
  vec4 albedo = _ufGpuMaterial._vPBR_baseColor;
  
  // We need to set this based on material blend param
  if(albedo.a < 0.001) 
  {
    discard;
  }

  setOutput_Color(albedo);  
  setOutput_Pick(_vsPick);  
  setOutput_Normal(vec4(_vsNormal,1));  //w unused right now
  setOutput_Position(vec4(_vsVertex, _ufGpuMaterial._flat));   
  setOutput_Material(_ufGpuMaterial._vBlinnPhong_Spec);   
}   
