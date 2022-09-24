#include "v_globals.glsl"

in vec3 _vsNormal;
in vec2 _vsTcoords;
in vec3 _vsVertex;
flat in uint _vsMaterialID;
in float _vsProjectionDist; //should be frag pos.
flat in uint _vsPick;
in vec3 _vsTangent;

void main(void)
{
  vec4 tx_albedo = texture(_ufGpuMaterial_s2Albedo, vec2(_vsTcoords));

  if(tx_albedo.a < 0.001) {
    discard;
  }

  vec3 tx_normal = texture(_ufGpuMaterial_s2Normal, vec2(_vsTcoords)).xyz * 2.0f - 1.0f;
  vec3 bitangent = normalize(cross(_vsTangent, _vsNormal));
  mat3 tbn = mat3(_vsTangent, _vsNormal ,bitangent );
  vec3 final_normal = tbn * tx_normal;//normalize in the deferred step

 // vec4 litFragment = vec4(tx_albedo.rgb * _vsColor, 1);//  tx_albedo.a);
  //vec4 foggedFragment = fog2(_vsProjectionDist, litFragment);

  setOutput_Color(tx_albedo);  
  setOutput_Pick(_vsPick);  
  setOutput_Normal(vec4(final_normal, 1));  
  setOutput_Position(vec4(_vsVertex, _ufGpuMaterial._flat));   
  setOutput_Plane(vec4(0,0,0, 1));       

 // _psColorOut.w = 1.0f; //tex.a - but alpha compositing needs to be implemented.
}