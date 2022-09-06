# Relay Controller
The controller is a Arduino  C application that implements a simple serial over USB connection to provide control of the three power control relays and the battery recharge input.  

A serial command is composed of ASCII text ended with carriage return. Supported commands:

| Command | Description |
|---------|---------|
| H | hello |
| ON | all relays on |
| OFF | all relays off |
| ON SS | sub-system relay on |
| ON RC | recharge input relay on |
| ON HA | high amp relay on |
| ON AS | arm servos relay on |
| OFF SS | sub-system reLay off |
| OFF RC | recharge input relay off |
| OFF HA | high amp relay off |
| OFF AS | arm servos relay off |

The physical location of the relays is provided in [structural.md]() and the electrical coverage of the relays is provided in the power diagram in [electronics.md]().