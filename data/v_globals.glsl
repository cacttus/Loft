﻿#include "v_global_outputs.glsl"

//Global funcs
float pointOnRay_t( vec3 a, vec3 p ) {
  vec3 AP = p - a;    
  vec3 AB = a*-1.0;    
  float ab2 = AB.x*AB.x + AB.y*AB.y + AB.z*AB.z;   
  float ap_ab = AP.x*AB.x + AP.y*AB.y + AP.z*AB.z;   
  float t = ap_ab / ab2;   
  return t;
}

#if defined(DEF_SHADER_STAGE_VERTEX)

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
vec4 toneMapLightFinal(in vec4 vFinalColor, in float toneExp)
{
    vec3 final;
    
    final = vec3(1.0) - exp(-vFinalColor.rgb * toneExp);
    final = pow(final, vec3(1.0f/toneExp));

    return vec4(final, 1.0f);
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
vec4 mirror(in vec4 vColor, in float fMirrorAmount, in vec3 vFragToViewDir, in vec3 fragNormal, in vec3 fragPos) {
  if(fMirrorAmount <= 0.0001) { 
      return vColor;
  }
    
  vec3 vReflect = reflect(vFragToViewDir, fragNormal);
  vec2 tex;
  tex.t = dot(normalize(vReflect), vec3(0.0, -1.0, 0.0));//This produces upside down ENV maps. swapped vec3.. with -1.0
  vReflect.y = 0.0;
  tex.s = dot(normalize(vReflect), vec3(1.0, 0.0, 0.0)) * 0.5;
    
  //float r = 10.0f;
    
  if (vReflect.z >= 0.0) {
      tex = (tex + 1.0) * 0.5;
  }
  else {
      tex.t = (tex.t + 1.0) * 0.5;
      tex.s = (-tex.s) * 0.5 + 1.0;
  }
    
  vec3 vOut = mix( vec3(vColor), vec3(texture(_ufGpuWorld_s2EnvironmentMap, tex)), fMirrorAmount);
  return vec4(vOut, vColor.a);
}
mat3 getLightMatrix(in vec3 planeNormal, in vec3 planeVertex){
	float d = - dot(planeVertex, planeNormal);
	vec3 pv = planeVertex+1.0;	//random neighbor vertex
	float dist = dot(planeNormal, pv) + d;	//distnace from p to plane
	vec3 pp = pv - (planeNormal * dist); // - project p onto plane
	vec3 tangent = normalize(pp - planeVertex); // normalize tangent arbitrary direction
	vec3 bitangent = cross(planeNormal, tangent);

	// vec3 tangent = normalize(cross(vec3(1,0,0), planeNormal));
	// vec3 binormal = normalize(cross(planeNormal, tangent));
		
	//note this should produce a matrix the same as the plaen matrix for normals at 0,1,0
	
	return mat3(tangent, planeNormal, bitangent);
}
vec3 lightFragment(vec3 vert_pos, vec4 albedo,vec3 surface_normal, float rough, float spec, float IOR) {

  vec3 finalColor = vec3(0,0,0);

  for(int iLight=0; iLight<_ufGpuWorld._iPointLightCount; iLight++) {
    vec3 vLightPos = _ufGpuPointLights[iLight]._pos;

    vec3 eye_vector = normalize(_ufGpuCamera._vViewPos - vert_pos);
    vec3 light_vector = normalize(_ufGpuPointLights[iLight]._pos - vert_pos);
    vec3 half_vector = (light_vector + eye_vector) / length(light_vector + eye_vector);

    float Dpow = 2.0 / pow(rough, 2.0) - 2.0;
    float D = 1.0 / (M_PI * pow(rough, 2.0)) * pow( dot(half_vector, surface_normal), Dpow);

    float Ga = 2 * dot(half_vector, surface_normal) * dot(surface_normal , eye_vector)  / dot(eye_vector, half_vector);
    float Gb = 2 * dot(half_vector, surface_normal) * dot(surface_normal , light_vector) / dot(eye_vector, half_vector);
    float G = min(1.0, min(Ga,Gb));

    float F0 = pow(IOR - 1.0, 2.0) / pow(IOR + 1.0, 2.0); //"Schlick's" approximation
    float F = F0 + (1.0 - F0) * pow( 1.0 - dot(eye_vector, half_vector), 5.0 );

    //DEBUGGING
    //F = 1;
    //G = 1;

    float rs = (D * G * F) / (4.0 * dot(light_vector, surface_normal) * dot(eye_vector, surface_normal) );

    float spec = rs * (spec);

    vec3 diffuse = (albedo.rgb  * _ufGpuMaterial._vPBR_baseColor.rgb) * (1.0-spec); //kd,  d = 1-s, s = 1-d
    
    //Attenuation
    float fFragToLightDistance = distance(_ufGpuPointLights[iLight]._pos, vert_pos);
    float power = clamp(_ufGpuPointLights[iLight]._power, 0.000001f, 0.999999f);
    float fQuadraticAttenuation = 1- pow(clamp(fFragToLightDistance/_ufGpuPointLights[iLight]._radius, 0, 1),(power)/(1-power)); //works well: x^(p/(1-p)) where x is pos/radius

    finalColor += _ufGpuPointLights[iLight]._color * dot(surface_normal , light_vector) *  (diffuse + spec) ;
  } 

  //TODO:
  //ka = Ambient
  finalColor += vec3(0.01,0.01,0.01); 

  return finalColor;
}

#endif//DEF_SHADER_STAGE_FRAGMENT





