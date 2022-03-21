#include "v_glsl_version.glsl"

//uniform Mat4f normal_matrix;            
uniform mat4 _ufMatrix_View;            
uniform mat4 _ufMatrix_Projection;

layout(location = 0)in vec3 _v301;
layout(location = 1)in vec3 _n301;
layout(location = 2)in vec2 _x201;

out vec3 _vsNormal;
out vec2 _vsTcoords;
out vec3 _vsVertex; //should be frag pos.

#define MAX_INSTANCES 32
layout(std140) uniform _ufInstanceData_Block {
  mat4 _ufInstanceData[MAX_INSTANCES];
};

void main(void)
{
  mat4 matrix_model;
  if( gl_InstanceID >= MAX_INSTANCES ) {
    matrix_model = mat4(1.0f);
  }
  else {
    matrix_model = _ufInstanceData[gl_InstanceID];
  }

  gl_Position =  (_ufMatrix_Projection* _ufMatrix_View * matrix_model ) * vec4(_v301, 1) ;
  _vsVertex = (matrix_model * vec4(_v301,1)).xyz;
  _vsNormal = normalize((matrix_model * vec4(_v301 + _n301, 1)).xyz - _vsVertex);
  _vsTcoords = _x201;
}