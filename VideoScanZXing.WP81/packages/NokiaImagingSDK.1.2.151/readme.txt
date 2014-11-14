
NuGet has successfully installed the SDK to your project !

Finalising the installation
===========================
  - Some versions of Visual Studio may not find the references to the Nokia Imaging SDK that were added to your project by NuGet.  To fix things, simply close your project and reopen it. 
  - Make sure that your project does not have "Any CPU" as an "Active solution platform". You will find the instructions how to do this here: http://developer.nokia.com/resources/library/Lumia/nokia-imaging-sdk/adding-libraries-to-the-project.html
  

New Users
=========

If this is your first time with the Nokia Imaging SDK, welcome, we are glad to have you with us! To get you started off on the right foot, take a quick peek at our documentation :   
   http://developer.nokia.com/resources/library/Lumia/nokia-imaging-sdk.html


Graduated from beta - 1.2.151
=============================

Since our last release (1.2.115), we verified that the SDK is in tip-top shape when running on Windows Phone 8.1, squashed a few bugs, fixed some corner cases along the way, and we are now ready to graduate the SDK from Beta to a production quality release. 

Version history
===============

-------------

Release 1.2.151

Bug fix release, several issues solved, here is a shortlist of the key fixes:
- Increased precision in internal color conversion gives significantly better color correctness.
- Fixed crash in complex blend-with-self scenarios.
- LensBlur:  Improved handling of border between focus area and blurred area, reducing the "halo" effect.
- Fixes the unspecified error situation when dealing with monochrome bitmaps.
- Fixed an issue with the creation of an animated gif from images captured with the burst mode of the Windows.Media.Capture (an API introduced in WP8.1).


-------------

Release 1.2.115

A bug fix release, providing a few important tweaks and fixes to the GifRenderer and the ImageAligner.


--------------

Release 1.2.99

Support for Windows Phone 8.1.
New cool filters and enhancements. 
 - ImageAligner : Aligns a series of images that differ by small movements. 
 - GifRendering : Create GIF images. Create animated gifs. 
 - TargetArea of the BlendFilter : It is now possible to blend images on top of others, defining the targetArea and rotation, with easy to use syntax. Great for stickers! 
 - DelegatingFilter/CustomFilterBase : Allows building your own custom block based filter, true tile based image processing in a memory efficient way. 

--------------

Release 1.1.177

Windows 8.1 support
Foreground Segmenter
Lens Blur
HDR effect

--------------

Copyright (c) 2014, Microsoft Mobile Oy
All rights reserved.





