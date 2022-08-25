#include "v_globals.glsl"

in vec2 _tcoordOut;

void main()
{
  vec4 fragColor  = getMRT_Color(_tcoordOut);
  vec3 fragNormal = getMRT_Normal(_tcoordOut).xyz;
  vec3 fragPos    = getMRT_Position(_tcoordOut).xyz;

  vec3 final_color = lightFragment(fragPos, fragColor, fragNormal, 0.2f, 0.5f, 1);

	setOutput_Color(vec4(fragColor.rgb, 1));
  setOutput_Pick(0);//TODO: we're going to create piieplien stages
}
