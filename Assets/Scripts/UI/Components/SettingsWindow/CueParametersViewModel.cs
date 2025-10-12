using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuciReality.UI
{
  /// <summary>
  /// This class is a <see href="https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm">view-model</see> for the settings interface.
  /// </summary>
  [System.Serializable]
  [ObservableObject]
  public partial class CueParametersViewModel
  {
    public const string PROMPT_TOOLTIP_TEXT = "This section generates an image using AI.\nDescribe what you would like to dream about in the text area, and clicking the button will prompt the AI to generate an image based on it.";
    public const string IMAGE_TOOLTIP_TEXT = "Once you are happy with the image that the AI generated, click the generate cues button.\nThis will configure the cues using your text prompt and selected image.\nAfter the cues have been generated, you are free to customise them as you wish.";
    public const string AUDITORY_TOOLTIP_TEXT = @"Please select an audio file for your auditory cue.
    The auditory cue should be a sound that reminds you of your dream environment. You may either find an audio file online or record one yourself.

Restrictions:
    Length: 8-10 seconds (no longer than 10 seconds)
    Format: MP3 or WAV
    Language: If using a voice recording, we suggest using your first language (either your own voice or someone else's).

Suggested sentence to record:
    If the provided sentence feels too complex, you may instead simply repeat the core concept of your dream environment for about 10 seconds.";

    [ObservableProperty]
    private bool _generatingCues = false;

    [ObservableProperty]
    private string _promptText;

    [ObservableProperty]
    [AlsoNotifyChangeFor(nameof(ReasoningText))]
    [AlsoNotifyChangeFor(nameof(LightsEnabled))]
    [AlsoNotifyChangeFor(nameof(LightsColour))]
    [AlsoNotifyChangeFor(nameof(LightsIntensity))]
    [AlsoNotifyChangeFor(nameof(AudioEnabled))]
    [AlsoNotifyChangeFor(nameof(AudioVolume))]
    [AlsoNotifyChangeFor(nameof(LraEnabled))]
    [AlsoNotifyChangeFor(nameof(LraIntensity))]
    [AlsoNotifyChangeFor(nameof(EmsEnabled))]
    [AlsoNotifyChangeFor(nameof(EmsIntensity))]
    [AlsoNotifyChangeFor(nameof(FanEnabled))]
    [AlsoNotifyChangeFor(nameof(FanIntensity))]
    [AlsoNotifyChangeFor(nameof(ThermalEnabled))]
    [AlsoNotifyChangeFor(nameof(ThermalIntensity))]
    [AlsoNotifyChangeFor(nameof(ThermalMode))]
    [AlsoNotifyChangeFor(nameof(AudioClipFile))]
    [AlsoNotifyChangeFor(nameof(DreamInductionScript))]
    [AlsoNotifyChangeFor(nameof(DelayHours))]
    [AlsoNotifyChangeFor(nameof(DelayMinutes))]
    [AlsoNotifyChangeFor(nameof(DelaySeconds))]
    private CueParametersModel _model = new();

    [CreateProperty]
    public string ReasoningText
    {
      get => _model.ReasoningText;
    }

    [CreateProperty]
    public bool LightsEnabled
    {
      get => _model.Lights.Enabled;
      set => SetProperty(_model.Lights.Enabled, value, _model, (u, n) => u.Lights.Enabled = n);
    }

    [CreateProperty]
    public Color LightsColour
    {
      get => _model.Lights.Colour;
      set => SetProperty(_model.Lights.Colour, value, _model, (u, n) => u.Lights.Colour = n);
    }
    public void SetLightsColourWithoutNotify(Color colour) => _model.Lights.Colour = colour;

    [CreateProperty]
    public float LightsIntensity
    {
      get => _model.Lights.Intensity;
      set => SetProperty(_model.Lights.Intensity, value, _model, (u, n) => u.Lights.Intensity = n);
    }

    [CreateProperty]
    public bool AudioEnabled
    {
      get => _model.Audio.Enabled;
      set => SetProperty(_model.Audio.Enabled, value, _model, (u, n) => u.Audio.Enabled = n);
    }
    [CreateProperty]
    public float AudioVolume
    {
      get => _model.Audio.Volume;
      set => SetProperty(_model.Audio.Volume, value, _model, (u, n) => u.Audio.Volume = n);
    }

    [CreateProperty]
    public bool LraEnabled
    {
      get => _model.Lra.Enabled;
      set => SetProperty(_model.Lra.Enabled, value, _model, (u, n) => u.Lra.Enabled = n);
    }
    [CreateProperty]
    public float LraIntensity
    {
      get => _model.Lra.Intensity;
      set => SetProperty(_model.Lra.Intensity, value, _model, (u, n) => u.Lra.Intensity = n);
    }

    [CreateProperty]
    public bool EmsEnabled
    {
      get => _model.Ems.Enabled;
      set => SetProperty(_model.Ems.Enabled, value, _model, (u, n) => u.Ems.Enabled = n);
    }
    [CreateProperty]
    public float EmsIntensity
    {
      get => _model.Ems.Intensity;
      set => SetProperty(_model.Ems.Intensity, value, _model, (u, n) => u.Ems.Intensity = n);
    }

    [CreateProperty]
    public bool FanEnabled
    {
      get => _model.Fan.Enabled;
      set => SetProperty(_model.Fan.Enabled, value, _model, (u, n) => u.Fan.Enabled = n);
    }
    [CreateProperty]
    public float FanIntensity
    {
      get => _model.Fan.Intensity;
      set => SetProperty(_model.Fan.Intensity, value, _model, (u, n) => u.Fan.Intensity = n);
    }

    [CreateProperty]
    public bool ThermalEnabled
    {
      get => _model.Thermal.Enabled;
      set => SetProperty(_model.Thermal.Enabled, value, _model, (u, n) => u.Thermal.Enabled = n);
    }

    [CreateProperty]
    public float ThermalIntensity
    {
      get => _model.Thermal.Intensity;
      set => SetProperty(_model.Thermal.Intensity, value, _model, (u, n) => u.Thermal.Intensity = n);
    }

    [CreateProperty]
    public string ThermalMode
    {
      get => _model.Thermal.Mode;
      set => SetProperty(_model.Thermal.Mode, value, _model, (u, n) => u.Thermal.Mode = n);
    }

    [CreateProperty]
    public string DreamInductionScript
    {
      get => _model.DreamInductionScript;
      set => SetProperty(_model.DreamInductionScript, value, _model, (u, n) => u.DreamInductionScript = n);
    }

    public void SetThermalModeWithoutNotify(string mode)
    {
      _model.Thermal.Mode = mode;
    }

    [CreateProperty]
    public string AudioClipFile
    {
      get => _model.Audio.AudioClipFile;
      set => SetProperty(_model.Audio.AudioClipFile, value, _model, (u, n) => u.Audio.AudioClipFile = n);
    }

    [CreateProperty]
    public int DelayHours
    {
      get => _model.DelayTime.Hours;
      set => SetProperty(_model.DelayTime.Hours, value, _model, (u, n) => u.DelayTime.Hours = n);
    }

    [CreateProperty]
    public int DelayMinutes
    {
      get => _model.DelayTime.Minutes;
      set => SetProperty(_model.DelayTime.Minutes, value, _model, (u, n) => u.DelayTime.Minutes = n);
    }

    [CreateProperty]
    public int DelaySeconds
    {
      get => _model.DelayTime.Seconds;
      set => SetProperty(_model.DelayTime.Seconds, value, _model, (u, n) => u.DelayTime.Seconds = n);
    }

    [CreateProperty]
    public int CancelHours
    {
      get => _model.CancelTime.Hours;
      set => SetProperty(_model.CancelTime.Hours, value, _model, (u, n) => u.CancelTime.Hours = n);
    }

    [CreateProperty]
    public int CancelMinutes
        {
      get => _model.CancelTime.Minutes;
      set => SetProperty(_model.CancelTime.Minutes, value, _model, (u, n) => u.CancelTime.Minutes = n);
    }

    [CreateProperty]
    public int CancelSeconds
        {
      get => _model.CancelTime.Seconds;
      set => SetProperty(_model.CancelTime.Seconds, value, _model, (u, n) => u.CancelTime.Seconds = n);
    }

    // Commands

    /// <summary>Opens a tooltip in the "image" section of the interface explaining how it works</summary>
    /// <param name="dreamImageInfo">The element that the tooltip should be attached to</param>
    [ICommand]
    public void OpenImageHelp(VisualElement dreamImageInfo)
      => MakeTooltipPopover(dreamImageInfo, PROMPT_TOOLTIP_TEXT);

    /// <summary>Opens a tooltip in the "prompt" section of the interface explaining how it works</summary>
    /// <param name="dreamPromptInfo">The element that the tooltip should be attached to</param>
    [ICommand]
    public void OpenPromptHelp(VisualElement dreamPromptInfo) =>
      MakeTooltipPopover(dreamPromptInfo, PROMPT_TOOLTIP_TEXT);

    /// <summary>Opens a tooltip in the "auditory" section of the interface explaining how it works</summary>
    /// <param name="auditoryInfo">The element that the tooltip should be attached to</param>
    [ICommand]
    public void OpenAuditoryHelp(VisualElement auditoryInfo)
      => MakeTooltipPopover(auditoryInfo, AUDITORY_TOOLTIP_TEXT);

    [ICommand]
    public void OpenReasoning(VisualElement target) => MakeReasoningWindow(target);

    [ICommand]
    public void OpenAudioClipBrowser(VisualElement target) => MakeFileBrowser(target);

    private void MakeReasoningWindow(VisualElement target)
    {
      var reasoningWindow = new ReasoningWindow();
      reasoningWindow.dataSource = this;
      reasoningWindow.SetBinding("text", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(ReasoningText))
      });
      var modal = Modal.Build(target, reasoningWindow)
      .SetKeyboardDismiss(false)
      .SetOutsideClickDismiss(true);

      reasoningWindow.OnCloseClicked += () => modal.Dismiss();

      modal.Show();
    }

    private void MakeFileBrowser(VisualElement target)
    {
      var fileBrowser = LuciRealityApp.current.services.GetService<FileBrowser>();

      var modal = Modal.Build(target, fileBrowser)
      .SetKeyboardDismiss(false)
      .SetOutsideClickDismiss(true);

      fileBrowser.OnCloseClicked += () => modal.Dismiss();

      modal.Show();
    }


    /// <summary>Builds a popover tooltip attached to an element, using the specified text</summary>
    /// <param name="target">The element the tooltip is attached to</param>
    /// <param name="tooltipText">The message the tooltip displays</param>
    private void MakeTooltipPopover(VisualElement target, string tooltipText)
    {
      var popoverContent = new VisualElement();
      var infoText = new Text
      {
        text = tooltipText
      };
      popoverContent.Add(infoText);

      Popover.Build(target, popoverContent)
      .SetKeyboardDismiss(true)
      .SetContainerPadding(4)
      .SetOutsideClickDismiss(true)
      .SetArrowVisible(true)
      .SetOffset(2)
      .Show();
    }
  }
}