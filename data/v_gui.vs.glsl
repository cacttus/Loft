#include "v_glsl_version.glsl"


layout(location = 0) in vec4 _v401;//rect
layout(location = 1) in vec4 _v402;//clip
layout(location = 2) in vec4 _v403;//tex
layout(location = 3) in vec2 _v201;//texsiz
layout(location = 4) in uvec2 _u201;//pick_color
layout(location = 5) in vec4 _v404;//r0
layout(location = 6) in vec4 _v405;//r1

out vec4 _rectVS;
out vec4 _clipVS;
out vec4 _texVS;
out vec4 _rtl_rtrVS;
out vec4 _rbr_rblVS;
out vec2 _texsizVS;
flat out uvec2 _pick_colorVS;

void main() {
  _rectVS = _v401;
  _clipVS = _v402;
  _texVS = _v403;
  _rtl_rtrVS = _v404;
  _rbr_rblVS = _v405;
  _texsizVS = _v201;
  _pick_colorVS = _u201;
    
  gl_Position =  vec4(_v401.x, _v401.y, -1, 1);	
}
