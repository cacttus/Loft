#include "v_glsl_version.glsl"

uniform sampler2D _ufTexture2D_Albedo;
uniform sampler2D _ufTexture2D_Albedo2;
uniform float _ufDayNightCycle_Blend;// 0 = night 1 = day

uniform float _ufTexture2D_Albedo_Blend;
uniform float _ufTexture2D_Albedo2_Blend;

uniform vec4 _ufWorldObject_Color;

in vec2 _vsTcoords;
in vec3 _vsVertex;

out vec4 _psColorOut;

void main(void)
{
  vec4 tx_cloud = texture(_ufTexture2D_Albedo, vec2(_vsTcoords));
  //vec4 tx_star = texture(_ufTexture2D_Albedo2, vec2(_vsTcoords));

  vec4 finalDiffuseColor = _ufWorldObject_Color;

  _psColorOut = /*finalDiffuseColor  *  tx_star * (1-_ufDayNightCycle_Blend) + */ finalDiffuseColor * tx_cloud /** (_ufDayNightCycle_Blend)*/;
}