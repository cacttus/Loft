#include "v_globals.glsl"

in vec2 _tcoordOut;

void main()
{
	vec4 diffuseTex = texture(_ufGpuMaterial_s2Albedo, vec2(_tcoordOut));
	diffuseTex.w = 1.0f;

	setOutput_Color(diffuseTex);
}
