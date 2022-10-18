//collide 2 objects SAT

layout(binding=0, location = 0)in vec3 _v301;
layout(binding=1, location = 0)in vec3 _v302;

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer out_sum { vec3 _ssOutSum[]; };

void main(void)
{
  _ssOutSum[local_size_x] = _v301 + v302;
}