#include "v_globals.glsl"

layout(location = 0)in vec3 _v301;
layout(location = 1)in vec3 _n301;
layout(location = 2)in vec2 _x201;//TODO: test if we can remove this
layout(location = 3)in vec3 _f301;
layout(location = 4)in vec3 _t301;

out vec3 _v3VS;
out vec3 _n3VS;
out vec3 _f3VS;
out vec3 _t3VS;

void main(void)
{
  _v3VS = _v301;
  _n3VS = _n301;
  _f3VS = _f301;
  _t3VS = _t301;

  setInstanceIDForGS();
}