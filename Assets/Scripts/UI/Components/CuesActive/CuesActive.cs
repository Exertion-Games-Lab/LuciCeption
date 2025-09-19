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

    float delaySec = 5;

    int cyclesToFull = 5;
    float cycleSecs = 10;

    float maxVolume = 0.2f;

    bool cuesActive = false;

    bool startOnConnect = false;

    Color startColor = new(0, 0, 0);
    VisualElement visualCue;

    CancellationTokenSource cancelCues;
    OpenBCIClient openBCIClient;

    public CuesActive(CuesActiveViewModel viewModel, OpenBCIClient openBCIClient)
    {
      this.openBCIClient = openBCIClient;
      this.viewModel = viewModel;

      openBCIClient.Connected += OnConnected;

      AddToClassList(className);
      var asset = Resources.Load<VisualTreeAsset>(VisualTreeResourcePath);
      asset.CloneTree(this);

      visualCue = this.Q<VisualElement>("visual-cue-element");
      visualCue.dataSource = viewModel;
    }

    private void ActivateCues()
    {
      if (cuesActive) { return; }
      visualCue.style.backgroundColor = startColor;

      cancelCues = new CancellationTokenSource();
      var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancelCues.Token, Application.exitCancellationToken);

      var cancelCuesClosure = cancelCues;

      UniTask.RunOnThreadPool(async () =>
      {
        cuesActive = true;
        try
        {
          await UpdateCues(cancel.Token);
        }
        catch
        {

        }
        await UniTask.SwitchToMainThread();

        cancelCuesClosure.Dispose();
        cancel.Dispose();
        cuesActive = false;
      });
    }

    private void StopCues()
    {
      visualCue.style.backgroundColor = startColor;
      cancelCues.Cancel();
    }

    private void PauseCues()
    {
      StopCues();
      ActivateCues();
    }

    #region CUE UPDATES
    private async UniTask UpdateCues(CancellationToken token)
    {
      Log.Info("[{logType}] waiting for {delaySec} seconds before starting cues.", "CUES", delaySec);
      await UniTask.Delay((int)delaySec * 1000, cancellationToken: token);

      try
      {
        await UniTask.WhenAll(
          UniTask.Create(FlashVisualCue, token),
          UniTask.Create(RampAudio, token),
          UniTask.Create(ThermalCue, token),
          UniTask.Create(FanCue, token)
        );
      }
      catch (OperationCanceledException e)
      {
        Log.Info("[{logType}] force stopping all cues!", "CUES");

        await UniTask.WhenAll(
          viewModel.SetThermalCue(false, 0, CancellationToken.None),
          viewModel.SetFanCue(1, CancellationToken.None)
        );

        AudioSingleton.Instance.AudioSource.volume = maxVolume;
        AudioSingleton.Instance.AudioSource.Stop();

        throw e;
      }
    }

    private async UniTask ThermalCue(CancellationToken token)
    {
      await UniTask.SwitchToMainThread();
      if (!viewModel.ThermalCueActive()) { return; }
      Log.Info("[{logType}] beginning thermal cues.", "CUES");

      int cycles = 1;

      while (true)
      {
        int intensity = (int)Mathf.Lerp(0, 255, cycles / (float)cyclesToFull);

        await UniTask.WhenAll(
          viewModel.SetThermalCue(true, intensity, token),
          UniTask.Delay((int)(cycleSecs * 1000), cancellationToken: token)
        );

        await UniTask.WhenAll(
          viewModel.SetThermalCue(false, 0, token),
          UniTask.Delay((int)(cycleSecs * 1000), cancellationToken: token)
        );

        cycles++;
      }
    }

    private async UniTask FanCue(CancellationToken token)
    {
      await UniTask.SwitchToMainThread();
      if (!viewModel.FanCueActive()) { return; }
      Log.Info("[{logType}] beginning fan cue.", "CUES");

      int cycles = 1;

      while (true)
      {
        int intensity = (int)Mathf.Lerp(1, 9, cycles / (float)cyclesToFull);

        await viewModel.SetFanCue(intensity, token);
        await UniTask.Delay((int)(cycleSecs * 1000), cancellationToken: token);

        await viewModel.SetFanCue(0, token);
        await UniTask.Delay((int)(cycleSecs * 1000), cancellationToken: token);

        cycles++;
      }
    }

    private async UniTask FlashVisualCue(CancellationToken token)
    {
      await UniTask.SwitchToMainThread();
      if (!viewModel.VisualCueActive()) { return; }
      Log.Info("[{logType}] beginning visual cues.", "CUES");

      int cycles = 1;

      while (true)
      {
        float intensity = Math.Min(cycles / (float)cyclesToFull, 1);
        Color cueColour = Color.Lerp(startColor, viewModel.Color, intensity);

        // on cycle
        Log.Info("[{logType}] visual cue intensity is now: {intensity}", "CUES", intensity * viewModel.VisualCueIntensity());

        bool active = true;
        for (int i = 0; i < cycleSecs; i++)
        {
          if (active)
          {
            visualCue.style.backgroundColor = viewModel.GetScaledColour(cueColour);
          }
          else
          {
            visualCue.style.backgroundColor = startColor;
          }
          active = !active;
          await UniTask.Delay(1000, cancellationToken: token); // 1 second
        }

        Log.Info("[{logType}] visual cue intensity is now: {intensity}", "CUES", 0);
        visualCue.style.backgroundColor = startColor;
        await UniTask.Delay((int)(cycleSecs * 1000), cancellationToken: token); // off cycle

        cycles++;
      }
    }

    private async UniTask RampAudio(CancellationToken token)
    {
      await UniTask.SwitchToMainThread();
      if (!viewModel.AuditoryCueActive()) { return; }
      Log.Info("[{logType}] beginning auditory cues.", "CUES");

      int cycles = 1;
      while (true)
      {
        float volume = Mathf.Lerp(0, maxVolume, cycles / (float)cyclesToFull);
        viewModel.PlayAudio(volume);
        await UniTask.Delay((int)(cycleSecs * 2 * 1000), cancellationToken: token);
        cycles++;
      }
    }
    #endregion

    #region CALLBACKS
    public void OnEnter(NavController controller, NavDestination destination, Argument[] args)
    {
      Focus();
      visualCue.style.backgroundColor = startColor;

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

      if (cuesActive)
      {
        StopCues();
      }
    }

    private void OnLrSignalDetected()
    {
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