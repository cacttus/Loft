#include "v_globals.glsl"

in vec2 _tcoordOut;

void main()
{
  vec4 fragColor  = getMRT_Color(_tcoordOut);
  vec3 fragNormal = getMRT_Normal(_tcoordOut).xyz;
  vec3 fragPos    = getMRT_Position(_tcoordOut).xyz;
  float flatc = getMRT_Material(_tcoordOut).w;//plane

  //vec3 final_color = lightFragmentCookTorrence(fragPos, fragColor, fragNormal, 0.2f, 0.5f, 1);
  vec3 final_color = lightFragmentBlinnPhong(fragPos, fragColor, fragNormal) * (1.0-flatc) + fragColor.rgb * flatc;

	setOutput_Color(vec4(final_color.rgb, 1));
	//setOutput_Color(vec4(fragColor.rgb, 1));
}
