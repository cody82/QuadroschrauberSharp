QuadroschrauberSharp
====================

A quadrocopter control program for the Raspberry Pi and a MPU-6050 written in C#.

![](Pictures/DSC_0818.MOV-1.jpg?raw=true)
![](Pictures/DSC_0813-small.JPG?raw=true)
![](Pictures/DSC_0814-small.JPG?raw=true)

Features
========
- Control and monitor your quadrocopter with your computer over WLAN with a fancy HTML5-Frontend.
- Only Raspberry Pi and MPU-6050 required. No microcontroller needed!
- C# instead of C/C++: More readable, easier and less verbose!
- No realtime features, maybe it will work nevertheless.

Required hardware
=================
- Raspberry Pi
- MPU-6050
- A Quadrocopter with motors, motor controllers and BEC-system.
- (optional) USB WLAN stick 
- (optional) Spektrum sat receiver for RC.
- (optional) DS1307 realtime clock.

Software dependencies
=====================
- Servoblaster(included): https://github.com/richardghirst/PiBits
- ServiceStack(included): https://github.com/ServiceStack/ServiceStack
- SuperWebSocket(included): https://github.com/kerryjiang/SuperWebSocket or http://superwebsocket.codeplex.com/
- Mono >= 2.11: http://www.mono-project.com/ or https://github.com/mono
- Raspbian or Arch for Raspberry Pi: http://www.raspberrypi.org/downloads

Compiling
=========
The QuadroschrauberSharp Project can be built with Visual Studio 2012.

QuadroschrauberSharp.Linux must be built on Linux with Mono, 
because it depends on Mono.Posix for some system calls for ServoBlaster.

