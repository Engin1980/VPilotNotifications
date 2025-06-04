@ECHO OFF
ECHO Copy base
COPY ..\Base\VPilotNetCoreBridge\bin\Debug\*.*  C:\Users\vajgma91\AppData\Local\vPilot\Plugins
rem copy PlugintTestToDelete\bin\Debug\*.*  C:\Users\vajgma91\AppData\Local\vPilot\Plugins

ECHO Copy Client
MKDIR C:\Users\vajgma91\AppData\Local\vPilot\Plugins\VPilotNotifications
COPY ..\Implementations\VPilotNotifications\bin\Debug\net6.0-windows\*.* C:\Users\vajgma91\AppData\Local\vPilot\Plugins\VPilotNotifications\

MKDIR C:\Users\vajgma91\AppData\Local\vPilot\Plugins\VPilotNotifications\Sounds
COPY ..\Implementations\VPilotNotifications\bin\Debug\net6.0-windows\Sounds\*.* C:\Users\vajgma91\AppData\Local\vPilot\Plugins\VPilotNotifications\Sounds\

DEL C:\Users\vajgma91\AppData\Local\vPilot\Plugins\RossCarlson.Vatsim.Vpilot.Plugins.dll
DEL C:\Users\vajgma91\AppData\Local\vPilot\Plugins\RossCarlson.Vatsim.Vpilot.Plugins.xml
DEL C:\Users\vajgma91\AppData\Local\vPilot\Plugins\VPilotnetCoreBridge.config.json
MOVE C:\Users\vajgma91\AppData\Local\vPilot\Plugins\VPilotnetCoreBridge.config-vpilot.json C:\Users\vajgma91\AppData\Local\vPilot\Plugins\VPilotnetCoreBridge.config.json

