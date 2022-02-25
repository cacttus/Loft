#include "v_glsl_version.glsl"

in vec4 _vsColorOut;

out vec4 _psColorOut;

void main(void)
{
    _psColorOut = _vsColorOut;
}