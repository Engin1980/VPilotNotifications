mkdir VPilotMessageAlertRelease
xcopy /e vpilotmessagealert\bin\debug\ VPilotMessageAlertRelease\

cd VPilotMessageAlertRelease
del RossCarlson* 
cd ..

copy readme.md VPilotMessageAlertRelease\
copy license VPilotMessageAlertRelease\

tar.exe -cf VPilotMessageAlertRelease.zip VPilotMessageAlertRelease

rmdir /s /q VPilotMessageAlertRelease