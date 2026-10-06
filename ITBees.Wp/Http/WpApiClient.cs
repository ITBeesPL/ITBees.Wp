using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITBees.Wp.Configuration;
using ITBees.Wp.Models;

namespace ITBees.Wp.Http;

public class WpApiClient : IWpApiClient
{
    /// <summary>Named HttpClient registered by the DI extensions. It has no base address - the site URL comes from the options.</summary>
    public const string HttpClientName = "ITBees.Wp";

    /// <summary>
    /// WordPress speaks snake_case JSON; nulls are skipped so that updates stay partial. The relaxed encoder keeps
    /// HTML tags and non-ASCII letters in page content as they are instead of unicode escape sequences - the JSON
    /// goes to an API, never into an HTML document.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private const string PagesRoute = "wp/v2/pages";
    private const string DateFormat = "yyyy-MM-dd'T'HH:mm:ss";

    private readonly HttpClient _http;
    private readonly WpOptions _options;
    private readonly AuthenticationHeaderValue? _authorization;

    /// <param name="http">Client used for every call. The timeout should already be set (see <see cref="WpOptions.HttpTimeoutSeconds"/>).</param>
    /// <param name="options">Site address and credentials; validated here.</param>
    public WpApiClient(HttpClient http, WpOptions options)
    {
        options.Validate();
        _http = http;
        _options = options;
        _authorization = BuildAuthorization(options);
    }

    public Task<WpSiteInfo> GetSiteInfoAsync(CancellationToken ct = default) =>
        SendAsync<WpSiteInfo>(HttpMethod.Get, BuildUrl(string.Empty), body: null, ct);

    public Task<WpUser> GetCurrentUserAsync(CancellationToken ct = default) =>
        SendAsync<WpUser>(HttpMethod.Get, BuildUrl("wp/v2/users/me", Parameter("context", ToParameter(WpContext.Edit))),
            body: null, ct);

    public async Task<WpPage?> GetPageAsync(int id, WpContext context = WpContext.Edit, CancellationToken ct = default)
    {
        using var response = await SendRawAsync(HttpMethod.Get,
            BuildUrl($"{PagesRoute}/{id}", Parameter("context", ToParameter(context))), body: null, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        return await ReadAsync<WpPage>(response, ct);
    }

    public async Task<WpPagedResult<WpPage>> GetPagesAsync(WpPageQuery query, CancellationToken ct = default)
    {
        using var response = await SendRawAsync(HttpMethod.Get, BuildUrl(PagesRoute, BuildPageQuery(query)),
            body: null, ct);
        var items = await ReadAsync<List<WpPage>>(response, ct);
        return new WpPagedResult<WpPage>
        {
            Items = items,
            Page = query.Page,
            PerPage = query.PerPage,
            Total = ReadHeaderInt(response, "X-WP-Total") ?? items.Count,
            TotalPages = ReadHeaderInt(response, "X-WP-TotalPages") ?? 1
        };
    }

    public Task<WpPage> CreatePageAsync(WpPageIm page, CancellationToken ct = default) =>
        SendAsync<WpPage>(HttpMethod.Post, BuildUrl(PagesRoute), page, ct);

    public Task<WpPage> UpdatePageAsync(int id, WpPageUm page, CancellationToken ct = default) =>
        SendAsync<WpPage>(HttpMethod.Post, BuildUrl($"{PagesRoute}/{id}"), page, ct);

    public async Task<WpPage> DeletePageAsync(int id, bool force = false, CancellationToken ct = default)
    {
        using var response = await SendRawAsync(HttpMethod.Delete,
            BuildUrl($"{PagesRoute}/{id}", Parameter("force", force ? "true" : "false")), body: null, ct);
        await EnsureSuccessAsync(response, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            // A permanent delete answers with { "deleted": true, "previous": { ...page... } },
            // moving to trash answers with the page itself.
            var pageElement = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("previous", out var previous)
                ? previous
                : root;
            return pageElement.Deserialize<WpPage>(JsonOptions)
                   ?? throw new WpApiException($"WordPress returned an empty body for {Describe(response)}.",
                       response.StatusCode, responseBody: body);
        }
        catch (JsonException exception)
        {
            throw NotJson(response, body, exception);
        }
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var response = await SendRawAsync(method, url, body, ct);
        return await ReadAsync<T>(response, ct);
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string url, object? body,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        if (_authorization != null)
            request.Headers.Authorization = _authorization;
        if (body != null)
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
        var response = await _http.SendAsync(request, ct);
        // The socket handler fills this in; custom handlers (tests, decorators) may not, and the error messages rely on it.
        response.RequestMessage ??= request;
        return response;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        await EnsureSuccessAsync(response, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        T? result;
        try
        {
            result = JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw NotJson(response, body, exception);
        }

        return result ?? throw new WpApiException($"WordPress returned an empty body for {Describe(response)}.",
            response.StatusCode, responseBody: body);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(ct);
        var error = TryParseError(body);
        var code = error?.Code != null ? $" ({error.Code})" : string.Empty;
        var reason = error?.Message ?? Truncate(body);
        throw new WpApiException(
            $"WordPress API call {Describe(response)} failed with HTTP {(int)response.StatusCode}{code}: {reason}",
            response.StatusCode, error?.Code, body);
    }

    private static WpApiException NotJson(HttpResponseMessage response, string body, JsonException exception) =>
        new(
            $"WordPress answered {Describe(response)} with HTTP {(int)response.StatusCode} but the body is not the expected JSON - " +
            "a security plugin, a login redirect or a maintenance page may be responding instead of the REST API: " +
            Truncate(body), response.StatusCode, responseBody: body, innerException: exception);

    private static WpApiError? TryParseError(string body)
    {
        if (!body.TrimStart().StartsWith('{'))
            return null;
        try
        {
            return JsonSerializer.Deserialize<WpApiError>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static int? ReadHeaderInt(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) &&
        int.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static AuthenticationHeaderValue? BuildAuthorization(WpOptions options) => options.AuthMode switch
    {
        WpAuthMode.ApplicationPassword => new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.ApplicationPassword.Trim()}"))),
        WpAuthMode.BearerToken => new AuthenticationHeaderValue("Bearer", options.BearerToken.Trim()),
        _ => null
    };

    /// <summary>
    /// Builds the absolute request URL. Pretty permalinks: "{site}/wp-json/{route}?{query}";
    /// otherwise "{site}/?rest_route=/{route}&amp;{query}".
    /// </summary>
    private string BuildUrl(string route, IEnumerable<KeyValuePair<string, string>>? parameters = null)
    {
        var site = _options.SiteUrl.TrimEnd('/');
        var query = parameters == null
            ? string.Empty
            : string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        if (_options.UseRestRouteQuery)
            return $"{site}/?rest_route=/{route}" + (query.Length > 0 ? "&" + query : string.Empty);

        return $"{site}/{_options.RestApiPath.Trim('/')}/{route}" + (query.Length > 0 ? "?" + query : string.Empty);
    }

    private static List<KeyValuePair<string, string>> BuildPageQuery(WpPageQuery query)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("context", ToParameter(query.Context)),
            new("page", query.Page.ToString(CultureInfo.InvariantCulture)),
            new("per_page", query.PerPage.ToString(CultureInfo.InvariantCulture))
        };

        Add(parameters, "search", query.Search);
        AddList(parameters, "status", query.Status);
        AddList(parameters, "slug", query.Slug);
        AddList(parameters, "parent", query.Parent?.Select(ToParameter));
        AddList(parameters, "parent_exclude", query.ParentExclude?.Select(ToParameter));
        AddList(parameters, "include", query.Include?.Select(ToParameter));
        AddList(parameters, "exclude", query.Exclude?.Select(ToParameter));
        Add(parameters, "author", query.Author?.ToString(CultureInfo.InvariantCulture));
        Add(parameters, "offset", query.Offset?.ToString(CultureInfo.InvariantCulture));
        Add(parameters, "menu_order", query.MenuOrder?.ToString(CultureInfo.InvariantCulture));
        Add(parameters, "after", ToParameter(query.After));
        Add(parameters, "before", ToParameter(query.Before));
        Add(parameters, "modified_after", ToParameter(query.ModifiedAfter));
        Add(parameters, "modified_before", ToParameter(query.ModifiedBefore));
        Add(parameters, "order", query.Order?.ToString().ToLowerInvariant());
        Add(parameters, "orderby", query.OrderBy);
        return parameters;
    }

    private static void Add(List<KeyValuePair<string, string>> parameters, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parameters.Add(new KeyValuePair<string, string>(name, value));
    }

    /// <summary>
    /// WordPress accepts array parameters as comma-separated lists (wp_parse_list splits on commas and whitespace).
    /// A single value containing either would silently turn into several values - e.g. a slug built from user input
    /// such as "nowa,regulamin" would also match the page "regulamin" - so such values are rejected.
    /// </summary>
    private static void AddList(List<KeyValuePair<string, string>> parameters, string name, IEnumerable<string>? values)
    {
        var items = values?.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).ToList();
        if (items is not { Count: > 0 })
            return;

        var invalid = items.FirstOrDefault(v => v.Contains(',') || v.Any(char.IsWhiteSpace));
        if (invalid != null)
        {
            throw new ArgumentException(
                $"Query parameter '{name}' value '{invalid}' must not contain commas or whitespace - WordPress would split it into several values.",
                name);
        }

        parameters.Add(new KeyValuePair<string, string>(name, string.Join(",", items)));
    }

    private static List<KeyValuePair<string, string>> Parameter(string name, string value) =>
        new() { new KeyValuePair<string, string>(name, value) };

    private static string ToParameter(WpContext context) => context.ToString().ToLowerInvariant();

    private static string ToParameter(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>ISO 8601 without fractions; a UTC value gets the "Z" suffix so that WordPress does not treat it as site-local time.</summary>
    private static string? ToParameter(DateTime? value) => value == null
        ? null
        : value.Value.ToString(DateFormat, CultureInfo.InvariantCulture) +
          (value.Value.Kind == DateTimeKind.Utc ? "Z" : string.Empty);

    private static string Describe(HttpResponseMessage response) =>
        $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}";

    private static string Truncate(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= 2000 ? trimmed : trimmed[..2000] + "...";
    }
}
