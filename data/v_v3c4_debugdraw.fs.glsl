#include "v_glsl_version.glsl"
#include "v_forward_header.glsl"

in vec4 _vsColorOut;

void main(void)
{
  setColorOutput(_vsColorOut);
  setPickOutput(0);
}