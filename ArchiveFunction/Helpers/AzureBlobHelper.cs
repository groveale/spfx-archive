using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public static async Task<BlobContainerClient> CreateContainerAsync(string containerName, string connectionString)
        {

            // Using SiteUrl for uniqueness
            // The container name must be lowercase and can only contain hypernymns.
            if(containerName[0] == '/') 
            {
                containerName = containerName.Remove(0, 1);
            }
            containerName = containerName.Replace('/','-').ToLowerInvariant();

            // As we have Psychopaths that put punctuation in URLs we need to remove those to
            containerName = StripNonCompliantCharacters(containerName);

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

        public static string StripNonCompliantCharacters(string input)
        {
            // Use a regular expression to remove all non-compliant characters
            return Regex.Replace(input, @"[^a-z\-]", "");
        }
    
        
        public static async Task SetBlobMetadataAsync(BlobClient blob,  IDictionary<string, string> metadata)
        {
            Console.WriteLine("Setting blob metadata...");

            try
            {
                // Set the blob's metadata.
                await blob.SetMetadataAsync(metadata);
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"HTTP error code {e.Status}: {e.ErrorCode}");
                Console.WriteLine(e.Message);
            }
        }

    
        //-------------------------------------------------
        // Create a blob (File stream)
        // blobName is filedriveid
        //-------------------------------------------------
        public static async Task UploadStream(BlobContainerClient containerClient, string blobName, List<Stream> fileStreamsFromSPO)
        {
            if (!await containerClient.ExistsAsync())
            {
                Console.WriteLine("No Container");
                return;
            }

            // reorder the list as we have active first, need active last
            fileStreamsFromSPO.Reverse();

             // Get a reference to the blob you want to upload the file to
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            for(int i = 0; i < fileStreamsFromSPO.Count; i++)
            {

                // ** No longer need as if using versions this happens automatically **
                // Create a snapshot if we have multiple versions
                // Default we will have 1 version, so no snapshots
                // if(i > 0)
                // {
                //     // Create another snapshot of the blob
                //     BlobSnapshotInfo anotherSnapshot = await blobClient.CreateSnapshotAsync();
                // }

                // Upload the stream
                await blobClient.UploadAsync(fileStreamsFromSPO[i], true);
            }
        }

        //-------------------------------------------------
        // Get a blob (stream)
        // blobName is filedriveid
        //-------------------------------------------------
        public static async Task<List<Stream>> DownloadBlobContentToSteam(BlobContainerClient containerClient, string blobName)
        {
            if (!await containerClient.ExistsAsync())
            {
                Console.WriteLine("No Container");
                return null;
            }

            // Get a reference to the blob you want to download the content from
            //BlobClient blobClient = containerClient.GetBlobClient(blobName);

            // Blob Streams
            List<Stream> blobStreams = new List<Stream>();

            // This will give us the newest version first
            var blobVersions = containerClient.GetBlobs
                    (BlobTraits.None, BlobStates.Version, prefix: blobName)
                    .OrderBy(version => version.VersionId).Where(blob => blob.Name == blobName);

            foreach (BlobItem blobItem in blobVersions)
            {
                BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name).WithVersion(blobItem.VersionId);
                
                // Open a stream to the content
                var blobStream = await blobClient.OpenReadAsync();
                blobStreams.Add(blobStream);
            }
            
        
            return blobStreams;
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
            //BlobClient blobClient = containerClient.GetBlobClient(blobName);

            try 
            {
                // List blobs in this container that match prefix. (also get verions)
                Pageable<BlobItem> blobItems = containerClient.GetBlobs
                                (BlobTraits.None, BlobStates.Version, prefix: blobName);
            
                // Delete all blobs
                foreach (BlobItem blobItem in blobItems)
                {
                    // Blob versions are distinguished by their unique version identifier.
                    BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name).WithVersion(blobItem.VersionId);;
                    await blobClient.DeleteIfExistsAsync();
                }
                
                // Upload the stream
                return true;
            }
            catch
            {
                // Error as occured whilst deleting
                return false;
            }
            
        }
    
        //-------------------------------------------------
        // Get all containers, get all blobs in hot tier with last modified date older than 30 days
        // Move to cool tier
        //-------------------------------------------------
        public static async Task MoveBlobsToCoolTier(string connectionString) 
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            // Get all containers
            await foreach (BlobContainerItem container in blobServiceClient.GetBlobContainersAsync())
            {
                // Get client for container
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(container.Name);

                BlobTraits blobTraits = BlobTraits.None;
                BlobStates blobStates = BlobStates.None;

                AsyncPageable<BlobItem> blobs = containerClient.GetBlobsAsync(blobTraits, blobStates);
                await foreach (BlobItem blob in blobs)
                {
                    BlobClient blobClient = containerClient.GetBlobClient(blob.Name);
                    BlobProperties properties = await blobClient.GetPropertiesAsync();
                    AccessTier accessTier = properties.AccessTier;

                    // Files stay in hot tier for 30 days
                    if (accessTier == AccessTier.Hot)
                    {
                        string blobName = blob.Name;
                        Console.WriteLine(blobName);

                        // Check last modified date
                        if (properties.LastModified < DateTime.Now.AddDays(-30))
                        {
                            // Move to cool tier
                            blobClient.SetAccessTier(AccessTier.Cool);
                            continue;
                        }
                    }

                    // Files stay in cool tier for 180 days
                    if (accessTier == AccessTier.Cool)
                    {
                        string blobName = blob.Name;
                        Console.WriteLine(blobName);

                        // Check last modified date
                        if (properties.LastModified < DateTime.Now.AddDays(-180))
                        {
                            // Move to cool tier
                            blobClient.SetAccessTier(AccessTier.Archive);
                            continue;
                        }
                    }
                }
            }            
        }
    }        
}