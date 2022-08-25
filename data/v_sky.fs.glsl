#include "v_globals.glsl"

//uniform float _ufDayNightCycle_Blend;// 0 = night 1 = day

in vec2 _vsTcoords;
in vec3 _vsVertex;
flat in uint _vsPickOut;

void main(void)
{
  vec4 tx_cloud = texture(_ufGpuMaterial_s2Albedo, vec2(_vsTcoords));

  vec4 finalDiffuseColor =  tx_cloud * _ufGpuMaterial._vPBR_baseColor;

  // _psColorOut = /*finalDiffuseColor  *  tx_star * (1-_ufDayNightCycle_Blend) + */ finalDiffuseColor * tx_cloud /** (_ufDayNightCycle_Blend)*/;
  setOutput_Color(finalDiffuseColor);
  setOutput_Pick(_vsPickOut);
}