#include "v_globals.glsl"

in vec4 _vsColorOut;
flat in uint _vsPickOut;

void main(void)
{
  setOutput_Color(_vsColorOut);
  setOutput_Pick(_vsPickOut);
}