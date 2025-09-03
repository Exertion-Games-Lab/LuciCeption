using Cysharp.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.Logging;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Networking;

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
  }
  
}