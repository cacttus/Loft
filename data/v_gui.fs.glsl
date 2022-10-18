#include "v_globals.glsl"

in vec2 _vert;
flat in vec4 _clip;
in vec2 _tex;
flat in vec2 _texPos;//Origin of the texture coords (top left)
flat in vec2 _texSiz;
flat in uvec2 _pick_color;
flat in vec4 _rtl_rtr;
flat in vec4 _rbr_rbl;
flat in vec4 _rect;
flat in vec4 _border_trbl;

void main(){
  //We flip everything in the VS
  vec2 bl = vec2(_rect.x, _rect.y);//"min"
  vec2 tr = vec2(_rect.z, _rect.w);//"max"
  vec2 tl = vec2(bl.x, tr.y);
  vec2 br = vec2(tr.x, bl.y);

  vec2 cxy = bl+(tr-bl)*0.5;
  float h2 = abs(tr.y - bl.y)/2.0;
  float w2 = abs(tr.x - bl.x)/2.0;

  //Round Corners
  vec2 rtl = _rtl_rtr.xy;
  vec2 rtr = _rtl_rtr.zw;
  vec2 rbr = _rbr_rbl.xy;
  vec2 rbl = _rbr_rbl.zw;
  rtl.x = clamp(rtl.x, 0, w2);
  rtl.y = clamp(rtl.y, 0, h2);
  rtr.x = clamp(rtr.x, 0, w2);
  rtr.y = clamp(rtr.y, 0, h2);
  rbr.x = clamp(rbr.x, 0, w2);
  rbr.y = clamp(rbr.y, 0, h2);
  rbl.x = clamp(rbl.x, 0, w2);
  rbl.y = clamp(rbl.y, 0, h2);
  vec2 ctr = vec2(tr.x - rtr.x, tr.y - rtr.y);
  vec2 cbr = vec2(br.x - rbr.x, br.y + rbr.y);
  vec2 ctl = vec2(tl.x + rtl.x, tl.y - rtl.y);
  vec2 cbl = vec2(bl.x + rbl.x, bl.y + rbl.y);
  float etr = pow( (clamp(_vert.x - ctr.x, 0, 9999999)) / rtr.x, 2.0) +  //Ellipse (x/rx)^2 + (y/ry)^2 = 1
              pow( (clamp(_vert.y - ctr.y, 0, 9999999)) / rtr.y, 2.0);

  float ebr = pow( (clamp(_vert.x - cbr.x, 0, 9999999)) / rbr.x, 2.0) + 
              pow( (clamp(_vert.y - cbr.y, -9999999, 0)) / rbr.y, 2.0);

  float etl = pow( (clamp(_vert.x - ctl.x, -9999999, 0 )) / rtl.x, 2.0) + 
              pow( (clamp(_vert.y - ctl.y, 0, 9999999)) / rtl.y, 2.0);
              
  float ebl = pow( (clamp(_vert.x - cbl.x, -9999999, 0 )) / rbl.x, 2.0) + 
              pow( (clamp(_vert.y - cbl.y, -9999999, 0 )) / rbl.y, 2.0);

  //round corner. check for vertex outside of ellipse boundaries.
  // Kind of ugly. We could enable border smoothing by muting a very small pixel radius.

  if(
    (etr > 1.0) || (ebr>1.0) || (etl>1.0) || (ebl>1.0)
    || _vert.x < _clip.x 
    || _vert.y < _clip.y
    || _vert.x > _clip.z 
    || _vert.y > _clip.w) { 
    discard; 
  }

  //Texture Scaling
  //We need texpos here = p + mod(a-p, siz);
  vec2 texmod;
  texmod.x = _texPos.x + mod(_tex.x - _texPos.x, _texSiz.x);
  texmod.y = _texPos.y + mod(_tex.y - _texPos.y, _texSiz.y);
  
  float r = float((_pick_color.y>>24) & 0xFF) / 255.0;
  float g = float((_pick_color.y>>16) & 0xFF) / 255.0;
  float b = float((_pick_color.y>>8) & 0xFF) / 255.0;
  float a = float((_pick_color.y>>0) & 0xFF) / 255.0;

  vec4 tx = texture(_ufGpuMaterial_s2Albedo, vec2(texmod));
  if(tx.a * a < 0.001) {
  	discard;
  } 
  
  setOutput_Color(tx * vec4(r, g, b, a));
  setOutput_Pick(_pick_color.x);

  //  setOutput_Color(albedo);
  //setOutput_Pick(_vsPick);
  setOutput_Normal(vec4(0,1,0,1));
  setOutput_Position(vec4(0,0,0,1));

}
