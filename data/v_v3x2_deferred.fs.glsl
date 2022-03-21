#include "v_glsl_version.glsl"
//Do not include forward header for the final blit

in vec2 _tcoordOut;

uniform sampler2D _ufTexture2D_Position;//position
uniform sampler2D _ufTexture2D_Color;//color
uniform sampler2D _ufTexture2D_Normal;//normals
uniform sampler2D _ufTexture2D_Spec;//spec

layout(location = 0) out vec4 _vColorOut_Texture_FBO;

void main()
{
	vec4 diffuseTex = texture(_ufTexture2D_Color, vec2(_tcoordOut));

	diffuseTex.w = 1.0f;
	
	_vColorOut_Texture_FBO = diffuseTex;
}
