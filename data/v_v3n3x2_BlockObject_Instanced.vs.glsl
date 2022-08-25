#include "v_globals.glsl"

layout(location = 0)in vec3 _v301;
layout(location = 1)in vec3 _n301;
layout(location = 2)in vec2 _x201;

out vec3 _vsNormal;
out vec2 _vsTcoords;
out vec3 _vsVertex; //should be frag pos.


void main(void)
{
  mat4 matrix_model;
  if( gl_InstanceID >= DEF_MAX_INSTANCES ) {
    matrix_model = _ufMatrix_Model;
  }
  else {
    matrix_model = _ufGpuInstanceData[gl_InstanceID] * _ufMatrix_Model;
  }

  gl_Position =  (_ufMatrix_Projection* _ufMatrix_View * matrix_model ) * vec4(_v301, 1) ;
  _vsVertex = (matrix_model * vec4(_v301,1)).xyz;
  _vsNormal = normalize((matrix_model * vec4(_v301 + _n301, 1)).xyz - _vsVertex);
  _vsTcoords = _x201;
}