using System;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace groveale
{
    public class MakeBlobsColder
    {
        [FunctionName("MakeBlobsColder")]
        //public async Task Run([TimerTrigger("0 0 2 * * *")]TimerInfo myTimer, ILogger log)
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            try
            {
                // Load settings 
                var settings = Settings.LoadSettings();

                // Cool blobs
                await AzureBlobHelper.MoveBlobsToCoolTier(settings.StorageConnectionString);

                // Return the active files count in response
                return new OkObjectResult("Yay");
            }
            catch (Exception ex)
            {
                // Return error in response
                log.LogError(ex.Message);
                return new BadRequestObjectResult($"Error in request: {ex.Message}");
            }
      
        }
    }
}
