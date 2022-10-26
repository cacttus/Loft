#include "v_globals.glsl"
#include "v_pixelGS.glsl"

layout(points) in;
layout(triangle_strip, max_vertices = 6) out;

in vec3 _v3VS[];
in vec4 _c4VS[];
in vec2 _sizeVS[];
in vec3 _outlVS[];

void main() 
{
  mat4 pvm = _ufGpuCamera._m4Projection * _ufGpuCamera._m4View * _ufGpuInstanceData[getInstanceID()]._model;

  emitPixelPoint(_v3VS[0], _c4VS[0], _sizeVS[0].x, _outlVS[0], pvm);
}
