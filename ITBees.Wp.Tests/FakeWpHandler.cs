using System.Net;
using System.Text;

namespace ITBees.Wp.Tests;

/// <summary>Records every request and answers with whatever the test's responder returns.</summary>
internal sealed class FakeWpHandler : HttpMessageHandler
{
    private readonly Func<CapturedRequest, HttpResponseMessage> _responder;

    public List<CapturedRequest> Requests { get; } = new();

    public FakeWpHandler(Func<CapturedRequest, HttpResponseMessage> responder) => _responder = responder;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var captured = new CapturedRequest(request.Method, request.RequestUri!.AbsoluteUri,
            request.Headers.Authorization?.ToString(), body);
        Requests.Add(captured);
        return _responder(captured);
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string json,
        IEnumerable<KeyValuePair<string, string>>? headers = null)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        foreach (var header in headers ?? Array.Empty<KeyValuePair<string, string>>())
            response.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return response;
    }

    public static HttpResponseMessage Html(HttpStatusCode status, string html) => new(status)
    {
        Content = new StringContent(html, Encoding.UTF8, "text/html")
    };

    /// <summary>A page object the way WordPress 7 renders it in the "edit" context, including the quirks (meta as [] and _links).</summary>
    public static string PageJson(int id, string slug, string status, string title = "O nas",
        string content = "<p>Treść</p>") => $$"""
        {
          "id": {{id}},
          "date": "2026-10-06T10:00:00",
          "date_gmt": null,
          "guid": { "rendered": "https://example.com/?page_id={{id}}", "raw": "https://example.com/?page_id={{id}}" },
          "modified": "2026-10-06T10:05:00",
          "modified_gmt": "2026-10-06T08:05:00",
          "password": "",
          "slug": "{{slug}}",
          "status": "{{status}}",
          "type": "page",
          "link": "https://example.com/{{slug}}/",
          "title": { "raw": "{{title}}", "rendered": "{{title}}" },
          "content": { "raw": "{{content}}", "rendered": "{{content}}\n", "protected": false, "block_version": 0 },
          "excerpt": { "raw": "", "rendered": "", "protected": false },
          "author": 1,
          "featured_media": 0,
          "parent": 0,
          "menu_order": 0,
          "comment_status": "closed",
          "ping_status": "closed",
          "template": "",
          "meta": [],
          "class_list": [ "post-{{id}}", "page", "type-page", "status-{{status}}" ],
          "generated_slug": "{{slug}}",
          "permalink_template": "https://example.com/%pagename%/",
          "_links": { "self": [ { "href": "https://example.com/wp-json/wp/v2/pages/{{id}}" } ] }
        }
        """;
}

internal sealed record CapturedRequest(HttpMethod Method, string Url, string? Authorization, string? Body)
{
    public string Path => new Uri(Url).AbsolutePath;
}
