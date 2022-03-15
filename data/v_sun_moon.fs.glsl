﻿#include "v_glsl_version.glsl"

uniform sampler2D _ufTexture2D_Albedo;

uniform vec4 _ufWorldObject_Color;

uniform vec3 _ufCamera_Basis_Z; //view normal

in vec2 _vsTcoords;
in vec3 _vsVertex;

out vec4 _psColorOut;

void main(void)
{
  //This will be white if it isn't present. For now .. we'll add 'moon texture' later.
  vec4 tx_albedo = texture(_ufTexture2D_Albedo, vec2(_vsTcoords));

  vec4 finalDiffuseColor = _ufWorldObject_Color;

  _psColorOut  = finalDiffuseColor *  tx_albedo;
}