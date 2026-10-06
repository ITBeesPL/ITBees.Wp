namespace ITBees.Wp.Models;

/// <summary>
/// Filters of GET /wp-json/wp/v2/pages. Null properties are not sent. Without <see cref="Status"/>
/// WordPress returns published pages only.
/// </summary>
public class WpPageQuery
{
    /// <summary>Upper limit WordPress enforces on <see cref="PerPage"/>.</summary>
    public const int MaxPerPage = 100;

    /// <summary>1-based page number.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Items per page, 1..<see cref="MaxPerPage"/>.</summary>
    public int PerPage { get; set; } = 10;

    /// <summary>
    /// Decides which fields come back. Edit (default) includes raw title/content and needs edit permissions;
    /// use <see cref="WpContext.View"/> for anonymous, read-only access.
    /// </summary>
    public WpContext Context { get; set; } = WpContext.Edit;

    public string? Search { get; set; }

    /// <summary>Statuses to include - values of <see cref="WpPageStatuses"/>. Non-public statuses require edit permissions.</summary>
    public List<string>? Status { get; set; }

    /// <summary>Exact slugs to match.</summary>
    public List<string>? Slug { get; set; }

    /// <summary>Parent page ids; 0 selects top-level pages.</summary>
    public List<int>? Parent { get; set; }

    public List<int>? ParentExclude { get; set; }

    public List<int>? Include { get; set; }

    public List<int>? Exclude { get; set; }

    public int? Author { get; set; }

    public int? Offset { get; set; }

    public int? MenuOrder { get; set; }

    public DateTime? After { get; set; }

    public DateTime? Before { get; set; }

    public DateTime? ModifiedAfter { get; set; }

    public DateTime? ModifiedBefore { get; set; }

    public WpSortOrder? Order { get; set; }

    /// <summary>One of <see cref="WpPageOrderBy"/>.</summary>
    public string? OrderBy { get; set; }

    public WpPageQuery Clone() => (WpPageQuery)MemberwiseClone();
}
