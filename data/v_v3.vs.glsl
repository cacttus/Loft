#version 400

//uniform Mat4f normal_matrix;            
uniform mat4 _ufMatrix_Model;            
uniform mat4 _ufMatrix_View;            
uniform mat4 _ufMatrix_Projection;

layout(location = 0)in vec3 _v;

void main(void)
{
    gl_Position =  (_ufMatrix_Projection* _ufMatrix_View * _ufMatrix_Model ) * vec4(_v, 1) ;
}