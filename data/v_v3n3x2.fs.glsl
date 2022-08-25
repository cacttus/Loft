#include "v_globals.glsl"

in vec3 _vsNormal;
in vec2 _vsTcoords;
in vec3 _vsVertex;
flat in uint _vsPick;

void main(void) 
{
  vec4 tx_albedo = texture(_ufGpuMaterial_s2Albedo, vec2(_vsTcoords));
  vec4 albedo = tx_albedo * _ufGpuMaterial._vPBR_baseColor;
 // We need tos et this based on material blend param
  if(albedo.a < 0.001) {
    discard;
  }

  vec3 tx_normal = normalize(texture(_ufGpuMaterial_s2Normal, vec2(_vsTcoords)).xyz * 2.0f - 1.0f);
  mat3 mLightMatrix = getLightMatrix(_vsNormal, _vsVertex);
  vec3 surface_normal = normalize(mLightMatrix * tx_normal);
  
  //DEBUGGING
  surface_normal = _vsNormal;

  float rough = _ufGpuMaterial._fPBR_roughness;
  float IOR = _ufGpuMaterial._fPBR_indexOfRefraction;
  float spec = _ufGpuMaterial._fPBR_specular;

  //vec3 finalColor = lightFragment(_vsVertex, tx_albedo, surface_normal, rough, spec, IOR);

  setOutput_Color(albedo);
  setOutput_Pick(_vsPick);
  setOutput_Normal(tx_normal);
  setOutput_Position(_vsVertex);
  setOutput_Plane(_vsNormal);
}






//  int _ufLightModel_Index = 1;
  //float _ufLightModel_GGX_X=1;
  //float _ufLightModel_GGX_Y=1;
  //float _ufNormalMap_Blend=0.5f;
  //Sun
  // vec3 sun_normal_surf = sun._dir * -1;
  // if(_ufLightModel_Index==0) {
  //     //Phong
  //     vReflect = reflect(-sun_normal_surf, bump_normal);
  // }
  // if(_ufLightModel_Index==1) {
  //     //Blinn-Phong
  //     vReflect = (sun_normal_surf + bump_normal)*0.5f;
  // }

    // Phong
    //float D = clamp( pow(clamp(dot(reflect(-light_vector, bump_normal), eye_vector), 0,1), fSpecHardness), 0,1 );
    
    //GGX 1
    //float bias = (bump_normal.x*bump_normal.x)/(_ufLightModel_GGX_X*_ufLightModel_GGX_X) +(bump_normal.z*bump_normal.z)/(_ufLightModel_GGX_Y*_ufLightModel_GGX_Y)+bump_normal.y*bump_normal.y;
    //float D = 1.0 / (M_PI*_ufLightModel_GGX_X*_ufLightModel_GGX_Y*bias*bias);
  
    //schlick GGX
    //float gamma = (-1 + ((bump_normal.x*bump_normal.x)*(_ufLightModel_GGX_X*_ufLightModel_GGX_X) +(bump_normal.z*bump_normal.z)*(_ufLightModel_GGX_Y*_ufLightModel_GGX_Y))/(bump_normal.y*bump_normal.y)) * 0.5f;
    //float D = 1.0 / (1+gamma);

    //Blinn phong
    //float D = clamp( pow(clamp(dot(half_vector, eye_vector), 0,1), fSpecHardness), 0,1 );
    

  // finalDiffuseColor += sun._color * dot(sun_normal_surf, bump_normal) *  sun._power; 
  // distribution = clamp( pow(clamp(dot(vReflect, eye), 0,1), fSpecHardness), 0,1 );
  // finalSpecColor += sun._color * fSpecIntensity * distribution;// * shadowMul; 
  // if(_ufLightModel_Index==2) {
  //     //Flat
  //     finalDiffuseColor = vec3(1,1,1);
  //     finalSpecColor = vec3(0,0,0);
  // }
