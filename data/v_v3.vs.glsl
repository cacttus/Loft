#include "v_globals.glsl"

layout(location = 0)in vec3 _v301;
flat out uint _vsPickOut;
void main(void)
{
    gl_Position =  getPVMMatrix() * vec4(_v301, 1) ;
    _vsPickOut = getPick();
}