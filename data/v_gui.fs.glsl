#include "v_glsl_version.glsl"
#include "v_forward_header.glsl"

uniform sampler2D _ufTexture2D_Albedo;

in vec2 _vert;
flat in vec4 _clip;
in vec2 _tex;
flat in vec2 _texPos;//Origin of the texture coords (top left)
flat in vec2 _texSiz;
flat in uvec2 _pick_color;

void main(){
  if(_vert.x < _clip.x 
    || _vert.y < _clip.y 
    || _vert.x > _clip.z 
    || _vert.y > _clip.w) { 
    discard; 
  }
  
  //Texture Scaling
  //We need texpos here = p + mod(a-p, siz);
  vec2 texmod;
  texmod.x = _texPos.x + mod(_tex.x - _texPos.x, _texSiz.x);
  texmod.y = _texPos.y + mod(_tex.y - _texPos.y, _texSiz.y);
  
  vec4 tx = texture(_ufTexture2D_Albedo, vec2(texmod));
  if(tx.a < 0.001){
  	discard;
  } 
  
  float r = float((_pick_color.y>>24) & 0xFF) / 255.0;
  float g = float((_pick_color.y>>16) & 0xFF) / 255.0;
  float b = float((_pick_color.y>>8) & 0xFF) / 255.0;
  float a = float((_pick_color.y>>0) & 0xFF) / 255.0;
  
  setColorOutput(tx * vec4(r, g, b, a));
  setPickOutput(_pick_color.x);

}
