using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;

namespace groveale
{
    public class SPOFileHelper
    {

        public static void UpdateReadOnlyMetaData(ClientContext clientContext, string serverRelativeUrl, Dictionary<string, object> readOnlyMetadata)
        {

            var file = clientContext.Web.GetFileByServerRelativeUrl(serverRelativeUrl);

            foreach(var metaData in readOnlyMetadata)
            {
                file.ListItemAllFields[metaData.Key] = metaData.Value;
            }

            // System update is a red herring
            //file.ListItemAllFields.SystemUpdate();
            file.ListItemAllFields.Update();

            clientContext.ExecuteQuery();
        }

        // All metadata should be SPO really
        public static Dictionary<string, object> GetReadOnlyMetaDataSPO(ClientContext clientContext, string serverRelativeUrl)
        {
            var metaData = new Dictionary<string, object>();
            var propsToGet = new List<string> { "Author" , "Created", "Modified", "Editor", "Modified_x0020_By", "Created_x0020_By" };

            var file = clientContext.Web.GetFileByServerRelativeUrl(serverRelativeUrl);
            clientContext.Load(file);
            clientContext.Load(file.ListItemAllFields);
            clientContext.ExecuteQuery();

            foreach(var field in file.ListItemAllFields.FieldValues)
            {
                if (propsToGet.Contains(field.Key))
                {
                    metaData.Add(field.Key, field.Value);
                }
                
            }

            return metaData;
        }
    
        public static string GetServerRelativeUrlFromSiteUrl(string siteUrl)
        {
            // Split the url
            var splitUrl = siteUrl.Split("/");

            // Use array slicing to get the remaining items after the first 3 (hostname)
            string[] serverRelativeElements = splitUrl[3..];

            // Return the joined array with a leading slash
            return $"/{string.Join("/", serverRelativeElements)}";
        }
    }
}