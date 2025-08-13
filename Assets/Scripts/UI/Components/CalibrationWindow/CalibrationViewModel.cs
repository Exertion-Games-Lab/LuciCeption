using System;
using LuciReality.OpenBCI;
using Unity.AppUI.MVVM;
using Unity.Properties;

namespace LuciReality.UI
{
  [System.Serializable]
  [ObservableObject]
  public partial class CalibrationViewModel
  {
    public CalibrationViewModel(IServiceProvider services)
    {
      var openBci = services.GetRequiredService<OpenBCIClient>();

      _isConnected = openBci.CurrentState == OpenBciClientState.CONNECTED;
    }

    [ObservableProperty] private bool _isCalibrating;
    [ObservableProperty] private bool _isConnected;

    [AlsoNotifyChangeFor(nameof(LeftCountMessage))]
    [AlsoNotifyChangeFor(nameof(LeftLRMessage))]
    [ObservableProperty] private int _leftSignalCount;

    [AlsoNotifyChangeFor(nameof(RightCountMessage))]
    [AlsoNotifyChangeFor(nameof(RightLRMessage))]
    [ObservableProperty] private int _rightSignalCount;

    [AlsoNotifyChangeFor(nameof(MinAmplitudeMessage))]
    [ObservableProperty] private int _minAmplitude;

    [AlsoNotifyChangeFor(nameof(MaxAmplitudeMessage))]
    [ObservableProperty] private int _maxAmplitude;

    [CreateProperty(ReadOnly = true)]
    public string LeftCountMessage => $"Left Count: {LeftSignalCount}";
    [CreateProperty(ReadOnly = true)]
    public string LeftLRMessage => "Left Signal: " + string.Concat(System.Linq.Enumerable.Repeat("LR", LeftSignalCount / 2));

    [CreateProperty(ReadOnly = true)]
    public string RightCountMessage => $"Right Count: {RightSignalCount}";
    [CreateProperty(ReadOnly = true)]
    public string RightLRMessage => "Right Signal: " + string.Concat(System.Linq.Enumerable.Repeat("LR", RightSignalCount / 2));

    [CreateProperty(ReadOnly = true)]
    public string MinAmplitudeMessage => $"Min Amplitude: {MinAmplitude}";

    [CreateProperty(ReadOnly = true)]
    public string MaxAmplitudeMessage => $"Max Amplitude: {MaxAmplitude}";
  }
}