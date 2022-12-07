## WORKING ON:
  Charmod
  Transforms
  Animation
    material generate shader
    default object + normal
    default object no normal
    other


## Bugs:
  HIGH - PickId must be the same for sub-objects ESP armature (entire defined asset )
    HIGH Show armature node to move objects or just make pick id the same for the asset
  HIGH PickId - Picker is not shared (individual pick Ids per RenderView), 
    but Meshes (which have PickID are shared)
    either tie pickId to a RenderView, or, use a global pick id for all views.

  LOW outline shader is blending with another buffer causing the outlines to look weird in dark places (low priority)
  MED fix the directional lights. shader.cs ~line 555
  MED Topology - bottom topo is incorrect -underneath terrain
  HIGH Topology normals are incorrect - ComputeNormalsTangents - the quad tops / sides verts are not linked
  X Library Dependencies must share pick ID with root node Gear -> root
  HIGH CRITICAL: slowdown issue - fps slows down .. some kind of serious issue
  HIGH Gear / objects dont rotate correctly
  * Fix current active view for moving objects (mouse movement and editing)

## TODO:
* Refactor: UI:  use a UiFloat4 for the margin padding and fix the LayoutH/V to use the LMin/LMax functions and remove LayoutH/V for a single layout
                do not comibne UIQuad with UiFloat4 because it messes up.
                remove vertical layout
* Optimize: BindBuffer called too much, call GL.BindBuffers and GL.BindBuffersBase with all buffers at one time.
* Optimize: Move animation data to GPU update all bone / node instances at one time
    [{sequencer, count, index,index..}, {sequencer,count,i..}]
    [node,node,node,bone,node,bone..et]
* add obj hiding to edit history
* Fix implementing "modified" update of all GpuData structures (optimization)
  * Mostly on just Instance data, immutable draw call structure
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


## More Todo:
* World Model 1 
  X Columns, menus, basic gen, gen flat, save world
  * Serialize Phase 1
    * Save World
    * Load World 
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
    * Stitch neighbor globs
    * Go To World
    X Culling
    * BACKLOG: Async topology
    * Catmull clark
      -> Editable Edge weights (click edge, drag for weight)
    * Select Top style (overhang / bevel / flat)
* World model - 3 (WALLS)
    * WALLS** / DOORS
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

