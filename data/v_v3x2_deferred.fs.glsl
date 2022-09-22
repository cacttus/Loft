#include "v_globals.glsl"

in vec2 _tcoordOut;

void main()
{
  vec4 fragColor                        = getMRT_Color(_tcoordOut);
  vec4 fragNormal_and_bump_intensity    = getMRT_Normal(_tcoordOut);
  vec4 fragPos_and_nolight              = getMRT_Position(_tcoordOut);
 
  vec3 fragNormal                       = normalize(fragNormal_and_bump_intensity.xyz);//we are normalizing here, not in deferred.
  vec3 fragPos                          = fragPos_and_nolight.xyz;
  
  float nolight                         = fragPos_and_nolight.w;
  float bump_intensity                  = fragNormal_and_bump_intensity.w;

//not using plane rn

  //vec3 final_color = lightFragmentCookTorrence(fragPos, fragColor, fragNormal, 0.2f, 0.5f, 1);
  vec3 final_color = lightFragmentBlinnPhong(fragPos, fragColor, fragNormal) * (1.0-nolight) + fragColor.rgb * nolight;

	setOutput_Color(vec4(final_color.rgb, 1));
}
