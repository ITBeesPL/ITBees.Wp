namespace ITBees.Wp.Models;

/// <summary>
/// The "context" parameter of the REST API - decides which fields WordPress includes in the response.
/// </summary>
public enum WpContext
{
    /// <summary>Public fields with rendered HTML only (title.rendered, content.rendered). Works without credentials.</summary>
    View,

    /// <summary>Minimal set used for embedding (id, link, title, excerpt).</summary>
    Embed,

    /// <summary>
    /// Everything, including the source values as stored in the database (title.raw, content.raw) - what you
    /// need to read a page, modify it and send it back. Requires a user allowed to edit pages.
    /// </summary>
    Edit
}
