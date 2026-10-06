using System.Net;
using System.Text.Json;
using ITBees.Wp.Configuration;
using ITBees.Wp.Http;
using ITBees.Wp.Models;
using ITBees.Wp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ITBees.Wp.Tests;

public class WpPageServiceTests
{
    private static WpPageService Service(FakeWpHandler handler) =>
        new(new WpApiClient(new HttpClient(handler), new WpOptions
        {
            SiteUrl = "https://example.com",
            Username = "editor",
            ApplicationPassword = "secret"
        }), NullLogger<WpPageService>.Instance);

    private static Dictionary<string, string> Totals(int total, int totalPages) => new()
    {
        ["X-WP-Total"] = total.ToString(),
        ["X-WP-TotalPages"] = totalPages.ToString()
    };

    [Fact]
    public async Task CreateOrUpdateBySlugAsync_CreatesWhenNoPageHasTheSlug()
    {
        var handler = new FakeWpHandler(request => request.Method == HttpMethod.Get
            ? FakeWpHandler.Json(HttpStatusCode.OK, "[]", Totals(0, 0))
            : FakeWpHandler.Json(HttpStatusCode.Created, FakeWpHandler.PageJson(5, "regulamin", "publish")));
        var service = Service(handler);

        var page = await service.CreateOrUpdateBySlugAsync(new WpPageIm
        {
            Slug = "regulamin",
            Title = "Regulamin",
            Content = "<p>...</p>",
            Status = WpPageStatuses.Publish
        });

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(
            "https://example.com/wp-json/wp/v2/pages?context=edit&page=1&per_page=1&status=any&slug=regulamin",
            handler.Requests[0].Url);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal("https://example.com/wp-json/wp/v2/pages", handler.Requests[1].Url);
        Assert.Equal(5, page.Id);
    }

    [Fact]
    public async Task CreateOrUpdateBySlugAsync_UpdatesTheExistingPageWithTheSameData()
    {
        var handler = new FakeWpHandler(request => request.Method == HttpMethod.Get
            ? FakeWpHandler.Json(HttpStatusCode.OK, "[" + FakeWpHandler.PageJson(21, "regulamin", "draft") + "]",
                Totals(1, 1))
            : FakeWpHandler.Json(HttpStatusCode.OK, FakeWpHandler.PageJson(21, "regulamin", "publish")));
        var service = Service(handler);

        var page = await service.CreateOrUpdateBySlugAsync(new WpPageIm
        {
            Slug = "regulamin",
            Title = "Regulamin",
            Content = "<p>v2</p>",
            Status = WpPageStatuses.Publish
        });

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("https://example.com/wp-json/wp/v2/pages/21", handler.Requests[1].Url);
        using var body = JsonDocument.Parse(handler.Requests[1].Body!);
        Assert.Equal("Regulamin", body.RootElement.GetProperty("title").GetString());
        Assert.Equal("<p>v2</p>", body.RootElement.GetProperty("content").GetString());
        Assert.Equal("publish", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("regulamin", body.RootElement.GetProperty("slug").GetString());
        Assert.Equal(21, page.Id);
    }

    [Fact]
    public async Task CreateOrUpdateBySlugAsync_RequiresSlug()
    {
        var service = Service(new FakeWpHandler(_ => throw new InvalidOperationException("no request expected")));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateOrUpdateBySlugAsync(new WpPageIm { Title = "Bez sluga" }));
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsNullWhenNothingMatches()
    {
        var handler = new FakeWpHandler(_ => FakeWpHandler.Json(HttpStatusCode.OK, "[]", Totals(0, 0)));
        var service = Service(handler);

        var page = await service.GetBySlugAsync("nie-ma", WpContext.View, new[] { WpPageStatuses.Publish });

        Assert.Null(page);
        Assert.Equal("https://example.com/wp-json/wp/v2/pages?context=view&page=1&per_page=1&status=publish&slug=nie-ma",
            handler.Requests.Single().Url);
    }

    [Fact]
    public async Task GetAllAsync_WalksEveryApiPage()
    {
        var handler = new FakeWpHandler(request => FakeWpHandler.Json(HttpStatusCode.OK,
            request.Url.Contains("page=1&")
                ? "[" + FakeWpHandler.PageJson(1, "a", "publish") + "," + FakeWpHandler.PageJson(2, "b", "publish") + "]"
                : "[" + FakeWpHandler.PageJson(3, "c", "publish") + "]",
            Totals(3, 2)));
        var service = Service(handler);

        var pages = await service.GetAllAsync(new WpPageQuery { Status = new List<string> { WpPageStatuses.Any } });

        Assert.Equal(new[] { 1, 2, 3 }, pages.Select(p => p.Id));
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("page=1&per_page=100&status=any", handler.Requests[0].Url);
        Assert.Contains("page=2&per_page=100&status=any", handler.Requests[1].Url);
    }

    [Fact]
    public async Task GetAllAsync_StopsAfterAnEmptyResult()
    {
        var handler = new FakeWpHandler(_ => FakeWpHandler.Json(HttpStatusCode.OK, "[]", Totals(0, 0)));
        var service = Service(handler);

        var pages = await service.GetAllAsync();

        Assert.Empty(pages);
        Assert.Single(handler.Requests);
    }
}
