using ITBees.Wp.Configuration;
using ITBees.Wp.Http;
using ITBees.Wp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ITBees.Wp.DependencyInjection;

public static class WpServiceCollectionExtensions
{
    /// <summary>Registers the connector for one site, binding <see cref="WpOptions"/> from the given configuration section (default "Wp").</summary>
    public static IServiceCollection AddITBeesWp(this IServiceCollection services, IConfiguration configuration,
        string sectionName = "Wp")
    {
        services.Configure<WpOptions>(configuration.GetSection(sectionName));
        return services.AddITBeesWpCore();
    }

    /// <summary>Registers the connector for one site with options set in code.</summary>
    public static IServiceCollection AddITBeesWp(this IServiceCollection services, Action<WpOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services.AddITBeesWpCore();
    }

    /// <summary>Registers the connector for one site with a ready <see cref="WpOptions"/> instance (e.g. read from a database at start-up).</summary>
    public static IServiceCollection AddITBeesWp(this IServiceCollection services, WpOptions options) =>
        services.AddITBeesWp(target => Copy(options, target));

    /// <summary>
    /// Registers only <see cref="IWpClientFactory"/> - for hosts that keep every site's credentials in their
    /// own storage and never bind a global "Wp" section.
    /// </summary>
    public static IServiceCollection AddITBeesWpClientFactory(this IServiceCollection services)
    {
        services.AddHttpClient(WpApiClient.HttpClientName);
        services.AddSingleton<IWpClientFactory, WpClientFactory>();
        return services;
    }

    private static IServiceCollection AddITBeesWpCore(this IServiceCollection services)
    {
        services.AddITBeesWpClientFactory();

        // The options are validated when the client is first resolved, not at start-up, so that a host
        // with an unconfigured section still starts and only the WordPress calls fail.
        services.AddTransient<IWpApiClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<WpOptions>>().Value;
            return serviceProvider.GetRequiredService<IWpClientFactory>().CreateApiClient(options);
        });
        services.AddTransient<IWpPageService, WpPageService>();
        return services;
    }

    private static void Copy(WpOptions source, WpOptions target)
    {
        target.SiteUrl = source.SiteUrl;
        target.RestApiPath = source.RestApiPath;
        target.UseRestRouteQuery = source.UseRestRouteQuery;
        target.AuthMode = source.AuthMode;
        target.Username = source.Username;
        target.ApplicationPassword = source.ApplicationPassword;
        target.BearerToken = source.BearerToken;
        target.AllowInsecureHttp = source.AllowInsecureHttp;
        target.HttpTimeoutSeconds = source.HttpTimeoutSeconds;
    }
}
