#include "v_globals.glsl"

layout(location = 0) in vec4 _v401;//rect
layout(location = 1) in vec4 _v402;//clip
layout(location = 2) in vec4 _v403;//tex
layout(location = 3) in vec2 _v201;//texsiz
layout(location = 4) in uvec2 _u201;//pick_color
layout(location = 5) in vec4 _v404;//border radius0
layout(location = 6) in vec4 _v405;//border radius1
layout(location = 7) in vec3 _v301;//quadrant

out vec4 _rectVS;
out vec4 _clipVS;
out vec4 _texVS;
out vec4 _rtl_rtrVS;
out vec4 _rbr_rblVS;
out vec2 _texsizVS;
flat out uvec2 _pick_colorVS;
out vec3 _quadrantVS;

// The GUI is computed relative to the window 0,0, even in multiple views, we compute absolute X/Y locations in the render quad
vec4 windowRect(in vec4 screen) {
  // Layout quad is in pixel screen coordinates relative to the window/screen Top Left
  // The resulting coordinates for the GPU are -0.5 +0.5 in both axes with the center being in the center of the screen
  // Translate a 2D screen quad to be rendered in a shader.
  // So* our quad is from TOP Left - OpenGL is Bottom Left - this fixes this. 
  //Device coordinates = [-0.5,+0.5] see glViewport
  // Xw = (Xd+1) (Wv/2) + Xv
  // Xd = (Xw-Xv)/(Wv/2) - 1
  // Xw = Window x, Xv = Viewport x, Xd = device X, Wv = Viewport width
  float wh = _ufGpuCamera._fRenderHeight;
  float vx = _ufGpuCamera._vWindowViewport.x;
  float vy = _ufGpuCamera._vWindowViewport.y;
  float vw = _ufGpuCamera._vWindowViewport.z;
  float vh = _ufGpuCamera._vWindowViewport.w;

  //*this piece resizes the GUI to be the size of the current FBO (_fRenderHeight)
  float rx = _ufGpuCamera._fRenderHeight / _ufGpuCamera._fWindowHeight;
  float ry = _ufGpuCamera._fRenderWidth / _ufGpuCamera._fWindowWidth;
  screen.x *= rx;
  screen.y *= ry;
  screen.z *= rx;
  screen.w *= ry;

  //Convert top left y into OpenGL bottom left Y & Flip min/max Y due to y conversion
  vy = wh - vy - vh;
  screen.y = wh - screen.y;
  screen.w = wh - screen.w;
  float tmp = screen.y;
  screen.y = screen.w;
  screen.w = tmp;

  screen.x = (screen.x - vx) / (vw / 2.0f) - 1.0f;
  screen.z = (screen.z - vx) / (vw / 2.0f) - 1.0f;

  screen.y = (screen.y - vy) / (vh / 2.0f) - 1.0f;
  screen.w = (screen.w - vy) / (vh / 2.0f) - 1.0f;

  return screen;
}
void main() {
  _rectVS = windowRect(_v401);
  _clipVS = windowRect(_v402);
  _texVS = _v403;

  float w2 = _ufGpuCamera._fRenderWidth * 0.5f; 
  float h2 = _ufGpuCamera._fRenderHeight * 0.5f;

    //Corners
  _rtl_rtrVS = _v404;
  _rtl_rtrVS.xy = vec2(_rtl_rtrVS.x / w2, _rtl_rtrVS.y / h2);
  _rtl_rtrVS.zw = vec2(_rtl_rtrVS.z / w2, _rtl_rtrVS.w / h2);
  _rbr_rblVS = _v405;
  _rbr_rblVS.xy = vec2(_rbr_rblVS.x / w2, _rbr_rblVS.y / h2);
  _rbr_rblVS.zw = vec2(_rbr_rblVS.z / w2, _rbr_rblVS.w / h2);

  _texsizVS = _v201;
  _pick_colorVS = _u201;
  _quadrantVS = _v301;

  gl_Position =  vec4(_rectVS.x, _rectVS.y, -1, 1);	
}
