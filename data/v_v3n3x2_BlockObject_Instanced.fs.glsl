#include "v_glsl_version.glsl"


uniform sampler2D _ufTexture2D_Albedo;
uniform sampler2D _ufTexture2D_Normal;

uniform vec4 _ufWorldObject_Color;

in vec3 _vsNormal;
in vec2 _vsTcoords;
in vec3 _vsVertex;

uniform vec3 _ufCamera_Position;

out vec4 _psColorOut;

void main(void)
{
  vec4 tx_albedo = texture(_ufTexture2D_Albedo, vec2(_vsTcoords));

  if(tx_albedo.a < 0.001) {
    discard;
  }

  //float nmap_blend = 0.5f;
  //vec3 tx_normal = normalize(texture(_ufTexture2D_Normal, vec2(_vsTcoords)).xyz * 2.0f - 1.0f);
 // vec3 bump_normal = normalize((1-nmap_blend)*_vsNormal + (nmap_blend)*tx_normal);

  vec4 finalDiffuseColor = _ufWorldObject_Color;// vec4(1,1,1,1);

  _psColorOut.xyz = finalDiffuseColor.rgb *  tx_albedo.rgb;
  _psColorOut.w = 1.0f; //tex.a - but alpha compositing needs to be implemented.
}