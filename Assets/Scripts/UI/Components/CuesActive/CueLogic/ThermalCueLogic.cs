using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Logging;
using UnityEngine;
using UnityEngine.Networking;

//serial connection
using System.IO.Ports;

namespace LuciReality.UI
{
  public class ThermalCueLogic
  {
    private bool enabled;
    private float intensity;
    int cyclesToFullIntensity;
    float cycleLengthSecs;
    string mode;

    // Add SerialPort field
    private SerialPort serialPort;
    private readonly string portName = "COM8"; // Adjust to your Arduino's port
    private readonly int baudRate = 9600; // Match your Arduino's baud rate

    public ThermalCueLogic()
    {
      // Initialize serial port
      try
      {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.Open();
        Log.Info("[{logType}:{cueType}] Serial port opened: {portName}", "CUES", "THERMAL", portName);
      }
      catch (Exception ex)
      {
        Log.Error("[{logType}:{cueType}] Failed to open serial port: {error}", "CUES", "THERMAL", ex.Message);
      }
    }

    TimeoutController timeoutController = new TimeoutController();

    public void Activate(
      bool enabled,
      float intensity,
      int cyclesToFullIntensity,
      float cycleLengthSecs,
      string mode)
    {
      this.cyclesToFullIntensity = cyclesToFullIntensity;
      this.cycleLengthSecs = cycleLengthSecs;
      this.enabled = enabled;
      this.intensity = intensity;
      this.mode = mode;
    }

    public async UniTask Run(CancellationToken token)
    {
      if (!enabled) { return; }

      await UniTask.SwitchToMainThread();
      Log.Info("[{logType}:{cueType}] beginning thermal cues.", "CUES", "THERMAL");

      int cycles = 1;
      while (true)
      {
        await RunOnce(cycles, token);
        cycles++;
      }
    }

    private async UniTask RunOnce(int cycles, CancellationToken token)
    {
      cycles = Mathf.Min(cycles, cyclesToFullIntensity);
      int setIntensity = Mathf.RoundToInt((int)Mathf.Lerp(0, 255, cycles / (float)cyclesToFullIntensity) * intensity);

      await UniTask.WhenAll(
        SetThermalCue(true, setIntensity, token),
        UniTask.Delay((int)(cycleLengthSecs * 1000), cancellationToken: token)
      );

      await UniTask.WhenAll(
        SetThermalCue(false, 0, token),
        UniTask.Delay((int)(cycleLengthSecs * 1000), cancellationToken: token)
      );
    }


    //wifi thermal cue
    // private async UniTask SetThermalCue(bool on, int setIntensity, CancellationToken token)
    // {
    //   string activeMode = on ? mode : "off";
    //   Log.Info("[{logType}:{cueType}] thermal cue intensity is now: {intensity} with {mode}", "CUES", "THERMAL", setIntensity, activeMode);

    //   if (CuesActive.UseApis)
    //   {
    //     try
    //     {
    //       UnityWebRequest request = UnityWebRequest.Get($"http://192.168.1.193/configure?mode={activeMode}&intensity={setIntensity}");
    //       await request.SendWebRequest().WithCancellation(timeoutController.Timeout(TimeSpan.FromSeconds(2)));
    //     }
    //     catch (Exception ex)
    //     {
    //       Debug.Log(ex);
    //     }
    //   }
    // }

    //serial thermal cue
    private async UniTask SetThermalCue(bool on, int setIntensity, CancellationToken token)
    {
      string activeMode = on ? mode : "off";
      Log.Info("[{logType}:{cueType}] thermal cue intensity is now: {intensity} with {mode}", "CUES", "THERMAL", setIntensity, activeMode);

      if (CuesActive.UseApis && serialPort != null && serialPort.IsOpen)
      {
        try
        {
          // Send command to Arduino via serial
          string command = $"{activeMode},{setIntensity}";
          await UniTask.RunOnThreadPool(() => serialPort.WriteLine(command));
          Log.Debug("[{logType}:{cueType}] Sent command to Arduino: {command}", "CUES", "THERMAL", command.Trim());
        }
        catch (Exception ex)
        {
          Log.Error("[{logType}:{cueType}] Serial communication error: {error}", "CUES", "THERMAL", ex.Message);
        }
      }
    }

    //wifi thermal cue stop
    // public async UniTask Stop() => await SetThermalCue(false, 0, CancellationToken.None);


    //serial thermal cue stop
    public async UniTask Stop() 
    {
      await SetThermalCue(false, 0, CancellationToken.None);
    }
  }
}