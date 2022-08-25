#include "v_global_uniforms.glsl"


//Don't comment this
<GLSL_CONTROL_OUTPUTS_HERE>

// #if defined(DEF_PIPELINE_STAGE_DEFERRED)

// layout(location = 0) out vec4 _mrtOutput_Color;
// layout(location = 1) out uint _mrtOutput_Pick;
// layout(location = 2) out vec4 _mrtOutput_Normal;
// layout(location = 3) out vec4 _mrtOutput_Position;
// layout(location = 4) out vec4 _mrtOutput_Plane;

// void setOutput_Color(in vec4 c)     { _mrtOutput_Color = c;             }
// void setOutput_Pick(in uint p)      { _mrtOutput_Pick = p;              }
// void setOutput_Normal(in vec3 p)    { _mrtOutput_Normal = vec4(p,1);    }
// void setOutput_Position(in vec3 p)  { _mrtOutput_Position = vec4(p,1);  }
// void setOutput_Plane(in vec3 p)     { _mrtOutput_Plane = vec4(p,1);     }

// #elif defined(DEF_PIPELINE_STAGE_FORWARD)

// layout(location = 0) out vec4 _mrtOutput_Color;
// layout(location = 1) out uint _mrtOutput_Pick;

// void setOutput_Color(in vec4 c)     { _mrtOutput_Color = c;             }
// void setOutput_Pick(in uint p)      { _mrtOutput_Pick = p;              }
// void setOutput_Normal(in vec3 p)    {                                   }
// void setOutput_Position(in vec3 p)  {                                   }
// void setOutput_Plane(in vec3 p)     {                                   }


// #elif defined(DEF_PIPELINE_STAGE_DEFERRED_BLIT) || defined(DEF_PIPELINE_STAGE_FORWARD_BLIT)

// layout(location = 0) out vec4 _mrtOutput_Color;
// layout(location = 1) out uint _mrtOutput_Pick;

// void setOutput_Color(in vec4 c)     { _mrtOutput_Color = c;             }
// void setOutput_Pick(in uint p)      { _mrtOutput_Pick = p;              }
// void setOutput_Normal(in vec3 p)    {                                   }
// void setOutput_Position(in vec3 p)  {                                   }
// void setOutput_Plane(in vec3 p)     {                                   }

// #elif defined(DEF_PIPELINE_STAGE_SHADOW_DEPTH)

// layout(location = 0) out vec4 _mrtOutput_Color;

// void setOutput_Color(in vec4 c)     { _mrtOutput_Color = c;             }
// void setOutput_Pick(in uint p)      {                                   }
// void setOutput_Normal(in vec3 p)    {                                   }
// void setOutput_Position(in vec3 p)  {                                   }
// void setOutput_Plane(in vec3 p)     {                                   } }

// #endif

//TODO: depth
