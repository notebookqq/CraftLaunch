namespace CraftLaunch.Models;

public class DownloadProgress
{
    public string Phase { get; set; } = "";
    public string CurrentFile { get; set; } = "";
    public long CompletedFiles { get; set; }
    public long TotalFiles { get; set; }

    public double Percent => TotalFiles == 0 ? 0 : CompletedFiles * 100.0 / TotalFiles;
}
