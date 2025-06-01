copy VPilotNetCoreBridge\bin\Debug\*.*  C:\Users\vajgma91\AppData\Local\vPilot\Plugins
rem copy PlugintTestToDelete\bin\Debug\*.*  C:\Users\vajgma91\AppData\Local\vPilot\Plugins

mkdir C:\Users\vajgma91\AppData\Local\vPilot\Plugins\VPilotNetCoreModuleTest
copy VPilotNetCoreModuleTest\bin\Debug\net6.0\*.* C:\Users\vajgma91\AppData\Local\vPilot\Plugins\VPilotNetCoreModuleTest\

del C:\Users\vajgma91\AppData\Local\vPilot\Plugins\RossCarlson.Vatsim.Vpilot.Plugins.dll
del C:\Users\vajgma91\AppData\Local\vPilot\Plugins\RossCarlson.Vatsim.Vpilot.Plugins.xml
del C:\Users\vajgma91\AppData\Local\vPilot\Plugins\VPilotnetCoreBridge.config.json
move C:\Users\vajgma91\AppData\Local\vPilot\Plugins\VPilotnetCoreBridge.config-vpilot.json C:\Users\vajgma91\AppData\Local\vPilot\Plugins\VPilotnetCoreBridge.config.json

