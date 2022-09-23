#include "v_globals.glsl"

layout(location = 0)in vec3 _v301;
layout(location = 1)in vec3 _n301;
layout(location = 2)in vec2 _x201;
layout(location = 3)in vec3 _t301;
layout(location = 4)in uint _u101;

out vec3 _vsNormal;
out vec2 _vsTcoords;
out vec3 _vsVertex; //should be frag pos.
flat out uint _vsPick; 
out vec3 _vsTangent;

void main(void)
{
  mat4 model = getModelMatrix();
  mat4 mi = getInverseModelMatrix();

  gl_Position =  (_ufGpuCamera._m4Projection * _ufGpuCamera._m4View * model ) * vec4(_v301, 1) ;
  _vsVertex = (model * vec4(_v301,1)).xyz;
  _vsNormal = normalize(mi * vec4(_n301,1)).xyz;
  _vsTangent =  normalize(mi * vec4(_t301,1)).xyz ;
  _vsTcoords = _x201;
  _vsPick = getPick();
}