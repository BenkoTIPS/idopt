using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class AzureBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobStorageService(string connectionString)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<BlobClient> GetBlobAsync(string containerName, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        return containerClient.GetBlobClient(blobName);
    }

    public async Task<IEnumerable<string>> GetVideoNamesAsync(string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
        var result = new List<string>();

        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            result.Add(blobItem.Name);
        }

        return result;
    }
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
        
        var blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(fileStream, overwrite: true);

        return blobClient.Uri.ToString();
    }
}
