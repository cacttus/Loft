#include "v_globals.glsl"

layout(triangles) in;
layout(line_strip, max_vertices = 6) out;

in vec3 _v3VS[];
in vec3 _n3VS[];
in vec3 _t3VS[];
flat in uint _primId[];
out vec4 _colorGS;

void doLine(vec4 v0, vec4 v1, vec4 c) {
  gl_Position = v0;
  _colorGS = c;
  EmitVertex();

  gl_Position = v1;
  _colorGS = c;
  EmitVertex();
  
  EndPrimitive(); 
}

void main() {
  float cage_dist = _ufGpuDebug._fWireframeCageDist;
  
  mat4 pvm = _ufGpuCamera._m4Projection * _ufGpuCamera._m4View * _ufGpuInstanceData[getInstanceID()]._model;

  vec4 v0 = pvm * vec4(_v3VS[0] + _n3VS[0] * cage_dist, 1);
  vec4 v1 = pvm * vec4(_v3VS[1] + _n3VS[1] * cage_dist, 1);
  vec4 v2 = pvm * vec4(_v3VS[2] + _n3VS[2] * cage_dist, 1);

  doLine(v0, v1, _ufGpuDebug._wireframeColor); 
  doLine(v1, v2, _ufGpuDebug._wireframeColor);
  doLine(v2, v0, _ufGpuDebug._wireframeColor);
}


// void doLine(vec4 v0, vec4 v1, vec4 c) {
//   gl_Position = v0;
//   _colorGS = c;
//   EmitVertex();

//   gl_Position = v1;
//   _colorGS = c;
//   EmitVertex();
  
//   EndPrimitive(); 
// }
// void doTri(vec4 v0, vec4 v1, vec4 v2, vec4 c) {
//   gl_Position = v0;
//   _colorGS = c;
//   EmitVertex();

//   gl_Position = v1;
//   _colorGS = c;
//   EmitVertex();
  
//   gl_Position = v2;
//   _colorGS = c;
//   EmitVertex();

//   EndPrimitive(); 
// }
// void doTQuad(vec4 v0, vec4 v1, vec4 v2, vec4 v3, vec4 c) {
//   doTri(v0, v1, v2, c);
//   doTri(v1, v3, v2, c);
// }

// void main() {
//   //0a         1a
//   //*---------*
//   //0b         1b
  
//   float cage_dist = _ufGpuDebug._fWireframeCageDist;
  
//   mat4 pvm = _ufGpuCamera._m4Projection * _ufGpuCamera._m4View * _ufGpuInstanceData[getInstanceID()]._model;


//   vec3 pv0 = (_v3VS[0] + _n3VS[0] * cage_dist);
//   vec3 pv1 = (_v3VS[1] + _n3VS[1] * cage_dist);
//   vec3 pv2 = (_v3VS[2] + _n3VS[2] * cage_dist);
  
//   //line width  
//   vec3 a0 = (cross(normalize(pv0.xyz - pv1.xyz), _n3VS[0]));
//   vec3 a1 = (cross(normalize(pv1.xyz - pv2.xyz), _n3VS[1]));
//   vec3 a2 = (cross(normalize(pv2.xyz - pv0.xyz), _n3VS[2]));
 
//   vec4 v0a = pvm * vec4(pv0 + a0 * -_ufGpuDebug._lineWidth, 1);
//   vec4 v0b = pvm * vec4(pv0 + a0 * _ufGpuDebug._lineWidth, 1);
//   vec4 v1a = pvm * vec4(pv1 + a1 * -_ufGpuDebug._lineWidth, 1);
//   vec4 v1b = pvm * vec4(pv1 + a1 * _ufGpuDebug._lineWidth, 1);
//   vec4 v2a = pvm * vec4(pv2 + a2 * -_ufGpuDebug._lineWidth, 1);
//   vec4 v2b = pvm * vec4(pv2 + a2 * _ufGpuDebug._lineWidth, 1);

//   doTQuad(v0a, v0b, v1a, v1b, _ufGpuDebug._wireframeColor);
//   doTQuad(v1a, v1b, v2a, v2b, _ufGpuDebug._wireframeColor);
//   doTQuad(v2a, v2b, v0a, v0b, _ufGpuDebug._wireframeColor);
// }