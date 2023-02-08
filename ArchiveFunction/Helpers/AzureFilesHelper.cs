using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Sas;

namespace groveale
{
    //-------------------------------------------------
    // UNUSED BUT KEPT FOR REFERENCE
    //-------------------------------------------------
    public class AzureFilesHelper 
    {
        //-------------------------------------------------
        // Create/Get a file share  (SPOSite Level)
        //-------------------------------------------------
        public async Task<ShareClient> CreateGetShareAsync(string shareName)
        {
            // Get the connection string from app settings
            string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];

            // Instantiate a ShareClient which will be used to create and manipulate the file share
            ShareClient share = new ShareClient(connectionString, shareName);

            // Create the share if it doesn't already exist
            await share.CreateIfNotExistsAsync();

            // Ensure that the share exists
            if (await share.ExistsAsync())
            {
                return share;
            }
            return null;
        }


        //-------------------------------------------------
        // Create/Get dir structure  (SubSite / Library / Folders)
        //-------------------------------------------------
        public async Task<ShareDirectoryClient> CreateGetDirPath(ShareClient share, string path)
        {
            // Get a reference to the sample directory
            ShareDirectoryClient directory = share.GetDirectoryClient(path);

            // Create the directory if it doesn't already exist
            await directory.CreateIfNotExistsAsync();

            // Ensure that the directory exists
            if (await directory.ExistsAsync())
            {
                return directory;
            }
            return null;
        }

        //-------------------------------------------------
        // Create/Get File (Items)
        //-------------------------------------------------
        public async Task GetFile(ShareDirectoryClient directory, string fileName)
        {
            // Get a reference to a file object
            ShareFileClient file = directory.GetFileClient(fileName);

            // Ensure that the file exists
            if (await file.ExistsAsync())
            {
                Console.WriteLine($"File exists: {file.Name}");

                // Download the file
                ShareFileDownloadInfo download = await file.DownloadAsync();

                // Save the data to a local file, overwrite if the file already exists
                using (FileStream stream = File.OpenWrite(@"downloadedLog1.txt"))
                {
                    await download.Content.CopyToAsync(stream);
                    await stream.FlushAsync();
                    stream.Close();

                    // Display where the file was saved
                    Console.WriteLine($"File downloaded: {stream.Name}");
                }
            }
        }
    

        //-------------------------------------------------
        // Create a SAS URI for a file
        //-------------------------------------------------
        public Uri GetFileSasUri(string shareName, string filePath, DateTime expiration, ShareFileSasPermissions permissions)
        {
            // Get the account details from app settings
            string accountName = ConfigurationManager.AppSettings["StorageAccountName"];
            string accountKey = ConfigurationManager.AppSettings["StorageAccountKey"];

            ShareSasBuilder fileSAS = new ShareSasBuilder()
            {
                ShareName = shareName,
                FilePath = filePath,

                // Specify an Azure file resource
                Resource = "f",

                // Expires in 24 hours
                ExpiresOn = expiration
            };

            // Set the permissions for the SAS
            fileSAS.SetPermissions(permissions);

            // Create a SharedKeyCredential that we can use to sign the SAS token
            StorageSharedKeyCredential credential = new StorageSharedKeyCredential(accountName, accountKey);

            // Build a SAS URI
            UriBuilder fileSasUri = new UriBuilder($"https://{accountName}.file.core.windows.net/{fileSAS.ShareName}/{fileSAS.FilePath}");
            fileSasUri.Query = fileSAS.ToSasQueryParameters(credential).ToString();

            // Return the URI
            return fileSasUri.Uri;
        }
    }
}