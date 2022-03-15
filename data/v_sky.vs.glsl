#include "v_glsl_version.glsl"

uniform mat4 _ufMatrix_View;            
uniform mat4 _ufMatrix_Projection;
uniform mat4 _ufMatrix_Model;

layout(location = 0)in vec3 _v301;
layout(location = 2)in vec2 _x201;

out vec2 _vsTcoords;
out vec3 _vsVertex;

void main(void)
{
  gl_Position =  (_ufMatrix_Projection* _ufMatrix_View * _ufMatrix_Model ) * vec4(_v301, 1) ;
  _vsVertex = (_ufMatrix_Model * vec4(_v301,1)).xyz;
  _vsTcoords = _x201;
}