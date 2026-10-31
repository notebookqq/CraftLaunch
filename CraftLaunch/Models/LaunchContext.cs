namespace CraftLaunch.Models;

public class LaunchContext
{
    public required McVersion Version { get; init; }
    public required string JavaPath { get; init; }
    public required string GameDirectory { get; init; }
    public int MemoryMB { get; init; } = 4096;
    public ProxySetting? Proxy { get; init; }
}
