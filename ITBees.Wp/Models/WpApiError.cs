using System.Text.Json;

namespace ITBees.Wp.Models;

/// <summary>Error envelope returned by the WordPress REST API: { "code": "...", "message": "...", "data": { "status": 404 } }.</summary>
public class WpApiError
{
    public string? Code { get; set; }

    public string? Message { get; set; }

    /// <summary>Kept raw - WordPress puts different shapes here (status, invalid params, details).</summary>
    public JsonElement? Data { get; set; }

    public int? Status =>
        Data is { ValueKind: JsonValueKind.Object } data && data.TryGetProperty("status", out var status) &&
        status.ValueKind == JsonValueKind.Number
            ? status.GetInt32()
            : null;
}
