#include "v_globals.glsl"


//** THIS FILE IS SHARED
in vec4 _colorGS;
in vec2 _posGS;
in vec2 _blendOriginGS;
in float _blendSizeGS;

void main(void)
{
  float sharp = 5;
  float blend = 1;
  if(_blendSizeGS > 0)
  { 
    blend = 1-clamp(pow(length(_posGS - _blendOriginGS),sharp) / pow(_blendSizeGS,sharp), 0, 1); 
  }

  if(_colorGS.a * blend <= 0.11)
  {
    discard;
  }

  setOutput_Color(vec4(_colorGS.rgb, _colorGS.a * blend));


}
