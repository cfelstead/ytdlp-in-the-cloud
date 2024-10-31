using Azure.Storage.Blobs;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Connectors.Storage.AzureBlob;

public class AzureBlobStorageService : IStorage
{
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly BlobContainerClient _blobContainerClient;
    private readonly string? _blobContainerName;
    
    public AzureBlobStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger
        )
    {
        _logger = logger;
        string? blobServiceConnectionString = configuration.GetValue<string>("AzureBlobStorageService:ConnectionString");
        ArgumentNullException.ThrowIfNull(blobServiceConnectionString);
        
        _blobContainerName = configuration.GetValue<string>("AzureBlobStorageService:ContainerName");
        ArgumentNullException.ThrowIfNull(_blobContainerName);
        
        
        
        BlobServiceClient blobServiceClient = new(blobServiceConnectionString);
        _blobContainerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);
    }
    
    public Task SaveFileToStorage(string filename, Stream fileContent)
    {
        var filenameWithoutPath = filename;
        if (filenameWithoutPath.Contains('/'))
        {
            filenameWithoutPath = filenameWithoutPath.Split('/').Last();
        }
        
        var blob = _blobContainerClient.GetBlobClient(filenameWithoutPath);
        // ReSharper disable once MethodHasAsyncOverload
        // Remarks in https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobcontainerclient.uploadblobasync?view=azure-dotnet
        // A RequestFailedException will be thrown if the blob already exists. To overwrite an existing block blob, get a BlobClient by calling GetBlobClient(String), and then call Upload(Stream, Boolean, CancellationToken) with the override parameter set to true.
        _logger.LogInformation("Uploading file {file} to Azure Blob Storage container {container}.", filenameWithoutPath, _blobContainerName);
        blob.Upload(fileContent, overwrite: true);
        return Task.CompletedTask;
    }
}