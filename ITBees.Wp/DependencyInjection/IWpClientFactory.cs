using ITBees.Wp.Configuration;
using ITBees.Wp.Http;
using ITBees.Wp.Services;

namespace ITBees.Wp.DependencyInjection;

/// <summary>
/// Builds connectors for options resolved at runtime instead of the "Wp" configuration section -
/// for hosts that manage many WordPress sites (one per customer) with credentials kept in a database.
/// </summary>
public interface IWpClientFactory
{
    IWpApiClient CreateApiClient(WpOptions options);

    IWpPageService CreatePageService(WpOptions options);
}
