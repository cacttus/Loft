
//Most of this will go away
struct GpuShaderData {
  float _fFogDamp;
  float _fFogBlend;    
  float _fFogDivisor;
  float _pad1;
//
  vec4  _vFogColor;
//
  int _iPointLightCount; 
  float _fHdrSampleExp; //Exponent to use converting input color textures to HDR
  float _fHdrToneMapExp; //Exponent to use when tone mapping back to LDR
  int _iShadowBoxCount;
//    
  vec3 _vViewPos; //Camera Position
  int _iDirLightCount;
//
  vec3 _vViewDir;
  float _fTimeSeconds;
//
  vec3 _vAmbientColor;
  float _fAmbientIntensity;
//
  float _fFocalDepth;
  float _fFocalRange;
  float _pad2;
  float _pad3;
};
struct GpuPointLight {
  vec3 _pos;
  float _radius;
//    
  vec3 _color;
  float _power;
};
struct GpuDirLight {
  vec3 _pos;
  float _fMaxDistance; // Radius
//
  vec3 _dir;
  float _fLinearAttenuation;
//    
  vec3 _color;
  float _power;
//
  mat4 _mView;
  mat4 _mProj;
  mat4 _mPVB;
 };

layout(std140) uniform _ufGpuShaderData_Block {
  GpuShaderData _ufGpuShaderData;
};

uniform sampler2D _ufTexture2D_Environment;

//#define MAX_POINT_LIGHTS 32
//#define MAX_DIR_LIGHTS 4
//#define MAX_CUBE_SHADOW_SAMPLES 4
//#define MAX_FRUS_SHADOW_SAMPLES 4
//layout(std140) uniform UfPointLights { GpuPointLight g_pointLights[MAX_POINT_LIGHTS]; };
//layout(std140) uniform UfDirLights { GpuDirLight g_dirLights[MAX_DIR_LIGHTS]; };
//uniform samplerCube _ufShadowBoxSamples[MAX_CUBE_SHADOW_SAMPLES];	//this constant must match MaxShadowInfluences in Engine.dat
//uniform sampler2D _ufShadowFrustumSamples[MAX_FRUS_SHADOW_SAMPLES];	//this constant must match MaxShadowInfluences in Engine.dat

layout(location = 0) out vec4 _mrtOutput_Color;
layout(location = 1) out uint _mrtOutput_Pick;


vec4 fog(in vec3 vFragPos, in vec4 vFragColor) {
  //Call this after final fragment color is calculatd
  // in: fragment position
  // in: frament (lit) color
  // out: fragment foggy color
  float fAmount;
  vec4 vRetColor;
  float density = 0.001;

  float fFragCamDistance = distance(_ufGpuShaderData._vViewPos, vFragPos);

  //OpenGL deafult fog
  //http://what-when-how.com/opengl-programming-guide/fog-blending-antialiasing-fog-and-polygon-offset-opengl-programming/
  //fAmount = exp(  -(fFragCamDistance * fFragCamDistance) );//new 
  fAmount = log(fFragCamDistance / _ufGpuShaderData._fFogDivisor * _ufGpuShaderData._fFogDamp); // 1- (1/x^2)
  fAmount = clamp(fAmount, 0,1);
  fAmount *= _ufGpuShaderData._fFogBlend; // blend factor, to change how much fog shows.
  
  vRetColor.rgb = mix(vFragColor.rgb, _ufGpuShaderData._vFogColor.rgb, fAmount);
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
  
  vRetColor.rgb = mix( _ufGpuShaderData._vFogColor.rgb, vFragColor.rgb, fAmount);
  vRetColor.a = vFragColor.a;
  return vRetColor;
}
float pointOnRay_t( vec3 a, vec3 p ) {
  vec3 AP = p - a;    
  vec3 AB = a*-1.0;    
  float ab2 = AB.x*AB.x + AB.y*AB.y + AB.z*AB.z;   
  float ap_ab = AP.x*AB.x + AP.y*AB.y + AP.z*AB.z;   
  float t = ap_ab / ab2;   
  return t;
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
    
  vec3 vOut = mix( vec3(vColor), vec3(texture(_ufTexture2D_Environment, tex)), fMirrorAmount);
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
void setColorOutput(vec4 color)
{
  _mrtOutput_Color = color;
}
void setPickOutput(uint color)
{
  _mrtOutput_Pick = color;
}






