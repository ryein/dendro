# Dendro
Dendro is a volumetric modeling plug-in for Grasshopper-3D built on top of the OpenVDB library. It provides multiple ways to wrap points, curves, and meshes as a volumetric data within Grasshopper-3D, allowing you to perform various operations on those volumes. Dendro includes components for boolean, smoothing, offsets, and morphing operations. You can find out more details of its features [here](https://www.ecrlabs.com/dendro) or download a working version [here](https://www.food4rhino.com/app/dendro)

## Design

We have been using the OpenVDB library for a couple years, but needed something to prototype quicker with. We had built a rough version of this for Grasshopper-3D, but decided to package it up nicer and put a release together. Hopefully it is something to build upon and our hope was it could serve as a starting point to add more features and functionality to.

The goal was to make Dendro integrate into Grasshopper-3D as seamlessly as possible. Whereas many voxel solutions require you to think of geometry as living with a bounding box, Dendro makes working with volumes no different than handling any other geometry in Grasshopper-3D. Dendro works with many native Grasshopper-3D components, avoiding the 'blocking' found in other plugins, and allowing you to move in and out of volume operations very quickly.

## Installation

Dendro contains two projects, a C++ project for working with OpenVDB and a C# project creating the Grasshopper-3D plugin.

##### DendroAPI (C++)
Dendro has multiple dependencies...

* blosc
* boost
* openexr
* openvdb
* tbb
* zlib

To make working with the library easier, we've included a file named "dendro_libs.7z". It includes all the libraries needed to run and compile DendroAPI. Just extract the 7z into the main solution directory and when you open the solution, all libraries will link to that directory automatically. Unless you want to work with different versions of these dependencies, you shouldn't need to do anything to get up and running.

##### DendroGH (C#)
Since there are multiple versions of Rhino, each with their specific SDK, we added the Rhinocommon and Grasshopper-3D libraries as a nuget package in order to let you specifically target your desired Rhino version. That can be changed by `Right-clicking the C# project`, then selecting `Manage Nuget Packages`, clicking the `Installed` tab, `Selecting` your desired package, and finally, changing the `Version` in the right panel.

It is targeted for Rhino 5 by default because that seems to be more universal and forward compatible in Rhino 6.

## Building

Dendro was built using Microsoft Visual Studio 2019, but you should be able to re-target for other versions. It will also copy all necessary dependency dlls into the output folder to provide an easy reference for where dependency dlls can be found.

## More Info

Dendro is using OpenVDB. For more information on the library, please visit [here](http://www.openvdb.org/).