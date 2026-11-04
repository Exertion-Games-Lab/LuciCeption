# Thermal Module

This folder contains the thermal cue module schematic and ESP32-S3 firmware:

- `Thermal_module_schematics.pdf` — wiring overview.
- `thermal_serial.ino` — Arduino firmware for serial control of the Peltier driver.

The firmware accepts commands from the Unity application over a USB serial connection and sets the Peltier operating mode and PWM intensity.

## Hardware Shown in the Schematic

The schematic depicts:

- ESP32-S3 development board
- L293D H-bridge driver IC
- Three Peltier modules
- 12 V wall power supply and a 1.5 m power cable
- Female and male barrel jacks with a 2 m cable
- Breadboard and jumper wires

The Arduino pin assignments in `thermal_serial.ino` are:

| ESP32-S3 pin | Firmware name | Driver signal |
| --- | --- | --- |
| GPIO 1 | `PWMControl` | PWM / enable |
| GPIO 2 | `IN1` | Direction input 1 |
| GPIO 3 | `IN2` | Direction input 2 |

Use the schematic as a wiring reference, and confirm the exact driver pinout and power connections against the datasheets for the hardware you have.

> **Electrical and thermal safety:** Peltier modules can draw substantial current and become very hot or cold. Verify that the driver, power supply, wiring, connectors, and heat dissipation are rated for the combined load of the modules. Do not assume the L293D or the illustrated wiring is suitable for your particular Peltier modules. Use an appropriately rated driver and power supply, provide suitable heat sinks, prevent shorts, and test at low intensity while monitoring temperatures. Keep 12 V on the power stage; power the ESP32-S3 according to its board specifications.

## Firmware Setup

1. Open `thermal_serial.ino` in the Arduino IDE.
2. Install/select the appropriate ESP32 Arduino board support and choose the ESP32-S3 board that matches your development board.
3. Check that GPIO 1, 2, and 3 are available and wired to the driver's PWM/enable and direction inputs as shown above.
4. Connect the ESP32-S3 to the computer over USB and upload the sketch.
5. Identify the serial port assigned to the board. The Unity thermal controller currently uses `COM8`; change the port in `ThermalCueLogic` if Windows assigns a different one.

The sketch initializes serial communication at **9600 baud**. Its active command protocol is newline-terminated text:

```text
<mode>,<intensity>\n
```

- `mode` is case-sensitive and accepts `hot` or `cool`.
- `intensity` is a PWM value from `0` to `255`.
- Any other mode sets both direction pins LOW (off).
- A command without a comma is treated as malformed and turns the output off with PWM set to `0`.

Examples for a serial terminal configured for 9600 baud and newline line endings:

```text
hot,128
cool,128
off,0
```

The firmware does not clamp or validate the intensity value; send values only in the `0`–`255` range. The Unity thermal cue controller sends the same `mode,intensity` format with a newline and uses `hot`/`cool` modes. In the application, intensity is converted to the PWM range before sending.

## Notes

- The sketch starts with PWM set to `0`.
- Wi-Fi and HTTP control code in the sketch is commented out; this version is controlled over serial.
- The legacy single-character serial handler and timed Peltier cycle are not called by the active `loop()`.
- Confirm the actual heating/cooling polarity of the assembled Peltier and driver circuit. The firmware's `hot` and `cool` labels determine the `IN1`/`IN2` states, but the result depends on the physical wiring.