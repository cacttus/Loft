#include "v_globals.glsl"


layout(location = 0)in vec3 _v;
layout(location = 1)in vec4 _c;

out vec4 _vsColorOut;

void main(void)
{
    gl_Position =  getPVMMatrix() * vec4(_v, 1) ;
    _vsColorOut = _c;
}