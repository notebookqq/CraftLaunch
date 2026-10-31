using System.Reflection;

namespace CraftLaunch.ViewModels;

public class AboutViewModel : ViewModelBase
{
    public string AppName => "CraftLaunch";

    public string VersionText
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"版本 {version?.ToString(3) ?? "0.1.0"}";
        }
    }

    public string Description => "一个使用原生 WPF 与 Fluent Design 风格构建的 Minecraft 启动器基础框架。";

    public string TeamText => "制作组：笔记本说电脑";
}
