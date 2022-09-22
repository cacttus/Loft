#include "v_globals.glsl"

layout(triangles) in;
layout(line_strip, max_vertices = 6) out;

in vec3 _v3VS[];
in vec3 _n3VS[];
in vec3 _f3VS[];
in vec3 _t3VS[];

out vec4 _colorGS;

void doLine(mat4 pvm, vec3 v, vec3 n, vec4 c) {
  gl_Position =  pvm * vec4(v, 1);
  _colorGS = c;
  EmitVertex();
  gl_Position = pvm * vec4(v + n *  _ufGpuDebug._normalLength, 1);
  _colorGS = c;
  EmitVertex();
  EndPrimitive(); 
}

void main() {
  //Move small amount away so T/B do not intersect plane
  float moveAmt = 0.01f;
  vec3 move = _n3VS[0] * moveAmt;

  vec3 the_centroid = (_v3VS[0] + _v3VS[1] + _v3VS[2]) / 3.0f;

  mat4 pvm = _ufGpuCamera._m4Projection * _ufGpuCamera._m4View * _ufGpuInstanceData[getInstanceID()]._model;

  //Face / TBN  
  doLine(pvm, move + the_centroid, _t3VS[0], _ufGpuDebug._tangentColor);
  doLine(pvm, move + the_centroid, _f3VS[0], _ufGpuDebug._faceNormalColor);
  
  //Vertex Normal
  doLine(pvm, move + _v3VS[0], _n3VS[0], _ufGpuDebug._vertexNormalColor);
}