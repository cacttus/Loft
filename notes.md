
So
* right/left etc of uielement are CHANGED while we mess weith them.. this is a problem.
characters have a fixed width/height - this must be set. - so we must account for this.
The actual visible quad is getting messed up - teh visible quad must not be altered (left/right/top/bot)
we might just have Width/Height be not changeable, but we can change left/right.etc
* also the bearing/adv wiedth in MtFont must be fixed. 
* We must allow for NEGATIVE paddings because character can be above each otehr.

UIScreen ** 
so we're goint o have multiple of them 
for whatever
but you can just use the same system
adn we just give the shader a normal and say that is the oritnetaiton of the thing
and so the vertes are still in 2d coorintsates

question

orgainzation of woerldobject and uiscreen

one mesh per uiscreen

megatex ? - one per or .. for all ? 

how bouat gameobject ?

uiscreen is really a gameobject so .
how to do that, idk.

gui - generic class to hold fonts?
then GUI needs to hold the MegaTex if that's the case. All fonts will be on there. 


* gui
* topo - build on top of World.cs
* get renderer to alias
* variable block heights for different world areas
* Rename world to WorldArea, we are not infinite.
* Remove generation code.
* So, keeping dromes, i think the reason for dromes was that Glob files were too many and it was slow.


UC
click "generate land"
create a flat land
click subdiv button (toggle)
land shows with subdiv
clidk again -> land shows without subdiv

UC
click block face
block is placed (this is already hadndled in world)



Converters
std\:\:shared_ptr<([a-zA-Z0-9_]+)>    $1
uint32_t                              uint
int32_t                               int
const string_t&                       string

transmission grid for block face
per face -> point
geom 

glob - stores transmission grid
update -> light changes -> update globs -> update light for globs via transmission grid

Quickies
  * TODO
    Replace Block.Is... with BlockTile.Opacity. this is how lighting is calculated

  * transparency to look good
    buffers

  * finish ui add minimal ui
    some debug text .. 
    ui at bottom


Issues right now Physics
  * box normal  STILL wrong -picking will place block inside of lbock sometimes
  * fix onground - gravity shouldn't be simulating every step
  * point / line collisions on the box - these are incorrect, the normal returned from Normal() is incorrect and has us sliding at an angle on the point or the edge
    * if point / edge .. select normal based on velocity - possibly
  * ellipsoid ..
  * lower the terminal velocity
  * lower the gravity, too fast
  * FIX STUPID VORBIS AUDIO SEGFAULTS

TODO 
  * TODO: fix glob gen visibility issue
  * TODO: change everyhting to double in the collision and velocity. cast to float for position
    the gravity vector is causing the slide vector to have value this is due to the margin vector not beingh tatnken into accutnt
  * seaweed does not show we're going to have to figure out how to have water at a block at the same time as "stuff"
  * water .. transparency .. 
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
    * water (transparent water)
    * sky
    * be able to move around, jump (physics)
