using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace groveale
{
    public class AzureBlobHelper 
    {

        //-------------------------------------------------
        // Create a container (SPO Site)
        // Container name is driveid
        //-------------------------------------------------
        public static async Task<BlobContainerClient> CreateContainerAsync(string containerName)
        {
            // Get the connection string from app settings
            string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];

            // Using SiteUrl for uniqueness
            // The container name must be lowercase.
            containerName = containerName.ToLowerInvariant();

            try
            {
                // Get the container
                BlobContainerClient container = new BlobContainerClient(connectionString, containerName);
                container.CreateIfNotExists(PublicAccessType.Blob);

                if (await container.ExistsAsync())
                {
                    Console.WriteLine("Created container {0}", container.Name);
                    return container;
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("HTTP error code {0}: {1}", e.Status, e.ErrorCode);
                Console.WriteLine(e.Message);
            }

            return null;
        }
    
    
        //-------------------------------------------------
        // Create a blob (File stream)
        // blobName is filedriveid
        //-------------------------------------------------
        public static async Task UploadStream(BlobContainerClient containerClient, string blobName, Stream fileStreamFromSPO)
        {
            if (!await containerClient.ExistsAsync())
            {
                Console.WriteLine("No Container");
                return;
            }

             // Get a reference to the blob you want to upload the file to
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            
            // Upload the stream
            await blobClient.UploadAsync(fileStreamFromSPO, true);
        }

        //-------------------------------------------------
        // Get a blob (stream)
        // blobName is filedriveid
        //-------------------------------------------------
        public static async Task DownloadBlobContentToSteam(BlobContainerClient containerClient, string blobName, Stream spoStream)
        {
            if (!await containerClient.ExistsAsync())
            {
                Console.WriteLine("No Container");
                return;
            }

             // Get a reference to the blob you want to download the content from
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            
            // Download to the spo stream
            await blobClient.DownloadToAsync(spoStream);
        }

        //-------------------------------------------------
        // Delete a blob (UnArchive)
        //-------------------------------------------------
        public static async Task<bool> DeleteBlob(BlobContainerClient containerClient, string blobName)
        {
            if (!await containerClient.ExistsAsync())
            {
                Console.WriteLine("No Container");
                return false;
            }

             // Get a reference to the blob you want to delete
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            
            // Upload the stream
            return await blobClient.DeleteIfExistsAsync();
        }
    }
}