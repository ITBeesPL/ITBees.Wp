using ITBees.Wp.Configuration;
using ITBees.Wp.Http;
using ITBees.Wp.Models;
using ITBees.Wp.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace WpTestConsoleApp;

internal class Program
{
    /// <summary>
    /// Smoke test against a real site: checks the credentials, then creates (or updates) a draft page
    /// and modifies its content.
    /// Usage: WpTestConsoleApp &lt;siteUrl&gt; &lt;username&gt; &lt;applicationPassword&gt; [slug]
    /// </summary>
    private static async Task<int> Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: WpTestConsoleApp <siteUrl> <username> <applicationPassword> [slug]");
            Console.WriteLine("Example: WpTestConsoleApp https://example.com editor \"abcd efgh ijkl mnop qrst uvwx\"");
            return 1;
        }

        var options = new WpOptions
        {
            SiteUrl = args[0],
            Username = args[1],
            ApplicationPassword = args[2]
        };
        var slug = args.Length > 3 ? args[3] : "itbees-wp-test";

        try
        {
            var apiClient = new WpApiClient(new HttpClient { Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds) },
                options);
            var pageService = new WpPageService(apiClient, NullLogger<WpPageService>.Instance);

            var site = await apiClient.GetSiteInfoAsync();
            Console.WriteLine($"Site: {site.Name} ({site.Home})");
            Console.WriteLine($"  wp/v2 namespace: {site.HasWpV2Namespace}, application passwords: {site.SupportsApplicationPasswords}");

            var user = await apiClient.GetCurrentUserAsync();
            Console.WriteLine($"User: {user.Name} ({user.Username}) roles: {string.Join(", ", user.Roles ?? new List<string>())}");

            var page = await pageService.CreateOrUpdateBySlugAsync(new WpPageIm
            {
                Slug = slug,
                Title = "ITBees.Wp test",
                Status = WpPageStatuses.Draft,
                Content = $"<p>Created by ITBees.Wp at {DateTime.Now:yyyy-MM-dd HH:mm:ss}.</p>"
            });
            Console.WriteLine($"Page: #{page.Id} '{page.Title.Rendered}' status={page.Status} link={page.Link}");

            var updated = await pageService.UpdateAsync(page.Id, new WpPageUm
            {
                Content = page.Content.Raw + $"<p>Updated by ITBees.Wp at {DateTime.Now:HH:mm:ss}.</p>"
            });
            Console.WriteLine($"Updated: #{updated.Id} modified={updated.Modified:yyyy-MM-dd HH:mm:ss}");
            return 0;
        }
        catch (WpApiException exception)
        {
            Console.WriteLine($"WordPress error ({(int?)exception.HttpStatusCode} {exception.WpErrorCode}): {exception.Message}");
            return 2;
        }
    }
}
