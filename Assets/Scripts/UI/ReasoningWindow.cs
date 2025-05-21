using System;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuciReality.UI
{
  [UxmlElement]
  public partial class ReasoningWindow : VisualElement
  {
    public event Action OnCloseClicked;
    internal static readonly BindingId textProperty = "text";

    private Text textElement;

    [UxmlAttribute("text")]
    [CreateProperty]
    public string text
    {
      get
      {
        return textElement.text;
      }
      set
      {
        bool isEqual = textElement.text == value;
        textElement.text = value;
        if (!isEqual)
        {
          NotifyPropertyChanged(in textProperty);
        }
      }
    }

    public ReasoningWindow()
    {

      var headingContainer = new VisualElement();
      headingContainer.style.flexDirection = FlexDirection.Row;
      headingContainer.style.justifyContent = Justify.SpaceBetween;

      var heading = new Heading("AI Reasoning");
      headingContainer.Add(heading);

      var closeButton = new IconButton();
      closeButton.icon = "x";
      closeButton.clicked += () => OnCloseClicked?.Invoke();
      headingContainer.Add(closeButton);

      Add(headingContainer);
      Add(new Divider());

      
      var scroller = new ScrollView();
      scroller.nestedInteractionKind = ScrollView.NestedInteractionKind.StopScrolling;
      scroller.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;

      Add(scroller);

      textElement = new Text();
      scroller.Add(textElement);

      textElement.text = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    }
  }
}
