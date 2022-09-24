Working On:
  World - Edit Flatten
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
    and create them manually
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


