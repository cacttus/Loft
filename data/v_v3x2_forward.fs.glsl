#include "v_glsl_version.glsl"
//Do not include forward header for the final blit

uniform sampler2D _ufTexture_Forward_Blitter_Input;
in vec2 _tcoordOut;

out vec4 _vColorOut_DEFAULT_FBO;

void main()
{
	vec4 diffuseTex = texture(_ufTexture_Forward_Blitter_Input, vec2(_tcoordOut));

	diffuseTex.w = 1.0f;
	
	_vColorOut_DEFAULT_FBO = diffuseTex;
}
