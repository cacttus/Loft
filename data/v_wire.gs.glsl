#include "v_globals.glsl"
#include "v_pixelGS.glsl"

layout(triangles) in;
layout(triangle_strip, max_vertices = 24) out;
//6 * 3

in vec3 _v3VS[];
in vec3 _n3VS[];
in vec3 _t3VS[];
flat in uint _primId[];

void main() 
{
  // float wh = _ufGpuCamera._fRenderHeight;
  //   float vx = _ufGpuCamera._vWindowViewport.x;
  // float vy = _ufGpuCamera._vWindowViewport.y;
  // float vw = _ufGpuCamera._vWindowViewport.z;
  // float vh = _ufGpuCamera._vWindowViewport.w;
  // float scl = 1.0;
  // float wn = _ufGpuCamera._widthNear;
  // float hn = _ufGpuCamera._heightNear;
  // float zn = _ufGpuCamera._fZNear;
  // float zf = _ufGpuCamera._fZFar;
  // mat4 orth  = get_ortho(wn/2, -wn/2, hn/2,-hn/2, zn, zf);

  mat4 pvm = _ufGpuCamera._m4Projection * _ufGpuCamera._m4View * _ufGpuInstanceData[getInstanceID()]._model;
  float dist = _ufGpuDebug._fWireframeCageDist;

  vec3 pv0 = (_v3VS[0] + _n3VS[0] * dist);
  vec3 pv1 = (_v3VS[1] + _n3VS[1] * dist);
  vec3 pv2 = (_v3VS[2] + _n3VS[2] * dist);

  vec4 c = _ufGpuDebug._wireframeColor;

  emitPixelLine(pv0, pv1, c, c, 1, 0, pvm);
  emitPixelLine(pv1, pv2, c, c, 1, 0, pvm);
  emitPixelLine(pv2, pv0, c, c, 1, 0, pvm);
}
