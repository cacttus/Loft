#include "v_globals.glsl"


//** THIS FILE IS SHARED
in vec4 _colorGS;

void main(void)
{
  setOutput_Color(_colorGS);

  setOutput_Pick(0);  
  setOutput_Normal(vec4(0,0,0,1));  
  setOutput_Position(vec4(0,0,0,1));   
}
