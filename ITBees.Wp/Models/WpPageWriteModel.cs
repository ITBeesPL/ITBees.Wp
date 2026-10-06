namespace ITBees.Wp.Models;

/// <summary>
/// Fields accepted by POST /wp-json/wp/v2/pages (create) and POST /wp-json/wp/v2/pages/{id} (update).
/// Every property is optional: null properties are not sent, so an update changes only what you set.
/// </summary>
public abstract class WpPageWriteModel
{
    public string? Title { get; set; }

    /// <summary>Page body - plain HTML or block markup (&lt;!-- wp:paragraph --&gt; ...).</summary>
    public string? Content { get; set; }

    public string? Excerpt { get; set; }

    /// <summary>One of <see cref="WpPageStatuses"/>. A page created without a status becomes a draft.</summary>
    public string? Status { get; set; }

    /// <summary>URL-friendly name; WordPress derives it from the title when omitted and the page is published.</summary>
    public string? Slug { get; set; }

    /// <summary>Id of the parent page; 0 makes the page top-level.</summary>
    public int? Parent { get; set; }

    public int? MenuOrder { get; set; }

    /// <summary>Theme template file name; empty string resets to the default template.</summary>
    public string? Template { get; set; }

    /// <summary>Media id of the featured image; 0 removes it.</summary>
    public int? FeaturedMedia { get; set; }

    /// <summary>User id of the author (requires permission to assign authors).</summary>
    public int? Author { get; set; }

    /// <summary>Publication date in the site's timezone. With <see cref="WpPageStatuses.Future"/> status it schedules the page.</summary>
    public DateTime? Date { get; set; }

    public DateTime? DateGmt { get; set; }

    /// <summary>One of <see cref="WpDiscussionStatuses"/>.</summary>
    public string? CommentStatus { get; set; }

    /// <summary>One of <see cref="WpDiscussionStatuses"/>.</summary>
    public string? PingStatus { get; set; }

    /// <summary>Password protecting the page; empty string removes the protection.</summary>
    public string? Password { get; set; }

    /// <summary>Custom fields registered with register_post_meta(..., ['show_in_rest' => true]).</summary>
    public Dictionary<string, object?>? Meta { get; set; }
}

/// <summary>Input model for creating a page.</summary>
public class WpPageIm : WpPageWriteModel
{
}

/// <summary>Update model - only the properties you set are sent to WordPress.</summary>
public class WpPageUm : WpPageWriteModel
{
    public WpPageUm()
    {
    }

    /// <summary>Copies every field of another write model, e.g. to update a page with the same data used for creation.</summary>
    public WpPageUm(WpPageWriteModel source)
    {
        Title = source.Title;
        Content = source.Content;
        Excerpt = source.Excerpt;
        Status = source.Status;
        Slug = source.Slug;
        Parent = source.Parent;
        MenuOrder = source.MenuOrder;
        Template = source.Template;
        FeaturedMedia = source.FeaturedMedia;
        Author = source.Author;
        Date = source.Date;
        DateGmt = source.DateGmt;
        CommentStatus = source.CommentStatus;
        PingStatus = source.PingStatus;
        Password = source.Password;
        Meta = source.Meta == null ? null : new Dictionary<string, object?>(source.Meta);
    }
}
