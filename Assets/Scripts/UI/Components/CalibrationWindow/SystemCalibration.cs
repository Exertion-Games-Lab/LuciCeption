using LuciReality.OpenBCI;
using Unity.AppUI.Core;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuciReality.UI
{
  [UxmlElement]
  public partial class SystemCalibration : VisualElement
  {
    public SystemCalibration()
    {
      Heading pageHeading = new Heading("System Calibration (LR Signal)");
      pageHeading.size = HeadingSize.XL;

      Heading explanationHeading = new Heading("Purpose of Calibration");
      explanationHeading.size = HeadingSize.M;
      Text explanationContent = new Text("For best results, the system must be calibrated before use. This is so the system can properly understand what kind of signals your eye movements make.");

      Heading instructionsHeading = new Heading("Instructions"); ;
      instructionsHeading.size = HeadingSize.M;
      Text instructionsContent = new Text("Before beginning system calibration, ensure XXX. Once setup, perform X consecutive LR signals (look from far left to far right with your eyes closed).");

      Divider divider = new Divider();

      Heading calibrationHeading = new Heading("Calibration");
      calibrationHeading.size = HeadingSize.M;
      Heading lastCalibrationAttempt = new Heading("Last Calibration Attempt");
      lastCalibrationAttempt.size = HeadingSize.S;
      Text calibrationInfoNumPeaks = new Text("Number of Peaks Detected: 16");
      Text calibrationInfoNumSignals = new Text("Number of LR Signals: 8");
      Text calibrationInfoPeakAmplitudes = new Text("Peak Amplitutdes: 20 mV, -18 mV");
      Text calibrationInfoSignalLength = new Text("Total Signal Length: 00m 20s");

      // TODO: Graph of EOG signal?

      Unity.AppUI.UI.Button startCalibration = new Unity.AppUI.UI.Button();
      startCalibration.title = "Start Calibration Attempt";
      startCalibration.clicked += StartCalibrationAction;

      Add(pageHeading);
      Add(explanationHeading);
      Add(explanationContent);
      Add(instructionsHeading);
      Add(instructionsContent);
      Add(divider);
      Add(calibrationHeading);
      Add(lastCalibrationAttempt);
      Add(calibrationInfoNumPeaks);
      Add(calibrationInfoNumSignals);
      Add(calibrationInfoPeakAmplitudes);
      Add(calibrationInfoSignalLength);
      Add(startCalibration);
    }

    public async void StartCalibrationAction() => await OpenBCIClient.Instance.StartCalibration();
  }
}