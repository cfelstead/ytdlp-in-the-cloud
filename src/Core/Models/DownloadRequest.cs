namespace Core.Models;

public record DownloadRequest
{
    public Guid DownloadId { get; init; }
    public string Url { get; init; } = default!;
}