using Unity.AppUI.MVVM;
using Unity.Properties;
using UnityEngine;

namespace LuciReality.UI
{
  [ObservableObject]
  public partial class PromptViewModel
  {
    [ObservableProperty]
    [AlsoNotifyChangeFor(nameof(Text))]
    [AlsoNotifyChangeFor(nameof(Image))]
    private PromptModel _model = new();

    [ObservableProperty]
    private bool _generatingImage = false;

    [CreateProperty]
    public string Text
    {
      get => _model.Text;
      set => SetProperty(_model.Text, value, _model, (u, n) => u.Text = n);
    }

    [CreateProperty]
    public Texture2D Image
    {
      get => _model.Image;
      set => SetProperty(_model.Image, value, _model, (u, n) => u.Image = n);
    }
  }
}