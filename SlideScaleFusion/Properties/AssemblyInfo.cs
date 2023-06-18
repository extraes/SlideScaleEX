using LabFusion.SDK.Modules;
using SlideScaleFusion;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyProduct(SlideScaleFusion.ModuleInfo.Name)]
[assembly: AssemblyCopyright("Created by " + SlideScaleFusion.ModuleInfo.Author)]
[assembly: AssemblyVersion(SlideScaleFusion.ModuleInfo.Version)]
[assembly: AssemblyFileVersion(SlideScaleFusion.ModuleInfo.Version)]

[assembly: LabFusion.SDK.Modules.ModuleInfo(typeof(ScaleModule), SlideScaleFusion.ModuleInfo.Name, SlideScaleFusion.ModuleInfo.Version, SlideScaleFusion.ModuleInfo.Author, SlideScaleFusion.ModuleInfo.Abbreviation, SlideScaleFusion.ModuleInfo.AutoRegister, SlideScaleFusion.ModuleInfo.Color)]