#include "v_globals.glsl"

in vec2 _tcoordOut;

float toGray(vec3 pix)
{
    return (11.0f * pix.r + 16.0f * pix.g + 5.0f * pix.b) / 32.0f;
}

vec4 outline_edge()
{
  int k_edge_33[3][3] = {
   {-1,-1,-1}, 
   {-1, 8,-1}, 
   {-1,-1,-1}};

  int ksize = 3;
  int koff = ((ksize-1)/2);
  vec4 pick_color = vec4(1,1,1,1);
  vec4 sel_color = vec4(1,1,1,1);
  vec4 act_color = vec4(0,1,1,1);
  float act =0;
  float sel =0;
  float pick =0;
  for (int yi=0; yi<ksize; yi++) 
  {
    for (int xi=0; xi<ksize; xi++)  
    {  
      uint id = textureOffset(_ufMRT_Pick, vec2(_tcoordOut), ivec2(xi-koff, yi-koff)).r;
      act += k_edge_33[yi][xi] *  (isActive(id) ? 1 : 0);
      sel += k_edge_33[yi][xi] *  (isSelected(id) ? 1 : 0);
      pick += k_edge_33[yi][xi] * (isPicked(id) ? 1 : 0);
    }
  }
  float thresh=0.3;
  vec4 color = vec4(0,0,0,0);
  if((pick)>thresh) { color = pick_color;}
  else if((act)>thresh){ color = act_color;}
  else if((sel)>thresh){ color = sel_color;}

  color=clamp(color,0,1);
  if(color.a>0){color.a=1;}
  vec4 outpix = color*2;

  return outpix;
}

void main()
{
  vec4 edge = outline_edge();
	setOutput_Color(edge);
}
