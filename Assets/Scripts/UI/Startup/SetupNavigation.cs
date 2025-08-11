using Unity.AppUI.Navigation;
using UnityEngine;
using UnityEngine.UIElements;

public class SetupNavigation : MonoBehaviour
{
  UIDocument uiDocument;
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  [SerializeField] NavGraphViewAsset navigationGraph;
  public NavHost navHost;
  public NavController navController;

  void Start()
  {
    uiDocument = GetComponent<UIDocument>();
    navHost = uiDocument.rootVisualElement.Q<NavHost>();
    navController = new NavController(navHost);
    navHost.navController.SetGraph(navigationGraph);
  }
}
