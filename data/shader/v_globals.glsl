#include "v_global_outputs.glsl"


//Global funcs
vec4 uintToVec4(uint val) {
  return vec4(
    float((val>>24) & 0xFF) / 255.0,
    float((val>>16) & 0xFF) / 255.0,
    float((val>>8) & 0xFF) / 255.0,
    float((val>>0) & 0xFF) / 255.0
  );
}
vec4 clip_to_screen(vec4 p) {
  //to screen space, p has been multiplied by MVP matrix
  vec2 rwh = vec2(_ufGpuCamera._vWindowViewport.z, 
                  _ufGpuCamera._vWindowViewport.w);

  //**NOTE: this does not put the screen origin in top 
  // 1 - p.y for top origin
  p.xyz /= p.w;
  p.xy = (p.xy + 1.0) * rwh * 0.5;

  return p;
}
vec4 screen_to_clip(vec4 p) {
  //back to clip
  vec2 rwh = vec2(_ufGpuCamera._vWindowViewport.z, 
                  _ufGpuCamera._vWindowViewport.w);

  p.xy = p.xy / rwh * 2.0 - 1.0;
  p.xyz *= p.w;
  return p;
}
vec2 device_vert_to_window_vert(in vec2 vert) {
    // Xw = Window x, Xv = Viewport x, Xd = device X, Wv = Viewport width
  float rh = _ufGpuCamera._fRenderHeight;
  float vx = _ufGpuCamera._vWindowViewport.x;
  float vy = _ufGpuCamera._vWindowViewport.y;
  float vw = _ufGpuCamera._vWindowViewport.z;
  float vh = _ufGpuCamera._vWindowViewport.w;

  //*this piece resizes the GUI to be the size of the current FBO (_fRenderHeight)
  float rx = _ufGpuCamera._fRenderWidth / _ufGpuCamera._fWindowWidth;
  float ry = _ufGpuCamera._fRenderHeight / _ufGpuCamera._fWindowHeight;
 
  vec2 ret = vec2(0, 0);
  vy = -vy - vh + rh ;

  ret.x = (vert.x + 1.0f) * (vw / 2.0f)  + vx ;
  ret.y = (vert.y + 1.0f) * (vh / 2.0f)  + vy ;
 
  ret.y = -ret.y + rh;

  ret.x /= rx;
  ret.y /= ry;

  return ret;
}
vec4 window_rect_to_device_rect(in vec4 screen) {
  // convert a top-left origin quad in screen (pixel) coords to a quad in device coords
  // Layout quad is in pixel screen coordinates relative to the window/screen Top Left
  // The resulting coordinates for the GPU are -0.5 +0.5 in both axes with the center being in the center of the screen
  // Translate a 2D screen quad to be rendered in a shader.
  // So* our quad is from TOP Left - OpenGL is Bottom Left - this fixes this. 
  //Device coordinates = [-0.5,+0.5] see glViewport
  // Xw = (Xd+1) (Wv/2) + Xv
  // Xd = (Xw-Xv)/(Wv/2) - 1
  // Xw = Window x, Xv = Viewport x, Xd = device X, Wv = Viewport width
  float rh = _ufGpuCamera._fRenderHeight;
  float vx = _ufGpuCamera._vWindowViewport.x;
  float vy = _ufGpuCamera._vWindowViewport.y;
  float vw = _ufGpuCamera._vWindowViewport.z;
  float vh = _ufGpuCamera._vWindowViewport.w;

  vy = rh - vy - vh;

  //*this piece resizes the GUI to be the size of the current FBO (_fRenderHeight)
  float rx = _ufGpuCamera._fRenderWidth / _ufGpuCamera._fWindowWidth;
  float ry = _ufGpuCamera._fRenderHeight / _ufGpuCamera._fWindowHeight;
  screen.x *= rx;
  screen.y *= ry;
  screen.z *= rx;
  screen.w *= ry;

  //Convert top left y into OpenGL bottom left Y & Flip min/max Y due to y conversion
  screen.y = rh - screen.y;
  screen.w = rh - screen.w;
  float tmp = screen.y;
  screen.y = screen.w;
  screen.w = tmp;

  //[0,w] => [-0.5,0.5]
  screen.x = (screen.x - vx) / (vw / 2.0f) - 1.0f;//l
  screen.z = (screen.z - vx) / (vw / 2.0f) - 1.0f;//r
  screen.y = (screen.y - vy) / (vh / 2.0f) - 1.0f;//t
  screen.w = (screen.w - vy) / (vh / 2.0f) - 1.0f;//b

  return screen;
}
vec3 pointOnLine(vec3 p0, vec3 p1, vec3 pt)
{
  //Returns closest point on this line.
  vec3 dP = pt - p0;
  vec3 dL = p1 - p0;
  float dPdL = dot(dP,dL);
  float dLdL = dot(dL,dL);
  float t = -dPdL / dLdL;

  return p0 + (p1-p0) * t;
}
float pointOnRay_t( vec3 a, vec3 p ) {
  vec3 AP = p - a;    
  vec3 AB = a*-1.0;    
  float ab2 = AB.x*AB.x + AB.y*AB.y + AB.z*AB.z;   
  float ap_ab = AP.x*AB.x + AP.y*AB.y + AP.z*AB.z;   
  float t = ap_ab / ab2;   
  return t;
}
mat4 get_ortho(float left, float right, float top, float bottom, float neard, float fard) {
  //ortho matrix
  float a1 = 2.0 / (right - left);
  float a2 = 2.0 / (top - bottom);  
  float a3 = -2.0 / (fard - neard); 
  float t1 = - (right + left) / (right - left);
  float t2 = - (top + bottom) / (top - bottom);
  float t3 = - (fard + neard) / (fard - neard);

  mat4 ret = mat4(
    a1,  0,  0, t1, 
     0, a2,  0, t2,
     0,  0, a3, t3, 
     0,  0,  0,  1
  );
  // mat4 ret = mat4(
  //   a1,  0,  0, 0, 
  //    0, a2,  0, 0,
  //    0,  0, a3, 0, 
  //   t1, t2, t3,  1
  // );  
  //ret = transpose(ret);

  return ret;
}

#if defined(DEF_SHADER_STAGE_VERTEX) //|| defined(DEF_SHADER_STAGE_GEOMETRY) 

mat4 getPVMMatrix() {
  mat4 m = _ufGpuCamera._m4Projection * _ufGpuCamera._m4View * getModelMatrix();
  return m;
}

#elif defined(DEF_SHADER_STAGE_GEOMETRY)

// void emitSimpleColoredLine(vec4 v0, vec4 v1, vec4 c) {
//   gl_Position = v0;
//   _colorGS = c;
//   EmitVertex();
//   gl_Position = v1;
//   _colorGS = c;
//   EmitVertex();
//   EndPrimitive(); 
// }
// void emitColoredTri(vec3 v0, vec3 v1, vec3 v2, vec4 c0, vec4 c1, vec4 c2, mat4 proj_view_model) {
//   gl_Position = proj_view_model * vec4(v0,1);
//   _colorGS = c0;
//   EmitVertex();
//   gl_Position = proj_view_model * vec4(v1,1);
//   _colorGS = c1;
//   EmitVertex();
//   gl_Position = proj_view_model * vec4(v2,1);
//   _colorGS = c2;
//   EmitVertex();
//   EndPrimitive(); 
// }
// void emitColoredQuad(vec3 v0, vec3 v1, vec3 v2, vec3 v3, vec4 c0, vec4 c1, vec4 c2, vec4 c3, mat4 proj_view_model) {
//   //CCW
//   //2   3
//   //  \
//   //0   1
//   emitColoredTri(v0, v1, v2, c0, c1, c2, proj_view_model);
//   emitColoredTri(v1, v3, v2, c1, c3, c2, proj_view_model);
// }
// void emitWideLine_Color_Plane_3D(vec3 p0, vec3 p1, vec3 n, vec4 color, float line_width, mat4 proj_view_model, float left = 1, float right = 1) {
//   //3D (world space) flat line
//   //left/right - emits the wide line to the left, or right based on the left/right parameter
//   //half = 1 if to emit on right and left of line
//   //b0------------b1
//   //^t    /        ^t
//   //| /            |      
//   //a0*__________*a1
//   float lw_l = -line_width * 0.5f * left;
//   float lw_r = line_width * 0.5f * right;
//   vec3 t0 = cross(normalize(p1 - p0), n);

//   vec3 n01 = normalize(p0-p1);
//   vec3 n10 = normalize(p1-p0);

//   vec3 a0 = p0 + t0 * lw_r;
//   vec3 b0 = p0 + t0 * lw_l;
//   vec3 a1 = p1 + t0 * lw_r;
//   vec3 b1 = p1 + t0 * lw_l; 

//   vec4 c1 = color * vec4(1,1,1,0.0);
//   vec4 c0 = color;

//   emitColoredQuad(b0, a0, b1, a1, c1, c0, c1, c0, proj_view_model);
// }

// void emitWideLine_Color_Plane_3D_Tri(vec3 p0, vec3 p1, vec3 p2, vec3 c, vec3 n, vec4 color, float line_width, mat4 proj_view_model) {
//   //3D (world space) flat line
//   //left/right - emits the wide line to the left, or right based on the left/right parameter
//   //half = 1 if to emit on right and left of line
//   //b0------------b1
//   //^t    /        ^t
//   //| /            |      
//   //a0*__________*a1
//   float lw = -line_width *0.5;

//   vec3 t0 = cross(normalize(p1 - p0), n);
//   vec3 a0 = p0;
//   vec3 b0 = p0 + t0 * lw;
//   vec3 a1 = p1;
//   vec3 b1 = p1 + t0 * lw; 

//   //fixing triangle joins: TODO:
//   // vec3 l0a = p0 + (p1 - p0)/2;
//   // vec3 l1a = p0 + (p2 - p0)/2;
//   // float w0a = distance(l1a, l0a);
//   // float wpw0a =  lw/(w0a);//w'/w0

//   // float dp0 = length(c-p0);

//   // vec3 l0b = p1 + (p0 - p1)/2;
//   // vec3 l1b = p1 + (p2 - p1)/2;
//   // float w0b = distance(l1b, l0b);
//   // float wpw0b =  lw/(w0b);//w'/w0

//   // float dp1 = length(c-p1);

//   // vec3 c01 = pointOnLine(p0,p1,c);
//   // vec3 c02 = pointOnLine(p1,p2,c);

//   // //vec3 d = lw/length(c01-c02);
//   // //;
//   // //lw/length(c, c02);

//   // vec3 dv0 = p0 + (c01 - p0) * lw/distance(c01, c) + t0 * lw;
//   // vec3 dv1 = p1 + (c02 - p1) * lw/distance(c02, c) + t0 * lw;// (c - p1) * wpw0b*dp1;

//   vec4 c1 = color;
//   vec4 c0 = color;

//   emitColoredQuad(b0, a0, b1, a1, c1, c0, c1, c0, proj_view_model);
// }


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

  for(int iLight = 0; iLight < DEF_MAX_LIGHTS; iLight++) {
    if(_ufGpuLight[iLight]._enabled==0) {
      continue;
    }
    vec3 vLightPos = _ufGpuLight[iLight]._pos;

    vec3 eye_vector = normalize(_ufGpuCamera._vViewPos - in_vpos);
    vec3 light_vector = normalize(_ufGpuLight[iLight]._pos - in_vpos);
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
    float fFragToLightDistance = distance(_ufGpuLight[iLight]._pos, in_vpos);
    float power = clamp(_ufGpuLight[iLight]._power, 0.000001f, 0.999999f);
    float fQuadraticAttenuation = 1- pow(clamp(fFragToLightDistance/_ufGpuLight[iLight]._radius, 0, 1),(power)/(1-power)); //works well: x^(p/(1-p)) where x is pos/radius

    finalColor += _ufGpuLight[iLight]._color * dot(in_normal , light_vector) *  (diffuse + spec) ;
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

  for(int iLight = 0; iLight <  DEF_MAX_LIGHTS; iLight++) 
  {
    if(_ufGpuLight[iLight]._enabled == 0) 
    {
      continue;
    } 
    vec3 vLightPos = _ufGpuLight[iLight]._pos;
    vec3 vLightColor =  _ufGpuLight[iLight]._color;
    float fLightPower = _ufGpuLight[iLight]._power+100;
    float fLightRadius = _ufGpuLight[iLight]._radius;

    vec3 light_vector;
    float atten=1;
    if(_ufGpuLight[iLight]._isDir == 1) 
    {
      light_vector = -_ufGpuLight[iLight]._dir;    
    }
    else 
    {
      light_vector = normalize(vLightPos - in_vertex);    
      atten = attenuate_light_distance(in_vertex, vLightPos, fLightPower, fLightRadius);
    }


    finalColor += doBlinnPhong(in_vertex, in_albedo.rgb, light_vector, eye_vector, in_normal, vLightColor, atten,  spec_color);
  }

  finalColor += in_albedo.rgb* _ufGpuWorld._vAmbientColor * _ufGpuWorld._fAmbientIntensity;

  return finalColor;
}

#endif//DEF_SHADER_STAGE_FRAGMENT






