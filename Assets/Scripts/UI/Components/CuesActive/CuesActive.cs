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
    public static readonly bool IsDebugging = true; // use the LR signal to activate
    public static readonly bool UseApis = false; // send network requests
    public static readonly bool UseSettings = true; // use the cue settings the user has selected

    public const string VisualTreeResourcePath = "CuesActive";
    public const string className = "luci-reality--cues-active";
    private CuesActiveViewModel viewModel;

    float delaySec = 5; // how long before cues should start.
    int cyclesToFull = 5; // how many cycles to full intensity.
    int motionDetectedPause = 5 * 60 * 1000; // how long should the system pause on motion detected.
    float cycleSecs = 10; // how long a cycle takes.
    int activationsCount = 0; // keeps track of how many times the cues have activated.

    bool cuesActive = false;
    bool startOnConnect = false;
    bool motionDetected = false;



    Color startColor = new(0, 0, 0);
    VisualElement visualCueUiElement;
    CancellationTokenSource cancelCues;
    CancellationTokenSource cancelPause;
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
        visualCue.Activate(
          viewModel.VisualCueActive(),
          viewModel.VisualCueIntensity(),
          cyclesToFull,
          cycleSecs,
          visualCueUiElement,
          viewModel.Color
        );
        auditoryCue.Activate(
          viewModel.AuditoryCueActive(),
          viewModel.AuditoryCueIntensity(),
          cyclesToFull,
          cycleSecs,
          await viewModel.GetAudioClip(cancel.Token)
        );
        thermalCueLogic.Activate(
          viewModel.ThermalCueActive(),
          viewModel.ThermalCueIntensity(),
          cyclesToFull,
          cycleSecs,
          viewModel.ThermalCueMode()
        );
        fanCueLogic.Activate(
          viewModel.FanCueActive(),
          viewModel.FanCueIntensity(),
          cyclesToFull,
          cycleSecs
        );
        try { await UpdateCues(cancel.Token); }
        catch { }
        await UniTask.SwitchToMainThread();
        cancelCuesClosure.Dispose();
        cancel.Dispose();
        cuesActive = false;
      });
    }
    private void StopCues() => cancelCues.Cancel();

    private async UniTask DetectMotion(CancellationToken token)
    {
      await UniTask.SwitchToMainThread();

      while (true)
      {
        Log.Info("[{logType}] Sending motion detection get request", "MOTION");
        UnityWebRequest request = new($"http://170.64.229.86:8080/get_motion_detected?key=xglluciception&participant=p1", "GET");
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

        await UniTask.Delay(10 * 1000, cancellationToken: token);
      }

    }

    #region CUE UPDATES
    private async UniTask UpdateCues(CancellationToken token)
    {
      Log.Info("[{logType}] waiting for {delaySec} seconds before starting cues.", "CUES", delaySec);
      await UniTask.Delay((int)delaySec * 1000, cancellationToken: token);

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
      Focus();
      visualCueUiElement.style.backgroundColor = startColor;

      if (CuesActive.IsDebugging)
      {
        OnLrSignalDetected();
        return;
      }

      openBCIClient.LrSignalDetected += ActivateCues;
      if (openBCIClient.IsConnected)
      {
        UniTask.RunOnThreadPool(openBCIClient.StartSystem, cancellationToken: Application.exitCancellationToken);
        startOnConnect = false;
      }
      else
      {
        startOnConnect = true;
      }
    }

    public void OnExit(NavController controller, NavDestination destination, Argument[] args)
    {
      startOnConnect = false;

      if (!IsDebugging)
      {
        if (openBCIClient.IsConnected)
        {
          UniTask.RunOnThreadPool(openBCIClient.StopSystem, cancellationToken: Application.exitCancellationToken);
        }

        openBCIClient.LrSignalDetected -= ActivateCues;
      }

      cancelPause?.Cancel();
      motionDetected = false;

      if (cuesActive)
      {
        StopCues();
      }
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
      await UniTask.Delay(10 * 1000, cancellationToken: cancel.Token); // 5 minutes
      Log.Info("[{logType}] resuming system after motion detection period over", "CUES");
      motionDetected = false;

      UnityWebRequest request = new UnityWebRequest($"http://170.64.229.86:8080/set_motion_detected?key=xglluciception&participant=p1&value=false", "GET");
      var res = await request.SendWebRequest().WithCancellation(cancel.Token);
      Log.Info("[{logType}] set motion detected state to false", "CUES");

      cancelPause.Dispose();
      cancel.Dispose();
      ActivateCues();
    }

    private void OnConnected()
    {
      if (startOnConnect)
      {
        UniTask.RunOnThreadPool(openBCIClient.StartSystem, cancellationToken: Application.exitCancellationToken);
      }
      startOnConnect = false;
    }
  }
  #endregion
}