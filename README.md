# Dendro
Dendro is a volumetric modeling plug-in for Grasshopper built on top of the OpenVDB library. It provides multiple ways to wrap points, curves, and meshes as a volumetric data within Grasshopper, allowing you to perform various operations on those volumes. Dendro includes components for boolean, smoothing, offsets, and morphing operations. You can find out more details of its features [here](https://www.ecrlabs.com/dendro) 

## Design

We have been using the OpenVDB library for a couple years, but needed something to prototype quicker with. We had built a rough version of this for Grasshopper, but decided to package it up nicer and put a release together. Hopefully it is something to build upon and our hope was it could serve as a starting point to add more features and functionality to.

The goal was to make Dendro integrate into Grasshopper as seamlessly as possible. Whereas many voxel solutions require you to think of geometry as living with a bounding box, Dendro makes working with volumes no different than handling any other geometry in Grasshopper. Dendro works with many native Grasshopper components, avoiding the 'blocking' found in other plugins, and allowing you to move in and out of volume operations very quickly.

## Installation

Dendro contains two projects, a C++ project for working with OpenVDB and a C# project creating the Grasshopper plugin.

##### DendroAPI (C++)
OpenVDB has multiple dependencies...

* blosc (v1.14.3)
* boost (v1.66.0)
* ilmbase (v2.2.1)
* openexr (v2.2.1)
* tbb (v2018.06.18)
* zlib (v1.2.11)

All these dependencies are packaged on a private nuget server which should automatically be downloaded and installed when loading the solution. If that is not the case, make sure that the dendro private server (http://dendro-nuget.azurewebsites.net/nuget) is listed as a package source. that information can be found at `Tools` > `Options` > `Package Manager` > `Package Sources`. You might need to `Restore Nuget packages` and `Rescan Solution` if they weren't found initially.

##### DendroGH (C#)
Since there are multiple versions of Rhino, each with their specific SDK, we added the Rhinocommon and Grasshopper libraries as a nuget package in order to let you specifically target your desired Rhino version. That can be changed by `Right-clicking the C# project`, then selecting `Manage Nuget Packages`, clicking the `Installed` tab, `Selecting` your desired package, and finally, changing the `Version` in the right panel.

It is targeted for Rhino 5 by default because that seems to be more universal and forward compatible in Rhino 6.

## Building

Dendro was built using Microsoft Visual Studio 2017, but you should be able to re-target for other versions. It will also copy all necessary dependency dlls into the output folder to provide an easy reference for where dependency dlls can be found.

## More Info

Dendro was using OpenVDB. For more information on the library, please visit [here](http://www.openvdb.org/).

## Contributors

* [ecr labs](https://www.ecrlabs.com)
* [MachineHistories](http://www.machinehistories.com)