#include "v_globals.glsl"

//Undefine to use Square borders
#define BORDER_ANGLE
//Blend the last pixel of the border to make it look less aliased
//#define BORDER_ANTIALIAS
#define ALPHA_CLIP_THRESHOLD 0.001

in vec2 _vert;
flat in vec4 _clip;
in vec2 _tex;
flat in vec2 _texPos;//Origin of the texture coords (top left)
flat in vec2 _texSiz;
flat in uvec2 _pick_color;
flat in vec4 _rtl_rtr;
flat in vec4 _rbr_rbl;
flat in vec4 _rect;
flat in vec4 _border;
flat in uvec4 _borderColor;
flat in float _weight;

vec4 compute_corners(vec2 tl, vec2 tr, vec2 br, vec2 bl, float w2, float h2, vec4 border, vec2 vert)
{
  //Returns vec4(top-left,top-right,bot-right,bot-left) in [0,1] if vertex is within radius

  //radii
  vec2 rtl = _rtl_rtr.xy;
  vec2 rtr = _rtl_rtr.zw;
  vec2 rbr = _rbr_rbl.xy;
  vec2 rbl = _rbr_rbl.zw;
  
  //max radius
  rtl.x = clamp(rtl.x, 0, w2);
  rtl.y = clamp(rtl.y, 0, h2);
  rtr.x = clamp(rtr.x, 0, w2);
  rtr.y = clamp(rtr.y, 0, h2);
  rbr.x = clamp(rbr.x, 0, w2);
  rbr.y = clamp(rbr.y, 0, h2);
  rbl.x = clamp(rbl.x, 0, w2);
  rbl.y = clamp(rbl.y, 0, h2);

  //ellipse center
  vec4 bd = border;
  vec2 ctl = vec2(tl.x + rtl.x + bd.w, tl.y - rtl.y - bd.x);
  vec2 ctr = vec2(tr.x - rtr.x - bd.y, tr.y - rtr.y - bd.x);
  vec2 cbr = vec2(br.x - rbr.x - bd.y, br.y + rbr.y + bd.z);
  vec2 cbl = vec2(bl.x + rbl.x + bd.w, bl.y + rbl.y + bd.z);

  //Ellipse := (x/rx)^2 + (y/ry)^2 = 1
  float etl = pow(clamp(vert.x - ctl.x, -9999999, 0 ) / rtl.x, 2.0) + 
              pow(clamp(vert.y - ctl.y, 0, 9999999  ) / rtl.y, 2.0);
              
  float etr = pow(clamp(vert.x - ctr.x, 0, 9999999  ) / rtr.x, 2.0) +  
              pow(clamp(vert.y - ctr.y, 0, 9999999  ) / rtr.y, 2.0);

  float ebr = pow(clamp(vert.x - cbr.x, 0, 9999999  ) / rbr.x, 2.0) + 
              pow(clamp(vert.y - cbr.y, -9999999, 0 ) / rbr.y, 2.0);

  float ebl = pow(clamp(vert.x - cbl.x, -9999999, 0 ) / rbl.x, 2.0) + 
              pow(clamp(vert.y - cbl.y, -9999999, 0 ) / rbl.y, 2.0);

  return vec4(etl, etr, ebr, ebl);
} 
vec2 perp2d(vec2 v)
{
  // -y,x = clockwise normal, quadrant, 0->1, 1->2, 2->3, 3->0
  return vec2(-v.y,v.x);
}
void main() {
  //Quad info
  vec2 bl = vec2(_rect.x, _rect.y);//"min"
  vec2 tr = vec2(_rect.z, _rect.w);//"max"
  vec2 tl = vec2(bl.x, tr.y);
  vec2 br = vec2(tr.x, bl.y);
  vec2 cxy = bl+(tr-bl) * 0.5; // quad center
  float h2 = abs(tr.y - bl.y) * 0.5;
  float w2 = abs(tr.x - bl.x) * 0.5;

  //Clipping ** Note: discard is importnat so to not write pixels to the pick buffer
  vec4 qcorn = compute_corners(tl, tr, br, bl, w2, h2, vec4(0,0,0,0), _vert);
  bool box_clip = (_vert.x < _clip.x) || (_vert.y < _clip.y) || (_vert.x > _clip.z) || (_vert.y > _clip.w);
  bool rad_clip = (qcorn.x > 1.0) || (qcorn.y>1.0) || (qcorn.z>1.0) || (qcorn.w>1.0);
  if(rad_clip || box_clip) { 
    discard; 
  }

  // Borders
  vec4 color = vec4(1,1,1,1);
  vec4 base_color = uintToVec4(_pick_color.y);
  bool btop   = _vert.y >= (tl.y - _border.x);
  bool bright = _vert.x >= (br.x - _border.y);
  bool bbot   = _vert.y <= (br.y + _border.z);
  bool bleft  = _vert.x <= (tl.x + _border.w);
  
  //inner border radius
  //TODO: hypotenuse length is greater than border size, must scale down radius and position of ellipse to make the inner border nicer
  vec4 bcorn = compute_corners(tl, tr, br, bl, w2, h2, _border, _vert);

#ifdef BORDER_ANGLE
  //Angled Borders
  //border corner vector pointing out
  vec2 v_tl = vec2(-_border.w, _border.x);
  vec2 v_tr = vec2( _border.y, _border.x);
  vec2 v_br = vec2( _border.y,-_border.z);
  vec2 v_bl = vec2(-_border.w,-_border.z);

  float wtl = dot(perp2d(v_tl), (_vert - tl));
  float wtr = dot(perp2d(v_tr), (_vert - tr));
  float wbr = dot(perp2d(v_br), (_vert - br)); 
  float wbl = dot(perp2d(v_bl), (_vert - bl));

  bool ctop   = ( btop   || bcorn.x > 1 || bcorn.y > 1 ) && -wtl > 0 && wtr >= 0;
  bool cright = ( bright || bcorn.y > 1 || bcorn.z > 1 ) && -wtr > 0 && wbr >= 0;
  bool cbot   = ( bbot   || bcorn.z > 1 || bcorn.w > 1 ) && -wbr > 0 && wbl >= 0;
  bool cleft  = ( bleft  || bcorn.w > 1 || bcorn.x > 1 ) && -wbl > 0 && wtl >= 0;

#else
  //Square borders
  //TODO: fix inner border for square borders
  bool ctop   = ( btop  ); // || (bcorn.x > 1 || bcorn.y > 1) 
  bool cright = ( bright); // || (bcorn.y > 1 || bcorn.z > 1) 
  bool cbot   = ( bbot  ); // || (bcorn.z > 1 || bcorn.w > 1) 
  bool cleft  = ( bleft ); // || (bcorn.w > 1 || bcorn.x > 1) 
#endif


  float antialias = 1;
  float bblend = 1;
  vec4 border_color = vec4(1,1,1,1);
  
#ifdef BORDER_ANTIALIAS
  //border is ugly and needs some kind of smoothing
  //TODO: this is not done
  //quad borders first
  bool aatop   = btop   && !bleft && !bright;
  bool aaright = bright && !btop  && !bbot;
  bool aabot   = bbot   && !bleft && !bright;
  bool aaleft  = bleft  && !btop  && !bbot;
  //angle borders
  bool aatl = btop && bleft;
  bool aatr = btop && bright;

  float fac = 1; //factor

  float d = 0;// O[-1..0..1]I
  if(aatop)        { d =  pow( abs((_vert.y - tl.y ) / _border.x) , fac); }
  else if(aaright) { d =  pow( abs((_vert.x - tr.x ) / _border.y) , fac); }
  else if(aabot)   { d =  pow( abs((_vert.y - br.y ) / _border.z) , fac); }
  else if(aaleft)  { d =  pow( abs((_vert.x - bl.x ) / _border.w) , fac); }
  //else if(aatl){  d = -((_vert.y-(tl.y-_border.x)) / _border.x * 2 - 1); }
  
  bblend = 1-clamp(d, 0, 1);//blend content with aa border
  antialias = clamp(1-abs(d*2-1), 0, 1);
  
#endif

  //order to make top/bot above r/l
  if(ctop) {
    border_color =  uintToVec4(_borderColor.x);//t
  }
  else if(cbot) {
    border_color = uintToVec4(_borderColor.z);//b
  }  
  else if(cright) {
    border_color = uintToVec4(_borderColor.y);//r
  }
  else if(cleft) {
    border_color = uintToVec4(_borderColor.w);//l
  }    
  else {
    border_color = base_color;
  }

  //antialias
  border_color.a *= antialias;
  color = (bblend)*border_color  + (1-bblend)*base_color;

  //texture
  vec2 texmod =_texPos + mod(_tex - _texPos, _texSiz); 
  vec4 tx = texture(_ufGpuMaterial_s2Albedo, vec2(texmod));
  color *= tx;

  //font weight
  color = pow(color, vec4(_weight,_weight,_weight,1/_weight));

  if(color.a < ALPHA_CLIP_THRESHOLD) {
  	discard;
  } 
  
  setOutput_Color(color);
  setOutput_Pick(_pick_color.x);
  setOutput_Normal(vec4(0,1,0,1));
  setOutput_Position(vec4(0,0,0,1));

}
