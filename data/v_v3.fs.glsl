#include "v_glsl_version.glsl"
#include "v_forward_header.glsl"

uniform vec4 _ufWorldObject_Color;

void main(void)
{
  setColorOutput(_ufWorldObject_Color);
  setPickOutput(0);
}