#include "v_glsl_version.glsl"


layout(points) in;
layout(triangle_strip, max_vertices=4) out;

uniform mat4 _ufMatrix_View;            
uniform mat4 _ufMatrix_Projection;

in vec3 _posVS[];
in vec3 _normalVS[];
in vec2 _sizeVS[];
in vec4 _texVS[];
flat in uvec2 _pickVS[];

out vec3 _vertGS;
out vec4 _colorGS;
out vec2 _texGS;
flat out uvec2 _pickGS;

void setGS() {
  _pickGS  = _pickVS[0];
  _colorGS  = _colorVS[0];
}
void main() {
  /*
                          z,w
      0------------ ----2
      |               / |
      |           /     |
      |        c        |
      |     /           |
      |  /              |
      1 ----------------3
  x,y                   
  */
  vec4 center = _posVS[0];
  vec4 right = cross(_normalVS[0], vec3(0,1,0));
  vec4 up = cross(right, _normalVS[0]);
  mat4 viewproj = _ufMatrix_Projection* _ufMatrix_View;

  setGS();
  _texGS          = _texVS[0].xw;
  _vertGS         = vec4(center - right + up, 1);
  gl_Position     = viewproj * vec4(_vertGS, 1);
  EmitVertex();

  setGS();
  _texGS          = _texVS[0].xy;
  _vertGS         = vec4(center - right - up, 1);
  gl_Position     = viewproj * vec4(_vertGS, 1);
  EmitVertex();

  setGS();
  _texGS          = _texVS[0].zw;
  _vertGS         = vec4(center + right + up, 1);
  gl_Position     = viewproj * vec4(_vertGS, 1);
  EmitVertex();

    setGS();
  _texGS          = _texVS[0].zy;
  _vertGS         = vec4(center + right - up, 1);
  gl_Position     = viewproj * vec4(_vertGS, 1);
  EmitVertex();
  
  EndPrimitive();
}





