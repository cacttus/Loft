#version 400

uniform vec4 _ufWorldObject_Color;

out vec4 _psColorOut;

void main(void)
{
    _psColorOut = _ufWorldObject_Color;
}