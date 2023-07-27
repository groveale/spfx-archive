using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace groveale
{
    public static class RehydrateFile
    {
        [FunctionName("RehydrateFile")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            // var authHeader = req.Headers["Authorization"];
            // if (!authHeader.Any() || !authHeader[0].StartsWith("Bearer "))
            // {
            //     return new UnauthorizedResult();
            // }

            // Get query parameters for spItemUrl (contains the driveID and FileID)
            string spItemUrl = req.Query["spItemUrl"];
            string fileLeafRef = req.Query["fileLeafRef"];
            string serverRelativeUrl = req.Query["serverRelativeUrl"];
            string siteUrl = req.Query["siteUrl"];
            string fileRelativeUrl = req.Query["fileRelativeUrl"];
            string archiveVersions = req.Query["archiveVersions"];
            string archiveVersionCount = req.Query["archiveVersionCount"];
            string archiveMethod = req.Query["archiveMethod"];
            string archiveUserEmail = req.Query["archiveUserEmail"];
            string associatedLabel = req.Query["associatedLabel"];

            // Read request body and deserialize it
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Use query parameters or request body data for spItemUrl
            spItemUrl = spItemUrl ?? data?.spItemUrl;
            fileLeafRef = fileLeafRef ?? data?.fileLeafRef;
            serverRelativeUrl = serverRelativeUrl ?? data?.serverRelativeUrl;

            // SPO data
            siteUrl = siteUrl ?? data?.siteUrl;
            fileRelativeUrl = fileRelativeUrl ?? data?.fileRelativeUrl;

            // Archive data
            archiveVersions = archiveVersions ?? data?.archiveVersions;
            archiveVersionCount = archiveVersionCount ?? data?.archiveVersionCount;

            // Log data
            archiveMethod = archiveMethod ?? data?.archiveMethod;
            archiveUserEmail = archiveUserEmail ?? data?.archiveUserEmail;

            // Extract the accessToken
            //var accessToken = authHeader[0].Substring("Bearer ".Length);

            try
            {

                // Load settings and initialize GraphHelper with app only auth
                // Method also extracts the required MSGraph data from the spItemURL
                var settings = Settings.LoadSettings();
                GraphHelper.InitializeGraphForAppOnlyAuth(settings, spItemUrl);

                var SPOAuthHelper = new SPOAuthHelper(siteUrl);
                var clientContext = await SPOAuthHelper.Init();

                var readOnlyMetadata = SPOFileHelper.GetReadOnlyMetaDataSPO(clientContext, fileRelativeUrl);

                // Get file content from Blob storage and create in SPO
                var containerClient = await AzureBlobHelper.CreateContainerAsync(serverRelativeUrl, settings.StorageConnectionString);
                var blobName = $"{GraphHelper._driveId}-{GraphHelper._itemId}";
                var orderedblobStreams = await AzureBlobHelper.DownloadBlobContentToSteam(containerClient, blobName);

                // Get metadata from stub (url) apply metadata to SPO item
                var columnsToRetrieve = await GraphHelper.GetListColumns();
                var metaData = await GraphHelper.GetItemMetadata(columnsToRetrieve);
                var spoFile = await GraphHelper.CreateItem(metaData, fileLeafRef, stub: false);

                
                // Will need to update metadata for each version otherwise dates won't match up
                // Check if required
                // todo creates a few too many versions
                long bytesGained = 0;
                foreach(var versionStream in orderedblobStreams)
                {
                    // Upload content from blob stream to SPO Item
                    bytesGained += await GraphHelper.UploadContentFromBlob(versionStream, spoFile.Id);
                    // Need to update the metadata post upload. Otherwise modified times get overwritten
                    await GraphHelper.UpdateMetadata(metaData, spoFile.Id);
                    // Strip off the _archive.txt to get the original file name
                    SPOFileHelper.UpdateReadOnlyMetaData(clientContext, $"{fileRelativeUrl.Substring(0, fileRelativeUrl.Length - 12)}", readOnlyMetadata);
                }
                var blobUri = $"{containerClient.Uri}/{blobName}";

                // Delete Stub, delete blob
                await GraphHelper.DeleteItem(false);
                await AzureBlobHelper.DeleteBlob(containerClient, blobName);

                // Log details in SPOList
                var success = await SPOLogHelper.LogArchiveDetails(settings, spItemUrl, archiveMethod, spoFile.WebUrl, -bytesGained, orderedblobStreams.Count, archiveUserEmail, siteUrl, blobUri, "Restore");

                // Return the active files count in response
                return new OkObjectResult("Yay");
            }
            catch (Exception ex)
            {
                // Return error in response
                return new BadRequestObjectResult($"Error in request: {ex.Message}");
            }
      
        }
    }
}
