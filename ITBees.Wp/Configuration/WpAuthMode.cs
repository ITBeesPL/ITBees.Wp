namespace ITBees.Wp.Configuration;

/// <summary>
/// How the connector proves its identity to the WordPress REST API.
/// </summary>
public enum WpAuthMode
{
    /// <summary>
    /// Application Password (WordPress core since 5.6) sent as HTTP Basic authentication.
    /// Generate one in WP Admin → Users → Profile → "Application Passwords". WordPress accepts
    /// them only over HTTPS (or on a local development site).
    /// </summary>
    ApplicationPassword,

    /// <summary>Bearer token issued by a JWT plugin (e.g. "JWT Authentication for WP REST API").</summary>
    BearerToken,

    /// <summary>
    /// No credentials. Only public, read-only calls work: published pages in <see cref="Models.WpContext.View"/>
    /// context and the site index. Creating or updating pages requires one of the other modes.
    /// </summary>
    None
}
