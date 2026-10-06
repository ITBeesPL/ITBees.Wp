using ITBees.Wp.Models;

namespace ITBees.Wp.Services;

/// <summary>
/// Creating and updating pages on one WordPress site. Built on <see cref="Http.IWpApiClient"/>;
/// adds lookup by slug, "create or update" and reading every page across the API's pagination.
/// </summary>
public interface IWpPageService
{
    /// <summary>Returns the page or null when it does not exist.</summary>
    Task<WpPage?> GetAsync(int id, WpContext context = WpContext.Edit, CancellationToken ct = default);

    /// <summary>
    /// Finds a page by its slug (the last segment of its address) in any status except trash. When several
    /// pages share the slug under different parents, the first one WordPress returns wins - filter by
    /// <see cref="WpPageQuery.Parent"/> through <see cref="GetPaginatedAsync"/> if that matters.
    /// </summary>
    Task<WpPage?> GetBySlugAsync(string slug, WpContext context = WpContext.Edit, IEnumerable<string>? statuses = null,
        CancellationToken ct = default);

    /// <summary>One page of results with the totals WordPress reports.</summary>
    Task<WpPagedResult<WpPage>> GetPaginatedAsync(WpPageQuery query, CancellationToken ct = default);

    /// <summary>
    /// Every page matching the query, fetched 100 at a time until the last API page. <see cref="WpPageQuery.Page"/>
    /// and <see cref="WpPageQuery.PerPage"/> of <paramref name="query"/> are ignored.
    /// </summary>
    Task<List<WpPage>> GetAllAsync(WpPageQuery? query = null, CancellationToken ct = default);

    /// <summary>Creates a page. Without <see cref="WpPageWriteModel.Status"/> it is created as a draft.</summary>
    Task<WpPage> CreateAsync(WpPageIm page, CancellationToken ct = default);

    /// <summary>Updates the given fields of an existing page; null fields stay unchanged.</summary>
    Task<WpPage> UpdateAsync(int id, WpPageUm page, CancellationToken ct = default);

    /// <summary>
    /// Updates the page whose slug equals <see cref="WpPageWriteModel.Slug"/>, or creates it when there is none.
    /// Lets an application own a page by a stable name ("regulamin", "cennik") and re-publish it at will.
    /// </summary>
    Task<WpPage> CreateOrUpdateBySlugAsync(WpPageIm page, CancellationToken ct = default);

    /// <summary>Moves the page to trash, or deletes it permanently when <paramref name="force"/> is true.</summary>
    Task<WpPage> DeleteAsync(int id, bool force = false, CancellationToken ct = default);
}
