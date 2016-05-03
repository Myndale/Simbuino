Simbuino v 1.0.0.19 - Gamebuino Emulator. Copyright (c) 2014-2015 Mark Feldman (aka "Myndale" on the Gamebuino forums).

Application Overview
====================
Simbuino is an open-source Windows-based emulator for the Gamebuino hand-held console (see http://gamebuino.com). The latest version of the code and binaries are
available from the following GitHub repository:

https://github.com/Myndale/Simbuino

All questions/bug reports etc should be directed to the official forum thread at http://gamebuino.com/forum/viewtopic.php?f=12&t=1028. Alternatively you can
email me directly at simbuino@ppl-pilot.com.

Source Code Overview
====================
Simbuino is a C#/WPF (.NET 4.5) Windows application developed for Microsoft Visual Studio Express 2013. In general the code attempts to adhere strictly to the MVVM
architectural pattern, although there are currently some minor violations that require clean up. The solution is divided into the following project workspaces:

	- Simbuino: Application entry point only, contains no logic itself other than to store the application class and bind the other workspaces.
	- Simbuino.UI: Responsible for all user interface code. Contains both views and view models for the sake of convenience, although these are kept
      architecturally separate.
	- Simbuino.Emulator: Contains main Gamebuino emulation code i.e. micro-controller and peripherals.
	- Simbuino.Hardware: Small project that handles communication with Gamebuino hardware (i.e. game uploads).
	- MvvmDialogs: Generic library used to add dialog box functionality to MVVM applications.

The SIMULATION.TXT file contains details as to how the simulation code works. In addition to the usual DEBUG and RELEASE project configurations there is one called
PROFILE which currently runs very slow but can be used to measure the execution time of the individual op-code handlers.

Simbuino4Web
==============
The Simbuino\src\Simbuino4Web folder contains an experimental HTML5 port of the core Simbuino engine developed for Microsoft Visual Studio Express 2012 for Web. A
runtime version of this project is currently hosted at http://simbuino4web.ppl-pilot.com. It is recommended that you run this version of Simbuino under Chome; it will
work with Internet Explorer but runs very slowly and without sound.

3rd-Party Code
==============
Simbuino uses the following 3rd-party libraries and code:

	- MVVM Lite (https://mvvmlight.codeplex.com). Tool-kit for developing MVVM applications in WPF.
	- Extended WPF Toolkit (http://wpftoolkit.codeplex.com/). Collection of controls, components and utilities for creating WPF applications.
	- HiPerfTimer (Daniel Strigl, http://www.codeproject.com/Articles/2635/High-Performance-Timer-in-C). High performance timer used to profile code.
	- MvvmDialogs (Mark Feldman, http://www.codeproject.com/Articles/820324/Implementing-Dialog-Boxes-in-MVVM). Library for displaying dialog boxes in MVVM applications.
	- AvalonDock (https://avalondock.codeplex.com). Docking Windows library for WPF.
	- Animated GIF Encoder (Kevin Weiner/FM Software, http://www.fmsware.com/stuff/gif.html). 
	- SharpDX (http://sharpdx.org). Managed DirectX API used by the sound engine.

License
=======
Simbuino is released under the MIT Expat License, please see the following links for details:

	http://opensource.org/licenses/MIT
	http://en.wikipedia.org/wiki/MIT_License	 

A short summary of the MIT license can be found at https://www.tldrlegal.com/l/mit:

	"A short, permissive software license. Basically, you can do whatever you want as long as you include the original copyright and license in their
	respective sources."

Release History
===============

Version: 1.0.0.0
Date: 16th Aug 2014
Changes:
	- Initial public release

Version: 1.0.0.1
Date: 16th Aug 2013
Changes:
	- Various changes to sound engine to improve streaming

Version: 1.0.0.2
Date: 19th Aug 2013
Changes:
	- Optimizations to sound engine
	- Fixed a bug that was causing audio buzzing
	- Added reload-and-rerun menu option
	- Greatly optimized graphics pipeline
	- Proper support for timer 1
	- Added screen persistence option
	- Various bug fixes to support my original 3d demo and grey scale demos.

Version: 1.0.0.3
Date: 20th Aug 2014
Changes:
	- Added animated GIF capture

Version: 1.0.0.5
Date: 25th Aug 2014
Changes:
	- Customizable LCD colors
	- Changes to the key mappings and support for customization.
	- Changed the default key scheme to match gbsim for more consistency across emulators.
	- Landscape/portrait modes for LCD, key mappings and GIF export.

Version: 1.0.0.6
Date: 26th Aug 2014
Changes:
	- Added support for PWM back light.

Version: 1.0.0.8
Date: 7th Oct 2014
Changes:
	- Added support for direct uploading of HEX files to the Gamebuino hardware.

Version: 1.0.0.11
Date: 31st Oct 2014
Changes:
	- Fixed bug involving some certain key-bindings not saving correctly.
	- Modified code so that game play keys only get updated when Simbuino is the active foreground app.
	- Finished basic support for Flash, EEPROM and RAM panels.
	- Fixed the slow-down issue when the RAM panel was active.
	- Added a basic IO panel implementation including register names,indexes and bit fields.
	- Added support for almost all documented op code aliases (e.g. brne, an alias of brbc).
	- Minor sound bug fixes.

Version: 1.0.0.13
Date: 18th Nov 2014
Changes:
	- Fixed op code aliases.
	- Fixed a bug involving certain op codes not setting the carry flag correctly.
	- Discovered and fixed a race condition which may be responsible for the occasional crashes people are seeing in certain GUI environments.

Version: 1.0.0.14
Date: 6th Jan 2015
Changes:
	- Added basic support for read-only SD card emulation.

Version: 1.0.0.15
Date: 26th Jan 2015
Changes:
	- Added port refresh prior to hardware upload
	- Fixed a bug where all buttons were appearing as pressed after a ctrl-f5.
	- Fixed an EEPROM display bug.
	- Fixed a rare logic bug in the sbic op-code handler.

Version: 1.0.0.17
Date: 4th Feb 2015 
Changes:
	- Re-wrote sound engine
	- Overhauled simulation threading code.
	- Fixed sporadic ctrl-f5 crash bug.
	- Added simple low-pass filter option to audio output stream. 

Version: 1.0.0.18
Date: 19th Feb 2015 
Changes:
	- Added read-only support for petit_fstfs and standard SD card librray.

Version: 1.0.0.19 (First Open Source Release)
Date: 12th Mar 2015
Changes:
	- Added version number to the About dialog.
	- Fixed a bug with EEPROM not persisting correctly across application launches.
	- Added EEPROM save/load menu options
	- Fixed bug involving output IO pins not being read back correctly
	- Fixed bug preventing IO pins not updating in gui pane
	- Fixed issues that have crept in preventing the profiler from working
	- Fixed minor bug in gif export dialog code
	- Prepared project for open-source release (documentation, clean-up etc)
