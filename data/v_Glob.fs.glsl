#include "v_glsl_version.glsl"
#include "v_forward_header.glsl"

uniform sampler2D _ufTexture2D_Albedo;
uniform sampler2D _ufTexture2D_Normal;

in vec3 _vsNormal;
in vec2 _vsTcoords;
in vec3 _vsVertex;
in vec3 _vsColor;
flat in uint _vsMaterialID;
in float _vsProjectionDist; //should be frag pos.

void main(void)
{
  vec4 tx_albedo = texture(_ufTexture2D_Albedo, vec2(_vsTcoords));

  if(tx_albedo.a < 0.001) {
    discard;
  }

  //float nmap_blend = 0.5f;
  //vec3 tx_normal = normalize(texture(_ufTexture2D_Normal, vec2(_vsTcoords)).xyz * 2.0f - 1.0f);
  //vec3 bump_normal = normalize((1-nmap_blend)*_vsNormal + (nmap_blend)*tx_normal);
  vec4 litFragment = vec4(tx_albedo.rgb * _vsColor, 1);//  tx_albedo.a);
  vec4 foggedFragment = fog2(_vsProjectionDist, litFragment);
  setColorOutput(foggedFragment);
  setPickOutput(0);
 // _psColorOut.w = 1.0f; //tex.a - but alpha compositing needs to be implemented.
}