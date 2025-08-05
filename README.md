# 🛪🔊 VPilot Notifications (plugin) 🔊🛪

A simple tool adding sound **notifications** to [VPilot](https://vpilot.rosscarlson.dev/):
* 🔊 when **your callsign, departure or destination airport is mentioned in radio communication**;
* 🔊 when **contact me ...** message was received but frequency were not tuned;
* 🔊 while **disconnected**;
* 🔊 when **departing without filled VATSIM FlightPlan**.

**Why** Cos I made an airprox with KLM1277 at EGPH, moreover accusing him of not using UNICOM. I overlooked the message in the mess of others chatting. My big apologies.

# 🖴 Installation

This tool is a *plugin* for [VPilot](https://vpilot.rosscarlson.dev/) connecting software. VPilot must be installed **prior** to the plugin installation. This tool is tested only on cases when VPilot is running on the same computer as the Flight Simulator.

To obtain the tool:

1. Go to the [Releases](https://github.com/Engin1980/VPilotNotifications/releases) page and download the latest ZIP archive.
2. (If using Windows ZIP extractor) Open _Properties_ of the ZIP archive file (context menu via right button, last item), and press `Unblock` button ([how](https://singularlabs.com/tips/how-to-unblock-a-zip-file-on-windows-10/), [why](https://superuser.com/questions/302146/can-i-stop-windows-from-blocking-zip-files#1870635)).
3. Extract the archive `Plugins` folder content into the `Plugins` folder of __VPilot__ installation. Note that all the files must be placed directly into the `Plugins` folder, not in any subfolder

The tool is based on .NET Framework 4.7.2 (which is used by VPilot by default, so if you are using VPilot, you should be fine) and .NET 6 (which is installed on Windows 10+ devices by default).

**Final check** You should have 
* `...\VPilot\Plugins\VPilotNetCoreBridge.dll` and 
* `...\VPilot\Plugins\VPilotNotifications\VPilotNotification.exe` 

... files, where `...\VPilot` represents VPilot installation folder.

# ✓ Validating installation
Ensure you are using the default `settings.json` file.

1. Start the Flight Simulator and the flight.
2. Start VPilot.
3. Connect to the network.
4. Check if a new tab with a private message confirming the plugin state is shown.

Also, you should see:
* VPilot plugin log file at `...\VPilot\Plugins\_VPilotNetCoreBridge.log`
* VPilot Alert log file at `...\VPilot\Plugins\VPilotNotifications\_VPilotNotifications.log` 

If anything goes wrong, check the content of those two files.

# 🛠 Setting up (optional)
The connection information and flight plan are automatically read from the VPilot connection and online VATSIM data. However, you can adjust the sounds, processed events and update intervals. All the settings are available in the configuration file at `...\VPilot\Plugins\VPilotNotifications\settings.json`.

# 🛪 Usage
Once added to the `Plugins` folder, there is no additional set-up. Just start VPilot and connect to the network. However, you can adjust settings - see the previous section.

# ⚠ Issues

For any issue, feel free to raise a new issue at the [Issues](https://github.com/Engin1980/VPilotNotifications/issues) tab. Please add the relevant content of the `log` files if relevant.

# Version history

**v2.1 - 2025-08-05**
* BuFix: Incorrect shutdown behavior fixed
* Private messages now cached and shown once connected to network.

**v2.0 rev 1 - 2025-06-04**
* BugFix: Incorrect behavior if VPilot started before FS is fully initialized.

**v2.0 - 2025-06-04**
* Internally completely rebuilt to support readouts from Flight Simulator
* Added support for no-flight-plan warning
* Added support for contact-me warning

**v1.1 - 2024-07-23**
* Repetitive disconnected warning.
* Private-message-like info messages.
* New sound files.

**v1.0 - 2024-04-27**
* Alerts when a message containing callsign/departure/arrival ICAO is received.
* Downloads and updates VATSIM flight plan at fixed intervals.

# Thanks
* Ross Carlson for his VPilot plugin support implementation.
* [Online Sequencer](https://onlinesequencer.net/) for providing a simple tool to create notification sounds.

# Credits

Created by Marek Vajgl.

Project pages: https://github.com/Engin1980/VPilotNotifications

Report issues via the Issues tab on the project GitHub pages.



