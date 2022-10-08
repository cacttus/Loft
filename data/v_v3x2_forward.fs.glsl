#include "v_globals.glsl"

in vec2 _tcoordOut;

void main()
{
  
	vec4 diffuseTex = getInput_Color(_tcoordOut);
	diffuseTex.w = 1.0f;

	setOutput_Color(diffuseTex);
}
