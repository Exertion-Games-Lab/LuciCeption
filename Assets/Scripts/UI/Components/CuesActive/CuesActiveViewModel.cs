using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

using Unity.AppUI.MVVM;
using Unity.Properties;
using System.Threading;
using Unity.Logging;
using LuciReality.Audio;

namespace LuciReality.UI
{
  [System.Serializable]
  [ObservableObject]
  public partial class CuesActiveViewModel
  {
    CueParametersViewModel cueParameters;

    public CuesActiveViewModel(CueParametersViewModel cueParametersViewModel)
    {
      cueParameters = cueParametersViewModel;

      Color = cueParametersViewModel.LightsColour;
      AudioFile = cueParametersViewModel.SelectedAudioFile;
      SetAudioClip();

      cueParameters.PropertyChanged += (_, evt) =>
      {
        if (evt.PropertyName == nameof(CueParametersViewModel.LightsColour))
        {
          Color = cueParameters.LightsColour;
        }

        if (evt.PropertyName == nameof(CueParametersViewModel.SelectedAudioFile))
        {
          AudioFile = cueParametersViewModel.SelectedAudioFile;

          SetAudioClip();
        }
      };
    }

    [ObservableProperty]
    private Color _color;

    [ObservableProperty]
    private string _audioFile;

    [ObservableProperty]
    private AudioClip _audioClip;

    private void SetAudioClip()
    {
      UniTask.RunOnThreadPool(async () =>
      {
        await UniTask.SwitchToMainThread();
        var req = UnityWebRequestMultimedia.GetAudioClip(AudioFile, AudioType.WAV);
        var res = await req.SendWebRequest();
        AudioClip = DownloadHandlerAudioClip.GetContent(req);
      });
    }
    public async UniTask SetFanCue(int speed, CancellationToken token)
    {
      if (!CuesActive.UseSettings && !cueParameters.FanEnabled) { return; }
      if (CuesActive.UseSettings)
      {
        speed = Mathf.Max(Mathf.RoundToInt(cueParameters.FanIntensity * speed), 1);
      }

      Log.Info("[{logType}] fan cue speed is now: {speed}", "CUES", speed);
      if (!CuesActive.UseApis) { return; }
      UnityWebRequest request = UnityWebRequest.Get($"https://maker.ifttt.com/trigger/fan_intensity_{speed}/with/key/88IAzsMm2FG355HNDbZjq");
      await request.SendWebRequest().WithCancellation(token);
    }

    public async UniTask SetThermalCue(bool active, int intensity, CancellationToken token)
    {
      string mode = active ? cueParameters.ThermalMode : "off";

      if (CuesActive.UseSettings)
      {
        intensity = Mathf.RoundToInt(intensity * cueParameters.ThermalIntensity);
      }

      Log.Info("[{logType}] thermal cue intensity is now: {intensity} with {mode}", "CUES", intensity, mode);
      if (!CuesActive.UseApis) { return; }
      const string ip = "192.168.1.183";
      UnityWebRequest request = UnityWebRequest.Get($"http://{ip}/configure?mode={mode}&intensity={intensity}");
      await request.SendWebRequest().WithCancellation(token);
    }

    public void PlayAudio(float volume)
    {
      if (CuesActive.UseSettings && !cueParameters.AudioEnabled) { return; }
      if (CuesActive.UseSettings)
      {
        volume = Mathf.RoundToInt(cueParameters.FanIntensity * volume);
      }

      Log.Info("[{logType}] playing audio cue clip at volume: {volume}.", "CUES", volume);
      AudioSingleton.Instance.AudioSource.volume = volume;
      AudioSingleton.Instance.AudioSource.PlayOneShot(AudioClip);
    }

    public Color GetScaledColour(Color colour)
    {
      if (cueParameters.LightsEnabled || !CuesActive.UseSettings)
      {
        if (CuesActive.UseSettings)
        {
          return Color.Lerp(new Color(0, 0, 0), colour, cueParameters.LightsIntensity);
        }
        else
        {
          return colour;
        }
      }
      else
      {
        return new Color(0, 0, 0);
      }
    }

    public float VisualCueIntensity() => CuesActive.UseSettings ? cueParameters.LightsIntensity : 1;
    public float AuditoryCueIntensity() => CuesActive.UseSettings ? cueParameters.AudioVolume : 1;
    public float ThermalCueIntensity() => CuesActive.UseSettings ? 1 : 1;
    public float FanCueIntensity() => CuesActive.UseSettings ? cueParameters.FanIntensity : 1;
    
    public bool VisualCueActive() => !CuesActive.UseSettings || cueParameters.LightsEnabled;
    public bool FanCueActive() => !CuesActive.UseSettings || cueParameters.FanEnabled;
    public bool ThermalCueActive() => true;
    public bool AuditoryCueActive() => !CuesActive.UseSettings || cueParameters.AudioEnabled;
  }

}