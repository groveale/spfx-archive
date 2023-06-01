using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace groveale
{
    public class MakeBlobsColder
    {
        [FunctionName("MakeBlobsColder")]
        public async Task Run([TimerTrigger("0 0 2 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            try
            {
                // Load settings 
                var settings = Settings.LoadSettings();

                // Cool blobs
                await AzureBlobHelper.MoveBlobsToCoolTier(settings.StorageConnectionString);
            }
            catch (Exception ex)
            {
                // Return error in response
                log.LogError(ex.Message);
            }
      
        }
    }
}
