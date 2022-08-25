#include "v_globals.glsl"

layout(location = 0)in vec3 _v301;
layout(location = 1)in vec3 _n301;
layout(location = 2)in vec2 _x201;

out vec3 _vsNormal;
out vec2 _vsTcoords;
out vec3 _vsVertex; //should be frag pos.
flat out uint _vsPick; 

void main(void)
{
  mat4 model = getModelMatrix();

  gl_Position =  (_ufGpuCamera._m4Projection * _ufGpuCamera._m4View * model ) * vec4(_v301, 1) ;
  _vsVertex = (model * vec4(_v301,1)).xyz;
  _vsNormal = normalize((model * vec4(_v301 + _n301, 1)).xyz - _vsVertex);
  _vsTcoords = _x201;
  _vsPick = getPick();
}