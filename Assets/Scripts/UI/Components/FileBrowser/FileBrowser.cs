using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AppUI.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.AppUI.UI;
using Unity.Logging;

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
    private CueParametersViewModel viewModel;

    public FileBrowser(CueParametersViewModel viewModel)
    {
      this.viewModel = viewModel;

      AddToClassList("lucireality--file-browser");
      Extensions = new List<string> {
        "wav",
      };
      files = new List<FileDescriptor>();
      
      listView = new MultiColumnListView();

      var headingContainer = new VisualElement();
      headingContainer.style.flexDirection = FlexDirection.Row;
      headingContainer.style.justifyContent = Justify.SpaceBetween;
      headingContainer.Add(new Heading("Audio Files"));

      var closeButton = new IconButton
      {
        icon = "x"
      };
      closeButton.clicked += () => OnCloseClicked?.Invoke();
      headingContainer.Add(closeButton);
      Add(headingContainer);
      Add(new Divider());

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

      selectButton.clicked += () =>
      {
        if (listView.selectedIndex == -1) { return; }
        
        var clip = ((FileDescriptor)listView.selectedItem).Path;
        viewModel.AudioClipFile = clip;
        Log.Info("[{logType}]: Selected audio clip {audioClip}", "USER_INTERFACE", clip);
      };

      var openClipsFolder = new ActionButton
      {
        label = "Open Clips Folder"
      };
      openClipsFolder.style.width = new StyleLength(140);
      openClipsFolder.clicked += () => Application.OpenURL(_path);

      footerGroup.Add(selectButton);
      footerGroup.Add(openClipsFolder);

      Add(footerGroup);

      RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
      RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    public void OnAttachToPanel(AttachToPanelEvent e)
    {
      files.Clear();
      PopulateFileList();
      Log.Info("[{logType}]: File browser opened", "USER_INTERFACE");
    }

    public void OnDetachFromPanel(DetachFromPanelEvent e)
    {
      foreach (Action d in OnCloseClicked.GetInvocationList().Cast<Action>())
      {
        OnCloseClicked -= d;
      }

      Log.Info("[{logType}]: File browser closed", "USER_INTERFACE");
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