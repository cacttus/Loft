#include "v_global_outputs.glsl"

//Global funcs
float pointOnRay_t( vec3 a, vec3 p ) {
  vec3 AP = p - a;    
  vec3 AB = a*-1.0;    
  float ab2 = AB.x*AB.x + AB.y*AB.y + AB.z*AB.z;   
  float ap_ab = AP.x*AB.x + AP.y*AB.y + AP.z*AB.z;   
  float t = ap_ab / ab2;   
  return t;
}

#if defined(DEF_SHADER_STAGE_VERTEX) //|| defined(DEF_SHADER_STAGE_GEOMETRY) 

mat4 getPVMMatrix() {
  mat4 m = _ufGpuCamera._m4Projection * _ufGpuCamera._m4View * getModelMatrix();
  return m;
}

#elif defined(DEF_SHADER_STAGE_FRAGMENT)

vec4 sample2d(in sampler2D smp, in vec2 pos)
{
  return texture(smp, vec2(pos));
}
//Convert a COLOR sample to a higher dynamic range (HDR)
vec4 sample2d_hdr(in sampler2D smp, in vec2 pos, in float exp)
{
  vec4 tx = texture(smp, vec2(pos));
  
  tx.r = pow(tx.r, exp);
  tx.g = pow(tx.g, exp);
  tx.b = pow(tx.b, exp);
  //tx.a = pow(tx.r,2.2f);
  
  return tx;
}
//Convert back from HDR to LDR
vec3 tonemap_Exposure(in vec3 color, in float exposure, in float gamma)
{
  vec3 final = vec3(1.0) - exp(-color * exposure);
  final = pow(final, vec3(1.0f/gamma));
  return final;
}
vec3 toneMap_Reinhard(in vec3 hdrColor, in float gamma)
{
  vec3 mapped = hdrColor / (hdrColor + vec3(1.0));
  // gamma correction 
  mapped = pow(mapped, vec3(1.0 / gamma));
  return mapped;
}
vec4 fog(in vec3 vFragPos, in vec4 vFragColor) {
  //Call this after final fragment color is calculatd
  // in: fragment position
  // in: frament (lit) color
  // out: fragment foggy color
  float fAmount;
  vec4 vRetColor;
  float density = 0.001;

  float fFragCamDistance = distance(_ufGpuCamera._vViewPos, vFragPos);

  //OpenGL deafult fog
  //http://what-when-how.com/opengl-programming-guide/fog-blending-antialiasing-fog-and-polygon-offset-opengl-programming/
  //fAmount = exp(  -(fFragCamDistance * fFragCamDistance) );//new 
  fAmount = log(fFragCamDistance / _ufGpuWorld._fFogDivisor * _ufGpuWorld._fFogDamp); // 1- (1/x^2)
  fAmount = clamp(fAmount, 0,1);
  fAmount *= _ufGpuWorld._fFogBlend; // blend factor, to change how much fog shows.
  
  vRetColor.rgb = mix(vFragColor.rgb, _ufGpuWorld._vFogColor.rgb, fAmount);
  vRetColor.a = vFragColor.a;
  return vRetColor;
}
vec4 fog2(in float dist, in vec4 vFragColor) {
  float fAmount;
  vec4 vRetColor;
  float density = 0.001;

  dist = dist * density;

  //fAmount = exp(  -(dist) );//EXP
  fAmount = exp(  -(dist * dist) );//EXP2
  
  vRetColor.rgb = mix( _ufGpuWorld._vFogColor.rgb, vFragColor.rgb, fAmount);
  vRetColor.a = vFragColor.a;
  return vRetColor;
}
// vec4 mirror(in vec4 vColor, in float fMirrorAmount, in vec3 vFragToViewDir, in vec3 fragNormal, in vec3 fragPos) {
//   if(fMirrorAmount <= 0.0001) { 
//       return vColor;
//   }
    
//   vec3 vReflect = reflect(vFragToViewDir, fragNormal);
//   vec2 tex;
//   tex.t = dot(normalize(vReflect), vec3(0.0, -1.0, 0.0));//This produces upside down ENV maps. swapped vec3.. with -1.0
//   vReflect.y = 0.0;
//   tex.s = dot(normalize(vReflect), vec3(1.0, 0.0, 0.0)) * 0.5;
    
//   //float r = 10.0f;
    
//   if (vReflect.z >= 0.0) {
//       tex = (tex + 1.0) * 0.5;
//   }
//   else {
//       tex.t = (tex.t + 1.0) * 0.5;
//       tex.s = (-tex.s) * 0.5 + 1.0;
//   }
    
//   vec3 vOut = mix( vec3(vColor), vec3(texture(_ufGpuWorld_s2EnvironmentMap, tex)), fMirrorAmount);
//   return vec4(vOut, vColor.a);
// }

float attenuate_light_radius(in vec3 in_vertex, in vec3 light_pos, in float light_power, in float light_radius)
{
  //x^(p/(1-p)) where x is pos/radius

  float fFragToLightDistance = distance(light_pos, in_vertex);
  float power = clamp(light_power, 0.000001f, 0.999999f);
  float fQuadraticAttenuation = 1- pow(clamp(fFragToLightDistance/light_radius, 0, 1),(power)/(1-power)); 
  
  return fQuadraticAttenuation;
}
float attenuate_light_distance(in vec3 in_vertex, in vec3 light_pos, in float light_power, in float light_radius)
{
  //more accurate function that uses light distance 
  //clamped to light radius.
  float dist = pow(min(length(light_pos - in_vertex),light_radius),2);
  dist = light_power / (pow(dist,1));
  return dist;
}
vec3 lightFragmentCookTorrence(in vec3 in_vpos, in vec4 in_albedo, in vec3 in_normal, in float in_rough, in float in_spec, in float in_IOR) {

  vec3 finalColor = vec3(0,0,0);

  for(int iLight = 0; iLight < _ufGpuWorld._iPointLightCount; iLight++) {
    vec3 vLightPos = _ufGpuPointLight[iLight]._pos;

    vec3 eye_vector = normalize(_ufGpuCamera._vViewPos - in_vpos);
    vec3 light_vector = normalize(_ufGpuPointLight[iLight]._pos - in_vpos);
    vec3 half_vector = (light_vector + eye_vector) / length(light_vector + eye_vector);

    //Cook-Torrence
    float Dpow = 2.0 / pow(in_rough, 2.0) - 2.0;
    float D = 1.0 / (M_PI * pow(in_rough, 2.0)) * pow( dot(half_vector, in_normal), Dpow);

    float Ga = 2 * dot(half_vector, in_normal) * dot(in_normal , eye_vector)  / dot(eye_vector, half_vector);
    float Gb = 2 * dot(half_vector, in_normal) * dot(in_normal , light_vector) / dot(eye_vector, half_vector);
    float G = min(1.0, min(Ga,Gb));

    float F0 = pow(in_IOR - 1.0, 2.0) / pow(in_IOR + 1.0, 2.0); //"Schlick's" approximation
    float F = F0 + (1.0 - F0) * pow( 1.0 - dot(eye_vector, half_vector), 5.0 );
    
    F = 1;//DEBUGGING
    G = 1;

    float rs = (D * G * F) / (4.0 * dot(light_vector, in_normal) * dot(eye_vector, in_normal) );

    float spec = rs * (in_spec);

    vec3 diffuse = (in_albedo.rgb  * _ufGpuMaterial._vPBR_baseColor.rgb) * (1.0-spec); //kd,  d = 1-s, s = 1-d
    
    //Attenuation
    float fFragToLightDistance = distance(_ufGpuPointLight[iLight]._pos, in_vpos);
    float power = clamp(_ufGpuPointLight[iLight]._power, 0.000001f, 0.999999f);
    float fQuadraticAttenuation = 1- pow(clamp(fFragToLightDistance/_ufGpuPointLight[iLight]._radius, 0, 1),(power)/(1-power)); //works well: x^(p/(1-p)) where x is pos/radius

    finalColor += _ufGpuPointLight[iLight]._color * dot(in_normal , light_vector) *  (diffuse + spec) ;
  } 

  finalColor += _ufGpuWorld._vAmbientColor * _ufGpuWorld._fAmbientIntensity;

  return finalColor;
}
vec3 doBlinnPhong(in vec3 in_vertex, in vec3 in_albedo, in vec3 light_vector, in vec3 eye_vector, in vec3 in_normal, in vec3 vLightColor, float atten,  vec4 spec_color)
{
  //Blinn-Phong
  vec3 half_vector = (light_vector + eye_vector) / length(light_vector + eye_vector);
  float lambert = max(dot(light_vector, in_normal), 0.0);
  float spec = pow(max(dot(normalize(light_vector + eye_vector), in_normal), 0.0), spec_color.w);
  return (in_albedo * lambert + spec_color.xyz * spec) * vLightColor * atten;
}
vec3 lightFragmentBlinnPhong(in vec3 in_vertex, in vec4 in_albedo, in vec3 in_normal, vec4 spec_color) {
  
  vec3 eye_vector = normalize(_ufGpuCamera._vViewPos - in_vertex);

  vec3 finalColor = vec3(0,0,0);

  for(int iLight = 0; iLight <  _ufGpuWorld._iPointLightCount; iLight++) {
    vec3 vLightPos = _ufGpuPointLight[iLight]._pos;
    vec3 vLightColor = _ufGpuPointLight[iLight]._color;
    float fLightPower = _ufGpuPointLight[iLight]._power+100;
    float fLightRadius = _ufGpuPointLight[iLight]._radius;

    vec3 light_vector = normalize(vLightPos - in_vertex);    

    float atten=1;
    atten = attenuate_light_distance(in_vertex, vLightPos, fLightPower, fLightRadius);

    finalColor += doBlinnPhong(in_vertex, in_albedo.rgb, light_vector, eye_vector, in_normal, vLightColor, atten,  spec_color);
  }

  for(int iLight = 0; iLight <  _ufGpuWorld._iDirLightCount; iLight++) 
  {
    vec3 vLightPos = _ufGpuDirLight[iLight]._pos;
    vec3 vLightColor = _ufGpuDirLight[iLight]._color;
    float fLightPower = _ufGpuDirLight[iLight]._power+100;
    float fLightRadius = _ufGpuDirLight[iLight]._radius;

    vec3 light_vector = -_ufGpuDirLight[iLight]._dir;    

    float atten=1;
    //atten = attenuate_light_distance(in_vertex, vLightPos, fLightPower, fLightRadius);

    finalColor += doBlinnPhong(in_vertex, in_albedo.rgb, light_vector, eye_vector, in_normal, vLightColor, atten, spec_color) ;
  }

  finalColor += in_albedo.rgb* _ufGpuWorld._vAmbientColor * _ufGpuWorld._fAmbientIntensity;

  return finalColor;
}

#endif//DEF_SHADER_STAGE_FRAGMENT






