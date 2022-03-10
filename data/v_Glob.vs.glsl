#include "v_glsl_version.glsl"

//uniform Mat4f normal_matrix;            
uniform mat4 _ufMatrix_View;            
uniform mat4 _ufMatrix_Projection;

layout(location = 0)in vec3 _v301;
layout(location = 1)in vec3 _c301;
layout(location = 2)in vec3 _n301;
layout(location = 3)in vec2 _x201;
layout(location = 4)in uint _u101;

out vec3 _vsNormal;
out vec3 _vsColor;
out vec2 _vsTcoords;
out vec3 _vsVertex; //should be frag pos.
flat out uint _vsMaterialID;

void main(void)
{
  _vsVertex     = _v301;
  _vsColor      = _c301;
  _vsNormal     = _n301;
  _vsTcoords    = _x201;
  _vsMaterialID = _u101;
  gl_Position =  (_ufMatrix_Projection* _ufMatrix_View) * vec4(_v301, 1) ;
}