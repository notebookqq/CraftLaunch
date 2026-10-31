namespace CraftLaunch.Models;

public class RemoteVersionInfo
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "release";
    public string Url { get; set; } = "";
    public DateTime ReleaseTime { get; set; }

    public string TypeLabel => Type switch
    {
        "release" => "正式版",
        "snapshot" => "快照",
        "old_beta" => "Beta",
        "old_alpha" => "Alpha",
        _ => Type
    };
}
