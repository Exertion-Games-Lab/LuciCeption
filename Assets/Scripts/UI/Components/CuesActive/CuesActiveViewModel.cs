using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

using Unity.AppUI.MVVM;
using Unity.Properties;
using System.Threading;
using Unity.Logging;
using LuciReality.Audio;
using System;

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
      AudioFile = cueParametersViewModel.AudioClipFile;

      cueParameters.PropertyChanged += (_, evt) =>
      {
        if (evt.PropertyName == nameof(CueParametersViewModel.LightsColour))
        {
          Color = cueParameters.LightsColour;
        }

        if (evt.PropertyName == nameof(CueParametersViewModel.AudioClipFile))
        {
          AudioFile = cueParametersViewModel.AudioClipFile;
        }
      };
    }

    [ObservableProperty]
    private Color _color;

    [ObservableProperty]
    private string _audioFile;

    [ObservableProperty]
    private AudioClip _audioClip;

    private TimeoutController timeoutController = new TimeoutController();

    public async UniTask<AudioClip> GetAudioClip()
    {
      await UniTask.SwitchToMainThread();
      var req = UnityWebRequestMultimedia.GetAudioClip(AudioFile, AudioType.WAV);
      await req.SendWebRequest().WithCancellation(timeoutController.Timeout(TimeSpan.FromSeconds(1)));
      return DownloadHandlerAudioClip.GetContent(req);
    }

    public string ThermalCueMode() => cueParameters.ThermalMode;
    public float VisualCueIntensity() => CuesActive.UseSettings ? cueParameters.LightsIntensity : 1;
    public float AuditoryCueIntensity() => CuesActive.UseSettings ? cueParameters.AudioVolume : 1;
    public float ThermalCueIntensity() => CuesActive.UseSettings ? cueParameters.ThermalIntensity : 1;
    public float FanCueIntensity() => CuesActive.UseSettings ? cueParameters.FanIntensity : 1;
    
    public bool VisualCueActive() => !CuesActive.UseSettings || cueParameters.LightsEnabled;
    public bool FanCueActive() => !CuesActive.UseSettings || cueParameters.FanEnabled;
    public bool ThermalCueActive() => !CuesActive.UseSettings || cueParameters.ThermalEnabled;
    public bool AuditoryCueActive() => !CuesActive.UseSettings || cueParameters.AudioEnabled;
  }

}