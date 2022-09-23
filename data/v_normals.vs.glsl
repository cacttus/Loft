#include "v_globals.glsl"

layout(location = 0)in vec3 _v301;
layout(location = 1)in vec3 _n301;
layout(location = 2)in vec2 _x201;
layout(location = 3)in vec3 _t301;
layout(location = 4)in uint _u101;

out vec3 _v3VS;
out vec3 _n3VS;
out vec3 _t3VS;
flat out uint _primId;

void main(void)
{
  _v3VS = _v301;
  _n3VS = _n301;
  _t3VS = _t301;
  _primId = _u101;

  setInstanceIDForGS();
}