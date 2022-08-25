#include "v_glsl_version.glsl"

layout(location = 0) in vec4 _v401;//pos
layout(location = 1) in vec2 _v201;//size
layout(location = 2) in vec4 _x401;//uv0, uv1
layout(location = 3) in uvec2 _u201;//pick_color

uniform mat4 _ufMatrix_Model;            
uniform vec3 _ufCamera_Position;

out vec3 _posVS;
out vec2 _sizeVS;
out vec4 _texVS;
out vec3 _normalVS;
flat out uvec2 _pickVS;

void main() {
  _posVS = (_ufMatrix_Model * vec4(_v401.xyz,1)).xyz;
  _sizeVS = _v201;
  _texVS = _x401;
  _normalVS = normalize(_ufCamera_Position - _v301);
  _pickVS = _u201;

  gl_Position = _posVS;
}
