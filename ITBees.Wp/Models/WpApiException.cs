using System.Net;

namespace ITBees.Wp.Models;

/// <summary>
/// Thrown when the WordPress REST API answers with a non-success HTTP status or with a body
/// that is not the expected JSON (e.g. an HTML page from a security plugin or maintenance mode).
/// </summary>
public class WpApiException : Exception
{
    public HttpStatusCode? HttpStatusCode { get; }

    /// <summary>WordPress error code from the response body, e.g. "rest_cannot_create" or "rest_post_invalid_id".</summary>
    public string? WpErrorCode { get; }

    public string? ResponseBody { get; }

    public WpApiException(string message, HttpStatusCode? httpStatusCode = null, string? wpErrorCode = null,
        string? responseBody = null, Exception? innerException = null)
        : base(message, innerException)
    {
        HttpStatusCode = httpStatusCode;
        WpErrorCode = wpErrorCode;
        ResponseBody = responseBody;
    }
}
