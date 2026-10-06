namespace ITBees.Wp.Models;

/// <summary>One page of a list response, with the totals WordPress reports in the X-WP-Total / X-WP-TotalPages headers.</summary>
public class WpPagedResult<T>
{
    public List<T> Items { get; set; } = new();

    /// <summary>Total number of matching items across all pages.</summary>
    public int Total { get; set; }

    public int TotalPages { get; set; }

    public int Page { get; set; }

    public int PerPage { get; set; }
}
