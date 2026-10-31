using System.Net;
using System.Net.Http;

namespace CraftLaunch.Services;

public static class HttpService
{
    private static readonly object Lock = new();
    private static HttpClient? _client;

    public static HttpClient Client
    {
        get { lock (Lock) return _client ??= Build(); }
    }

    public static void ConfigureProxy()
    {
        lock (Lock) _client = Build();
    }

    private static HttpClient Build()
    {
        var settings = ConfigService.Current;
        var handler = new HttpClientHandler();

        if (settings.ProxyEnabled && !string.IsNullOrEmpty(settings.ProxyHost))
            handler.Proxy = new WebProxy(settings.ProxyHost, settings.ProxyPort);

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(120)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CraftLaunch/0.1");
        return client;
    }
}
