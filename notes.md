


Working On:

Bugs:
  * CRITICAL: slowdown issue - fps slows down .. some kind of serious issue
  * Gear / objects dont rotate correctly
  * Fix current active view for moving objects (mouse movement and editing)
  * HUGE ISSUE with PickId - Picker is not shared (individual pick Ids per RenderView), 
    but Meshes (which have PickID are shared)
    either tie pickId to a RenderView, or, use a global pick id for all views.
    I like the first one.  (fixed?)

Roadmap:
* World Model phase 1 - columns
    globs are not auto generated instead we load them from file
    x generate flat glob
    x generate boring columns from data
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
* Dagland Slums City
    Electro Character
    Katrina Character
    Ozman Character
    Don  Character
    MNTeck Warrior Enemy
    Turtbot Enemy
    Dozan Thug Enemy
    Dozan Archer Enemy
    Cockroaches (effect)
    Ooze barrel (chest)
    Shanty houses
    Huge wall
    "The substance"
    The Don's house Area
    Electro's house Area
    Benedict Manor Mainway Area
    The Benedict Manor Safe Area
    Dagland Slums Area
    Ozan River Bay Area
    Black Leech Mines Area
* Outer Moraine Country
    Outer Dozan Forest Area
    Halfway House Area
    Old Knight Character


TODO list
* Timed reference deletion << ?
* CS Scripts (use msbulid to build external script to DLL) (fun!)
* Dir lights
* BR_DYNAMIC_GLOB_LOADING Dynamic Glob loading from world file
* Beam / BeamEdge /BeamVert as structs
  doing this will compact data by 400%
  remove the ptr's on each class 
* MegaTex Margins for prevent bleed
  Linear filtering does not work without some kind of margins.



* Fix vertex types**
    Dont use class name ,instead use reflection and generate the vertex type based on teh sequential order of the eles
      vec3 _v
      vec2 _x -> v301, x201
    Then, we can replace the shaders with <VertexType> so that it will populate the shader automatically.
    It would be nice if we could just use variables in the shader, THEN create ourselves the vertex types based on what was used. That is kind of overkill.
* Menus
* Scrollbar for props window
* Character model





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
********************88 More junk about resource manager ... ignore..
* testing out deleteing objects (wiht F2)
  testing out the new history / action system

* naming datasource things
  1 apply filename prefix
  2 custom space for datasource names
  3
  So we can just add a reference like this:
**  DataSourceReference **
      string _sourceName - name inside of datasource file.
  Then we rename the loaded data object when it is loaded.

  MeshData: "mesh-1"
    DataSource: Gear.glb, DataSourceReferenceName: "mesh", DataSourceReferenceType: MeshData

  if the datasource is newer, then we remove the datasource.

  Another option: 
    Since datasource is not considered to be editable unless cloned, we can just make new loaded names each time.
    And don't add them to the resourcemanager
    Instead add a "DataSource" type to the resource manager and just that name in there.
    We don't make unique name/etc for data source trees.


* somethign gains an uid / uname when it is attached to the scene
  we must recursively give ID & unique name to things when attached to scne.
    * doesnt work for datasources - datasource trees does not get resourced - they are attached to a DataSource

  //the internatl hting is a problem because for example gltf can return multipel objects and cnat be internal to one object.
  //
  //one file can return multiple resources\
  //
  //all datablocks are serializable to binary data, but if they have af ile, we can prefer to read from the file.
  //Everything roots back to WorldObject so don't worry about misc nodes.
  //image - hasFile..
  //tex2d - I still like the idea of storing an image or imagegenerator, and based on what it is - we get, either.
  //wo - 
  //var wo = ImportModel()
  //var t = ImportMaterial()
  //
  //so avoid mujltiple filie probelm
  // do away with GLTF file .. and just load data manually. kind of lame.
  //
  // Loader { File, Generated, Raw }
  // MeshDataLoader { }
  // EllipsoidMeshLoader : MeshDataLoader{ vec3 radius }
  //    Primitive 
  //
  //
  // ** So the root world object node must have transient children..
  //    this way we can reload the file as much as we want and not cause conflicts.
  //    DataSource for WorldObject is a GLTF file.. right..
  // what if we modify an object?? - well we have to specify
  // - object is a raw saved object
  //  - object is a Gltf object
  //  - for simplicity

DataFile : Resource -> <string,object> [string_data_id, file_data ]
    LastUpdated // last time we updated the data generated from this file.
    Load()->
      Load file, and create list of string -> data map.
   Cache the loaded file in the system.

DataBlock
   DataFile? DataFile {get;set;} << setting to null unlinks the object and allows it to modifyh
   *CanModify = False - Flag must be set to prevent modification to File-Dependent objects.
   UnlinkFromFile(){
    CanModify = true; DataFile = null;  
   }
   
WorldObject : Resource
   DataFile? gltf - if this is set, this WO is the ROOT of the GLTF file.
     if no DataFile - then we deserialize all children.
     if dataFile -> then we do not serialize children.
   serializes its obj data and refs, not hard
   
Tex2d : Resource
   basic props
   <Img32> 
     s/d
     just write the image ref
     datafile not used
   
Img32
   GenMode { raw, gen..x, gen..y}
     if DataFile - use file
     if raw -> serialzie to png where we stand.
     if gen -> serialize gen params.

Material
  <Tex2d> 
   deserialize/serial
     write data
     write refs, easy
     datafile not used

MeshData
   VertexType
   MeshGenerator Gen -> ellipsidLoader..boxLoader..sphereLoader..
   DataFile? File -> Obj file, Could be GLTF file too.
   serialize
     Xget verts from GPU <<< This is very unlikely, we'll be mostly generating simple shapes, or using files.
     Xcompress?
     Xserialize verts

 Shader
    Serializes file names - easy

 meh.. ImportModel.. this is to complex.
to convert files we can create a new namespace and copy teh clases, then map the fields. ALso store the field name when serializing.



Img32     ^
 ^        ^
 Loader   ^
          Texture
            ^ loader

3 cases of resource cration
 1 - call constructor Texture(name,..).. always makes a new resource by name
 2 - load from disk via serialization, then call protected constructor Texture(){} the UniqueID will already exist on the DataBlock
 3 - load default resource, this requires a loader, because it uses a resourceDescriptor

automatic resources are  problem because we ahve lot so f"temps" that we dont want stored
  but Datablock->createResource creates the resource automatically.
what to do?

Orphaned/Temp resource nodes
  DataBlock creates a new temporary resource node "dummy" that returns itself.
  The Resource node property is set to "Temporary" - temp variable or "transient.
  when out of scope, we just delete it. 
  * otherwise, when we call Save() the resource gets saved.
  * no need for unique names in transient resources
  * no need for unique ID's
  ONLY IF SAVED

So how to save resources that we *need* ?

Gu.Library.ImportModel();
Gu.LIbrary.LoadModel();

We only save a resource hierarchy when attached to a world object that is  persistent

Everything else must work like a temporary class and the interface is the same
Material mat = new Mateiral() // temporary material
WorldObject wo = new WorldObject("dad") // temporary wo
wo.Material = mat
Gu.World.AddObject(wo) // Persistent WO and Material now

TRANSIENT cached loading though
  Shader/Texture2D - We need to re-use these, but they may be transient...
  Model - Same thing .. may be transient, or not.
  Img32 - typically goes away when texture created -- we can unload it

Ok, so like blender, Lazy Loader
  We load a cached resource
    Only Un-Load the resource if done manually.. otherwise keep in memory
      ResourceManager.UnloadResource(rsc)
    If Texture2D - we can unload the image.
  After the application closes:
    All unused resources are not serialized and will be lost.

  Gu.Resources.LoadModel() //same exact call
  World.attach.. attach objs. 

  This way we can even pre-load all resources at startup

  SO We only attach a non-file resource to the resouce manager when we save.
  File resources /gltf/image/ are automatically reside in memory, unless specified by the user
    Unload(), or parameter, Persistent=false
  When seaving we clean up the resources that are unused.

  DataBlock does not create resource, it creates a dummy resourcenode for itself
  We create file resource when we call ResourceManger.Load*, it is temporary and gets destroyed if th world is nt referncihn tgit

  Gu.Library.LoadImage
  Gu.Library.LoadModel -> id
  WorldObject
    LoadModel(id) << LoadModel must allow us not to return WorldObject(s) if not needed, just MeshData/Texure/mat..

ALL world objects, dont worrya gbout particles or something they're probably goin to be their own busines nayway

most Gu.Load* functions should be the same

Resources are specified by unique name, not file.\
doign away with the parameters




worldobject is not a datablock. it uses mesh/material datablocks, we should change it to be more of an instance.

shit..idk why not.. just serialize the whole friggin library into a file, then we can easily load shit without having to process it....hard
  MeshData . serialize all the data
  Shader . serialize jsut the files
  AnimationData . keys .. serializer already written
  Texture .. serialize pixels 32bit .. but would need to be .png .. which .. makes no sense to do.. unless it is generated

easy:text, med: generated text, hard: save everything
  ^ a simple text file wont work for generated data, aka, worldobject
    probably not possible to do it the way we are currently AND have the editable world I am thinking of, 
      because we are going to change a lot of stuff on the imported .glb
    NO this is bunk - just use the text file, and, we can save WorldObject as normal objects in th worldfile
      worldobject is not supposed to be some kind of archetype, it is an instance that links data.
      Blender is the main editor for object details
    ** all world objects are generated in the world, loaded from .glb, or created with script, ex. particles - why save all of them as resources? they must be small instances

ResourceFile -> ResourceManager
  load resources file
    filename -> id, name (generated) (datablock), rendered image thumb
  
    IF we are going to do the asset-file, else, we can use a lightweight file.
      check resource exists on disk
        does not exist? Prompt for the resource -> Ignore / (load empty resource) -> Change filename (probably changed filename)
    
    or, we can manually do this "mymesh.glb" = "mymesh",0..
      could even be an enum.
      
      ObjectType { @Shader, @MeshData, @WorldObject, }
      
      struct DataReference { ObjecType enum, int Id } //must not be class
        ^ this must also work for WorldObject / Parents
      
      class Resource { List<FileLoc> files, object? obj }
        ^ how to make a WorldObject into a resource, when we do not load it from file?
          ^ we DO load it from file, from worldfile
            WorldObject needs to be moved to ResourceManager so we can resolve references
            BUT it must be saved.. so here is another problem.. we do not have a unified resource file.
            Objects in a world must be clones of their Resource WorlDobject - like the archetype system before, but with Clone() being hte way to do it
      
      ResourceManager List<DataReference, FileResource> files

     summary:basically all we need is a hard coded ID (ulong) i.e., an enum to --> List of FileLoc<> it can be hard coded, like the tiles.

     **1 ok, so generalyl what we are saying is we want to make the Tile map more generic and include world objects and datablocks

Weak Instance - just a matrix -> shares animation
Strong Instance - WorldObject + components -> animates independently
Clone - worldobject + components + mesh + material -> animates independently, and, can change mesh/material without affecting others


  get resource (filename)
    load resource <filename without path> -> datablock id, name, type
      look up resource in table, 
        exists? -> return table->resource , else make new resource
        save resource file

serialization
basic overlay class for 
  object outline
  object origin
then
  InputState/ObjectSleector to select  glob v/e/f
then
  glob -> pick v/e/f via selection
then
  fix UI -> 
    add v/e/f button, 
    "linked " button
    overlay dropdown
    "show object origin button / checkbox
      UI -> Checkbox
        UI -> Menus
then
  overlay for v/e/f

  world save - requires some kind of world edit to test.

  Gui Menus
    Move events from style to UiElement
    We need to put the contextmenu on the Gui2d, there is no other way, other than 
    destroying the current order of the UI
    automatic PickRoot 
