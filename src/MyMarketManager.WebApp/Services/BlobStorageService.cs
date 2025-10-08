using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Service for managing blob storage operations for supplier data files.
/// </summary>
public class BlobStorageService
{
    private const string SupplierUploadsContainer = "supplier-uploads";
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Ensures the supplier-uploads container exists.
    /// </summary>
    public async Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(SupplierUploadsContainer);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        _logger.LogInformation("Ensured container {ContainerName} exists", SupplierUploadsContainer);
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
    /// Uploads a file stream to blob storage with a unique name based on hash.
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
        var extension = System.IO.Path.GetExtension(originalFileName);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var blobName = $"{timestamp}_{fileHash.Substring(0, 8)}{extension}";

        await EnsureContainerExistsAsync(cancellationToken);

        var containerClient = _blobServiceClient.GetBlobContainerClient(SupplierUploadsContainer);
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(fileStream, overwrite: false, cancellationToken);

        _logger.LogInformation("Uploaded file {FileName} (hash: {FileHash}) to blob storage as {BlobName}", 
            originalFileName, fileHash, blobName);

        return (blobClient.Uri.ToString(), fileHash);
    }

    /// <summary>
    /// Uploads a file stream to blob storage.
    /// </summary>
    public async Task<string> UploadFileAsync(
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerExistsAsync(cancellationToken);

        var containerClient = _blobServiceClient.GetBlobContainerClient(SupplierUploadsContainer);
        var blobClient = containerClient.GetBlobClient(fileName);

        await blobClient.UploadAsync(fileStream, overwrite: false, cancellationToken);

        _logger.LogInformation("Uploaded file {FileName} to blob storage", fileName);
        return blobClient.Uri.ToString();
    }

    /// <summary>
    /// Downloads a blob to a stream.
    /// </summary>
    public async Task<Stream> DownloadFileAsync(
        string blobUrl,
        CancellationToken cancellationToken = default)
    {
        var blobClient = new BlobClient(new Uri(blobUrl));
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    /// <summary>
    /// Lists all blobs in the supplier-uploads container.
    /// </summary>
    public async Task<List<BlobItem>> ListBlobsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureContainerExistsAsync(cancellationToken);

        var containerClient = _blobServiceClient.GetBlobContainerClient(SupplierUploadsContainer);
        var blobs = new List<BlobItem>();

        await foreach (var blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            blobs.Add(blobItem);
        }

        return blobs;
    }

    /// <summary>
    /// Gets the full URL for a blob by name.
    /// </summary>
    public string GetBlobUrl(string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(SupplierUploadsContainer);
        var blobClient = containerClient.GetBlobClient(blobName);
        return blobClient.Uri.ToString();
    }
}
