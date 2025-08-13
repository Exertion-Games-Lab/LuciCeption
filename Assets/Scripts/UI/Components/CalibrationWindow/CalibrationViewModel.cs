using System;
using System.Threading;
using System.Threading.Tasks;
using LuciReality.OpenBCI;
using Unity.AppUI.MVVM;
using Unity.Properties;

namespace LuciReality.UI
{
  [System.Serializable]
  [ObservableObject]
  public partial class CalibrationViewModel
  {
    private OpenBCIClient openBCIClient;
    public CalibrationViewModel(OpenBCIClient openBCIClient)
    {
      this.openBCIClient = openBCIClient;
      openBCIClient.Connected += OnConnectedCommand.Execute;
      openBCIClient.Disconnected += OnDisconnectedCommand.Execute;
      openBCIClient.Calibrated += OnCalibratedCommand.Execute;

      _isConnected = openBCIClient.CurrentState == OpenBciClientState.CONNECTED;
    }

    [AlsoNotifyChangeFor(nameof(ThresholdMessage))]
    [ObservableProperty] private int _threshold = 5;

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

    [CreateProperty(ReadOnly = true)]
    public string ThresholdMessage => $"Threshold: {Threshold}";

    [ICommand]
    void OnConnected()
    {
      IsConnected = true;
    }

    [ICommand]
    void OnDisconnected()
    {
      IsConnected = false;
      IsCalibrating = false;
    }

    [ICommand]
    void OnCalibrated(CalibrationResult result)
    {
      IsCalibrating = false;
      LeftSignalCount = result.leftSignals;
      RightSignalCount = result.rightSignals;
      MinAmplitude = result.minAmplitude;
      MaxAmplitude = result.maxAmplitude;
    }

    [ICommand]
    async Task StartCalibrationAsync(CancellationToken token)
    {
      IsCalibrating = true;
      await openBCIClient.StartCalibration();
    }

    [ICommand]
    async Task IncrementThresholdAsync(CancellationToken token)
    {
      Threshold++;
      await openBCIClient.SetThreshold(Threshold, token);
    }

    [ICommand]
    async Task DecrementThresholdAsync(CancellationToken token)
    {
      if (Threshold == 0) { return; }
      Threshold--;
      await openBCIClient.SetThreshold(Threshold, token);
    }
  }
}