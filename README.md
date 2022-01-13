
# OpenGL + C# On Linux via OpenTK

Making this minecraft clone thing and we're not going C++. No way. Java, not a fan. C# .. but on Linux. OpenTK can handle that.

This is a test of OpenTK on Linux. I used this in the past and really liked it. It seems to work so far.

# How to Run

1. MonoDevelop
The easy road here is MonoDevelop. Simply go to MonoDevelop.com and follow the steps to install it. I am on Ubuntu right now and the steps worked perfectly as of Jan 10 2022. Create a project .. 

The package manager in MonoDevelop should detect the correct OpenTK packages. The newest versions of OpenTK aren't compatible, probably due to .NET version. I selected v3.1.

2. VSCode
I really like MonoDevelop, but it's not near as efficient as VSCode. VSCode was kind of hairy to get working the way I wanted. Though it's possible to easily do it if you install MSBuild.

* Install
  ** Honestly I would just install MonoDevelop as it will install everything you need to work in VSCode.**
  mono-complete
  .NET 5
  vscode-mono-debug for VSCode
  

* Build 
  run msbuild in project directory (easy)

* Run 
  mono -debug MyProgram.exe

This is after I created a solution with MonoDevelop.