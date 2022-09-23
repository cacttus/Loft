Working On:
  World - Edit Flatten
    Edit History

  Gui Menus
    Move events from style to UiElement
    We need to put the contextmenu on the Gui2d, there is no other way, other than 
    destroying the current order of the UI
    automatic PickRoot 

Bugs:
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

TODO list
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


