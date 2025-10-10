using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace MyMarketManager.Data.Services;

/// <summary>
/// Abstract base class for blob storage operations.
/// Provides common functionality for file hashing and upload/download operations.
/// </summary>
public abstract class BlobStorageService
{
    protected ILogger Logger { get; }

    protected BlobStorageService(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Computes SHA-256 hash of a file stream.
    /// </summary>
    public static async Task<string> ComputeFileHashAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        var hash = await SHA256.HashDataAsync(fileStream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Uploads a file stream with a unique name based on hash.
    /// Returns the blob URL and file hash.
    /// </summary>
    public async Task<(string BlobUrl, string FileHash)> UploadFileWithHashAsync(
        string originalFileName,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        // Compute hash before upload
        var fileHash = await ComputeFileHashAsync(fileStream, cancellationToken);
        fileStream.Position = 0; // Reset stream position

        // Generate unique blob name with timestamp and original extension
        var extension = Path.GetExtension(originalFileName);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var blobName = $"{timestamp}_{fileHash.Substring(0, 8)}{extension}";

        // Upload using implementation-specific method
        var blobUrl = await UploadFileAsync(blobName, fileStream, cancellationToken);

        Logger.LogInformation("Uploaded file {FileName} (hash: {FileHash}) as {BlobName}", 
            originalFileName, fileHash, blobName);

        return (blobUrl, fileHash);
    }

    /// <summary>
    /// Uploads a file stream to storage.
    /// </summary>
    public abstract Task<string> UploadFileAsync(
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from storage.
    /// </summary>
    public abstract Task<Stream> DownloadFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);
}
