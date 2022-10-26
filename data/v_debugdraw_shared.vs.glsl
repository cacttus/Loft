#include "v_globals.glsl"

//** THIS FILE IS SHARED

layout(location = 0)in vec3 _v301;
layout(location = 1)in vec4 _c401;
layout(location = 2)in vec2 _v201;
layout(location = 3)in vec3 _v302;

out vec3 _v3VS;
out vec4 _c4VS;
out vec2 _sizeVS;
out vec3 _outlVS;

void main(void)
{
  _v3VS = _v301;
  _c4VS = _c401;
  _sizeVS = _v201;
  _outlVS = _v302;
}