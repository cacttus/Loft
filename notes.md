
please just end it
undo, undo.
memory grows, fps sucks..

so when I say

Style.Height
I get the height of the inherited element as the Inline style will probably not have it. 

Style.Height

  UiStyle _height // pointer to UiStyle that has the inherited height
  return _height;
  NO ** this would cause super long chains of style references s.width(s.width(s.width..))
  compiling is the answer

So yes there is a way around the compile bullshit

UiStyle Width //references to the style , could be this
UiStyle Height

set Width
  if value == null
   this.Width = this.Super.Width
  // Super will already have its width Style set to its parent style., or this, if it is the base style. 
   //** however that's not supers work, the super chain may be different .. so
   // either we can not allow for css like styles, or, we need to figure this guy out
  else
   this.Width = this
   _props.Width = value
_props


So really, all props are pointers to other UiStyles, but we access theri values.
The base style has the base value, and these MUST be set. .. 


events.. should we inherit.. ugh

if a uiprop changes, then we only need to set that property (sort of event driven)

props _props
props _changed

foreach chagned
 ob._props[prop] = changed

no compiled props, just styles,

dothing(passed_props)

e.Inline.. { return styles[styles.count-1]} //inline is at end
e.<Style>.. { return styles[..] } //styles applied left to right
this_props = passed_props.clone
foreach style
   //iterate, inclding inline
   apply style(passed_props)
foreach child
  dothing(passed_props)
we compile per element on update..

or

color [ color, color, color ] .. 
left [left left ] .. 

pushstyle
popstyle

** i think we should have a compiled props, for debug purposes .. sure more data, but who cares.** 
However styles do not need all props.

props._left << fastest
props.Left { get{ return Dict.Find(PropName.Left); }} << slow, clunky

issue is inline style, how to know if the inline is set? !important

ways to use compiled class as inline class
1 boolean value for each prop, i.e. !important boolean
2 generic props as Dictionary<Prop> added ? it's set
3 don't use compiled props, just have inline, and compile them to a new structure on the fly
  ^ not smart. having the compiled props is a huge debug help

* i want compiled props for all elements, perhaps, besides glyph

UiGlyph needs to be a separate thing..meh..it's not necessary right? it make sit otoo complex.

the way i have it is correct
InlineStyle
<Style>
_compiled
But the Style and InlineStyle could be property bags. _compiled would be a props.


NEED SCcrollbar for porps window
background texture .. 
parent classses dont' work .. these need to work .. 


so what we do is we add a class to a single root element, 
  then the class gets inherited to each other element. That way we don't nave to pass a class in to each new().
setting uiref texture to null means .. that we use defaultpixel.


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
