using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace LuciReality.UI
{
  public static class SaveSettings
  {
    public const string CUE_SETTINGS_DIR = "saved-cues";
    public const string PROMPT_SETTINGS_DIR = "saved-prompts";
    public const string IMAGE_PERSISTANCE_DIR = "generated-images";

    private static string GetDirectoryPath(string directoryName)
      => Path.Combine(Application.persistentDataPath, directoryName);
    private static string GetPromptSettingsFilePath(string name)
      => Path.Combine(GetDirectoryPath(PROMPT_SETTINGS_DIR), AddSettingsExtension(name));
    private static string GetCueSettingsFilePath(string name)
      => Path.Combine(GetDirectoryPath(CUE_SETTINGS_DIR), AddSettingsExtension(name));
    private static string GetImageFilePath(string name)
      => Path.Combine(GetDirectoryPath(IMAGE_PERSISTANCE_DIR), AddImageExtension(name));

    private static string AddSettingsExtension(string name) => $"{name}.json";
    private static string AddImageExtension(string name) => $"{name}.png";

    // Load
    public static async Task<(CueParametersModel, PromptModel)> Load(string name)
    {
      var prompts = await LoadPrompts(name);
      var cues = await LoadCues(name);

      return (cues, prompts);
    }

    private static async Task<CueParametersModel> LoadCues(string name)
    {
      var text = await File.ReadAllTextAsync(GetCueSettingsFilePath(name));
      return JsonConvert.DeserializeObject<CueParametersModel>(text);
    }

    private static async Task<PromptModel> LoadPrompts(string name)
    {
      var prompts = new PromptModel();

      prompts.Text = await LoadTextPrompt(name);
      prompts.Image = await LoadImagePrompt(name);

      return prompts;
    }
    private static async Task<Texture2D> LoadImagePrompt(string name)
    {
      var tex = new Texture2D(2, 2);
      tex.hideFlags = HideFlags.DontSave;

      byte[] imageData = await File.ReadAllBytesAsync(GetImageFilePath(name), Application.exitCancellationToken);
      ImageConversion.LoadImage(tex, imageData);

      return tex;
    }

    private static async Task<string> LoadTextPrompt(string name)
      => await File.ReadAllTextAsync(GetPromptSettingsFilePath(name), Application.exitCancellationToken);

    // Save
    public static async Task Save(string name, CueParametersModel cues, PromptModel prompt)
    {
      await SaveCues(name, cues);
      await SavePrompts(name, prompt);
    }

    private static async Task SaveCues(string name, CueParametersModel cues)
    {
      CreateDirIfNotExists(CUE_SETTINGS_DIR);
      await SaveToFile(GetCueSettingsFilePath(name), JsonConvert.SerializeObject(cues));
    }


    private static async Task SavePrompts(string name, PromptModel prompt)
    {
      await SaveTextPrompt(name, prompt.Text);
      await SaveImagePrompt(name, prompt.Image);
    }

    private static async Task SaveTextPrompt(string name, string text)
    {
      CreateDirIfNotExists(PROMPT_SETTINGS_DIR);
      await SaveToFile(GetPromptSettingsFilePath(name), text);
    }

    private static async Task SaveImagePrompt(string name, Texture2D image)
    {
      CreateDirIfNotExists(IMAGE_PERSISTANCE_DIR);
      await SaveToFile(GetImageFilePath(name), image);
    }

    private static async Task SaveToFile(string filePath, string text)
    {
      byte[] fileData = System.Text.Encoding.ASCII.GetBytes(text);
      await SaveToFile(filePath, fileData);
    }

    private static async Task SaveToFile(string filePath, Texture2D image)
    {
      byte[] imageData = ImageConversion.EncodeToPNG(image);
      await SaveToFile(filePath, imageData);
    }

    private static async Task SaveToFile(string filePath, byte[] data)
    {
      DeleteFileIfExists(filePath); // Delete existing file so we can overwrite it.
      using var file = File.Open(filePath, FileMode.CreateNew);
      await file.WriteAsync(data, 0, data.Length, Application.exitCancellationToken);
    }

    private static void DeleteFileIfExists(string imageFilePath)
    {
      if (File.Exists(imageFilePath))
      {
        File.Delete(imageFilePath);
      }
    }

    private static void CreateDirIfNotExists(string directory)
    {
      var directoryPath = GetDirectoryPath(directory);

      if (!Directory.Exists(directoryPath))
      {
        Directory.CreateDirectory(directoryPath);
      }
    }

    // Delete
    public static void Delete(string name)
    {
      DeleteFileIfExists(GetCueSettingsFilePath(name));
      DeleteFileIfExists(GetPromptSettingsFilePath(name));
      DeleteFileIfExists(GetImageFilePath(name));
    }
  }
}