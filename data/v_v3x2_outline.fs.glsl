#include "v_globals.glsl"

in vec2 _tcoordOut;

float toGray(vec3 pix)
{
    return (11.0f * pix.r + 16.0f * pix.g + 5.0f * pix.b) / 32.0f;
}

vec4 do_edge()
{
  float k_edge_33[3][3] = {
   {-1,-1,-1}, 
   {-1, 8,-1}, 
   {-1,-1,-1}};

  int ksize = 3;
  int koff = ((ksize-1)/2);

  vec4 pick_color = vec4(1,1,1,1);
  vec4 sel_color = vec4(0,1,1,1);
  vec4 act_color = vec4(1,1,1,1);//active

  float outline_strength = 1.45f;//increase/decrease strength

  vec4 outpix = vec4(0,0,0,0);
  for (int yi=0; yi<ksize; yi++) 
  {
    for (int xi=0; xi<ksize; xi++) 
    { 
      uint id = textureOffset(_ufMRT_Pick, vec2(_tcoordOut), ivec2(xi-koff, yi-koff)).r;//getInput_Pick(_tcoordOut);// texture(_ufGpuMaterial_s2Pick, vec2(_tcoordOut)).r;
      vec4 val = (isActive(id) ? act_color : isSelected(id) ? sel_color : isPicked(id) ? pick_color : vec4(0,0,0,0)) * outline_strength;
    
      outpix += val * k_edge_33[yi][xi];
    }
  }

  return outpix;
}

void main()
{
  //binding 0 = color
  //binding 1 = pick
  vec4 edge = do_edge();

  //vec4 cin = textureOffset(_ufGpuMaterial_s2Albedo, vec2(_tcoordOut), ivec2(0,0)).rgba;
  //vec4 final = clamp(cin + vec4(edge,0), 0, 1);

	setOutput_Color(edge);
}
