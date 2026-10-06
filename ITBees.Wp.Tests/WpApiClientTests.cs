using System.Net;
using System.Text;
using System.Text.Json;
using ITBees.Wp.Configuration;
using ITBees.Wp.Http;
using ITBees.Wp.Models;
using Xunit;

namespace ITBees.Wp.Tests;

public class WpApiClientTests
{
    private static WpOptions Options(Action<WpOptions>? configure = null)
    {
        var options = new WpOptions
        {
            SiteUrl = "https://example.com/",
            Username = "editor",
            ApplicationPassword = "abcd efgh ijkl mnop"
        };
        configure?.Invoke(options);
        return options;
    }

    private static WpApiClient Client(FakeWpHandler handler, Action<WpOptions>? configure = null) =>
        new(new HttpClient(handler), Options(configure));

    [Fact]
    public async Task CreatePageAsync_PostsSnakeCaseJsonWithBasicAuthAndSkipsNulls()
    {
        var handler = new FakeWpHandler(_ =>
            FakeWpHandler.Json(HttpStatusCode.Created, FakeWpHandler.PageJson(21, "o-nas", "draft")));
        var client = Client(handler);

        var page = await client.CreatePageAsync(new WpPageIm
        {
            Title = "O nas",
            Content = "<p>Treść</p>",
            Slug = "o-nas",
            MenuOrder = 3,
            Parent = 7,
            Meta = new Dictionary<string, object?> { ["seo_title"] = "O nas - Firma" }
        });

        var sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, sent.Method);
        Assert.Equal("https://example.com/wp-json/wp/v2/pages", sent.Url);
        Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("editor:abcd efgh ijkl mnop")),
            sent.Authorization);

        using var json = JsonDocument.Parse(sent.Body!);
        var root = json.RootElement;
        Assert.Equal("O nas", root.GetProperty("title").GetString());
        Assert.Equal("<p>Treść</p>", root.GetProperty("content").GetString());
        Assert.Equal(3, root.GetProperty("menu_order").GetInt32());
        Assert.Equal(7, root.GetProperty("parent").GetInt32());
        Assert.Equal("O nas - Firma", root.GetProperty("meta").GetProperty("seo_title").GetString());
        Assert.False(root.TryGetProperty("status", out _));
        Assert.False(root.TryGetProperty("excerpt", out _));

        Assert.Equal(21, page.Id);
        Assert.Equal("o-nas", page.Slug);
        Assert.Equal("draft", page.Status);
        Assert.Equal("O nas", page.Title.Raw);
        Assert.Equal("<p>Treść</p>", page.Content.Raw);
        Assert.Equal(new DateTime(2026, 10, 6, 10, 0, 0), page.Date);
        Assert.Null(page.DateGmt);
        Assert.Equal(new DateTime(2026, 10, 6, 8, 5, 0), page.ModifiedGmt);
        Assert.Equal("https://example.com/o-nas/", page.Link);
        Assert.Equal(JsonValueKind.Array, page.Meta!.Value.ValueKind);
    }

    [Fact]
    public async Task UpdatePageAsync_PostsOnlyTheProvidedFieldsToThePageRoute()
    {
        var handler = new FakeWpHandler(_ =>
            FakeWpHandler.Json(HttpStatusCode.OK, FakeWpHandler.PageJson(21, "o-nas", "publish", content: "<p>Nowa treść</p>")));
        var client = Client(handler);

        var page = await client.UpdatePageAsync(21, new WpPageUm { Content = "<p>Nowa treść</p>" });

        var sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, sent.Method);
        Assert.Equal("https://example.com/wp-json/wp/v2/pages/21", sent.Url);
        Assert.Equal("{\"content\":\"<p>Nowa treść</p>\"}", sent.Body);
        Assert.Equal("<p>Nowa treść</p>", page.Content.Raw);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsNullOnNotFound()
    {
        var handler = new FakeWpHandler(_ => FakeWpHandler.Json(HttpStatusCode.NotFound,
            """{"code":"rest_post_invalid_id","message":"Invalid post ID.","data":{"status":404}}"""));
        var client = Client(handler);

        var page = await client.GetPageAsync(999);

        Assert.Null(page);
        Assert.Equal("https://example.com/wp-json/wp/v2/pages/999?context=edit", handler.Requests.Single().Url);
    }

    [Fact]
    public async Task GetPagesAsync_BuildsQueryStringAndReadsTotalsFromHeaders()
    {
        var handler = new FakeWpHandler(_ => FakeWpHandler.Json(HttpStatusCode.OK,
            "[" + FakeWpHandler.PageJson(21, "o-nas", "publish") + "]",
            new Dictionary<string, string> { ["X-WP-Total"] = "41", ["X-WP-TotalPages"] = "5" }));
        var client = Client(handler);

        var result = await client.GetPagesAsync(new WpPageQuery
        {
            Page = 2,
            PerPage = 10,
            Context = WpContext.View,
            Status = new List<string> { WpPageStatuses.Draft, WpPageStatuses.Publish },
            Slug = new List<string> { "o-nas" },
            Parent = new List<int> { 0 },
            Search = "firma",
            Order = WpSortOrder.Desc,
            OrderBy = WpPageOrderBy.MenuOrder,
            ModifiedAfter = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.Equal(
            "https://example.com/wp-json/wp/v2/pages?context=view&page=2&per_page=10&search=firma&status=draft%2Cpublish&slug=o-nas&parent=0&modified_after=2026-01-01T00%3A00%3A00Z&order=desc&orderby=menu_order",
            handler.Requests.Single().Url);
        Assert.Single(result.Items);
        Assert.Equal(41, result.Total);
        Assert.Equal(5, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PerPage);
    }

    [Fact]
    public async Task UseRestRouteQuery_BuildsPlainPermalinkUrls()
    {
        var handler = new FakeWpHandler(_ =>
            FakeWpHandler.Json(HttpStatusCode.OK, FakeWpHandler.PageJson(21, "o-nas", "publish")));
        var client = Client(handler, o => o.UseRestRouteQuery = true);

        await client.GetPageAsync(21);
        await client.GetSiteInfoAsync();

        Assert.Equal("https://example.com/?rest_route=/wp/v2/pages/21&context=edit", handler.Requests[0].Url);
        Assert.Equal("https://example.com/?rest_route=/", handler.Requests[1].Url);
    }

    [Fact]
    public async Task BearerTokenMode_SendsBearerHeader()
    {
        var handler = new FakeWpHandler(_ =>
            FakeWpHandler.Json(HttpStatusCode.OK, FakeWpHandler.PageJson(21, "o-nas", "publish")));
        var client = Client(handler, o =>
        {
            o.AuthMode = WpAuthMode.BearerToken;
            o.BearerToken = "jwt-token";
        });

        await client.GetPageAsync(21);

        Assert.Equal("Bearer jwt-token", handler.Requests.Single().Authorization);
    }

    [Fact]
    public async Task ErrorResponse_ThrowsWpApiExceptionWithWordPressErrorCode()
    {
        var handler = new FakeWpHandler(_ => FakeWpHandler.Json(HttpStatusCode.Unauthorized,
            """{"code":"rest_cannot_create","message":"Sorry, you are not allowed to create posts as this user.","data":{"status":401}}"""));
        var client = Client(handler);

        var exception = await Assert.ThrowsAsync<WpApiException>(() =>
            client.CreatePageAsync(new WpPageIm { Title = "x" }));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.HttpStatusCode);
        Assert.Equal("rest_cannot_create", exception.WpErrorCode);
        Assert.Contains("Sorry, you are not allowed to create posts as this user.", exception.Message);
        Assert.Contains("POST https://example.com/wp-json/wp/v2/pages", exception.Message);
    }

    [Fact]
    public async Task HtmlInsteadOfJson_ThrowsWpApiExceptionExplainingTheBodyIsNotJson()
    {
        var handler = new FakeWpHandler(_ =>
            FakeWpHandler.Html(HttpStatusCode.OK, "<html><body>Checking your browser...</body></html>"));
        var client = Client(handler);

        var exception = await Assert.ThrowsAsync<WpApiException>(() => client.GetSiteInfoAsync());

        Assert.Equal(HttpStatusCode.OK, exception.HttpStatusCode);
        Assert.Contains("not the expected JSON", exception.Message);
        Assert.Contains("Checking your browser", exception.ResponseBody);
    }

    [Fact]
    public async Task DeletePageAsync_WithForce_ReturnsThePreviousPageState()
    {
        var handler = new FakeWpHandler(request => FakeWpHandler.Json(HttpStatusCode.OK,
            request.Url.Contains("force=true")
                ? "{\"deleted\":true,\"previous\":" + FakeWpHandler.PageJson(21, "o-nas", "publish") + "}"
                : FakeWpHandler.PageJson(21, "o-nas", "trash")));
        var client = Client(handler);

        var trashed = await client.DeletePageAsync(21);
        var deleted = await client.DeletePageAsync(21, force: true);

        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Equal("https://example.com/wp-json/wp/v2/pages/21?force=false", handler.Requests[0].Url);
        Assert.Equal("https://example.com/wp-json/wp/v2/pages/21?force=true", handler.Requests[1].Url);
        Assert.Equal("trash", trashed.Status);
        Assert.Equal(21, deleted.Id);
        Assert.Equal("publish", deleted.Status);
    }

    [Fact]
    public async Task GetSiteInfoAndCurrentUser_ParseTheIndexAndUserDocuments()
    {
        var handler = new FakeWpHandler(request => request.Path.EndsWith("/users/me")
            ? FakeWpHandler.Json(HttpStatusCode.OK,
                """{"id":1,"username":"editor","name":"Redaktor","email":"redaktor@example.com","slug":"editor","link":"https://example.com/author/editor/","roles":["editor"],"capabilities":{"edit_pages":true}}""")
            : FakeWpHandler.Json(HttpStatusCode.OK,
                """{"name":"Firma","description":"Strona firmowa","url":"https://example.com","home":"https://example.com","gmt_offset":"2","timezone_string":"Europe/Warsaw","namespaces":["oembed/1.0","wp/v2","wp-site-health/v1"],"authentication":{"application-passwords":{"endpoints":{"authorization":"https://example.com/wp-admin/authorize-application.php"}}},"site_logo":0,"site_icon":0,"site_icon_url":"","routes":{"/":{"namespace":"","methods":["GET"]}}}"""));
        var client = Client(handler);

        var site = await client.GetSiteInfoAsync();
        var user = await client.GetCurrentUserAsync();

        Assert.Equal("Firma", site.Name);
        Assert.Equal("Europe/Warsaw", site.TimezoneString);
        Assert.True(site.HasWpV2Namespace);
        Assert.True(site.SupportsApplicationPasswords);
        Assert.Equal("https://example.com/wp-json/", handler.Requests[0].Url);

        Assert.Equal("editor", user.Username);
        Assert.Equal("Redaktor", user.Name);
        Assert.Equal(new[] { "editor" }, user.Roles);
        Assert.Equal("https://example.com/wp-json/wp/v2/users/me?context=edit", handler.Requests[1].Url);
    }

    [Fact]
    public void Options_Validate_RejectsMissingSiteUrlAndCredentials()
    {
        Assert.Throws<InvalidOperationException>(() => new WpOptions().Validate());
        Assert.Throws<InvalidOperationException>(() => new WpOptions { SiteUrl = "example.com" }.Validate());
        Assert.Throws<InvalidOperationException>(() => new WpOptions { SiteUrl = "https://example.com" }.Validate());
        Assert.Throws<InvalidOperationException>(() =>
            new WpOptions { SiteUrl = "https://example.com", AuthMode = WpAuthMode.BearerToken }.Validate());

        new WpOptions { SiteUrl = "https://example.com", AuthMode = WpAuthMode.None }.Validate();
        Options().Validate();
        Assert.Equal("https://example.com/wp-json/", Options().GetRestApiUrl());
        Assert.Equal("https://example.com/?rest_route=/", Options(o => o.UseRestRouteQuery = true).GetRestApiUrl());
    }
}
