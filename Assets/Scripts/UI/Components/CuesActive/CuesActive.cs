using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LuciReality.Audio;
using LuciReality.OpenBCI;
using Unity.AppUI.Navigation;
using Unity.Logging;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LuciReality.UI
{
  public class CuesActive : VisualElement, INavigationScreen
  {
    public static bool IsTesting = false; // use the LR signal to activate
    public static readonly bool UseApis = true; // send network requests
    public static readonly bool UseSettings = true; // use the cue settings the user has selected

    public const string VisualTreeResourcePath = "CuesActive";
    public const string className = "luci-reality--cues-active";
    private CuesActiveViewModel viewModel;

    // float StartDelaySeconds { get => IsTesting ? 1 : 10*60; } // how long before cues should start.
    //for demo, set delay to 10 seconds
    float StartDelaySeconds { get => IsTesting ? 1 : 10; } // how long before cues should start.
    int CyclesToFull { get => IsTesting ? 1 : 10; } // how many cycles to full intensity.
    int motionDetectedPause { get => IsTesting ? 20*1000 : 10*60*1000; } // how long should the system pause on motion detected.
    float cycleSecs = 10; // how long a cycle takes.
    int activationsCount = 0; // keeps track of how many times the cues have activated.

    bool cuesActive = false;
    bool startOnConnect = false;
    bool motionDetected = false;

    Color startColor = new(0, 0, 0);
    VisualElement visualCueUiElement;
    CancellationTokenSource cancelCues;
    CancellationTokenSource cancelPause;
    CancellationTokenSource cancelStart;

    OpenBCIClient openBCIClient;
    AuditoryCueLogic auditoryCue;
    VisualCueLogic visualCue;
    ThermalCueLogic thermalCueLogic;
    FanCueLogic fanCueLogic;

    public CuesActive(CuesActiveViewModel viewModel, OpenBCIClient openBCIClient)
    {
      this.openBCIClient = openBCIClient;
      this.viewModel = viewModel;

      auditoryCue = new AuditoryCueLogic();
      visualCue = new VisualCueLogic();
      thermalCueLogic = new ThermalCueLogic();
      fanCueLogic = new FanCueLogic();

      openBCIClient.Connected += OnConnected;

      AddToClassList(className);
      var asset = Resources.Load<VisualTreeAsset>(VisualTreeResourcePath);
      asset.CloneTree(this);

      visualCueUiElement = this.Q<VisualElement>("visual-cue-element");
      visualCueUiElement.dataSource = viewModel;
    }

    private void ActivateCues()
    {
      if (cuesActive) { return; }
      activationsCount++;
      visualCueUiElement.style.backgroundColor = startColor;

      Log.Info("[{logType}] Activating the system. Total activations: {activationsCount}", "CUES", activationsCount);

      // Create the token which lets us cancel the cues.

      cancelCues = new CancellationTokenSource();
      var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancelCues.Token, Application.exitCancellationToken);
      var cancelCuesClosure = cancelCues;

      UniTask.RunOnThreadPool(async () =>
      {
        cuesActive = true;
        AudioClip audioClip = null;
        try
        {
          audioClip = await viewModel.GetAudioClip();
        }
        catch { }

        await ResetMotionDetector(cancel.Token);

        visualCue.Activate(
          viewModel.VisualCueActive(),
          viewModel.VisualCueIntensity(),
          CyclesToFull,
          cycleSecs,
          visualCueUiElement,
          viewModel.Color
        );
        auditoryCue.Activate(
          viewModel.AuditoryCueActive(),
          viewModel.AuditoryCueIntensity(),
          CyclesToFull,
          cycleSecs,
          audioClip
        );
        thermalCueLogic.Activate(
          viewModel.ThermalCueActive(),
          viewModel.ThermalCueIntensity(),
          CyclesToFull,
          cycleSecs,
          viewModel.ThermalCueMode()
        );
        fanCueLogic.Activate(
          viewModel.FanCueActive(),
          viewModel.FanCueIntensity(),
          CyclesToFull,
          cycleSecs
        );
        try { await UpdateCues(cancel.Token); }
        catch (OperationCanceledException e) { Debug.Log(e); }
        finally
        {
          Debug.Log("Disposing cancellation tokens.");
          await UniTask.SwitchToMainThread();
          cancelCuesClosure.Dispose();
          cancel.Dispose();
          cuesActive = false;

          throw new OperationCanceledException();
        }
      }).Forget();
    }

    private static async UniTask ResetMotionDetector(CancellationToken cancel)
    {
      UnityWebRequest request = new UnityWebRequest(ParticipantConfig.SetMotionCommand(), "GET");
      var res = await request.SendWebRequest().WithCancellation(cancel);
      Log.Info("[{logType}] Cleared motion detected flag.", "CUES");
    }

    private void StopCues() => cancelCues.Cancel();

    private async UniTask DetectMotion(CancellationToken token)
    {
      await UniTask.SwitchToMainThread();

      while (true)
      {
        Log.Info("[{logType}] Sending motion detection get request", "MOTION");
        UnityWebRequest request = new(ParticipantConfig.GetMotionCommand(), "GET");
        request.SetRequestHeader("Content-Type", "application/json");
        request.downloadHandler = new DownloadHandlerBuffer();
        await request.SendWebRequest().WithCancellation(token);
        var text = request.downloadHandler.text;

        JObject motionState = JObject.Parse(text);
        bool isDetected = (bool)motionState["value"];

        Log.Debug("[{logType}] motion detection state: {isDetected}", "MOTION", isDetected);
        if (isDetected)
        {
          await OnMotionDetected();
        }

        await UniTask.Delay(10 * 1000, ignoreTimeScale: true, cancellationToken: token);
      }

    }

    #region CUE UPDATES
    private async UniTask UpdateCues(CancellationToken token)
    {
      try
      {
        await UniTask.WhenAll(
          UniTask.Create(visualCue.Run, token),
          UniTask.Create(auditoryCue.Run, token),
          UniTask.Create(thermalCueLogic.Run, token),
          UniTask.Create(fanCueLogic.Run, token),
          UniTask.Create(DetectMotion, token)
        );
      }
      catch (OperationCanceledException e)
      {
        Log.Info("[{logType}] force stopping all cues!", "CUES");

        await UniTask.WhenAll(
          visualCue.Stop(),
          auditoryCue.Stop(),
          thermalCueLogic.Stop(),
          fanCueLogic.Stop()
        );

        throw e;
      }
    }
    #endregion

    #region CALLBACKS
    public void OnEnter(NavController controller, NavDestination destination, Argument[] args)
    {
      var testing = args[0];
      IsTesting = bool.Parse(testing.value);

      Focus();
      visualCueUiElement.style.backgroundColor = startColor;

      if (IsTesting)
      {
        OnLrSignalDetected();
        return;
      }


      cancelStart = new CancellationTokenSource();
      var cancelStartDetection = CancellationTokenSource.CreateLinkedTokenSource(cancelStart.Token, Application.exitCancellationToken);
      UniTask.RunOnThreadPool(async () =>
      {
        // await start delay

        Log.Info("[{logType}] waiting for {delaySec} seconds before allowing LR signal detection to start the cues.", "CUES", StartDelaySeconds);

        try
        {
          await UniTask.Delay(TimeSpan.FromSeconds(StartDelaySeconds), ignoreTimeScale: true, cancellationToken: cancelStartDetection.Token);

        Log.Info("[{logType}] LR signal detection will now start the cues.", "CUES");
          openBCIClient.LrSignalDetected += ActivateCues;
        }
        catch (OperationCanceledException ex)
        {
          Log.Debug(ex);
        }

        cancelStart.Dispose();
        cancelStartDetection.Dispose();
      }).Forget();

      UniTask.RunOnThreadPool(async () =>
      {
        try
        {
          await UniTask.Delay(GetDelayTimeSpan(), ignoreTimeScale: true, cancellationToken: cancelStartDetection.Token);
          ActivateCues();
        }
        catch (OperationCanceledException ex)
        {
          Log.Debug(ex);
        }
      }).Forget();
      
      // Start LR signal detection on python server.
      if (openBCIClient.IsConnected)
      {
        StartDetection();
      }
      else
      {
        startOnConnect = true;
      }
    }

    public void OnExit(NavController controller, NavDestination destination, Argument[] args)
    {
      if (cuesActive)
      {
        StopCues();
      }

      startOnConnect = false;

      if (!IsTesting)
      {
        if (openBCIClient.IsConnected)
        {
          UniTask.RunOnThreadPool(openBCIClient.StopSystem, cancellationToken: Application.exitCancellationToken);
        }

        openBCIClient.LrSignalDetected -= ActivateCues;
      }

      cancelPause?.Cancel();
      motionDetected = false;

    }

    private void OnLrSignalDetected()
    {
      if (motionDetected) { return; }
      ActivateCues();
    }

    private async UniTask OnMotionDetected()
    {
      if (!cuesActive) { return; }
      await UniTask.SwitchToMainThread();
      motionDetected = true;
      Log.Info("[{logType}] pausing for motion detection!", "CUES");
      cancelPause = new CancellationTokenSource();
      var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancelPause.Token, Application.exitCancellationToken);
      StopCues();
      await UniTask.Delay(motionDetectedPause, ignoreTimeScale: true, cancellationToken: cancel.Token);
      Log.Info("[{logType}] resuming system after motion detection period over", "CUES");
      motionDetected = false;

      await ResetMotionDetector(CancellationToken.None);

      cancelPause.Dispose();
      cancel.Dispose();
      cancelStart.Dispose();
      cancelPause = null;

      ActivateCues();
    }

    private void OnConnected()
    {
      if (startOnConnect)
      {
        StartDetection();
      }
    }

    private void StartDetection()
    {
      UniTask.RunOnThreadPool(openBCIClient.StartSystem, cancellationToken: Application.exitCancellationToken).Forget();
      startOnConnect = false;
    }

    private TimeSpan GetDelayTimeSpan()
    {
        var hours = viewModel.DelayHours;
        var minutes = viewModel.DelayMinutes;
        var seconds = viewModel.DelaySeconds;
        var totalSeconds = hours * 3600 + minutes * 60 + seconds;

        return TimeSpan.FromSeconds(totalSeconds);
    }
  }
  #endregion
}