using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuciReality.UI
{
  public class VisualCueLogic
  {
    VisualElement visualCue;
    private bool enabled;
    private float intensity;
    int cyclesToFullIntensity;
    float cycleLengthSecs;
    Color offColor = new(0, 0, 0, 1);
    Color onColor;

    public VisualCueLogic() { }

    public void Activate(
      bool enabled,
      float intensity,
      int cyclesToFullIntensity,
      float cycleLengthSecs,
      VisualElement visualCue,
      Color onColor)
    {
      this.visualCue = visualCue;
      this.onColor = onColor;
      this.cyclesToFullIntensity = cyclesToFullIntensity;
      this.cycleLengthSecs = cycleLengthSecs;
      this.enabled = enabled;
      this.intensity = intensity;
    }

    public async UniTask Run(CancellationToken token)
    {
      if (!enabled) { return; }
      await UniTask.SwitchToMainThread();
      Log.Info("[{logType}:{cueType}] beginning visual cues.", "CUES", "VISUAL");

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
      float t = cycles / (float)cyclesToFullIntensity * intensity;
      Color color = Color.Lerp(offColor, onColor, t);

      Log.Info(
        "[{logType}:{cueType}] starting flash cycle with intensity of '{intensity}' and flash count of '{count}'",
        "CUES",
        "VISUAL",
        t,
        cycleLengthSecs
      );

      bool active = true;
      for (int i = 0; i < cycleLengthSecs; i++)
      {
        visualCue.style.backgroundColor = active ? color : offColor;
        active = !active;
        await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: token); // 1 second
      }

      Log.Info(
        "[{logType}:{cueType}] ending flash cycle",
        "CUES",
        "VISUAL"
      );

      visualCue.style.backgroundColor = offColor;
      await UniTask.Delay((int)(cycleLengthSecs * 1000), ignoreTimeScale: true, cancellationToken: token);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async UniTask Stop()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
      visualCue.style.backgroundColor = offColor;
    }
  }
}