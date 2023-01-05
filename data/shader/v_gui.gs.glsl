#include "v_globals.glsl"

layout(points) in;
layout(triangle_strip, max_vertices=4) out;

//Since we are points, it is already flat.
flat in vec4 _rectVS[];//min, max
flat in vec4 _clipVS[];
in vec4 _texVS[];
flat in vec4 _rtl_rtrVS[];
flat in vec4 _rbr_rblVS[];
flat in vec2 _texsizVS[];
flat in uvec2 _pick_colorVS[];
flat in vec4 _borderVS[];
flat in uvec4 _borderColorVS[];
flat in float _weightVS[];

out vec2 _vert;
out vec2 _tex;
flat out vec4 _clip;
flat out vec2 _texSiz;//We can do compute this in the GS and output better data before the PS
flat out uvec2 _pick_color;
flat out vec2 _texPos;//don't interpolate
flat out vec4 _rtl_rtr;
flat out vec4 _rbr_rbl;
flat out vec4 _rect;
flat out vec4 _border;
flat out uvec4 _borderColor;
flat out float _weight;

//note:GL: bottom left corner
float p0x(vec4 f) { return f.x; }
float p0y(vec4 f) { return f.y; }
float p1x(vec4 f) { return f.z; }
float p1y(vec4 f) { return f.w; }

void setGS(){
  //Uniform primitive values
  _clip        = _clipVS[0];
  _texSiz      = _texsizVS[0];
  _pick_color  = _pick_colorVS[0];
  _texPos      = _texVS[0].xy;
  _rtl_rtr     = _rtl_rtrVS[0];
  _rbr_rbl     = _rbr_rblVS[0];
  _rect        = _rectVS[0];
  _border      = _borderVS[0];
  _borderColor = _borderColorVS[0];
  _weight = _weightVS[0];
}

void main() {
  /*
                         P1
      0------------ ----2
      |          /      |
      |  /              |
      1 ----------------3
  P0
  */

  //We flipped Y for OpenGL, but the texs are still in window coords.
  setGS();
  _tex            = vec2(p0x(_texVS [0]), p0y(_texVS [0]));
  _vert           = vec2(p0x(_rectVS[0]), p1y(_rectVS[0]));
  gl_Position     = vec4(p0x(_rectVS[0]), p1y(_rectVS[0]), -1, 1);
  EmitVertex();
  
  setGS();
  _tex            = vec2(p0x(_texVS [0]), p1y(_texVS [0]));
  _vert           = vec2(p0x(_rectVS[0]), p0y(_rectVS[0]));
  gl_Position     = vec4(p0x(_rectVS[0]), p0y(_rectVS[0]), -1, 1);
  EmitVertex();
  
  setGS();
  _tex            = vec2(p1x(_texVS [0]), p0y(_texVS [0]));
  _vert           = vec2(p1x(_rectVS[0]), p1y(_rectVS[0]));
  gl_Position     = vec4(p1x(_rectVS[0]), p1y(_rectVS[0]), -1, 1);
  EmitVertex();
  
  setGS();
  _tex            = vec2(p1x(_texVS [0]), p1y(_texVS [0]));
  _vert           = vec2(p1x(_rectVS[0]), p0y(_rectVS[0]));
  gl_Position     = vec4(p1x(_rectVS[0]), p0y(_rectVS[0]), -1, 1);
  EmitVertex();
  
  EndPrimitive();
}





