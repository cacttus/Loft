#version 400

//uniform Mat4f normal_matrix;            
uniform mat4 _ufMatrix_Model;            
uniform mat4 _ufMatrix_View;            
uniform mat4 _ufMatrix_Projection;

layout(location = 0)in vec3 _v;
layout(location = 1)in vec3 _n;
layout(location = 2)in vec2 _x;

out vec3 _vsNormal;
out vec2 _vsTcoords;
out vec3 _vsVertex; //should be frag pos.

void main(void)
{
    gl_Position =  (_ufMatrix_Projection* _ufMatrix_View * _ufMatrix_Model ) * vec4(_v, 1) ;
    _vsVertex = (_ufMatrix_Model * vec4(_v,1)).xyz;
    _vsNormal = normalize((_ufMatrix_Model * vec4(_v + _n, 1)).xyz - _vsVertex);
    _vsTcoords = _x;
}