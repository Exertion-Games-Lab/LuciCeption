# LuciCeption

LuciCeption is a Unity and Python sustem for sensory cues during sleep to shape dream content. Users can describe a dream they would like to experience, generate and adjust cue parameters, and deliver cues through visual, audio, fan, and thermal devices. The Python application receives EOG signals and detects alternating left- and right-eye movement patterns to trigger the cue delivery.

This project is intended for research and demonstration purposes. It is not a medical device and does not aim to induce lucid dreaming.

## Features

- Uses Gemini to generate cue parameters and a dream induction script from a text prompt, and generate a dream image separately.
- Lets users adjust cue settings, select an audio file, set start delays and cancellation times, and save or load settings in the Unity interface (The induction script is part of the cue settings and is not automatically read aloud by the application).
- Detects alternating left/right eye movement patterns in Python. Unity and Python communicate over local TCP to exchange calibration, start, and threshold-setting messages.
- Includes control logic for visual flashes, audio, fan, and thermal cues.

The generated image is a visual reference in the interface; the visual cue itself is a change in screen color. 

## Requirements

- 64-bit Windows (the project includes a Windows build and native BLE components).
- Unity Editor **6000.2.7f2**. Open the project with this version and allow Unity Package Manager to restore the packages listed in `Packages/manifest.json`.
- Python 3.11.
- The Python application uses BrainFlow, NumPy, MNE, SciPy, and Matplotlib. The project currently has no Python dependency lock file.
- Gemini text/image generation requires a valid Gemini API key and network access. 
- To use the full setup in our paper, please use
    - XReal Air 2 Ultra for visual and auditory cues
    - IFTTT-controlled circulator fan for cutaneous wind cues (https://eu.switch-bot.com/products/switchbot-battery-circulator-fan)
    - OpenBCI for EOG singals
    - a motion sensor (https://eu.switch-bot.com/products/motion-sensor)
    - a handmade thermal module (see thermal module folder for more details)
    - All of the components could be swapped to other modules depending on the need.


## Getting Started

### Run from the Unity Editor

1. Install Unity 6000.2.7f2 using Unity Hub and open the repository with that version.
2. Wait for Unity to restore the packages.
3. Open `Assets/Scenes/User Interface.unity` and press Play. This is the scene currently enabled in the build settings.
4. Open a separate terminal at the repository root, install the packages required by the Python application, and start the bridge server:

   ```powershell
   py -3.11 -m pip install numpy brainflow mne scipy matplotlib
   py -3.11 .\Python\LuciController.py
   ```

   The Python server listens for Unity connections on `127.0.0.1:50007`. Start Python before using Unity features that require EOG detection.

### Run the Windows Build

The repository includes `Build/LuciCeption.exe` and the `start_LuciCeption.bat` launch script. The batch file starts the build and the Python application, but its Python executable path is an absolute path specific to one computer. Before running it on another computer, update that path to a valid Python 3.11 executable, or use the manual launch instructions above.

## Configuration and Hardware Notes

- **Gemini API key:** Unity's Gemini API calls use `ApiKeys.Gemini` in `Assets/Scripts/ApiKeys.cs`. The key is currently read from a source-code field. Before publishing or sharing the project, move it to a secure local configuration and revoke or rotate any key that has already been committed. Do not put keys in version control or publicly distributed builds.
- **EOG detection:** `Python/LuciController.py` uses `SyntheticDetector` by default, so it can start without a physical EEG/EOG board, but it will not represent real sensor data. An OpenBCI Cyton detector is also available. To use physical hardware, select that detector, configure the correct serial port (the current default is `COM7`), and ensure the electrodes and hardware are connected properly.
- **Cue devices:** Thermal control currently uses serial port `COM8` at 9600 baud. Fan control and motion detection depend on external services configured in the project. Before use, verify that the devices, serial ports, service endpoints, and network match your environment. Test at low intensity or without connected hardware first.
- **Audio:** To use audio cues, select an MP3 or WAV file in the interface.
- **Other modules:** BLE/EMS and some signal-training code are experimental. The presence of models or test programs does not mean they are connected to the active cue workflow.

## Key Directories

| Path | Description |
| --- | --- |
| `Assets/Scenes/` | Unity scenes; `User Interface.unity` is the scene currently enabled for builds. |
| `Assets/Scripts/UI/` | Unity UI, cue settings, navigation, and sensory cue workflow. |
| `Assets/Scripts/AI/` | Gemini API request and response handling. |
| `Assets/Scripts/OpenBCI/` | Unity-side EOG server connection, messages, and calibration result handling. |
| `Python/LuciController.py` | Python TCP server and Unity message handling. |
| `Python/LuciDetector.py` | BrainFlow data sources, signal processing, calibration, and left/right sequence detection. |
| `Python/LR signal training/` | Standalone signal-training/experiment programs and related data. |
| `Packages/`, `ProjectSettings/` | Unity package and project settings. |
| `Build/` | Built Windows application. |

Runtime logs and signal data generated by the Python detector are written to `Python/results/`.
