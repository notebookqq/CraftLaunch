namespace CraftLaunch.Models;

public class McVersion
{
    public string Id { get; set; } = "";
    public string Directory { get; set; } = "";
    public string JsonPath { get; set; } = "";
    public string JarPath { get; set; } = "";
    public string Type { get; set; } = "Release";
    public DateTime ModifiedTime { get; set; }

    public override string ToString() => Id;
}
