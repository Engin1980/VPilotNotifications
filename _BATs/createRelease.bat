@echo off
echo Deleting previous content
del /q /s ..\_Release\*
rd /s /q ..\_Release

echo Building dirs
mkdir ..\_Release
mkdir ..\_Release\VPilotNotificationsPlugin
mkdir ..\_Release\VPilotNotificationsPlugin\Plugins
mkdir ..\_Release\VPilotNotificationsPlugin\Plugins\VPilotNotifications

echo Copying .NET FW 4.7.2 plugin files
xcopy /e ..\Base\VPilotNetCoreBridge\bin\debug\ ..\_Release\VPilotNotificationsPlugin\Plugins\
xcopy ..\_DLLs\_NET_CORE_8\SimConnect.dll ..\_Release\VPilotNotificationsPlugin\Plugins\VPilotNotifications\

echo Copying .NET 8 plugin files
xcopy /e ..\Implementations\VPilotNotifications\bin\Debug\net8.0-windows7.0\ ..\_Release\VPilotNotificationsPlugin\Plugins\VPilotNotifications\

echo Removing prohibited .DLLs
del ..\_Release\VPilotNotificationsPlugin\Plugins\RossCarlson.Vatsim.Vpilot.Plugins.dll
del ..\_Release\VPilotNotificationsPlugin\Plugins\RossCarlson.Vatsim.Vpilot.Plugins.xml

echo Adding documentation
copy ..\readme.md ..\_Release\VPilotNotificationsPlugin
copy ..\readme.md ..\_Release\VPilotNotificationsPlugin\Plugins\VPilotNotifications\
copy ..\license ..\_Release\VPilotNotificationsPlugin
copy ..\license ..\_Release\VPilotNotificationsPlugin\Plugins\VPilotNotifications\

echo Preparing configuration file
del ..\_Release\VPilotNotificationsPlugin\Plugins\VPilotnetCoreBridge.config.json
move ..\_Release\VPilotNotificationsPlugin\Plugins\VPilotnetCoreBridge.config-vpilot.json ..\_Release\VPilotNotificationsPlugin\Plugins\VPilotnetCoreBridge.config.json

echo Packing to ZIP
cd ..\_Release
tar.exe -a -c -f VPilotNotificationsPlugin.zip VPilotNotificationsPlugin

echo Done