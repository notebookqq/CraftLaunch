namespace CraftLaunch.Models;

public class AppSettings
{
    public string Theme { get; set; } = "Light";
    public string GameDirectory { get; set; } = "";
    public string JavaPath { get; set; } = "";
    public int MemoryMB { get; set; } = 4096;
    public bool ProxyEnabled { get; set; }
    public string ProxyHost { get; set; } = "127.0.0.1";
    public int ProxyPort { get; set; } = 7890;
}
