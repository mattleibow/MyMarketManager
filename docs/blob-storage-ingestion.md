# Blob Storage Ingestion Pipeline

## Overview

The blob storage ingestion pipeline enables automated processing of supplier data files uploaded to Azure Blob Storage. This feature is designed to handle password-protected ZIP files (starting with Shein "Request My Data" exports) and create staging batches for validation and promotion into production data.

## Architecture

### Components

1. **BlobStorageService** (`Services/BlobStorageService.cs`)
   - Manages blob storage operations
   - Provides upload, download, and list functionality
   - Ensures the `supplier-uploads` container exists

2. **BlobIngestionService** (`Services/BlobIngestionService.cs`)
   - Background service that polls blob storage every 5 minutes
   - Downloads new blobs and creates StagingBatch records
   - Performs file hash-based deduplication
   - Links batches to blob storage URLs for audit trail

3. **StagingBatch Entity** (`Data/Entities/StagingBatch.cs`)
   - Extended with `BlobStorageUrl` property
   - Tracks the Azure Blob Storage location of uploaded files

## Workflow

### Upload Process

1. **User Upload**: Users manually upload supplier data files (ZIP format) via a web interface (to be implemented)
2. **Blob Storage**: Files are stored in Azure Blob Storage in the `supplier-uploads` container
3. **Background Processing**: BlobIngestionService detects new files and processes them

### Processing Pipeline

```
┌─────────────────┐
│  User Uploads   │
│   ZIP File      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Azure Blob      │
│ Storage         │
│ (supplier-      │
│  uploads)       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Background      │
│ Service Polls   │
│ (5 min interval)│
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Download &      │
│ Hash File       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Check for       │
│ Duplicates      │
│ (by hash & URL) │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Create          │
│ StagingBatch    │
│ Record          │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Status: Pending │
│ Ready for       │
│ Processing      │
└─────────────────┘
```

## Configuration

### Aspire Configuration

The blob storage is configured in the AppHost (`src/MyMarketManager.AppHost/AppHost.cs`):

```csharp
var blobStorage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator =>
    {
        emulator.WithImageTag("latest");
        if (builder.Configuration.GetValue("UseVolumes", true))
            emulator.WithDataVolume();
    });

var blobs = blobStorage.AddBlobs("blobs");
```

### WebApp Configuration

The WebApp registers the blob service client in `Program.cs`:

```csharp
// Configure Azure Blob Storage client provided by Aspire
builder.AddAzureBlobServiceClient("blobs");

// Add blob storage services
builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddHostedService<BlobIngestionService>();
```

## Development

### Running Locally

1. Ensure Docker Desktop is running (required for Azurite emulator)
2. Run the application through AppHost:
   ```bash
   dotnet run --project src/MyMarketManager.AppHost
   ```
3. The Azurite storage emulator will start automatically
4. Upload files to blob storage (API/UI to be implemented)
5. Background service will process files every 5 minutes

### Testing

The blob storage URL functionality is tested in `tests/MyMarketManager.Data.Tests/StagingEntityTests.cs`:

```csharp
[Fact]
public async Task StagingBatch_CanStoreBlobUrl()
{
    // Test that verifies BlobStorageUrl can be saved and retrieved
}
```

Run tests:
```bash
dotnet test --configuration Release --verbosity normal --filter "Category!=LongRunning"
```

## Database Schema Changes

A migration was added to support the blob storage URL:

- Migration: `20251008015438_AddBlobStorageUrlToStagingBatch`
- Added column: `StagingBatches.BlobStorageUrl` (nvarchar(max), nullable)

## Deduplication Strategy

Files are deduplicated using two mechanisms:

1. **File Hash**: SHA-256 hash of file contents
   - Prevents processing of identical files uploaded under different names
   
2. **Blob URL**: Direct URL comparison
   - Prevents reprocessing of the same blob multiple times

## Future Enhancements

### Planned Features

1. **Upload UI Component**: Blazor component for file upload
2. **ZIP Extraction**: Extract and parse password-protected ZIP files (Shein format)
3. **Parser Registry**: Configurable parsers for different supplier formats
4. **Progress Tracking**: Real-time status updates during processing
5. **Error Handling**: Retry logic and dead-letter queue for failed uploads
6. **Webhook Support**: Trigger processing on blob upload events instead of polling
7. **Cleanup Service**: Archive or delete processed blobs after retention period

### Parser Implementation

The current implementation creates basic staging batches. Full parser implementation will:

1. Extract ZIP files (handle password protection)
2. Parse supplier-specific data formats (CSV, JSON, XML)
3. Create StagingPurchaseOrder and StagingPurchaseOrderItem records
4. Perform smart linking to existing products
5. Generate StagingProductCandidate records for unmatched items

## Troubleshooting

### Common Issues

1. **Blob storage emulator not starting**
   - Ensure Docker Desktop is running
   - Check Aspire Dashboard for emulator status
   - Verify port 10000-10002 are not in use

2. **Files not being processed**
   - Check BlobIngestionService logs for errors
   - Verify files are in the `supplier-uploads` container
   - Ensure file hash is unique (not a duplicate)

3. **Migration errors**
   - Run migrations manually: `dotnet ef database update --project src/MyMarketManager.Data`
   - Check database connection in Aspire Dashboard

## References

- [Azure Blob Storage Documentation](https://learn.microsoft.com/en-us/azure/storage/blobs/)
- [.NET Aspire Azure Storage Component](https://learn.microsoft.com/en-us/dotnet/aspire/storage/azure-storage-blobs-component)
- [Azurite Storage Emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite)
- [Data Model Documentation](data-model.md)
- [Product Requirements Document](product-requirements.md)
