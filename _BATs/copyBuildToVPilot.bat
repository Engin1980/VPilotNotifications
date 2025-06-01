copy VPilotNetCoreBridge\bin\Debug\*.*  C:\Users\marek\AppData\Local\vPilot\Plugins
rem copy PlugintTestToDelete\bin\Debug\*.*  C:\Users\marek\AppData\Local\vPilot\Plugins

mkdir C:\Users\marek\AppData\Local\vPilot\Plugins\VPilotNetCoreModuleTest
copy VPilotNetCoreModuleTest\bin\Debug\net6.0\*.* C:\Users\marek\AppData\Local\vPilot\Plugins\VPilotNetCoreModuleTest\

del C:\Users\marek\AppData\Local\vPilot\Plugins\RossCarlson.Vatsim.Vpilot.Plugins.dll
del C:\Users\marek\AppData\Local\vPilot\Plugins\RossCarlson.Vatsim.Vpilot.Plugins.xml
del C:\Users\marek\AppData\Local\vPilot\Plugins\VPilotnetCoreBridge.config.json
move C:\Users\marek\AppData\Local\vPilot\Plugins\VPilotnetCoreBridge.config-vpilot.json C:\Users\marek\AppData\Local\vPilot\Plugins\VPilotnetCoreBridge.config.json

