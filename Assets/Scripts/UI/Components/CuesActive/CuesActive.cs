using System;
using System.Collections.Generic;
using System.Threading;
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

    float delaySec = 10;
    float lerpSec = 10;
    float betweenAudioClipsSec = 10;

    Color startColor = new(0, 0, 0);
    VisualElement visualCue;

    CancellationTokenSource cancelAnimate;
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

      cancelAnimate?.Dispose();
      cancelAnimate = new CancellationTokenSource();

      try
      {
        UniTask.Void(UpdateCues, cancelAnimate.Token);
      }
      catch (OperationCanceledException)
      {
      }
    }

    private void PauseCues()
    {
      visualCue.style.backgroundColor = startColor;

    }

    public void OnEnter(NavController controller, NavDestination destination, Argument[] args)
    {
      Focus();

      visualCue.style.backgroundColor = startColor;

      // eventBus.LrSignalDetected += OnLrSignalDetected;
      // eventBus.MovementDetected += OnMovementDetected;

      OnLrSignalDetected(); // Debugging
    }

    private async UniTaskVoid UpdateCues(CancellationToken token)
    {
      Log.Info("[CUES] waiting for {delaySec} seconds before starting cues.", delaySec);
      await UniTask.Delay((int)delaySec * 1000, cancellationToken: token);

      if (token.IsCancellationRequested)
      {
        throw new OperationCanceledException();
      }

      try
      {
        await UniTask.WhenAll(
          UniTask.Create(AnimateVisualCue, token),
          UniTask.Create(PlayAudio, token));
      }
      catch (OperationCanceledException e)
      {
        Log.Info("[CUES] force stopping audio clip!");
        AudioSingleton.Instance.AudioSource.Stop();
        throw e;
      }
    }

    private async UniTask AnimateVisualCue(CancellationToken token)
    {
      await UniTask.SwitchToMainThread();
      Log.Info("[CUES] starting cues with interpolation time of {lerpSec}", lerpSec);

      float endTime = Time.time + lerpSec;
      float startTime = Time.time;


      while (Time.time < endTime)
      {
        var interpColor = Color.Lerp(startColor, viewModel.Color, (Time.time - startTime) / (endTime - startTime));
        visualCue.style.backgroundColor = interpColor;
        await UniTask.NextFrame(PlayerLoopTiming.Update, token);
      }

      Log.Info("[CUES] visual cue is now at full intensity.");
    }

    private async UniTask PlayAudio(CancellationToken token)
    {
      await UniTask.SwitchToMainThread();
      while (true)
      {
        Log.Info("[CUES] playing audio cue clip.");
        AudioSingleton.Instance.AudioSource.PlayOneShot(viewModel.AudioClip);
        await UniTask.Delay((int)(betweenAudioClipsSec * 1000), cancellationToken: token);
      }
    }

    public void OnExit(NavController controller, NavDestination destination, Argument[] args)
    {
      // eventBus.LrSignalDetected -= OnLrSignalDetected;
      // eventBus.MovementDetected -= OnMovementDetected;

      cancelAnimate.Cancel();
      cancelAnimate = null;
    }
  }
}