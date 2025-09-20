using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace LuciReality.UI
{
  public class ThermalCueLogic
  {
    private bool enabled;
    private float intensity;
    int cyclesToFullIntensity;
    float cycleLengthSecs;
    string mode;

    public ThermalCueLogic() { }

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

    private async UniTask SetThermalCue(bool on, int setIntensity, CancellationToken token)
    {
      string activeMode = on ? mode : "off";
      Log.Info("[{logType}:{cueType}] thermal cue intensity is now: {intensity} with {mode}", "CUES", "THERMAL", setIntensity, activeMode);

      if (CuesActive.UseApis)
      {
        UnityWebRequest request = UnityWebRequest.Get($"http://192.168.1.183/configure?mode={activeMode}&intensity={intensity}");
        await request.SendWebRequest().WithCancellation(token);
      }
    }

    public async UniTask Stop() => await SetThermalCue(false, 0, CancellationToken.None);
  }
}