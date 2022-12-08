#include "v_globals.glsl"

layout(location = 0) in vec3 _v301;
layout(location = 1) in vec2 _x201;

out vec2 _tcoordOut;

void main()
{
	_tcoordOut = _x201;
	gl_Position =  _ufGpuCamera._m4Projection * vec4(_v301.xy,0,1);
}
