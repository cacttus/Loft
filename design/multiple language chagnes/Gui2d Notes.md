# Gui API Overview
## Usage
 gui.StyleSheet.AddStyle(
    new UiStyle(new List<string>(){ "myinheritedclass1" , "myinheritedclass2" } , "mystyle") 
    {
       Margin = 0,
       Padding = 2,
       Color = new vec4(1,1,0,1),
       FontFace = FontFace.Lato,
       BorderRadius = 2,
       Border = 10,
       BorderColor = vec4(0,1,0,0.5f)
       //...
    }
 );
 // Other styles.. 
 var ele = new UiElement(gui.StyleSheet, new List<string>(){ "myotherstyle", "mystyle" }, "lblMyElement",  "Hello world!");// mystyle will override anything in myotherstyle
 gui.Add(ele)
 var ele2 = new UiElement(gui.StyleSheet, null/*Does not use any classes*/, "lblDefault", "Default Styled Element");//default styled element
 ele.AddChild(ele2);

## Styles
UiStyle is a CSS style. Like CSS, UiStyle can be inherited and thus, "compiles" to a final "inline" style on each element. 
Elements have "inline" styles as well, like in HTML,  <div class=".." style="..."/>, style="" is the inline style.
UiStyle compilation is dynamic, and happens during the layout, when a value has been changed. The system then refreshes all elements that use that UiStyle.
   (We refresh the ENTIRE element(s) when changing ANY attribute, meaning like changing Color/Texture WILL affect position of an element. 
   Would be a nice optimization to fix this.)

Note: a few properties are different from HTML/CSS, notably:
  * Color: is the CSS background-color attribute, NOT text color. Colors can be semi-transparent as well using the alpha channel.
  * FontColor: The color of the text/font
  * Texture: Like background-image in CSS. Allows you to set a texture instead of a color. Images may be semi-transparent, alpha-blended or alpha masked. Note: a pixel alpha value of less than 0.01f is discarded.
  * ImageTiling: fill, repeat, wrap, clamp, etc.
  * SizeMode: allows you to expand an element to its parent, or shrink an element to its children. MaxWidth MaxHeight are important with Expand, as, without a maxWidth, child elements will epxand to the parent size (ultimately, RenderView size). 
       (We should also allow for Fixed width elements..)
  * PositionMode: is like the CSS Static, Relative or Absolute. 
       Static: Automatic layout; Top/Left are ignored. Width/Height
       Relative: Top/Left are relative to parent, Width/Height are used. 
       Absolute: Top/Left are relative to the RenderViewport, Width/Height are used.
  * OverflowMode: allows you to show or hide the contents of a child element.
  * DisplayMode: For PositionMode=Static elements only. Whether there is a break <br/> after the element.
 
  * Events: Mouse Down/Hover..etc.. These work, but their "inheritance" is questionable. We may end up not inheriting events.

## Values
Note: There are 2 Kinds of Values.
The **UiStyle** value (Get/Set ClassValue) , is nullable , and will return null if this style does not have the Prop set, for instance:
   div { width:200px; }
 is:
   UiStyle s = new UiStyle(<style1 style2>,"mystyle")
   s.Width = 200px;
 The **UiProp** value (Get/Set PropValue) can NEVER be null , if unset, uses the default field value:
    s._props.Width = 200
 And on UiStyle:
   s.Width = null
 will clear Width from the class, and it will INHERIT Width from a superclass, or, if there are no superclasses, it will set to the Default value for Width.
 So to actually use a value in the Layout algorithm, you muse use the UiStyle._prop field.
 In other words, UiProps stores concrete values, and cannot be null. UiStyle uses null (via the user) to signal that a property is to be inherited.
