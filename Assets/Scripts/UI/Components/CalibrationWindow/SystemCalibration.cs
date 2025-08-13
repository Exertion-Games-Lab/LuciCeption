using Cysharp.Threading.Tasks;
using LuciReality.OpenBCI;
using Unity.AppUI.Core;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using Unity.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuciReality.UI
{
  [UxmlElement]
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

    public SystemCalibration()
    {

      AddToClassList(className);

      var asset = Resources.Load<VisualTreeAsset>("SystemCalibration");
      asset.CloneTree(this);

      CalibrateSystemButton.clicked += StartCalibration;

      OpenBCIClient.Instance.Calibrated += OnCalibrated;
      OpenBCIClient.Instance.Disconnected += OnDisconnected;
      OpenBCIClient.Instance.Connected += OnConnected;

      // Heading pageHeading = new Heading("System Calibration (LR Signal)");
      // pageHeading.size = HeadingSize.XL;

      // Heading explanationHeading = new Heading("Purpose of Calibration");
      // explanationHeading.size = HeadingSize.M;
      // Text explanationContent = new Text("For best results, the system must be calibrated before use. This is so the system can properly understand what kind of signals your eye movements make.");

      // Heading instructionsHeading = new Heading("Instructions"); ;
      // instructionsHeading.size = HeadingSize.M;
      // Text instructionsContent = new Text("Before beginning system calibration, ensure XXX. Once setup, perform X consecutive LR signals (look from far left to far right with your eyes closed).");

      // Divider divider = new Divider();

      // Heading calibrationHeading = new Heading("Calibration");
      // calibrationHeading.size = HeadingSize.M;
      // Heading lastCalibrationAttempt = new Heading("Last Calibration Attempt");
      // lastCalibrationAttempt.size = HeadingSize.S;
      // Text calibrationInfoNumPeaks = new Text("Number of Peaks Detected: 16");
      // Text calibrationInfoNumSignals = new Text("Number of LR Signals: 8");
      // Text calibrationInfoPeakAmplitudes = new Text("Peak Amplitutdes: 20 mV, -18 mV");
      // Text calibrationInfoSignalLength = new Text("Total Signal Length: 00m 20s");

      // // TODO: Graph of EOG signal?

      // Unity.AppUI.UI.Button startCalibration = new Unity.AppUI.UI.Button();
      // startCalibration.title = "Start Calibration Attempt";
      // startCalibration.clicked += StartCalibrationAction;

      // Add(pageHeading);
      // Add(explanationHeading);
      // Add(explanationContent);
      // Add(instructionsHeading);
      // Add(instructionsContent);
      // Add(divider);
      // Add(calibrationHeading);
      // Add(lastCalibrationAttempt);
      // Add(calibrationInfoNumPeaks);
      // Add(calibrationInfoNumSignals);
      // Add(calibrationInfoPeakAmplitudes);
      // Add(calibrationInfoSignalLength);
      // Add(startCalibration);
    }

    public void OnEnter(NavController controller, NavDestination destination, Argument[] args)
    {
      // throw new System.NotImplementedException();
    }

    public void OnExit(NavController controller, NavDestination destination, Argument[] args)
    {
      // throw new System.NotImplementedException();
    }

    public async void StartCalibration() => await OpenBCIClient.Instance.StartCalibration();

    public async void OnCalibrated(CalibrationResult result)
    {
      await UniTask.SwitchToMainThread(); // Have to be on the main thread to modify the UI like this.

      Log.Debug(result);
      Debug.Log(LeftLrsText.text);
      LeftLrsText.text = $"Left LRs: {result.leftSignals}";
      Debug.Log(LeftLrsText.text);
      LeftCountText.text = $"Left Count: {result.leftSignals}";
      RightLrsText.text = $"Right LRs: {result.rightSignals}";
      RightCountText.text = $"Right Count: {result.rightSignals}";
      MinAmplitudeText.text = $"Min Amplitude: {result.minAmplitude}";
      MaxAmplitudeText.text = $"Max Amplitude: {result.maxAmplitude}";
      Debug.Log("SET!");
    }

    public void OnDisconnected()
    {

    }

    public void OnConnected()
    {

    }

  }
}