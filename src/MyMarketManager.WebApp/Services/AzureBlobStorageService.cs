using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MyMarketManager.Data.Services;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Azure Blob Storage implementation for managing supplier data files.
/// </summary>
public class AzureBlobStorageService : BlobStorageService
{
    private const string SupplierUploadsContainer = "supplier-uploads";
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<AzureBlobStorageService> logger) : base(logger)
    {
        _blobServiceClient = blobServiceClient;
    }

    /// <summary>
    /// Ensures the supplier-uploads container exists.
    /// </summary>
    public async Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(SupplierUploadsContainer);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        Logger.LogInformation("Ensured container {ContainerName} exists", SupplierUploadsContainer);
    }

    /// <summary>
    /// Uploads a file stream to Azure Blob Storage.
    /// </summary>
    public override async Task<string> UploadFileAsync(
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerExistsAsync(cancellationToken);

        var containerClient = _blobServiceClient.GetBlobContainerClient(SupplierUploadsContainer);
        var blobClient = containerClient.GetBlobClient(fileName);

        await blobClient.UploadAsync(fileStream, overwrite: false, cancellationToken);

        Logger.LogInformation("Uploaded file {FileName} to blob storage", fileName);
        return blobClient.Uri.ToString();
    }

    /// <summary>
    /// Downloads a blob from Azure Blob Storage.
    /// </summary>
    public override async Task<Stream> DownloadFileAsync(
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
