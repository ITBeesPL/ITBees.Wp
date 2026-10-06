using ITBees.Wp.Models;

namespace ITBees.Wp.Http;

/// <summary>
/// Low-level, 1:1 wrapper over the WordPress REST API ("wp/v2" namespace) for one site.
/// Credentials and the site address come from <see cref="Configuration.WpOptions"/>.
/// Every non-success HTTP status is thrown as <see cref="WpApiException"/>.
/// </summary>
public interface IWpApiClient
{
    /// <summary>GET /wp-json/ - site name, address and the namespaces/authentication schemes it supports. Works anonymously.</summary>
    Task<WpSiteInfo> GetSiteInfoAsync(CancellationToken ct = default);

    /// <summary>GET /wp-json/wp/v2/users/me?context=edit - the user the credentials belong to; the quickest way to verify them.</summary>
    Task<WpUser> GetCurrentUserAsync(CancellationToken ct = default);

    /// <summary>GET /wp-json/wp/v2/pages/{id}. Returns null when WordPress answers 404 (no such page).</summary>
    Task<WpPage?> GetPageAsync(int id, WpContext context = WpContext.Edit, CancellationToken ct = default);

    /// <summary>GET /wp-json/wp/v2/pages with the given filters; totals are read from the X-WP-Total headers.</summary>
    Task<WpPagedResult<WpPage>> GetPagesAsync(WpPageQuery query, CancellationToken ct = default);

    /// <summary>POST /wp-json/wp/v2/pages. The response is in "edit" context (raw title/content included).</summary>
    Task<WpPage> CreatePageAsync(WpPageIm page, CancellationToken ct = default);

    /// <summary>POST /wp-json/wp/v2/pages/{id} - partial update: only the non-null properties of <paramref name="page"/> change.</summary>
    Task<WpPage> UpdatePageAsync(int id, WpPageUm page, CancellationToken ct = default);

    /// <summary>
    /// DELETE /wp-json/wp/v2/pages/{id}. Without <paramref name="force"/> the page is moved to trash and
    /// returned with status "trash"; with it the page is deleted permanently and its last state is returned.
    /// </summary>
    Task<WpPage> DeletePageAsync(int id, bool force = false, CancellationToken ct = default);
}
