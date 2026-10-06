using ITBees.Wp.Configuration;
using ITBees.Wp.DependencyInjection;
using ITBees.Wp.Http;
using ITBees.Wp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ITBees.Wp.Tests;

public class WpServiceCollectionExtensionsTests
{
    [Fact]
    public void AddITBeesWp_BindsTheWpSectionAndResolvesTheServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Wp:SiteUrl"] = "https://example.com",
                ["Wp:Username"] = "editor",
                ["Wp:ApplicationPassword"] = "abcd efgh",
                ["Wp:HttpTimeoutSeconds"] = "30"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddITBeesWp(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<WpOptions>>().Value;

        Assert.Equal("https://example.com", options.SiteUrl);
        Assert.Equal(WpAuthMode.ApplicationPassword, options.AuthMode);
        Assert.Equal(30, options.HttpTimeoutSeconds);
        Assert.IsType<WpApiClient>(provider.GetRequiredService<IWpApiClient>());
        Assert.IsType<WpPageService>(provider.GetRequiredService<IWpPageService>());
        Assert.NotNull(provider.GetRequiredService<IWpClientFactory>());
    }

    [Fact]
    public void AddITBeesWp_WithOptionsInstance_CopiesEveryField()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddITBeesWp(new WpOptions
        {
            SiteUrl = "http://localhost:8080",
            AuthMode = WpAuthMode.BearerToken,
            BearerToken = "jwt",
            UseRestRouteQuery = true,
            AllowInsecureHttp = true,
            HttpTimeoutSeconds = 15
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<WpOptions>>().Value;

        Assert.Equal(WpAuthMode.BearerToken, options.AuthMode);
        Assert.Equal("jwt", options.BearerToken);
        Assert.True(options.UseRestRouteQuery);
        Assert.True(options.AllowInsecureHttp);
        Assert.Equal(15, options.HttpTimeoutSeconds);
        // Resolving the client validates the options - http:// passes only because AllowInsecureHttp was copied too.
        Assert.NotNull(provider.GetRequiredService<IWpApiClient>());
    }

    [Fact]
    public void AddITBeesWp_WithInvalidOptions_FailsOnlyWhenTheClientIsResolved()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddITBeesWp(o => o.SiteUrl = string.Empty);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IWpApiClient>());
    }

    [Fact]
    public void AddITBeesWpClientFactory_CreatesServicesForRuntimeOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddITBeesWpClientFactory();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IWpClientFactory>();
        var pageService = factory.CreatePageService(new WpOptions
        {
            SiteUrl = "https://tenant.example.com",
            Username = "editor",
            ApplicationPassword = "secret"
        });

        Assert.IsType<WpPageService>(pageService);
        Assert.Null(provider.GetService<IWpPageService>());
    }
}
