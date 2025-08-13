using Cysharp.Threading.Tasks;
using LuciReality.OpenBCI;
using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using Unity.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuciReality.UI
{
  public partial class SystemCalibration : VisualElement, INavigationScreen
  {
    public const string className = "luci-reality--system-calibration";

    private Unity.AppUI.UI.Button CalibrateSystemButton => this.Q<Unity.AppUI.UI.Button>("calibrate-system-button");

    private Text LeftLrsText => this.Q<Text>("result-left-detected");
    private Text LeftCountText => this.Q<Text>("result-left-count");
    private Text RightLrsText => this.Q<Text>("result-right-detected");
    private Text RightCountText => this.Q<Text>("result-right-count");
    private Text MinAmplitudeText => this.Q<Text>("result-min-amplitude");
    private Text MaxAmplitudeText => this.Q<Text>("result-max-amplitude");
    private OpenBCIClient openBCIClient;

    public SystemCalibration(OpenBCIClient openBCIClient)
    {
      this.openBCIClient = openBCIClient;
      
      AddToClassList(className);

      var asset = Resources.Load<VisualTreeAsset>("SystemCalibration");
      asset.CloneTree(this);

      openBCIClient = LuciRealityApp.current.services.GetRequiredService<OpenBCIClient>();

      CalibrateSystemButton.clicked += StartCalibration;

      openBCIClient.Calibrated += OnCalibrated;
      openBCIClient.Disconnected += OnDisconnected;
      openBCIClient.Connected += OnConnected;
    }

    public void OnEnter(NavController controller, NavDestination destination, Argument[] args)
    {
      // throw new System.NotImplementedException();
    }

    public void OnExit(NavController controller, NavDestination destination, Argument[] args)
    {
      // throw new System.NotImplementedException();
    }

    public async void StartCalibration() => await openBCIClient.StartCalibration();

    public async void OnCalibrated(CalibrationResult result)
    {
      await UniTask.SwitchToMainThread(); // Have to be on the main thread to modify the UI like this.

      Log.Debug(result);
      LeftLrsText.text = $"Left LRs: {result.leftSignals}";
      LeftCountText.text = $"Left Count: {result.leftSignals}";
      RightLrsText.text = $"Right LRs: {result.rightSignals}";
      RightCountText.text = $"Right Count: {result.rightSignals}";
      MinAmplitudeText.text = $"Min Amplitude: {result.minAmplitude}";
      MaxAmplitudeText.text = $"Max Amplitude: {result.maxAmplitude}";
    }

    public void OnDisconnected()
    {

    }

    public void OnConnected()
    {

    }

  }
}