#include "v_globals.glsl"


layout(location = 0)in vec3 _v301;
layout(location = 2)in vec2 _x201;

out vec2 _vsTcoords;
out vec3 _vsVertex;
flat out uint _vsPickOut;

void main(void)
{
  gl_Position =  getPVMMatrix() * vec4(_v301, 1) ;
  _vsVertex = (getModelMatrix() * vec4(_v301,1)).xyz;
  _vsTcoords = _x201;
  _vsPickOut= getPick();
}