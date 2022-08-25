#include "v_globals.glsl"

layout(location = 0)in vec3 _v301;
layout(location = 1)in vec3 _n301;
layout(location = 2)in vec2 _x201;
layout(location = 3)in uint _u101;
//then .. another buffer for _rColor//custom

layout(location = 4)in vec3 _c301;

out vec3 _vsNormal;
out vec3 _vsColor;
out vec2 _vsTcoords;
out vec3 _vsVertex; //should be frag pos.
out float _vsProjectionDist; //should be frag pos.
flat out uint _vsMaterialID;
flat out uint _vsPickOut;
void main(void) 
{
  vec4 outv = (_ufGpuCamera._m4Projection * _ufGpuCamera._m4View) * vec4(_v301, 1) ;

  _vsColor      = _c301;// vec3(1,1,1);//_ufBlockColor[_u101]; //TODO:
  _vsVertex     = _v301;
  _vsNormal     = _n301;
  _vsTcoords    = _x201;
  _vsMaterialID = _u101;
  _vsProjectionDist = (outv.w + 1) / 2;
  _vsPickOut = getPick();
    gl_Position =  outv;
}