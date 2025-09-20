using System.Threading;
using Cysharp.Threading.Tasks;
using LuciReality.Audio;
using Unity.Logging;
using UnityEngine;

namespace LuciReality.UI
{
  public class AuditoryCueLogic
  {
    private bool enabled;
    private float intensity;
    int cyclesToFullIntensity;
    float cycleLengthSecs;
    float maxVolume;
    AudioClip audioClip;

    public AuditoryCueLogic() { }

    public void Activate(
      bool enabled,
      float intensity,
      int cyclesToFullIntensity,
      float cycleLengthSecs,
      AudioClip audioClip,
      float maxVolume = 0.2f)
    {
      this.cyclesToFullIntensity = cyclesToFullIntensity;
      this.cycleLengthSecs = cycleLengthSecs;
      this.enabled = enabled;
      this.intensity = intensity;
      this.audioClip = audioClip;
      this.maxVolume = maxVolume;
    }

    public async UniTask Run(CancellationToken token)
    {
      if (!enabled) { return; }

      await UniTask.SwitchToMainThread();
      Log.Info("[{logType}:{cueType}] beginning auditory cues.", "CUES", "AUDITORY");

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
      float volume = Mathf.Lerp(0, maxVolume * intensity, cycles / (float)cyclesToFullIntensity);
      Log.Info("[{logType}:{cueType}] playing audio cue clip at volume: {volume}.", "CUES", "AUDITORY", volume);
      AudioSingleton.Instance.AudioSource.volume = volume;
      AudioSingleton.Instance.AudioSource.PlayOneShot(audioClip);
      await UniTask.Delay((int)(cycleLengthSecs * 2 * 1000), cancellationToken: token);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async UniTask Stop()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
      AudioSingleton.Instance.AudioSource.volume = maxVolume;
      AudioSingleton.Instance.AudioSource.Stop();
    }
  }
}