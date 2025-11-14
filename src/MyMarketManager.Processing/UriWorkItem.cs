namespace MyMarketManager.Processing;

/// <summary>
/// Work item that contains an ID, a URI, and a MIME type.
/// Used for work items that need to fetch data by ID and download/process content from a URI.
/// </summary>
public class UriWorkItem(Guid id, string uri, string mimeType) : IWorkItem
{
    public Guid Id { get; } = id;

    public string Uri { get; } = uri ?? throw new ArgumentNullException(nameof(uri));

    public string MimeType { get; } = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
}
