namespace ITBees.Wp.Configuration;

/// <summary>
/// Connection settings of a single WordPress site. Bind from the "Wp" configuration section
/// (<c>services.AddITBeesWp(configuration)</c>) or build at runtime - e.g. from a database row -
/// and pass to <see cref="DependencyInjection.IWpClientFactory"/>.
/// </summary>
public class WpOptions
{
    /// <summary>Site address, e.g. "https://example.com" - without "/wp-json".</summary>
    public string SiteUrl { get; set; } = string.Empty;

    /// <summary>
    /// REST API prefix under <see cref="SiteUrl"/>. "wp-json" on every site with pretty permalinks enabled.
    /// </summary>
    public string RestApiPath { get; set; } = "wp-json";

    /// <summary>
    /// Sites without pretty permalinks expose the API only as "/?rest_route=/wp/v2/...".
    /// Set to true for such sites; <see cref="RestApiPath"/> is then ignored.
    /// </summary>
    public bool UseRestRouteQuery { get; set; }

    public WpAuthMode AuthMode { get; set; } = WpAuthMode.ApplicationPassword;

    /// <summary>WordPress user login. The user needs the "edit_pages" capability (Editor or Administrator).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Application Password generated for <see cref="Username"/>. WordPress displays it with spaces
    /// ("abcd efgh ijkl mnop qrst uvwx") - both forms, with and without spaces, are accepted.
    /// </summary>
    public string ApplicationPassword { get; set; } = string.Empty;

    /// <summary>Token sent as "Authorization: Bearer" when <see cref="AuthMode"/> is <see cref="WpAuthMode.BearerToken"/>.</summary>
    public string BearerToken { get; set; } = string.Empty;

    /// <summary>
    /// Allows credentials to be sent to a plain "http://" site. Off by default: an Application Password or bearer
    /// token travels in clear text over HTTP, and WordPress itself accepts Application Passwords without HTTPS only
    /// on local development sites - the single case this switch is meant for.
    /// </summary>
    public bool AllowInsecureHttp { get; set; }

    public int HttpTimeoutSeconds { get; set; } = 100;

    /// <summary>Absolute URL of the REST API root with a trailing slash, e.g. "https://example.com/wp-json/".</summary>
    public string GetRestApiUrl()
    {
        var site = SiteUrl.TrimEnd('/');
        return UseRestRouteQuery ? $"{site}/?rest_route=/" : $"{site}/{RestApiPath.Trim('/')}/";
    }

    /// <summary>Throws <see cref="InvalidOperationException"/> when the options cannot produce a working client.</summary>
    public void Validate()
    {
        if (!Uri.TryCreate(SiteUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "WpOptions.SiteUrl must be an absolute http(s) address of the WordPress site, e.g. https://example.com.");
        }

        // Checked before any request is built: the Authorization header goes out with the very first call.
        if (uri.Scheme == Uri.UriSchemeHttp && AuthMode != WpAuthMode.None && !AllowInsecureHttp)
        {
            throw new InvalidOperationException(
                "WpOptions.SiteUrl uses plain http:// - the credentials would be sent in clear text. Use https://, " +
                "or set AllowInsecureHttp = true for a local development site only.");
        }

        if (!UseRestRouteQuery && string.IsNullOrWhiteSpace(RestApiPath))
            throw new InvalidOperationException("WpOptions.RestApiPath must not be empty (default: wp-json).");

        switch (AuthMode)
        {
            case WpAuthMode.ApplicationPassword when string.IsNullOrWhiteSpace(Username) ||
                                                    string.IsNullOrWhiteSpace(ApplicationPassword):
                throw new InvalidOperationException(
                    "WpOptions.Username and WpOptions.ApplicationPassword are required for AuthMode = ApplicationPassword.");
            case WpAuthMode.BearerToken when string.IsNullOrWhiteSpace(BearerToken):
                throw new InvalidOperationException("WpOptions.BearerToken is required for AuthMode = BearerToken.");
        }

        if (HttpTimeoutSeconds <= 0)
            throw new InvalidOperationException("WpOptions.HttpTimeoutSeconds must be greater than zero.");
    }
}
