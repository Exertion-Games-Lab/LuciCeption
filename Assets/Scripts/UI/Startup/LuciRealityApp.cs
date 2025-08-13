using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using UnityEngine;

namespace LuciReality.UI
{
  public class LuciRealityApp : App
  {
    public new static LuciRealityApp current => (LuciRealityApp)App.current;

    public void CreatePage()
    {
      rootVisualElement.Add(services.GetRequiredService<MainPage>());
    }

    public override void InitializeComponent()
    {
      base.InitializeComponent();
    }

    public override void Shutdown()
    {
      base.Shutdown();
      Debug.Log("LuciRealityApp.Shutdown");
    }
  }
}