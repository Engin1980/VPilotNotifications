A simple tool playing sound when your callsign, departure or destination airport is mentioned in [VPilot](https://vpilot.rosscarlson.dev/) communication messages.

**Why** Cos I made an airprox with KLM1277 at EGPH, moreover occusing him for not using UNICOM. I overlooked the message in the mess of other chatting. My big apologies.

# Installation

VPilot must be installed prior the plugin installation.

1. Go to the [Releases](https://github.com/Engin1980/VPilotMessageAlert/releases) page and download the latest ZIP archive.
2. Extract the archive content into the `Plugins` folder in __VPilot__ instalation. Note that all the files must be placed directly into the `Plugins` folder, not in any subfolder

# Validating instalation
Ensure you are using the default `settings.json` file.

1. Start Flight simulator and the flight.
2. Start VPilot.
3. Connect to the network.
4. Check if a new tab with a private message confirming the plugin startup is shown.

If not, go to the `...\vpilot\plugins` folder and open the `_log.txt` file for any errors.

# Setting up
The connection info and flightplan is automatically read from the VPilot connection and online Vatsim data. However, you can adjust the sounds and monitored events and update intervals. All the settings are available in the file `settings.json`.

# Usage
Once added into the `Plugins` folder, there is no additional set up. Just start VPilot and connect into the network. However, you can adjust settings - see the previous section.

# Issues

For any issue, feel free to raise a new issue at the [Issues](https://github.com/Engin1980/fs2020-com-to-vpilot-volume/issues) tab.

# Version history

**v1.1 - 2024-07-23**
* Repetitive disconnected warning.
* Private-message-like info messages.
* New sound files.

**v1.0 - 2024-04-27**
* Alerts when message containing callsign/departure/arrival ICAO is received.
* Downloads and updates VATSIM flight plan at fixed intervals.


# Credits

Created by Marek Vajgl.

Project pages: https://github.com/Engin1980/VPilotMessageAlert/

Report issues via the Issues tab on the project GitHub pages.

