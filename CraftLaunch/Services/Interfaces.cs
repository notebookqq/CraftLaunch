using CraftLaunch.Models;

namespace CraftLaunch.Services;

public interface IVersionProvider
{
    Task<List<McVersion>> GetVersionsAsync(string gameDirectory);
    void Delete(McVersion version);
}

public interface IModProvider
{
    Task<List<ModInfo>> GetModsAsync(string gameDirectory);
}

public interface ILaunchArgumentBuilder
{
    string Build(LaunchContext context);
}
