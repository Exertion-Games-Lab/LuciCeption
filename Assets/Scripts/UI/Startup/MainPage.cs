using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuciReality.UI
{
  public class MainPage : VisualElement
  {
    private NavHost navHost;

    public MainPage(NavigationStoreService navigationStore)
    {
      styleSheets.Add(Resources.Load<StyleSheet>("SettingsStyles"));
      AddToClassList("luci-reality--fullscreen");
      navHost = new NavHost();
      navHost.navController.SetGraph(navigationStore.GraphAsset);


      Add(navHost);
      navHost.StretchToParentSize();
    }
  }
}