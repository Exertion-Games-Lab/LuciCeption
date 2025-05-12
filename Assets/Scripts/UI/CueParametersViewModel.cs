using Unity.AppUI.MVVM;
using Unity.Properties;
using UnityEngine;

namespace LuciReality.UI
{
  [System.Serializable]
  [ObservableObject]
  public partial class CueParametersViewModel
  {
    [ObservableProperty]
    private bool _isGenerating = false;

    [ObservableProperty]
    private string _promptText;

    [ObservableProperty]
    [AlsoNotifyChangeFor(nameof(LightsEnabled))]
    [AlsoNotifyChangeFor(nameof(LightsColour))]
    [AlsoNotifyChangeFor(nameof(LightsIntensity))]
    [AlsoNotifyChangeFor(nameof(AudioEnabled))]
    [AlsoNotifyChangeFor(nameof(AudioVolume))]
    [AlsoNotifyChangeFor(nameof(LraEnabled))]
    [AlsoNotifyChangeFor(nameof(LraIntensity))]
    [AlsoNotifyChangeFor(nameof(EmsEnabled))]
    [AlsoNotifyChangeFor(nameof(EmsIntensity))]
    [AlsoNotifyChangeFor(nameof(FanEnabled))]
    [AlsoNotifyChangeFor(nameof(FanIntensity))]
    private CueParametersModel _model = new();

    [CreateProperty]
    public bool LightsEnabled
    {
      get => _model.Lights.Enabled;
      set => SetProperty(_model.Lights.Enabled, value, _model, (u, n) => u.Lights.Enabled = n);
    }

    [CreateProperty]
    public Color LightsColour
    {
      get => _model.Lights.Colour;
      set => SetProperty(_model.Lights.Colour, value, _model, (u, n) => u.Lights.Colour = n);
    }
    public void SetLightsColourWithoutNotify(Color colour) => _model.Lights.Colour = colour;

    [CreateProperty]
    public float LightsIntensity
    {
      get => _model.Lights.Intensity;
      set => SetProperty(_model.Lights.Intensity, value, _model, (u, n) => u.Lights.Intensity = n);
    }


    [CreateProperty]
    public bool AudioEnabled
    {
      get => _model.Audio.Enabled;
      set => SetProperty(_model.Audio.Enabled, value, _model, (u, n) => u.Audio.Enabled = n);
    }
    [CreateProperty]
    public float AudioVolume
    {
      get => _model.Audio.Volume;
      set => SetProperty(_model.Audio.Volume, value, _model, (u, n) => u.Audio.Volume = n);
    }

    [CreateProperty]
    public bool LraEnabled
    {
      get => _model.Lra.Enabled;
      set => SetProperty(_model.Lra.Enabled, value, _model, (u, n) => u.Lra.Enabled = n);
    }
    [CreateProperty]
    public float LraIntensity
    {
      get => _model.Lra.Intensity;
      set => SetProperty(_model.Lra.Intensity, value, _model, (u, n) => u.Lra.Intensity = n);
    }

    [CreateProperty]
    public bool EmsEnabled
    {
      get => _model.Lights.Enabled;
      set => SetProperty(_model.Lights.Enabled, value, _model, (u, n) => u.Lights.Enabled = n);
    }
    [CreateProperty]
    public float EmsIntensity
    {
      get => _model.Ems.Intensity;
      set => SetProperty(_model.Ems.Intensity, value, _model, (u, n) => u.Ems.Intensity = n);
    }

    [CreateProperty]
    public bool FanEnabled
    {
      get => _model.Fan.Enabled;
      set => SetProperty(_model.Fan.Enabled, value, _model, (u, n) => u.Fan.Enabled = n);
    }
    [CreateProperty]
    public float FanIntensity
    {
      get => _model.Fan.Intensity;
      set => SetProperty(_model.Fan.Intensity, value, _model, (u, n) => u.Fan.Intensity = n);
    }
  }
}