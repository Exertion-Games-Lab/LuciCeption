using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace LuciReality.UI
{
  public class FanCueLogic
  {
    private bool enabled;
    private float intensity;
    int cyclesToFullIntensity;
    float cycleLengthSecs;

    TimeoutController timeoutController = new TimeoutController();

    public FanCueLogic() { }

    public void Activate(
      bool enabled,
      float intensity,
      int cyclesToFullIntensity,
      float cycleLengthSecs)
    {
      this.cyclesToFullIntensity = cyclesToFullIntensity;
      this.cycleLengthSecs = cycleLengthSecs;
      this.enabled = enabled;
      this.intensity = intensity;
    }

    public async UniTask Run(CancellationToken token)
    {
      if (!enabled) { return; }

      await UniTask.SwitchToMainThread();
      Log.Info("[{logType}:{cueType}] beginning fan cues.", "FAN", "CUES");

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
      int speed = Mathf.RoundToInt((int)Mathf.Lerp(1, 9, cycles / (float)cyclesToFullIntensity) * intensity);

      await UniTask.WhenAll(
        SetFanCue(speed, token),
        UniTask.Delay((int)(cycleLengthSecs * 1000), cancellationToken: token)
      );

      await UniTask.WhenAll(
        SetFanCue(1, token),
        UniTask.Delay((int)(cycleLengthSecs * 1000), cancellationToken: token)
      );
    }

    private async UniTask SetFanCue(int speed, CancellationToken token)
    {
      Log.Info("[{logType}:{cueType}] fan cue speed is now: {intensity}", "CUES", "FAN", speed);

      if (CuesActive.UseApis)
      {
        try
        {
          UnityWebRequest request = UnityWebRequest.Get($"https://maker.ifttt.com/trigger/fan_speed_{speed}/with/key/88IAzsMm2FG355HNDbZjq");
          await request.SendWebRequest().WithCancellation(timeoutController.Timeout(TimeSpan.FromSeconds(2)));
        }
        catch (Exception ex)
        {
          Debug.Log(ex);
        }
      }
    }

    public async UniTask Stop() => await SetFanCue(1, CancellationToken.None);
  }
}