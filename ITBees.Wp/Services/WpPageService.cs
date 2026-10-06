using ITBees.Wp.Http;
using ITBees.Wp.Models;
using Microsoft.Extensions.Logging;

namespace ITBees.Wp.Services;

public class WpPageService : IWpPageService
{
    private readonly IWpApiClient _wpApiClient;
    private readonly ILogger<WpPageService> _logger;

    public WpPageService(IWpApiClient wpApiClient, ILogger<WpPageService> logger)
    {
        _wpApiClient = wpApiClient;
        _logger = logger;
    }

    public Task<WpPage?> GetAsync(int id, WpContext context = WpContext.Edit, CancellationToken ct = default) =>
        _wpApiClient.GetPageAsync(id, context, ct);

    public async Task<WpPage?> GetBySlugAsync(string slug, WpContext context = WpContext.Edit,
        IEnumerable<string>? statuses = null, CancellationToken ct = default)
    {
        var requestedSlug = ValidateSingleSlug(slug, nameof(slug));

        var result = await _wpApiClient.GetPagesAsync(new WpPageQuery
        {
            Slug = new List<string> { requestedSlug },
            // Drafts and private pages have slugs too - without "any" WordPress would search published pages only.
            Status = statuses?.ToList() ?? new List<string> { WpPageStatuses.Any },
            Context = context,
            PerPage = 1
        }, ct);

        // A write may follow (CreateOrUpdateBySlugAsync), so never trust the filter alone: only an exact slug match
        // counts. WordPress stores slugs in lower case, hence the case-insensitive comparison.
        return result.Items.FirstOrDefault(page => string.Equals(page.Slug, requestedSlug, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// A WordPress slug never contains whitespace or commas, and the REST API would read a value with them as a list
    /// of several slugs - a slug assembled from untrusted input could then point the lookup at another page.
    /// </summary>
    private static string ValidateSingleSlug(string? slug, string parameterName)
    {
        var trimmed = slug?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Slug must not be empty.", parameterName);
        if (trimmed.Contains(',') || trimmed.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException(
                $"Slug '{trimmed}' is not a single WordPress slug - commas and whitespace are not allowed.", parameterName);
        }

        return trimmed;
    }

    public Task<WpPagedResult<WpPage>> GetPaginatedAsync(WpPageQuery query, CancellationToken ct = default) =>
        _wpApiClient.GetPagesAsync(query, ct);

    public async Task<List<WpPage>> GetAllAsync(WpPageQuery? query = null, CancellationToken ct = default)
    {
        var template = query ?? new WpPageQuery();
        var pages = new List<WpPage>();
        var page = 1;

        while (true)
        {
            var current = template.Clone();
            current.Page = page;
            current.PerPage = WpPageQuery.MaxPerPage;

            var result = await _wpApiClient.GetPagesAsync(current, ct);
            pages.AddRange(result.Items);

            // Asking for a page beyond the last one is a 400 (rest_post_invalid_page_number), so stop on the totals.
            if (result.Items.Count == 0 || page >= result.TotalPages)
                break;
            page++;
        }

        return pages;
    }

    public async Task<WpPage> CreateAsync(WpPageIm page, CancellationToken ct = default)
    {
        var created = await _wpApiClient.CreatePageAsync(page, ct);
        _logger.LogInformation("Created WordPress page #{PageId} '{Slug}' with status {Status}", created.Id, created.Slug,
            created.Status);
        return created;
    }

    public async Task<WpPage> UpdateAsync(int id, WpPageUm page, CancellationToken ct = default)
    {
        var updated = await _wpApiClient.UpdatePageAsync(id, page, ct);
        _logger.LogInformation("Updated WordPress page #{PageId} '{Slug}' with status {Status}", updated.Id, updated.Slug,
            updated.Status);
        return updated;
    }

    public async Task<WpPage> CreateOrUpdateBySlugAsync(WpPageIm page, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(page.Slug))
            throw new ArgumentException("WpPageIm.Slug is required to create or update a page by slug.", nameof(page));

        var slug = ValidateSingleSlug(page.Slug, nameof(page));
        var existing = await GetBySlugAsync(slug, WpContext.Edit, statuses: null, ct);
        if (existing == null)
            return await CreateAsync(page, ct);

        _logger.LogDebug("WordPress page with slug '{Slug}' exists as #{PageId} - updating", page.Slug, existing.Id);
        return await UpdateAsync(existing.Id, new WpPageUm(page), ct);
    }

    public async Task<WpPage> DeleteAsync(int id, bool force = false, CancellationToken ct = default)
    {
        var deleted = await _wpApiClient.DeletePageAsync(id, force, ct);
        _logger.LogInformation("{Action} WordPress page #{PageId} '{Slug}'", force ? "Permanently deleted" : "Trashed",
            id, deleted.Slug);
        return deleted;
    }
}
