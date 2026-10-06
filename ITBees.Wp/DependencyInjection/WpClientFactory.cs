using ITBees.Wp.Configuration;
using ITBees.Wp.Http;
using ITBees.Wp.Services;
using Microsoft.Extensions.Logging;

namespace ITBees.Wp.DependencyInjection;

public class WpClientFactory : IWpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public WpClientFactory(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    public IWpApiClient CreateApiClient(WpOptions options)
    {
        var http = _httpClientFactory.CreateClient(WpApiClient.HttpClientName);
        http.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
        return new WpApiClient(http, options);
    }

    public IWpPageService CreatePageService(WpOptions options) =>
        new WpPageService(CreateApiClient(options), _loggerFactory.CreateLogger<WpPageService>());
}
