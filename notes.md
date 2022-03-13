

Issues right now Physics
  * fix onground - gravity shouldn't be simulating every step
  * point / line collisions on the box - these are incorrect and the normal returned from Normal() is incorrect

TODO 
  * TODO: fix glob gen visibility issue
  * TODO: change everyhting to double in the collision and velocity. cast to float for position
    the gravity vector is causing the slide vector to have value this is due to the margin vector not beingh tatnken into accutnt
  * seaweed does not show we're going to have to figure out how to have water at a block at the same time as "stuff"
  * The light alg isn't working yet. X only too. But looks promising
  * TODO: Cull solid globs.
  * Optimize block data in dromes - remove empty data or compress it
  * Fix lighting
  * Stitch Globs (neighbor mofidied block )
  * Optimize modified block to include 6 neighbors only
  * TODO: multiple index buffers (with prim type) per mesh. - currently rendering 2 meshes opaque and transparent
  * Put the Sky back
  * Environment Map

Way Later  / Never

  * noise - biome - chebyshev voronoi - later
  * Real materials / closure
  * Mesh pools for globs (and .. everything)

* Roadmap
  * Goal is minecraft looking thing - must have
    * voxel world with textures
    * grass, trees, top bot textures
    * islands
    * water
    * be able to move around, jump (physics)

struct Closure {
#ifdef VOLUMETRICS
  Vec3f absorption;
  Vec3f scatter;
  Vec3f emission;
  float anisotropy;

#else /* SURFACE */
  Vec3f radiance;
  Vec3f transmittance;
  float holdout;
  Vec4f ssr_data;
  Vec2f ssr_normal;
  int flag;
#  ifdef USE_SSS
  Vec3f sss_irradiance;
  Vec3f sss_albedo;
  float sss_radius;
#  endif

#endif
};

 
 