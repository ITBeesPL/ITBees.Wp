using System.Text.Json;

namespace ITBees.Wp.Models;

/// <summary>Site description from the REST API index (GET /wp-json/).</summary>
public class WpSiteInfo
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>WordPress address (where the core files live).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Site address visitors use.</summary>
    public string Home { get; set; } = string.Empty;

    public string? TimezoneString { get; set; }

    public string? SiteIconUrl { get; set; }

    /// <summary>Registered API namespaces, e.g. "wp/v2", "wp-site-health/v1".</summary>
    public List<string> Namespaces { get; set; } = new();

    /// <summary>Authentication schemes advertised by the site, e.g. { "application-passwords": { "endpoints": { "authorization": "..." } } }.</summary>
    public JsonElement? Authentication { get; set; }

    /// <summary>True when the core "wp/v2" namespace (pages, posts, users...) is available.</summary>
    public bool HasWpV2Namespace => Namespaces.Contains("wp/v2");

    /// <summary>True when the site advertises Application Passwords (WordPress 5.6+ over HTTPS).</summary>
    public bool SupportsApplicationPasswords =>
        Authentication is { ValueKind: JsonValueKind.Object } authentication &&
        authentication.TryGetProperty("application-passwords", out _);
}
