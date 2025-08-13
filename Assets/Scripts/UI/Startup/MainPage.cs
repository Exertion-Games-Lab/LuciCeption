using Unity.AppUI.Navigation;
using UnityEngine.UIElements;

namespace LuciReality.UI
{
  public class MainPage : VisualElement
  {
    private NavHost navHost;
    private NavController navController;

    public MainPage()
    {
      navHost = new NavHost();
      navController = new NavController(navHost);
      // navHost.navController.SetGraph(navigationGraph);

      Add(navHost);
    }
  }
}