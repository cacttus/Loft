#version 400

//uniform Mat4f normal_matrix;            
uniform mat4 model_matrix;            
uniform mat4 view_matrix;            
uniform mat4 projection_matrix;

layout(location = 0)in vec3 _v;
layout(location = 1)in vec3 _n;
layout(location = 2)in vec2 _x;

out vec3 _vsNormal;
out vec2 _vsTcoords;
out vec3 _vsVertex; //should be frag pos.

void main(void)
{
    gl_Position =  (projection_matrix* view_matrix ) * vec4(_v, 1) ;
    _vsVertex = (model_matrix * vec4(_v,1)).xyz;
    _vsNormal = normalize((model_matrix * vec4(_v + _n, 1)).xyz - _vsVertex);
    _vsTcoords = _x;
}