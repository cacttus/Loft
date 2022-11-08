
## WORKING ON:
 Transforms
 Animation

## NOTES:
1107
  fixed xforms

1030
  fixed solid shade bug - DrawMode == Deferred
  investigate topology normals error

1026
  Register on scene attach
    
    Worldobject.material = new material(new shader()) // shader will get registered  when OB is attached to scene.
      
    World.AddObject(ob)
    >>
      if(!ob is registered)
        Register(ob) //recursively register all resources
        >>
          Find object by id
            if object id == id, && object == stored object
              dont register - this is an instance

  m = new Material("mat")
  Lib.Add(m)

  wo.Material = Lib.LoadMaterial("mat") << the named loaders are necessary. but we have no need to dupe the object ctors 

  World.AddObject(wo)
    Register(wo)
      find ob by id
        name does not have to equal name if id is unique
          if id==id && name!=name
            ob is an instance of the same data object with new name.
      exist? - instance - no uid, no name
      material

  Load embedded resource anywhere.
  bool Lib.Load(string name, out obj) - load object from existing - no constructor copying.

1023
transofrms
  it is impossible to debug all this without proper visual deubg
    wide lines/oints
    
1022
  Issue: Shortcut labels go outside bounds
  Issue: clipping is not working

  goinhg to remove UISpan - it's causing problems
    element padding passed in to layoutSpan and we add double padding due to htis.

10/21
  must move Camera3D to be main object in scene as it messes up the rotator

  or, just fix mouse rotator to be ON the camera.

10/20


  word wrap, 
  glyph props share/unify, 
  fix align

  prop sharing
    text needs to hide
      but we need to have separate props for text so we get rid of that element crap
        pass in a default glyph style to the layout and vert compute with special props set.
          dont set glyph props

  fix the align text not working.
    fix prop inheritance first - to see text when it hides
    then chekcin and we do word wrap and prop sharing


  word wrap

  fixing PRS edit

  X UI animation rough-in
  X TextAlign / Align buggy

10/19
  animation
  
  X move Window init to world script
  X startup script for world
  X guy test
  X world scripts 
  X dynamic load fix

10/18

  next up 
    GLTF load animation
    skin/bone mats
      array of matrix pallets on instance data (uniform)
    shared skeles'
    test model

  fix glyphs and rendering (mipmapping was enabled on megtexture)

10/17


  We have to sort ui elements
  
  inherited events - subchildren need to fire events - but we should be able to
    override them instead of calling all subclass events

  TODO: if an element is fixed width since max/min wh are used as static WH for fixed elements, the element should set the max/min wh to this value. or something.

  TODO: If a parent element shrinks and a child element expands, 
        then the width of the parent is equal to the content width of all the children

10/16
  added tone mapping 
  fixed more shader hot reload

  todo:
    * gui2d fix
        menu is in wrong place.
        fix sort order - causing overlay to break
          glyphs being on separatre layer is a problem - they dont properly layout. with other elements (ist his a problem?)
            separate array is problem - we then duplicate all algorithms
              `solutin: remove all sorting, put glyphs at the beginning of children and keep count
        word wrap
        put back borders
        generic static layout directions
        fix _layoutchanged being disabled / optimization

    * world actions - xform fix

10/14
  ** fix the sky rendering out of order .. ok

  ** working on fixing rendering system and removing the sloppy lambdas from pipe stages
    created VisibleStuff
      ***REALLY the only blended would be the materials - so if GPU state has blend enabled, then we depth peel them
          otherwise we can just use depth buffer for opaque materials and render any order we want.
          sans culling.

  **UI is messed up - looks bad, cant read text
    LayoutChanged is disabled (performance problem)
    Also no longer works ..
      borders are no longer working.

  **got info window to work (again)
  **added UI-only mode to RenderView to allow us to render just UI
      ** HUGE TODO - prevent re-rendering if UI does not change 
        *(or even tiled rendering)

10/13
  For menus adding RegionMode
  RegionMode -> Contained | Floating 

  Removing Border Quads (too confusing) this will break borders.
    ** border area - I think instead we'll just pass border params into the shader and make 2 quads in there
  Removing Raster Quad (unneeded now)
  We can now remove "Offset Quad" from the glyphs since that was the purpose for floating now controls..

  **Technically we need to replace mesh view everywhere from meshdata
    meshview is required for the Gui to draw a subset of its quads.
    do not create new mesh views when setting Mesh()
    
10/12
  moving gui events to world editor (click)
    gui menus / dropdowns
      gui objects must be able to overlap
      i might want a child element to go above a parent or sibling element

10/11
  gui horizontal/vertical build & statusbar (at bottom)
  fix M/R/S action
  save/load/lib

## Bugs:
  * Topology - bottom topo is incorrect -underneath terrain
  * Topology normals are incorrect - ComputeNormalsTangents - the quad tops / sides verts are not linked
  * Library Dependencies must share pick ID with root node Gear -> root
  * Hide root node default box.
  * CRITICAL: slowdown issue - fps slows down .. some kind of serious issue
  * Gear / objects dont rotate correctly
  * Fix current active view for moving objects (mouse movement and editing)
  * HUGE ISSUE with PickId - Picker is not shared (individual pick Ids per RenderView), 
    but Meshes (which have PickID are shared)
    either tie pickId to a RenderView, or, use a global pick id for all views.
    I like the first one.  (fixed?)

## ROADMAP:
* World Model 1 
  X Columns, menus, basic gen, gen flat, save world
  * Save / load assets (serialize) objects + world (phase 1)
  * Library
* Edit World 1  
    * Pick glob at y = 0
    * Edit Overlay 
      -> Highlight for glob / clicking / picking    
      -> Click to generate Flat Glob in selected area
      -> select vertex, edge, or face ->  #1, 2, 3 = edge, vert, face (blender)
      -> Show selected vef (object halos)
        -> Turn on Edge snapping button  
        -> Edge snapping icon
    * Weld edge
        -> select 2 edges with Shift+w 
* Character model
    * Create test character (electro /blender)
    * Load full character GLTF With anims
      -> Process animations
      -> Skin GPU BUffer
      -> Weights GPU Buffer
    * Character Instances for NPC
* World Controls 
    * NPC Paths
        * Edit mode -> Path
        * Select object -> Path Dialog -> "Edit Path", "Add Path"
          <- Combo Box, Window
        -> Click to edit path. 
        -> Add path point.  
    * Cinematic System / World Activity 
      * Go To World (World Model 2)
      * click character -> click to make path -> save path (in order) < Path System
      * play object / character animation / open door animation, etc
      * set character to hold object < Requires Character Hot Spots
      * character "look at someone's face" (for dialog / interaction)
      * Show game dialog box
* World Model 2
    * Top Texture
    * Side Textures 
    * Bot texture
    * Door Hole (maybe, check the design)
    * Stitch neighbor globs
    * Go To World
    X Culling
    * BACKLOG: Async topology
    * Catmull clark
      -> Editable Edge weights (click edge, drag for weight)
    * Select Top style (overhang / bevel / flat)
* World model - 3
    * Wall grid Topology.
    * Separate mesh for walls (terrain_op, terrain_tp, wall_op)
* Edit World 2
  * Click on ground to place wall
      Highlight Ground 
        <- Edit Overlay 
  * Click top of wall to build wall on top of another wall
  * Dropper
      * Object dropper window
      * Model screenshot
      * Drag & drop
      * Save objects
        <- Serialization
* Particles   
    <- Object Placer
    * Save particles 
    <- Serialization
* Collisions / Physics
    <- Pixvoxel, GJK
* Props 1
  * Electro Character Rough
  * Dagland Slums - Turtbot Enemy
  * Dagland Slums - Dozan Thug Enemy
  * Dagland Slums - Dozan Archer Enemy
  * Katrina Character
* Mechanics 1
  * Building Entrances
    * Sidescroller Camera
  * Character Movement
  * Battle
* Environment 1  
  * Birds & Bugs 
* Props 2
  * Don Character
  * MNTeck Warrior Enemy
* Props 3
  * Cockroaches (effect)
  * Ooze barrel (chest)
  * Shantys
  * Huge wall
* Environment 2
  * Don's house Area
  * Electro house Area
  * Benedict Manor Mainway Area
  * The Benedict Manor Safe Area
* Environment 3
  * Dagland Slums Area
  * Ozan River Bay Area
  * Black Leech Mines Area
* Environment 4
  * Moraine Forest
  * Knight Character
* Prototype Complete


## TODO list:
* add obj hiding to edit history
* Fix implementing "modified" update of all GpuData structures (optimization)
* Fix "modified" in the UI (optimization)
* Fix draw line / point v_line v_point- perspective distortion is causing them to not appear to be pixel lines with fixed width. they must not have perspective.
* Multi Lang - multiple language support
  * See the failed implementation of multilang in the project folder
  * Change MegaTexture to compile any unicode point, 
  * ZH, RSTU
  * For multiple fonts figure out how to deserialize a class from this data
  * UI - RTL text (implemented) - See Translator
* Globs must inherit from world object and have the same edit properties of objects
  Object Mode -> Glob edit mode -> 
* Implement Prop Animation Timer in Gui2D to animate props on a timer, versus, all at once
* Inheriting from parents is very annoying - remove parent inheritance
* XML for the UI - relies on Unified Diff
    * XDocument doc= XDocument.Parse("<e>this is some text<e Position=\"Fixed\" Background=\"image.png\"></e>some more text.<e Position=\"Fixed\">interior element with text</e></e>");
* Unified UI
    * Unify Glyphs + Elements (UiElement._glyphs UiElement._children) and change SlidingDiff to work with all elements
    * Make it work iwth changes to the XDocument DOM
* LaunchFileWithSelectedShaderErrorLine (shaders & Scripts)
    * when an error occurs - launch the file in the editor.
* Do away with generic shader names - files must exist, if they dont exist then it wont be reported if the shader still compiles.
* KeyMap - Read from file.
* Optimized UI-ONLY Window Updates - 
    Prevent rendering to UI-only windows if the UI does not change, do not clear the buffer.

  Sun/Moon and default object creation
* Generic Texture - 
    Refactor the RenderTExture and Depth texture constructors in Texture - make generic, and fit with 2d, 3d, or make a 3d class
* Memory growth bug, FPS drop bug
* Mesh Pools.

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

## DONE:
 
X Put line width / point size on Material (perhaps GPU state, or just a material param)
X Move World Initialization to scripts.
X GLYPHS - replace UiElement with Glyphs for chars
X WORD WRAP - make word wrap work for labels.
X Move the window debug keys (F1..) to KeyMaP
X FIX CS Scripts 
X Dir lights


## DESIGN / BRAIN DUMP
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

..idk why not.. just serialize the whole friggin library into a file, then we can easily load  without having to process it....hard
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



 //Removing grow/shrink - it makes layout harder and html does not do it, instead width=100%
 //position:absolute/relative
 //bottom: set this - top r l - shrink
 //
 // NO i think rels should be fixed size. NO variable size for rels. too complex.
 //    remove grow, w/h= 100%
 // by default width = 100%, no grow
 //    width not specified - width = 100%
 //
 //We can actually do this in one sweep.
 //2 basic ideas:
 //  fixed parent: (root = screen) - determines max size of auto sized children
 //  fixed child: text element - determine MIN size of container (added up)
 //
 //input: screen boundary (superclip)
 //1 calc superclip
 //  sizemode   shrink: if parent clip mode is shrink, then max wh would be it's parent max wh, up to the "fixed root"
 //             grow: maxwh is parent max wh however 
 //             fixed: fixed
 //
 //2 plop rels (expand clip/render quads)
 //3 iterate rels recursive
 //  default w/h is 100% of parent, can be "auto"
 //3.2 clamp by min/max wh of rel, and max wh of superclip
 //3.3 expand clipquad & renderquad max = superclip
 //4 iterate stats recursive
 //4.1 expand stats (calc min w/h of stats based on Max(sum of stat+rel element Min W/Hs, Min w/h))
 //    default width: 100% defalt height: shrink
 //5 Add min w/h for stats
 //6 Layout stats (position them)
 //6.1 Expand clipquad and renderquad while laying out - max = superclip
 //7 Layout floats
 //7.1 expand clipquad only    



   Resource Promotion
  The purpose of this system is to determine what gets a unique ID, and name, and what we check for,
  because temps don't need to get unique ids as -- this would require running registration logic for thousands of temps
  per frame. Temps are constructor objects, new WorldObject, new Shader().. -e.g. not deserialized or loaded from an external file.
  Saving requires us to call temp.PromoteResource() - and this is called by the system in various places.
    We instruct the library when we want to hang on to a resource by promoting the resource
  to a savable state, and ensuring it:
    1 has a unique ID, id is valid
    2 unique name
    3 has a data source of some kind attached
    4 sub-resources for loaded/generated items are not registered
    5 is not already a resource 
 when the resource is removed from the scene we just keep it around..
  sub - sub-resource (object->material->texture->image)
  UTable - unique name table for global resources --on GU.lib
  RTable - resource local table -- on datasource
  DS - data source
    Namespaces
      temp - none 
      scene - UT only - created in the application/script
      libroot - root library object - RT and UT - generated/loaded
      libdep - RT only - generated/loaded
    Serialization Types
      temp - not saved, can be promoted
      scene - all nodes saved
      libroot - data source saved
      libdep - not saved, cannot be promoted, but can be clone()'d
    Conditions
      libadd 
        if sub is lib - no change, do not traverse children
        if sub is scene - error - must clone resource to add to library
        if sub is temp -  set DS (for all) - if no DS - then is raw
                          if is root resource
                            set lib - item will exist in UTable
                            gets uid/uname (creteresrouce)
                          else
                            set libdep
                            rname from DataSource RTable (must be unique within resource context) - usually loaded from file.
                            if has no DS - raw serialized
                            if has DS - not serialized
      sceneadd
        if lib - no change, do not traverse
        if scene - error - sub-object already added to scene, should not be possible
        if temp - set to scene, make uid (createresource) for all temp sub-items
      sceneremove
        if sub is lib - no change
        if sub is scene - remove uid/un 
        if sub is temp - error
      libremove
        if sub is lib - remove uid/un
        if sub is scene - error
        if sub is temp - error
Overview
  Resource library saves resource generators, and loaders (data sources)
    -> unique ID, uname
    LoadModel, LoadImage, LoadTexture, LoadMaterial, LoadShader, LoadMesh
    -> creates a new shader if the UName does not exist
    -> if uname does exist, load the DS->
      IF -> parameters are the same (DataSource::Equals)
      ELSE -> destroy the DS, create a new DS with the changed parameters, **keep the existing name/ID
  Temps:
    Temporary data created through constructors are not saved, unless created through Load* attached to a worldobject node
      Material() Shader() Texture() Image() MeshData()..
      So to update an image/shader etc - create with Load* 
    Ok so what about WorldObject?
      -> Saved only when attached to the scene, or, marked as a library item.
