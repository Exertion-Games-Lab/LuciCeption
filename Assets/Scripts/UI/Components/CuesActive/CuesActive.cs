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


    const string ip = "192.168.1.183";
    private string thermalModuleCommand(string mode, int intensity)
    {
      return $"http://{ip}/configure?mode={mode}&intensity={intensity}";
    }

    private async UniTask setThermalCue(string mode, int intensity)
    {
      UnityWebRequest request = UnityWebRequest.Get($"http://{ip}/configure?mode={mode}&intensity={intensity}");
      await request.SendWebRequest();
    }

    private async UniTask setFanCue(int speed)
    {
      UnityWebRequest request = UnityWebRequest.Get($"https://maker.ifttt.com/trigger/fan_intensity_{speed}/with/key/88IAzsMm2FG355HNDbZjq");
      await request.SendWebRequest();
    }

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

    private void OnLrSignalDetected()
    {
      ActivateCues();
    }

    private void OnMovementDetected()
    {
      PauseCues();
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
        Log.Info("[{logType}] force stopping audio clip!", "CUES");
        AudioSingleton.Instance.AudioSource.volume = maxVolume;
        AudioSingleton.Instance.AudioSource.Stop();

        Log.Info("[{logType}] force stopping thermal cue!", "CUES");
        await setThermalCue("off", 0);

        Log.Info("[{logType}] force stopping fan cue!", "CUES");
        await setFanCue(1);

        throw e;
      }
    }

    private async UniTask ThermalCue(CancellationToken token)
    {
      await UniTask.SwitchToMainThread();
      Log.Info("[{logType}] beginning thermal cues.", "CUES");

      int cycles = 1;

      while (true)
      {
        string mode = "cool";
        int intensity = (int)Mathf.Lerp(0, 255, cycles / (float)cyclesToFull);

        await setThermalCue(mode, intensity);
        Log.Info("[{logType}] thermal cue intensity is now: {intensity} with {mode}", "CUES", intensity, mode);
        await UniTask.Delay((int)(cycleSecs * 1000), cancellationToken: token);

        await setThermalCue("off", 0);
        Log.Info("[{logType}] thermal cue intensity is now: {intensity} with {mode}", "CUES", 0, "off");
        await UniTask.Delay((int)(cycleSecs * 1000), cancellationToken: token);

        cycles++;
      }
    }

    private async UniTask FanCue(CancellationToken token)
    {
      await UniTask.SwitchToMainThread();
      Log.Info("[{logType}] beginning fan cue.", "CUES");

      int cycles = 1;

      while (true)
      {
        int intensity = (int)Mathf.Lerp(1, 9, cycles / (float)cyclesToFull);

        await setFanCue(intensity);
        Log.Info("[{logType}] fan cue intensity is now: {intensity}", "CUES", intensity);
        await UniTask.Delay((int)(cycleSecs * 1000), cancellationToken: token);

        await setFanCue(1);
        Log.Info("[{logType}] fan cue intensity is now: {intensity}", "CUES", 1);
        await UniTask.Delay((int)(cycleSecs * 1000), cancellationToken: token);

        cycles++;
      }
    }

    private async UniTask FlashVisualCue(CancellationToken token)
    {
      await UniTask.SwitchToMainThread();
      Log.Info("[{logType}] beginning visual cues.", "CUES");

      int cycles = 1;

      while (true)
      {
        float intensity = Math.Min(cycles / (float)cyclesToFull, 1);
        Color cueColour = Color.Lerp(startColor, viewModel.Color, intensity);

        // on cycle
        Log.Info("[{logType}] visual cue intensity is now: {intensity}", "CUES", intensity);

        bool active = true;
        for (int i = 0; i < cycleSecs; i++)
        {
          if (active)
          {
            visualCue.style.backgroundColor = cueColour;
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

      int cycles = 1;
      while (true)
      {
        float volume = Mathf.Lerp(0, maxVolume, cycles / (float)cyclesToFull);
        Log.Info("[{logType}] playing audio cue clip at volume: {volume}.", "CUES", volume);
        AudioSingleton.Instance.AudioSource.volume = volume;
        AudioSingleton.Instance.AudioSource.PlayOneShot(viewModel.AudioClip);
        await UniTask.Delay((int)(cycleSecs * 2 * 1000), cancellationToken: token);
        cycles++;
      }

    }

    public void OnEnter(NavController controller, NavDestination destination, Argument[] args)
    {
      Focus();
      visualCue.style.backgroundColor = startColor;

      // openBCIClient.LrSignalDetected += ActivateCues;

      // if (openBCIClient.IsConnected)
      // {
      //   UniTask.RunOnThreadPool(openBCIClient.StartSystem, cancellationToken: Application.exitCancellationToken);
      //   startOnConnect = false;
      // }
      // else
      // {
      //   startOnConnect = true;
      // }

      // eventBus.MovementDetected += OnMovementDetected;
      OnLrSignalDetected(); // Debugging
    }

    public void OnExit(NavController controller, NavDestination destination, Argument[] args)
    {
      startOnConnect = false;
      if (openBCIClient.IsConnected)
      {
        UniTask.RunOnThreadPool(openBCIClient.StopSystem, cancellationToken: Application.exitCancellationToken);
      }

      openBCIClient.LrSignalDetected -= ActivateCues;
      // eventBus.MovementDetected -= OnMovementDetected;

      if (cuesActive)
      {
        StopCues();
      }
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
}