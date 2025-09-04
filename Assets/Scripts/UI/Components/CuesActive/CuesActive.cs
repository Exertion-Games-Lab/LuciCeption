using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LuciReality.Audio;
using LuciReality.Events;
using Unity.AppUI.Navigation;
using Unity.Logging;
using UnityEngine;
using UnityEngine.UIElements;

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


    Color startColor = new(0, 0, 0);
    VisualElement visualCue;

    CancellationTokenSource cancelCues;
    private EventBus eventBus;


    public CuesActive(CuesActiveViewModel viewModel)
    {
      // this.eventBus = eventBus;
      this.viewModel = viewModel;

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
      visualCue.style.backgroundColor = startColor;

      cancelCues = new CancellationTokenSource();
      var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancelCues.Token, Application.exitCancellationToken);

      var cancelCuesClosure = cancelCues;

      UniTask.RunOnThreadPool(async () =>
      {
        
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

    public void OnEnter(NavController controller, NavDestination destination, Argument[] args)
    {
      Focus();
      visualCue.style.backgroundColor = startColor;

      // eventBus.LrSignalDetected += OnLrSignalDetected;
      // eventBus.MovementDetected += OnMovementDetected;

      OnLrSignalDetected(); // Debugging
    }

    private async UniTask UpdateCues(CancellationToken token)
    {
      Log.Info("[{logType}] waiting for {delaySec} seconds before starting cues.", "CUES", delaySec);
      await UniTask.Delay((int)delaySec * 1000, cancellationToken: token);

      try
      {
        await UniTask.WhenAll(
          UniTask.Create(FlashVisualCue, token),
          UniTask.Create(RampAudio, token)
        );
      }
      catch (OperationCanceledException e)
      {
        Log.Info("[{logType}] force stopping audio clip!", "CUES");
        AudioSingleton.Instance.AudioSource.volume = maxVolume;
        AudioSingleton.Instance.AudioSource.Stop();
        throw e;
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

    public void OnExit(NavController controller, NavDestination destination, Argument[] args)
    {
      // eventBus.LrSignalDetected -= OnLrSignalDetected;
      // eventBus.MovementDetected -= OnMovementDetected;
      StopCues();
    }
  }
}