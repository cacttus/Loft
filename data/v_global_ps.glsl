
#define length2(x) dot(x,x)


//20160527
// HDR rendering 
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

// Global deferred MRT targets
layout(location = 0) out vec4 _gPositionOut; 
layout(location = 1) out vec4 _gColorOut;
layout(location = 2) out vec4 _gNormalOut;
layout(location = 3) out vec4 _gPlaneOut;
layout(location = 4) out uint _gPickOut;

//*Pick Id
uniform uint _ufPickId;

#include "df_material.sh"

//void setBloom()
//{
//    //Apply a default bloom on a per color basis/
//    //this is incorrect, but demonstrates global bloom
//    float fBloomLevel = 0.2195;
//    
//    //transform color back to linar space.
//    vec4 tmColor = toneMapLightFinal(_gColorOut);
//    
//    float sum = (tmColor.r + tmColor.g + tmColor.b) / 3.0f;
//    if(sum > fBloomLevel)
//    {
//        _gBloomOut = tmColor;
//        _gBloomOut.a = 1.0f;
//    }
//    else
//    {
//        _gBloomOut = vec4(0,0,0,0);
//    }
//}