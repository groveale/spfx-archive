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

            var authHeader = req.Headers["Authorization"];
            if (!authHeader.Any() || !authHeader[0].StartsWith("Bearer "))
            {
                return new UnauthorizedResult();
            }

            // Get query parameters for spItemUrl (contains the driveID and FileID)
            string spItemUrl = req.Query["spItemUrl"];
            string fileLeafRef = req.Query["fileLeafRef"];

            // Read request body and deserialize it
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Use query parameters or request body data for spItemUrl
            spItemUrl = spItemUrl ?? data?.spItemUrl;
            fileLeafRef = fileLeafRef ?? data?.fileLeafRef;

            // Extract the accessToken
            var accessToken = authHeader[0].Substring("Bearer ".Length);

            try
            {

                // Load settings and initialize GraphHelper with app only auth
                // Method also extracts the required MSGraph data from the spItemURL
                var settings = Settings.LoadSettings();
                GraphHelper.InitializeGraphForAppOnlyAuth(settings, spItemUrl);

                // Get file conetent from Blob storage and create in SPO
                //var blobContent = AzureBlobHelper.

                // Get metadata from stub (url) apply metadata to SPO item
                var metaData = await GraphHelper.GetItemMetadata();
                var spoFile = await GraphHelper.CreateItem(metaData, fileLeafRef, stub: false);


                // Delete Stub, delete blob
                await GraphHelper.DeleteItem();
                //await AzureBlobHelper.DeleteBlob();

                

                // Get metadata content and create stub in SPO (.url)
                
                var stub = await GraphHelper.CreateItem(metaData, fileLeafRef, stub: true);

                // Get file content and create in Azure blob (using stub file id)
                var containerClient = await AzureBlobHelper.CreateContainerAsync(GraphHelper._driveId);
                var stream = await GraphHelper.GetFileStreamContent();
                await AzureBlobHelper.UploadStream(containerClient, GraphHelper._stubId, stream);

                // Delete file in SPO
                await GraphHelper.DeleteItem();

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
