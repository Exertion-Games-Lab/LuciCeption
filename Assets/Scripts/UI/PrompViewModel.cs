using Unity.AppUI.MVVM;

namespace LuciReality.UI
{
  [ObservableObject]
  public partial class PromptViewModel
  {
    [ObservableProperty]
    private string _text;
  }
}