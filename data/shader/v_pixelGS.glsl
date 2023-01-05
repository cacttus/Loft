

//GS funcs for drawing pixel/scren stuff

out vec4 _colorGS;
out vec2 _posGS;
out vec2 _blendOriginGS;
out float _blendSizeGS;
out vec3 _outlGS;


void emitPixelLine(vec3 p0, vec3 p1, vec4 c0, vec4 c1, float leftsize, float rightsize, mat4 mvp) 
{
  //Draw line with blend
  //it's applying some perspective distortion via w when we project it back. 

  //left/right size is for drawing mesh e.g., triangles where you want half the line
  //https://stackoverflow.com/questions/60440682/drawing-a-line-in-modern-opengl
  //  p2 b p3    
  //     |     
  //  p0 a p1            
  vec4 la = clip_to_screen(mvp * vec4(p0,1));
  vec4 lb = clip_to_screen(mvp * vec4(p1,1));  

  float lwl = (leftsize ) / 2;
  float lwr = (rightsize) / 2;
  vec2 dlab = normalize(lb.xy-la.xy);
  vec2 n = vec2(-dlab.y, dlab.x);
  vec4 lp[4];
  lp[0] = vec4(la.xy - n * lwl, la.z, la.w); 
  lp[1] = vec4(la.xy + n * lwr, la.z, la.w); 
          
  lp[2] = vec4(lb.xy - n * lwl, lb.z, lb.w); 
  lp[3] = vec4(lb.xy + n * lwr, lb.z, lb.w);     
 
  vec2 pg[4];
  pg[0] = lp[0].xy;
  pg[1] = lp[1].xy;
  pg[2] = lp[2].xy;
  pg[3] = lp[3].xy;

  for(int vi=0; vi<4; vi++)
  {
    lp[vi] = screen_to_clip(lp[vi]);
  }

  _outlGS = vec3(0,0,0);
  _blendSizeGS = lwl;

  _colorGS = c0;
  _blendOriginGS = la.xy;
  _posGS         = pg[0];
  gl_Position    = lp[0];
  EmitVertex(); 
  _colorGS = c0;
  _blendOriginGS = la.xy;
  _posGS         = pg[1];
  gl_Position    = lp[1];
  EmitVertex(); 
  _colorGS = c1;
  _blendOriginGS = lb.xy;
  _posGS         = pg[3];
  gl_Position    = lp[3];
  EmitVertex();  
  EndPrimitive();  

  _colorGS = c0;
  _blendOriginGS = la.xy;
  _posGS         = pg[0];
  gl_Position    = lp[0];
  EmitVertex(); 
  _colorGS = c1;
  _blendOriginGS = lb.xy;
  _posGS         = pg[3];
  gl_Position    = lp[3];
  EmitVertex(); 
  _colorGS = c1;
  _blendOriginGS = lb.xy;
  _posGS         = pg[2];
  gl_Position    = lp[2];
  EmitVertex();  
  EndPrimitive(); 
  
}
void emitPixelPoint(vec3 p0, vec4 c0, float size, vec3 outl, mat4 mvp) 
{
  //  p2   p3 
  //     c--- pointsize/2    
  //  p0   p1 

  vec4 ptclip = mvp * vec4(p0,1);
  vec4 pt = clip_to_screen(ptclip);
  float ps = (size) / 2;
  vec4 lp[4];
  lp[0] = vec4(pt.xy + vec2(-ps,-ps) , pt.z, pt.w); 
  lp[1] = vec4(pt.xy + vec2( ps,-ps) , pt.z, pt.w);         
  lp[2] = vec4(pt.xy + vec2(-ps, ps) , pt.z, pt.w); 
  lp[3] = vec4(pt.xy + vec2( ps, ps) , pt.z, pt.w);   
 
  vec2 pg[4];
  pg[0] = lp[0].xy;
  pg[1] = lp[1].xy;
  pg[2] = lp[2].xy;
  pg[3] = lp[3].xy;

  for(int i=0; i<4; i++)
  {
    lp[i] = screen_to_clip(lp[i]);
  }

  _blendSizeGS = ps;
  _blendOriginGS = pt.xy;
  _colorGS = c0 ;
  _outlGS = outl;

  _posGS        = pg[0];
  gl_Position   = lp[0];
  EmitVertex(); 

  _posGS        = pg[1];
  gl_Position   = lp[1];
  EmitVertex(); 

  _posGS        = pg[3];
  gl_Position   = lp[3];
  EmitVertex();  

  EndPrimitive();  

  _posGS        = pg[0];
  gl_Position   = lp[0];
  EmitVertex(); 
  
  _posGS        = pg[3];
  gl_Position   = lp[3];
  EmitVertex(); 

  _posGS        = pg[2];
  gl_Position   = lp[2];
  EmitVertex();  

  EndPrimitive(); 
  
}
void emitPixelTriangle(vec3 p0, vec3 p1, vec3 p2, vec4 c0, vec4 c1, vec4 c2, mat4 mvp)
{
  vec4 tp0 = mvp * vec4(p0, 1);
  vec4 tp1 = mvp * vec4(p1, 1);
  vec4 tp2 = mvp * vec4(p2, 1);
    
  vec4 ts0 = clip_to_screen(tp0);
  vec4 ts1 = clip_to_screen(tp1);
  vec4 ts2 = clip_to_screen(tp2);

  vec2 c = (ts0.xy + ts1.xy + ts2.xy) / 3;

  _outlGS = vec3(0,0,0);
  _blendSizeGS = 0;

  //disable blend
  _colorGS = c0;
  _blendOriginGS = ts0.xy;
  _posGS         = ts0.xy;
  gl_Position    = tp0;
  EmitVertex(); 

  _colorGS = c1;
  _blendOriginGS = ts1.xy;
  _posGS         = ts1.xy;
  gl_Position    = tp1;
  EmitVertex(); 

  _colorGS = c2;
  _blendOriginGS = ts2.xy;
  _posGS         = ts2.xy;
  gl_Position    = tp2;
  EmitVertex();  
  
  EndPrimitive(); 
}