using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AppUI.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.AppUI.UI;

namespace LuciReality.UI
{
  [Serializable]
  public struct FileDescriptor
  {
    public string Path;
    public string Extension;
    public string Name;
    public DateTime Created;
    public DateTime LastModified;
    public long Size;

    public override string ToString()
    {
      return $"{Name} | {Created} | {Size}";
    }
  }

  [UxmlElement]
  public partial class FileBrowser : VisualElement
  {
    public event Action OnCloseClicked;
    private string _path = Path.Join(Application.dataPath, "AudioTest");

    public string RootDirectory
    {
      get => _path;
      set => _path = value;
    }

    public List<string> Extensions;
    private List<FileDescriptor> files;
    private MultiColumnListView listView;

    public FileBrowser()
    {
      RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
      RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    public void OnAttachToPanel(AttachToPanelEvent e)
    {
      AddToClassList("lucireality--file-browser");
      Extensions = new List<string> {
        "wav",
        "aiff",
        "mp3",
        "ogg"
      };
      files = new List<FileDescriptor>();
      
      listView = new MultiColumnListView();

      var headingContainer = new VisualElement();
      headingContainer.style.flexDirection = FlexDirection.Row;
      headingContainer.style.justifyContent = Justify.SpaceBetween;
      headingContainer.Add(new Heading("Audio Files"));
      var closeButton = new IconButton();
      closeButton.icon = "x";
      closeButton.clicked += () => OnCloseClicked?.Invoke();
      headingContainer.Add(closeButton);
      Add(headingContainer);
      Add(new Divider());

      // listView.makeHeader = () => {
      //   var element = new VisualElement();
      //   element.Add(new Heading("Audio Files"));
      //   element.Add(new Divider());
      //   return element;
      // };

      var fileNameColumn = new Column
      {
        title = "File Name",
        stretchable = true,
        makeCell = () => new Text(),
        bindCell = (element, index) => (element as Text).text = files[index].Name
      };

      listView.columns.Add(fileNameColumn);

      var fileExtensionColumn = new Column
      {
        title = "File Extension",
        stretchable = true,
        makeCell = () => new Text(),
        bindCell = (element, index) => (element as Text).text = files[index].Extension
      };
      listView.columns.Add(fileExtensionColumn);

      var fileSizeColumn = new Column
      {
        title = "Size",
        stretchable = true,
        makeCell = () => new Text(),
        bindCell = (element, index) => (element as Text).text = $"{files[index].Size / 1024} KB"
      };
      listView.columns.Add(fileSizeColumn);
      listView.itemsSource = files;

      Add(listView);
      listView.AddToClassList("lucireality--file-browser--list-view");

      PopulateFileList();
      
      Add(new Divider());

      var footerGroup = new ActionGroup();

      var selectButton = new Unity.AppUI.UI.Button()
      {
        title = "Select",
        variant = ButtonVariant.Accent
      };
      selectButton.style.width = new StyleLength(100);

      var openClipsFolder = new ActionButton
      {
        label = "Open Clips Folder"
      };
      openClipsFolder.style.width = new StyleLength(140);

      footerGroup.Add(selectButton);
      footerGroup.Add(openClipsFolder);

      Add(footerGroup);
    }

    public void OnDetachFromPanel(DetachFromPanelEvent e)
    {
      Clear();
    }

    private VisualElement MakeItem()
    {
      var item = new VisualElement();

      item.style.flexDirection = FlexDirection.Row;

      item.Add(new Text());
      var divider = new Divider
      {
        direction = Direction.Vertical
      };
      item.Add(divider);
      item.Add(new Text());

      return item;
    }

    private void BindItem(VisualElement item, int index)
    {
      var data = files[index];

      var textElements = item.Query<Text>();

      textElements.AtIndex(0).text = data.Name;
      textElements.AtIndex(1).text = data.Extension;


    }

    private void UnbindItem(VisualElement item, int index)
    {

    }

    private void PopulateFileList()
    {
      if (!Directory.Exists(_path)) { return; }

      string[] filesEntries = Directory.GetFiles(_path);

      foreach (string file in filesEntries)
      {
        FileDescriptor fileDescriptor = GetFileDescriptor(file);
        if (!Extensions.Contains(fileDescriptor.Extension[1..])) {continue;}
        files.Add(fileDescriptor);
      }

      listView.itemsSource = files;
      listView.Rebuild();
    }

    private static FileDescriptor GetFileDescriptor(string file)
    {
      var fsInfo = new FileInfo(file);
      var fileDescriptor = new FileDescriptor
      {
        Path = file,
        Extension = fsInfo.Extension,
        Name = fsInfo.Name,
        Created = fsInfo.CreationTime,
        LastModified = fsInfo.LastWriteTime,
        Size = fsInfo.Length,
      };
      return fileDescriptor;
    }
  }
}