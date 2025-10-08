# Blob Storage Ingestion Pipeline

## Overview

The blob storage ingestion pipeline enables automated processing of supplier data files uploaded to Azure Blob Storage. Users upload files through a web UI, which creates staging batches that are then processed by a background service. This feature is designed to handle password-protected ZIP files (starting with Shein "Request My Data" exports) and create staging batches for validation and promotion into production data.

## Architecture

### Components

1. **BlobStorageService** (`Services/BlobStorageService.cs`)
   - Manages blob storage operations
   - Provides upload, download, and list functionality
   - Computes file hashes before upload for deduplication
   - Ensures the `supplier-uploads` container exists

2. **BlobIngestionService** (`Services/BlobIngestionService.cs`)
   - Background service that processes pending staging batches every 5 minutes
   - Downloads files from blob storage
   - Processes data and marks batches as complete
   - Handles errors gracefully with partial status

3. **UploadSupplierData Page** (`Components/Pages/UploadSupplierData.razor`)
   - Web UI for uploading supplier data files
   - Validates file type and size (100 MB limit)
   - Computes file hash before upload to detect duplicates
   - Creates StagingBatch record immediately upon upload

4. **StagingBatch Entity** (`Data/Entities/StagingBatch.cs`)
   - Extended with `BlobStorageUrl` property
   - Extended with `BatchType` property (SupplierData or SalesData)
   - Tracks the Azure Blob Storage location of uploaded files
   - Status tracks processing state (Pending → Complete)

## Workflow

### Upload Process

1. **User Upload**: Users upload supplier data files (ZIP format) via the `/upload-supplier-data` page
2. **Duplicate Detection**: System computes SHA-256 hash and checks if file already exists
3. **Blob Storage**: If not duplicate, file is uploaded to Azure Blob Storage in the `supplier-uploads` container
4. **Batch Creation**: StagingBatch record is created immediately with `Status = Pending` and `BatchType = SupplierData`
5. **Background Processing**: BlobIngestionService processes pending batches every 5 minutes

### Processing Pipeline

```
┌─────────────────┐
│  User Uploads   │
│   ZIP File      │
│  (via Web UI)   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Check for       │
│ Duplicates      │
│ (SHA-256 Hash)  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Upload to       │
│ Azure Blob      │
│ Storage         │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Create          │
│ StagingBatch    │
│ Status: Pending │
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
│ Process File    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Mark Batch      │
│ Status:Complete │
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
4. Navigate to `/upload-supplier-data` to upload files
5. Background service will process files every 5 minutes

### Using the Upload UI

1. Navigate to `/upload-supplier-data` in your browser
2. Select a supplier from the dropdown
3. Choose a ZIP file to upload (max 100 MB)
4. The system will compute the file hash and check for duplicates
5. If the file is not a duplicate, click "Upload"
6. A staging batch will be created with status "Pending"
7. The background service will process it within 5 minutes

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

Files are deduplicated using SHA-256 file hashing:

1. **Before Upload**: File hash is computed on the client side before upload
2. **Database Check**: System checks if a StagingBatch with the same FileHash already exists
3. **Prevention**: If duplicate is found, upload is blocked and user is notified
4. **Storage Savings**: Prevents duplicate files from being stored in blob storage

This approach saves both storage space and processing time by detecting duplicates before any upload occurs.

## Future Enhancements

### Planned Features

1. ✅ **Upload UI Component**: Implemented - Blazor page at `/upload-supplier-data`
2. **ZIP Extraction**: Extract and parse password-protected ZIP files (Shein format)
3. **Parser Registry**: Configurable parsers for different supplier formats
4. **Progress Tracking**: Real-time status updates during processing
5. **Batch Status Page**: View all batches and their processing status
6. **Error Handling**: Enhanced retry logic and dead-letter queue for failed uploads
7. **Webhook Support**: Trigger processing on blob upload events instead of polling
8. **Cleanup Service**: Archive or delete processed blobs after retention period

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
