@ECHO OFF

SET "PLUGINS=%USERPROFILE%\AppData\Local\vPilot\Plugins"
ECHO VPilot-Plugin path detected as '%PLUGINS%'

ECHO Copy base

COPY ..\Base\VPilotNetCoreBridge\bin\Debug\*  %PLUGINS%

ECHO Copy Client
MKDIR %PLUGINS%\VPilotNotifications
COPY ..\Implementations\VPilotNotifications\bin\Debug\net8.0-windows7.0\* %PLUGINS%\VPilotNotifications\

MKDIR %HOMEPATH%\AppData\Local\vPilot\Plugins\VPilotNotifications\Sounds
COPY ..\Implementations\VPilotNotifications\bin\Debug\net8.0-windows7.0\Sounds\*.* %PLUGINS%\VPilotNotifications\Sounds\
COPY ..\_DLLs\_NET_CORE_8\SimConnect.dll %PLUGINS%\VPilotNotifications\

DEL %PLUGINS%\RossCarlson.Vatsim.Vpilot.Plugins.dll
DEL %PLUGINS%\RossCarlson.Vatsim.Vpilot.Plugins.xml
DEL %PLUGINS%\VPilotnetCoreBridge.config.json
MOVE %PLUGINS%\VPilotnetCoreBridge.config-vpilot.json %PLUGINS%\VPilotnetCoreBridge.config.json

