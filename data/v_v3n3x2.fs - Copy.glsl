#version 400
#define M_PI 3.1415926535897932384626433832795
//#define OREN_NAYAR_DIFFUSE 1
#define PHONG 1
//#define BLINN_PHONG 1
//#define GGX 1
struct GpuLight {
    vec3 _pos;
    float _radius;
    vec3 _color;
    float _power; // This would be the falloff curve  ^x
};

uniform sampler2D _ufTextureId_i0;

in vec3 _vsNormal;
in vec2 _vsTcoords;
in vec3 _vsVertex;

uniform vec3 _ufCamera_Position;
uniform int _ufLightModel_Index;
uniform float _ufLightModel_GGX_X;
uniform float _ufLightModel_GGX_Y;


out vec4 _psColorOut;

void main(void)
{

#define NUM_LIGHTS 2
    GpuLight lights[NUM_LIGHTS];
    lights[0]._pos = vec3(10,10,-10);
    lights[0]._radius = 200.0f;
    lights[0]._color = vec3(1,1,1);
    lights[0]._power = 0.67; // power within radius of light. 1 = is constant, 0.5 linear, 0 would be no light. <0.5 power falloff >0.5 is slow faloff. y=x^(1/p), p=[0,1], p!=0
    lights[1]._pos = _ufCamera_Position;
    lights[1]._radius = 200.0f;
    lights[1]._color = vec3(1,1,1);
    lights[1]._power = 0.63;// power within radius of light. 1 = is constant, 0.5 linear, 0 would be no light. <0.5 power falloff >0.5 is slow faloff. y=x^(1/p), p=[0,1] ,p!=0

    vec4 tex = texture(_ufTextureId_i0, vec2(_vsTcoords));

    vec3 eye = normalize(_ufCamera_Position - _vsVertex); // vec3(0, 10, -10);
    //[Param]
    float rho = 0.17f; //Albedo [0,1], 1 = 100% white, 0 = black body.
    //[Param]
    float E0 = 1f; // Strength [0,1]
    //[Param]
    float fSpecIntensity =.7; //[0,1] // I guess tecnhically speaking these two should be controlled by 'roughness'
    //[Param]
    float fSpecHardness =10; //[1,inf] 0=n

#ifdef OREN_NAYAR_DIFFUSE
    //[Param]
    float sig = .37122f; //Roughness [0,1], 1 = roguh, 0=gloss

    float cos_theta_radiant = dot(eye, _vsNormal);
    float theta_radiant = acos(cos_theta_radiant);
    vec3 projected = normalize(eye - _vsNormal * dot(eye - _vsVertex,_vsNormal));
    float phi_radiant= acos(dot(eye, projected));

    float sig2 = sig*sig;
    float A = 1 - 0.5*((sig2)/(sig2+0.33));
    float B = 0.45*((sig2)/(sig2+0.09));
#endif

    vec3 finalDiffuseColor = vec3(0,0,0);
    vec3 finalSpecColor = vec3(0,0,0);

    for(int i=0; i<NUM_LIGHTS; i++) {
        vec3 lightpos_normal = normalize(lights[i]._pos - _vsVertex);
        float cos_theta_incident = dot(lightpos_normal, _vsNormal);
        float fFragToLightDistance = distance(lights[i]._pos, _vsVertex);

        lights[i]._power = clamp(lights[i]._power, 0.000001f, 0.999999f);

        float fQuadraticAttenuation = 1- pow(clamp(fFragToLightDistance/lights[i]._radius, 0, 1),(lights[i]._power)/(1-lights[i]._power)); //works well: x^(p/(1-p)) where x is pos/radius

#ifdef OREN_NAYAR_DIFFUSE
        float theta_incident = acos(cos_theta_incident);

        //get phi n . p + d
        float phi_incident = acos(dot(lightpos_normal, -projected));

        float alpha = max(theta_incident,theta_radiant);
        float beta = min(theta_incident, theta_radiant);

        float Lr = rho * cos_theta_incident * ( A + (B * max(0, cos(phi_incident - phi_radiant)) * sin(alpha) * tan(beta) )) * E0;
        finalDiffuseColor += lights[i]._color * Lr * fQuadraticAttenuation;
#else
        // Lambert
        float Lr = rho * cos_theta_incident *  E0;
        finalDiffuseColor += lights[i]._color * Lr * fQuadraticAttenuation; 
#endif
        //Phong
        float distribution = 0;
        if(_ufLightModel_Index == 0){
            //Phong
            vec3 vReflect= reflect(-lightpos_normal, _vsNormal);
            distribution = clamp( pow(clamp(dot(vReflect, eye), 0,1), fSpecHardness), 0,1 );
        }
        if(_ufLightModel_Index == 1){
            //Blinn-Phong
            vec3 vReflect = (lightpos_normal+ _vsNormal)*0.5f;
            distribution = clamp( pow(clamp(dot(vReflect, eye), 0,1), fSpecHardness), 0,1 );
        }
        if(_ufLightModel_Index == 2){
            //GGX only
            //https://jcgt.org/published/0007/04/01/paper.pdf
            float bias = (_vsNormal.x*_vsNormal.x)/(_ufLightModel_GGX_X*_ufLightModel_GGX_X) +(_vsNormal.z*_vsNormal.z)/(_ufLightModel_GGX_Y*_ufLightModel_GGX_Y)+_vsNormal.y*_vsNormal.y;
             distribution = 1.0 / (M_PI*_ufLightModel_GGX_X*_ufLightModel_GGX_Y*bias*bias);
        }
        if(_ufLightModel_Index == 3){
            //Smith shadowing model with GGX for microfacet distributions
            //https://jcgt.org/published/0007/04/01/paper.pdf
            float gamma = (-1 + ((_vsNormal.x*_vsNormal.x)*(_ufLightModel_GGX_X*_ufLightModel_GGX_X) +(_vsNormal.z*_vsNormal.z)*(_ufLightModel_GGX_Y*_ufLightModel_GGX_Y))/(_vsNormal.y*_vsNormal.y)) * 0.5f;
             distribution = 1.0 / (1+gamma);
        }
        
        finalSpecColor += lights[i]._color * fSpecIntensity * fQuadraticAttenuation * distribution;// * shadowMul; 
        
    }
    if(_ufLightModel_Index==4){
    //Flat shaidn
        finalDiffuseColor= vec3(1,1,1);
        finalSpecColor = vec3(0,0,0);
        }
    _psColorOut.xyz = finalDiffuseColor *  tex.rgb;// + finalSpecColor;
    _psColorOut.w = 1.0f; //tex.a - but alpha compositing needs to be implemented.
}