#include "v_glsl_version.glsl"

//uniform Mat4f normal_matrix;            
uniform mat4 _ufMatrix_Model;            
uniform mat4 _ufMatrix_View;            
uniform mat4 _ufMatrix_Projection;

layout(location = 0)in vec3 _v301;
layout(location = 1)in vec3 _n301;
layout(location = 2)in vec2 _x201;

out vec3 _vsNormal;
out vec2 _vsTcoords;
out vec3 _vsVertex; //should be frag pos.

void main(void)
{
    gl_Position =  (_ufMatrix_Projection* _ufMatrix_View * _ufMatrix_Model ) * vec4(_v301, 1) ;
    _vsVertex = (_ufMatrix_Model * vec4(_v301,1)).xyz;
    _vsNormal = normalize((_ufMatrix_Model * vec4(_v301 + _n301, 1)).xyz - _vsVertex);
    _vsTcoords = _x201;
}