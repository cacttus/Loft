#include "v_globals.glsl"

in vec3 _vsNormal;
in vec2 _vsTcoords;
in vec3 _vsVertex;
flat in _vsPickOut;

void main(void)
{
  vec4 tx_albedo = texture(_ufGpuMaterial_s2Albedo, vec2(_vsTcoords));

  if(tx_albedo.a < 0.001) {
    discard;
  }

  //float nmap_blend = 0.5f;
  //vec3 tx_normal = normalize(texture(_ufGpuMaterial_s2Normal, vec2(_vsTcoords)).xyz * 2.0f - 1.0f);
 // vec3 bump_normal = normalize((1-nmap_blend)*_vsNormal + (nmap_blend)*tx_normal);

  vec4 finalDiffuseColor = _ufGpuMaterial._vPBR_baseColor;// vec4(1,1,1,1);

  setOutput_Color(new vec4(finalDiffuseColor.rgb *  tx_albedo.rgb, 1));
  setOutput_Pick(_vsPickOut);
  setOutput_Position(vec4(_vsVertex,1));
}