#include "v_globals.glsl"

in vec3 _vertGS;
in vec4 _colorGS;
in vec2 _texGS;
flat in uvec2 _pickGS;

void main(){

  vec4 tx = texture(_ufGpuMaterial_s2Albedo, _texGS);
  if(tx.a < 0.001) {
  	discard;
  } 

  setOutput_Color(tx * _colorGS);
  setOutput_Pick(_pickGS.x);
  setOutput_Position(_vertGS);
}
