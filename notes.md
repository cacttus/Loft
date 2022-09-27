Working On:
  Got base data model, and quantitization, and the accelleration structures down, 
  generate with height map &c
  Next up: linked vertexes!!!!!!!!!!!!!!!!!!!!

load drome
..for ..topo .. 
load drome array (C27)


  goals today
  basic topo. no uvs (maybe).
  linked verts for hills &c

  real topology
    we're putting block y back, reason is, this makes it easier to cull faces if we segment them (and for side indent effect), however
    the actual height of an edge is a real number, but we quantitize it in order to get exact edge values.
    Edit History



  Gui Menus
    Move events from style to UiElement
    We need to put the contextmenu on the Gui2d, there is no other way, other than 
    destroying the current order of the UI
    automatic PickRoot 

Bugs:
  * CRITICAL: slowdown issue - fps slows down .. some kind of serious issue
  * Gear / objects dont rotate correctly
  * Face normals invalid ..in the v_normals.gs
  * Fix current active view for moving objects (mouse movement and editing)
  * HUGE ISSUE with PickId - Picker is not shared (individual pick Ids per RenderView), 
    but Meshes (which have PickID are shared)
    either tie pickId to a RenderView, or, use a global pick id for all views.
    I like the first one. 

Done:
    WorldInfo
    Object movement
    Global edit state
    Wrap mouse when moving objects.
    SSBO's
    Face normals - not working correctly.
    WorldLoader, WorldFile

Roadmap:
* World Model phase 1 - columns
    globs are not auto generated instead we load them from file
    x generate flat glob
    generate boring columns from data
    save / load world
* Edit world phase 1
    Edit Mode -> World  (global edit mode, world, vs object)
      1, 2, 3 = edge, vert, face (blender) <Change viewport overlay based on modeee
    Pick glob at y = 0
    Fix menu
    Click to generate Flat Glob
    Edit Overlay
      Highlight for glob / clicking / picking
    Edit Actions
      Click to select vertex, edge, or face < requires Edit Overlay
      Show selected v,e,f (object halos)
      If edge 
        Elick + drag to move v,e,f
        Edge snapping button  < Edit Overlay>
          Edge snapping icon
    weld edge
      World Edit Mode, select 2 edges with Shift, -> w 

* Character model
    Test load full character (possibly, one we made, like electro)
    Skin GPU BUffer
    Weights GPU Buffer
    FIgure out how to make Characters Instances
* In-Game / Real-Time Cinematic system (IGC), or Acting System, or Play system (not pre-rendered)
    Paths
      Either
        Edit mode -> Path, or
        select object -> Path Dialog -> Edit Path, Add Path < requires Combo Box, Window
      click to edit path. Add path point.
    Cinematic Script System
      -- Basically take the other systems and kind of drag and drop what they can do.
      Actions needed:
      1 go to new world
      1 click character -> click to make path -> save path (in order) < Path System
      2 play object / character animation / open door animation, etc
      3 set character to hold object < Requires Character Hot Spots
      5 character "look at someone's face" (for dialog / interaction)
      6 Show game dialog box
* Edit World - 2
    Top Texture
    Side Textures 
    bot texture
    Door Hole (maybe, check the design)
* World model - 2
    stitch neighbor globs
    load different world (go to world)
    Culling
    async topology
    catmull clark
      edge weights (click edge, drag for weight)
    top style -> 
      overhang / bevel / flat
* World model - 3
    wall grid
    click on ground to place wall
      ground must highlight < requires Edit Overlay 
    click top of wall to build wall on top of another wall.
    separate mesh for walls (terrain_op, terrain_tp, wall_op)
* Object placer
    object selection window
    object screenshot
    click to place object in world
    save object on world data file
* Particles object < requires Object Placer
    < window 
    save particle effect to file
* Collisions / Physics
    Get this down
* Birds & Bugs < depends on particles / path
* Dagland Slums
    Electro, Katrina, Ozman, Don, 3-4 bad guys
    Cockroaches effect
    oozey barrel (chest)
    shanty house 
    huge wall
    The substance.
    The Don's house
    Electro's house
    The Benedict Manor Safe


TODO list
* Beam / BeamEdge /BeamVert as structs
  doing this will compact data by 400%
  remove the ptr's on each class, and remove 
* MegaTex Margins for prevent bleed
  Linear filtering does not work without some kind of margins.
* Automatic FBO Outputs for Shaders, changing FBO state.
  There are many instances in shaders where we don't need to uoutput pick/plane..
  Something like <OUTPUT_COLOR> etc. or just parse setOutput_Color to know that we are outputting to the color Target
  This will automatically enable/disable framebuffer targets (FBO's actually), and create FBO's for new targets. 

* Global ID generator, which gets saved
  We will end up overwriting ID's 
    Otherwise we can use a slot map, or other sturcture and get rid of the id counter
* Fix vertex types**
    Dont use class name ,instead use reflection and generate the vertex type based on teh sequential order of the eles
      vec3 _v
      vec2 _x -> v301, x201
    Then, we can replace the shaders with <VertexType> so that it will populate the shader automatically.
    It would be nice if we could just use variables in the shader, THEN create ourselves the vertex types based on what was used. That is kind of overkill.
* Key combos / global input
* Menus
* Selected object halo
* Scrollbar for props window
* World model 
* Aliased renderer
* Character model
* CSS hotload
* Edit History
        IEditHistoryData {
          abstract void PerformUndo();
        }
        GlobEdits { 
          Dictionary<object values, box3i value range> Edits 
        }
        GlobsEditHistoryData : IEditHistoryData{ 
          //Note this is just one edit in hisotry - one action
          Dictionary<Glob, GlobEdits> // multiple edits for each glob
            override void PerformUndo(){ .. }
        }
        var d = new GlobsEditHistoryData() ...
        EditHistory.Push( d)
          // erase history after current history index.
        
        d = EditHistory.Previous();  //decrement history
        d.PerformUndo();






edge vertex heights = height face height - less flexibility but way easier to code
different - more difficult, could be several poly configurations on each face, 
further we are going to allow for extruded height faces, and, the tesselation factor, 
  ^ this is the problem. huge difficiutly
however, tesselation should come after we generate the base mesh, as we have all the edge info already

1 check each 3-polyside for occlusion
  check entire side
    check each height face

foreach height face |--| e01 e23

check both edges to see if face is occluded by neighbor glob
if not - draw face.


we need to rethink this, if we are going to inset the faces.. then we need a voxel data model..
i dont want voxel because its just too data heavy, but for a custom build, it's not too bad, idk..
plus we can't do "basic" voxel with sloped terrain.
columns are very data sparse, but we need to have these inset sides or else the thing won't look good
POSSIBLY - make the columns small, then, we can have a 3-grid and inset columns that way. this would be
way easier, and also possibly data sparse: picture
columns:
123
---  < inset is actually just another column.
\ /
 | 
 |  <center column is "base" column. Sort of like a 9-patch , or other RPG type thing.
 |   


So here we go: we have 4 columns at center as the larger "base tile" then the "inset edges" would be 12 more columns
----
\  /
 || 
 ||  <looks like an actual eroded cliff.
/  \ 

Let's examine how much data we save / footprint. Each column is about 
 Each column is about 36 bytes without vptr (if we go back to structs) 
 That ends up being 12 edge columns for the inset + 4 columns for the center = 16 columns ~600 bytes for a single RPG tile.
 Voxels  will grow with height, and require more data = 6 faces * 2 short tileids per face, flags for each face / edge / vert,
  also we would need short values for each vtx, or a really nasty byte 
  (2+2+1)* 8 + (2*12) + (12*2) = 89 bytes, almost 3x the data, and that is just one voxel, then we end up stacking those up with EQUIVALENT height for one column and we have
  (89 * 16) * h >> (36*16) * 1

for entire face - divide e01 and e12 by occluded neighbor

**********************MERGE COLUMS / DIV COLUMNS
So instead of a fixed grid we can merge the column for less data. Assuming 
This would be pretty easy to add We would simply 
We could also have several "levels' of column size, and have a"min size"
\
  \
    \
      \ 4 aggregate columns treated as one column.
-------
[
  **NOTE this is the DATA MODEL only - we actually topologize the entire top cap / side as separate columns to create a uniform topology
 ??? What is the purpose? It would make collision detection faster, possibly, depending on how close we want to get to the smoothing factor.
]
Globs would be Uniform, but columns (data) would not.
** Honestly it's not a huge deal, column data isn't htat big anyway. ** 

float MinColumnSizeX // the smallest column we can have.
float MinColumnSizeY
float MinColumnSizeZ
..or int MaxColumnDivisionCount

** The default column size will be the default quad/triangle/topo size as well (an entire GLOB will not be merged into one column)
** This is necessary because when we subdiv the topology there needs to be some uniformity, or it will look bad
float DefaultColumnSizeX // some reasonable value.
float DefaultColumnSizeY
float DefaultColumnSizeZ

Issue though is that without uniformity we may have smoothing issues. 
  1 separate subdiv levels do not smooth into higher levels,but into themselves
    if this.. separate subdiv levels are treatd as separate meshes, as our subdiv will work on mesh "patches" generally
  2 divide all higher divs (no, no no)
  
Likely we'll have just a few divisions, if we decide to do this.. but it does kind of flow naturally with what I'm thinking here.

  **topology / occlusion /overdraw is also a problem, because the topology is not uniform, how do i cull a sub-dived tile on th side of a larger one?
    then we get complex, and that's very complex.

honestly I don't think all this is necessary for Loft world, but I may put it in later.

  for each x->z
    col = (x+1, z+1)
    if col->isLinkedToThisCol()
    thiscol->area2d += col->area2d
  
  this will help for things like Walls which will have lots of unused column data.
  question is then, what kind of data structure do you use for htis? a kind of tree?
  
     i have a fixed grid of blocks
     all the blocks are rectangular, not odd blocks
     what is the tree algorithm for this?  
     ******we could simply have a map of beamid to "beam block" could be a large map though. ** Note:
        ***Note: beam blocks are 3D, not just the column grid, but actual beams
     Otherwise spend time to create a BSP tree.
      there may not be a binary algorithm, - possibly n-ary, very unlikely there is a binary algorithm actually
        possibly "cyclic" BSP tree 
          some blocks may be at multiple leaf nodes, so there would be cycles in the tree
        I am not a fan.
      
      ***Note** this is basically the reverse image placement algorithm.****
        
        Maybe just treat all beams as beam-blocks and modify connected beams with edge vert flag. Sounds legit.

      again note the realy reason for this is not for topology or culling,
       but for data compaction and possibly physics, assuming we're going to do something like that
      this is really an optimization thing
    

***** WALLS AS COLUMNS
So we are going to do walls, perhaps we can just use the columns for the walls as well? 
Perhaps dividing each "RPG tile" into multiple "columns" we can extend some of those columns into a wall?
Well, IDK. for now Walls are a separate mesh.


It would be interesting to have columns that aren't square.. then we could merge the two - walls + columns

****FUNCTIONAL SIZE CAN MAKE WALLS AS COLUMNS EASIER
We could also use the "functional column size" to do this This would create a grid with varying column topology sizes. 
Or.. yet again, make the columns small and just "paint" the walls differnetly.
I like this approach. easy, easy.
|x|x|x|x < regular grid
|xx|x|xx|x|xx|x < xx=face, x=wall
  
 o
 Y  < a character will take up 4 columns 2x2
 ^
--

----  < an RPG tile will have an "inset" for the cliff. This is sconfigurable, and we don't even "need" an inset.
\  /

****** VERTEX MERGE CAN BE ON FACE / CLIFF
Consider "outcrops"
----
   |
   \__ < this would need to (optionally) merge into the cliff. it is not a separate tile.
_____/
    

Final verdict Is:
1 Don't use functional column sisze , neat idea, there is no need though
2 Don't use Div columns (smaller triangles) that would just get to be a mess and it is not necessary.
3 We do want to use merge columns because collisions are going to be a bitch to code and we want very little data to play with.
  Do not implement merge columns right away, this is enhancement for later, it will be pretty easy to do once the base column topo is working
  We will implement Merge Columns by finding the square area on the column grid. This is easy to do.

Verdict:
  So we are just going to implement side occlusion as usual - no insets, just cull planar faces


So another issue - smoothing topology 
