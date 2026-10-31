namespace CraftLaunch.Models;

public class ProxySetting
{
    public bool Enabled { get; init; }
    public string Host { get; init; } = "";
    public int Port { get; init; }

    public static ProxySetting? From(AppSettings settings)
    {
        if (settings is { ProxyEnabled: true, ProxyHost: var host } && host.Length > 0)
            return new ProxySetting { Enabled = true, Host = host, Port = settings.ProxyPort };
        return null;
    }
}
