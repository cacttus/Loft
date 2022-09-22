#include "v_globals.glsl"

in vec4 _colorGS;

void main(void)
{
  setOutput_Color(_colorGS);

  setOutput_Pick(0);  
  setOutput_Normal(vec4(0,0,0,1));  
  setOutput_Position(vec4(0,0,0,1));   
  setOutput_Plane(vec4(0,0,0,1));  //1 = flat color
}
