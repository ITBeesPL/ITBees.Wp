using System.Text.Json;

namespace ITBees.Wp.Models;

/// <summary>
/// A page as returned by GET/POST /wp-json/wp/v2/pages. Property names map 1:1 to the snake_case
/// JSON fields of the REST API (menu_order → MenuOrder etc.).
/// </summary>
public class WpPage
{
    public int Id { get; set; }

    /// <summary>Publication date in the site's timezone (no offset in the JSON).</summary>
    public DateTime? Date { get; set; }

    /// <summary>Publication date in UTC; null for drafts that have never been scheduled.</summary>
    public DateTime? DateGmt { get; set; }

    public DateTime? Modified { get; set; }

    public DateTime? ModifiedGmt { get; set; }

    /// <summary>URL-friendly name (the last segment of the page address). Empty for drafts without an explicit slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>One of <see cref="WpPageStatuses"/>.</summary>
    public string Status { get; set; } = string.Empty;

    public string Type { get; set; } = "page";

    /// <summary>Public address of the page.</summary>
    public string Link { get; set; } = string.Empty;

    public WpRenderedField Title { get; set; } = new();

    public WpRenderedField Content { get; set; } = new();

    public WpRenderedField Excerpt { get; set; } = new();

    public WpRenderedField? Guid { get; set; }

    /// <summary>User id of the author.</summary>
    public int Author { get; set; }

    /// <summary>Media id of the featured image; 0 when none.</summary>
    public int FeaturedMedia { get; set; }

    /// <summary>Id of the parent page; 0 for top-level pages.</summary>
    public int Parent { get; set; }

    public int MenuOrder { get; set; }

    /// <summary>Theme template file; empty string for the default template.</summary>
    public string? Template { get; set; }

    public string? CommentStatus { get; set; }

    public string? PingStatus { get; set; }

    /// <summary>Password protecting the page (edit context only; empty when none).</summary>
    public string? Password { get; set; }

    /// <summary>Slug WordPress would assign from the title (edit context only).</summary>
    public string? GeneratedSlug { get; set; }

    /// <summary>
    /// Registered custom fields. Kept raw because WordPress answers with an empty JSON array
    /// (not an object) when no meta field is registered for the REST API.
    /// </summary>
    public JsonElement? Meta { get; set; }

    public override string ToString() => $"#{Id} {Title.Rendered} [{Status}]";
}
