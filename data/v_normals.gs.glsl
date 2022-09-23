#include "v_globals.glsl"

layout(triangles) in;
layout(line_strip, max_vertices = 8) out;

in vec3 _v3VS[];
in vec3 _n3VS[];
in vec3 _t3VS[];
flat in uint _primId[];
out vec4 _colorGS;
layout(std430, binding = <UBO_BINDING_ID>) buffer _ufGpuFaceData_Block {
  GpuFaceData _ufGpuFaceData[];
};

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
  mat4 pvm = _ufGpuCamera._m4Projection * _ufGpuCamera._m4View * _ufGpuInstanceData[getInstanceID()]._model;

  //Face Normal / Tangent
  vec3 ft = _ufGpuFaceData[_primId[0]]._tangent;
  vec3 fn = _ufGpuFaceData[_primId[0]]._normal;
  vec3 the_centroid = (_v3VS[0] + _v3VS[1] + _v3VS[2]) / 3.0f;
  doLine(pvm, move + the_centroid, ft, _ufGpuDebug._faceTangentColor); 
  doLine(pvm, move + the_centroid, fn, _ufGpuDebug._faceNormalColor);
  
  //Vertex Normal / Tangent
  doLine(pvm, move + _v3VS[0], _t3VS[0], _ufGpuDebug._vertexTangentColor);
  doLine(pvm, move + _v3VS[0], _n3VS[0], _ufGpuDebug._vertexNormalColor);
}