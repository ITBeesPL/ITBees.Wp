namespace ITBees.Wp.Models;

/// <summary>A WordPress user (GET /wp-json/wp/v2/users/me). Login, e-mail and roles are present in the "edit" context only.</summary>
public class WpUser
{
    public int Id { get; set; }

    public string? Username { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Link { get; set; } = string.Empty;

    public string? Locale { get; set; }

    /// <summary>Role slugs, e.g. "administrator", "editor".</summary>
    public List<string>? Roles { get; set; }
}
