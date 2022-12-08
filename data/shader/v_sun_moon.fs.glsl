#include "v_globals.glsl"

in vec2 _vsTcoords;
in vec3 _vsVertex;

flat in uint _vsPickOut;

void main(void)
{
  //This will be white if it isn't present. For now .. we'll add 'moon texture' later.
  vec4 tx_albedo = texture(_ufGpuMaterial_s2Albedo, vec2(_vsTcoords));

  vec4 finalDiffuseColor = _ufGpuMaterial._vPBR_baseColor;

  setOutput_Color(finalDiffuseColor *  tx_albedo * 0.5f);
  setOutput_Pick(_vsPickOut);
  setOutput_Position(vec4(_vsVertex,1));
}