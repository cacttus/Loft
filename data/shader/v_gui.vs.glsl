#include "v_globals.glsl"

//#define UI_ROUND_BLOCKS


layout(location = 0) in vec4 _v401;//rect
layout(location = 1) in vec4 _v402;//clip
layout(location = 2) in vec4 _v403;//tex
layout(location = 3) in vec2 _v201;//texsiz
layout(location = 4) in uvec2 _u201;//pick_color
layout(location = 5) in vec4 _v404;//border radius0
layout(location = 6) in vec4 _v405;//border radius1
layout(location = 7) in vec4 _v406;//border
layout(location = 8) in uvec4 _u401;//bordre color
layout(location = 9) in vec4 _v407;//weight

flat out vec4 _rectVS;
flat out vec4 _clipVS;
out vec4 _texVS;
flat out vec4 _rtl_rtrVS;
flat out vec4 _rbr_rblVS;
flat out vec2 _texsizVS;
flat out uvec2 _pick_colorVS;
flat out vec4 _borderVS;
flat out uvec4 _borderColorVS;
flat out float _weightVS;

// The GUI is computed relative to the window 0,0, even in multiple views, we compute absolute X/Y locations in the render quad


void main() {
  
  //ROUND - FIXES A LOT OF ISSUES
    _rectVS = window_rect_to_device_rect(round(_v401));
    _clipVS = window_rect_to_device_rect(round(_v402));  
    //_rectVS = window_rect_to_device_rect(_v401);
    //_clipVS = window_rect_to_device_rect(_v402);

  _texVS = _v403;

  float rx = _ufGpuCamera._fRenderWidth / _ufGpuCamera._fWindowWidth;
  float ry = _ufGpuCamera._fRenderHeight / _ufGpuCamera._fWindowHeight;
  float w2 =_ufGpuCamera._vWindowViewport.z *0.5f ;
  float h2 = _ufGpuCamera._vWindowViewport.w * 0.5f ;

    //Corners
  _rtl_rtrVS = _v404;
  _rtl_rtrVS.xy = vec2(_rtl_rtrVS.x / w2*rx, _rtl_rtrVS.y / h2 * ry);
  _rtl_rtrVS.zw = vec2(_rtl_rtrVS.z / w2*rx, _rtl_rtrVS.w / h2 * ry);
  _rbr_rblVS = _v405;
  _rbr_rblVS.xy = vec2(_rbr_rblVS.x / w2*rx, _rbr_rblVS.y / h2 * ry);
  _rbr_rblVS.zw = vec2(_rbr_rblVS.z / w2*rx, _rbr_rblVS.w / h2 * ry);

  _texsizVS = _v201;
  _pick_colorVS = _u201;
  _borderVS =  vec4((_v406.x* ry)/h2 , (_v406.y* rx)/w2, (_v406.z *ry)/h2, (_v406.w*rx)/w2);//trbl
  _borderColorVS = _u401;
  _weightVS = _v407.x;

  gl_Position =  vec4(_rectVS.x, _rectVS.y, -1, 1);	
}
