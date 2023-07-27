using System;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;

namespace groveale
{
    public static class SPOLogHelper
    {
        public static async Task<bool> LogArchiveDetails(Settings settings, string sourceUrl, string archiveMethod, string destinationUrl, long bytesSaved, int versionCountArchived, string archiveUserEmail, string siteUrl, string blobUri, string actionType)
        {
            try 
            {
                var SPOAuthHelper = new SPOAuthHelper(settings.ArchiveHubUrl);
                var clientContext = await SPOAuthHelper.Init();

                var list = clientContext.Web.Lists.GetByTitle(settings.ArchiveHubListName);

                var itemCreateInfo = new Microsoft.SharePoint.Client.ListItemCreationInformation();
                var listItem = list.AddItem(itemCreateInfo);

                // Set field values for the new item
                listItem["LogTime"] = DateTime.Now;
                listItem["SourceUrl"] = sourceUrl;
                listItem["ArchiveMethod"] = archiveMethod;
                listItem["DestinationUrl"] = destinationUrl;
                listItem["StorageSavedBytes"] = bytesSaved;
                listItem["VersionCountArchived"] = versionCountArchived;
                listItem["ArchiveUser"] = GetUserFieldValue(clientContext, archiveUserEmail);
                listItem["SiteUrl"] = siteUrl;
                listItem["ArchiveUrl"] = blobUri;
                listItem["ActionType"] = actionType;

                // Commit changes to SharePoint
                listItem.Update();
                clientContext.ExecuteQuery();

                Console.WriteLine("New list item created successfully!");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating new list item: {0}", ex.Message);
                return false;
            }
        }




        // Helper method to get a FieldUserValue object for a specific user login name
        private static FieldUserValue GetUserFieldValue(ClientContext context, string userLoginName)
        {
            // User user = context.Web.EnsureUser(userLoginName);
            // context.Load(user);
            // context.ExecuteQuery();

            return FieldUserValue.FromUser(userLoginName);
        }
        public static void LogRehydrateDetails(object archiveDetails)
        {
            
        }
    }
}