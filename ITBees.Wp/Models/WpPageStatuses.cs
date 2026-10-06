namespace ITBees.Wp.Models;

/// <summary>Values of the page "status" field.</summary>
public static class WpPageStatuses
{
    /// <summary>Visible to everyone.</summary>
    public const string Publish = "publish";

    /// <summary>Scheduled - published automatically at the page "date".</summary>
    public const string Future = "future";

    /// <summary>The default status of a page created without an explicit status.</summary>
    public const string Draft = "draft";

    /// <summary>Waiting for review by an editor.</summary>
    public const string Pending = "pending";

    /// <summary>Visible only to logged-in users allowed to read private pages.</summary>
    public const string Private = "private";

    public const string Trash = "trash";

    /// <summary>Query-only value: every status except trash and auto-drafts. Requires edit permissions.</summary>
    public const string Any = "any";
}

/// <summary>Values of the "comment_status" and "ping_status" fields.</summary>
public static class WpDiscussionStatuses
{
    public const string Open = "open";
    public const string Closed = "closed";
}

/// <summary>Values of the "orderby" query parameter for pages.</summary>
public static class WpPageOrderBy
{
    public const string Author = "author";
    public const string Date = "date";
    public const string Id = "id";
    public const string Include = "include";
    public const string Modified = "modified";
    public const string Parent = "parent";
    public const string Relevance = "relevance";
    public const string Slug = "slug";
    public const string IncludeSlugs = "include_slugs";
    public const string Title = "title";
    public const string MenuOrder = "menu_order";
}

public enum WpSortOrder
{
    Asc,
    Desc
}
