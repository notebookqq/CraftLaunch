using System.IO;
using System.Text.Json;
using CraftLaunch.Models;

namespace CraftLaunch.Services;

public static class ConfigService
{
    private static string DirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CraftLaunch");

    private static string FilePath => Path.Combine(DirectoryPath, "config.json");

    public static AppSettings Current { get; private set; } = new();

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Current = new AppSettings();
            LogManager.Instance.Warn($"配置读取失败，已重置: {ex.Message}");
        }

        if (string.IsNullOrEmpty(Current.GameDirectory))
            Current.GameDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");

        return Current;
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            LogManager.Instance.Error($"配置保存失败: {ex.Message}");
        }
    }
}
