#include "v_globals.glsl"
#include "v_pixelGS.glsl"

layout(lines) in;
layout(triangle_strip, max_vertices = 6) out;

in vec3 _v3VS[];
in vec4 _c4VS[];
in vec2 _sizeVS[];
void main() 
{
//   float wh = _ufGpuCamera._fRenderHeight;
//   float vx = _ufGpuCamera._vWindowViewport.x;
//   float vy = _ufGpuCamera._vWindowViewport.y;
//   float vw = _ufGpuCamera._vWindowViewport.z;
//   float vh = _ufGpuCamera._vWindowViewport.w;

// float aspect = vw/vh;

//  mat4 orth  = get_ortho(vx, vw, vy, vh, -1, 1);
  //mat4 orth  = get_ortho(-aspect, aspect, -1, 1, -10, 10);

  mat4 pvm = _ufGpuCamera._m4Projection * _ufGpuCamera._m4View * _ufGpuInstanceData[getInstanceID()]._model;

  emitPixelLine(_v3VS[0], _v3VS[1], _c4VS[0], _c4VS[1], _sizeVS[0].x, _sizeVS[0].y, pvm);
}
