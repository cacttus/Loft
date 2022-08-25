#include "v_globals.glsl"

flat in uint _vsPickOut;
void main(void)
{
  setOutput_Color(_ufGpuMaterial._vPBR_baseColor);
  setOutput_Pick(_vsPickOut);
}