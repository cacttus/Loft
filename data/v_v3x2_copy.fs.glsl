#include "v_globals.glsl"

in vec2 _tcoordOut;

void main()
{
  vec4 c = getInput_Color(_tcoordOut);

	setOutput_Color(c);
}
