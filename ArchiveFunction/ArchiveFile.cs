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
    public static class ArchiveFile
    {
        [FunctionName("ArchiveFile")]
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

            // Only populated from label webhook
            string siteId = req.Query["siteId"];
            string listId = req.Query["listId"];
            string itemId = req.Query["itemId"];
            string folderPath = req.Query["folderPath"];

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

            // From label webhook
            siteId = siteId ?? data?.siteId;
            listId = listId ?? data?.listId;
            itemId = itemId ?? data?.itemId;
            folderPath = folderPath ?? data?.folderPath;


            try
            {
                // Load settings and initialize GraphHelper with app only auth
                // Method also extracts the required MSGraph data from the spItemURL
                var settings = Settings.LoadSettings();
                
                GraphHelper.InitializeGraphForAppOnlyAuth(settings, spItemUrl);
                
                // spItemUrl, serverRelativeUrl and  fileRelativeUrl are not populated from automated Label webhook
                if (archiveMethod == "Label" )
                {
                    // Populate driveId and ItemId using SPO data
                    await GraphHelper.PopulateDriveAndItemIdFromSPO(siteId, listId, itemId);
                    serverRelativeUrl = SPOFileHelper.GetServerRelativeUrlFromSiteUrl(siteUrl);
                    fileRelativeUrl = $"{serverRelativeUrl}/{folderPath}/{fileLeafRef}";
                }

                // spItemUrl is not populated from admin PowerShell script
                if (archiveMethod == "Admin" )
                {
                    // Populate driveId and ItemId using SPO data
                    await GraphHelper.PopulateDriveAndItemIdFromSPO(siteId, listId, itemId);
                }

                var SPOAuthHelper = new SPOAuthHelper(siteUrl);
                var clientContext = await SPOAuthHelper.Init();

                // Get the site name to prove we have SPO auth
                clientContext.Load(clientContext.Web, w => w.Title);
                clientContext.ExecuteQuery();
                Console.WriteLine($"Site name: {clientContext.Web.Title}");

                

                // Get metadata content and create stub in SPO (.url)
                var columnsToRetrieve = await GraphHelper.GetListColumns();
                // This is not used
                var metaData = await GraphHelper.GetItemMetadata(columnsToRetrieve);

                var spoMetadata = SPOFileHelper.GetMetaDataSPO(clientContext, fileRelativeUrl, columnsToRetrieve);

                var stub = await GraphHelper.CreateItem(metaData, fileLeafRef, stub: true);
                await GraphHelper.UpdateStubContent(stub.Id);

                // This icrements the version number (not good)
                //await GraphHelper.UpdateMetadata(metaData, stub.Id);

                var newFileRelative = $"{fileRelativeUrl}_archive.txt";
                if (stub.Name != $"{fileLeafRef}_archive.txt")
                {
                    // We have had a conflict so needed to rename the file so need a new file relative url
                    string[] parts = newFileRelative.Split('/');
                    parts[^1] = stub.Name;
                    newFileRelative = string.Join('/', parts);
                }


                SPOFileHelper.UpdateMetaData(clientContext, newFileRelative, spoMetadata);

                // Get file content and create in Azure blob (using stub file id)
                var containerClient = await AzureBlobHelper.CreateContainerAsync(serverRelativeUrl, settings.StorageConnectionString);
                var listOfStreams = await GraphHelper.GetFileStreamContent(archiveVersions, archiveVersionCount);

                var blobName = $"{GraphHelper._driveId}-{GraphHelper._stubId}";
                
                await AzureBlobHelper.UploadStream(containerClient, blobName, listOfStreams);

                // Build Blob URI
                // https://azureachivegen2.blob.core.windows.net/sites-archivedev/b!WfqaZ0NAkUeFmlS3n6LyFylhyrNUJcxOhCZ5iI92GLE769AhxyQiRr731FN_EAJo-01WYN5PQKX572V653T7RCJXQKOV43UCEDM
                var blobUri = $"{containerClient.Uri}/{blobName}";

                // Delete file in SPO
                var bytesSaved = await GraphHelper.DeleteItem(getSizeSaved: true);

                // Log details in SPOList
                var success = await SPOLogHelper.LogArchiveDetails(settings, spItemUrl, archiveMethod, stub.WebUrl, bytesSaved, listOfStreams.Count, archiveUserEmail, siteUrl, blobUri, "Archive");

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
