# SteerAntennaDish
A SpaceEngineers ingame Script to track moving or stationary targets.

Any combination of routers and hinges can be controlled.
Requires https://github.com/AdrianDeWinter/SE-Utilities


Version History:

0.0.6:
 - Added configuration via custom section (For PBs, Rotors, Hinges, Antennas)
 - Rewrote Data structure to enable grouping of blocks into individually targetable groups
 - Modified behaivour close to target orientation, now linearly reduces speed down to minSpeed
 - Moved position broadcast to 10-Tick loop, broadcasting every 100 ticks was insufficient for tracking fast or close targets
 - Added a switch for the broadcasting behaviour to the PB config

0.0.5:
 - Introduced Antenna Comunication, can track any command block running this script within antenna range

0.0.4:
 - Rewrote and modularized large portions of code

0.0.3:
 - Introduced hinge control
 - Removed LOTS of code duplication

0.0.2:
 - First draft of rotor control
 - Fixed inconsistent behaviour of version info Display with regards to how long it gets displayed
 - Removed unnesscary delay between script startup and coordinate parsing

0.0.1:
 - Initial implementation of mathematics and calculation for base rotor
