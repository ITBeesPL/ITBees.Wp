namespace ITBees.Wp.Models;

/// <summary>
/// WordPress returns title, content, excerpt and guid as objects: the HTML after shortcodes/blocks
/// have been rendered, plus - in the "edit" context only - the raw source stored in the database.
/// </summary>
public class WpRenderedField
{
    public string Rendered { get; set; } = string.Empty;

    /// <summary>Source value as stored in the database; null outside <see cref="WpContext.Edit"/> context.</summary>
    public string? Raw { get; set; }

    /// <summary>True when the page is password protected and the content was withheld.</summary>
    public bool? Protected { get; set; }

    /// <summary>Block editor format version (content only, edit context).</summary>
    public int? BlockVersion { get; set; }

    public override string ToString() => Raw ?? Rendered;
}
