#include "v_globals.glsl"

in vec3 _vsNormal;
in vec2 _vsTcoords;
in vec3 _vsVertex;
in vec3 _vsColor;
flat in uint _vsMaterialID;
in float _vsProjectionDist; //should be frag pos.
flat in uint _vsPickOut;
void main(void)
{
  vec4 tx_albedo = texture(_ufGpuMaterial_s2Albedo, vec2(_vsTcoords));

  if(tx_albedo.a < 0.001) {
    discard;
  }

  vec4 litFragment = vec4(tx_albedo.rgb * _vsColor, 1);//  tx_albedo.a);
  vec4 foggedFragment = fog2(_vsProjectionDist, litFragment);
  setOutput_Color(foggedFragment);
  setOutput_Pick(_vsPickOut);
 // _psColorOut.w = 1.0f; //tex.a - but alpha compositing needs to be implemented.
}