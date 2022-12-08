#include "v_globals.glsl"


//** THIS FILE IS SHARED
in vec4 _colorGS;
in vec2 _posGS;
in vec2 _blendOriginGS;
in float _blendSizeGS;
in vec3 _outlGS;

void main(void)
{
  float sharp = 5;
  float blend = 1;
  if(_blendSizeGS > 0)
  { 
    blend = 1-clamp(pow(length(_posGS - _blendOriginGS),sharp) / pow(_blendSizeGS,sharp), 0, 1); 
  }
  vec4 colorszz; 

  if(_colorGS.a * blend <= 0.1)
  {
    discard;
  }
  //Outline - todo - parameter in vtx
  colorszz = vec4(_colorGS.rgb, _colorGS.a * blend);
  if(dot(_outlGS,_outlGS) > 0 && _colorGS.a * blend <= 0.4)
  {
    colorszz = vec4(_outlGS,1);
  }


  setOutput_Color(colorszz);
}
