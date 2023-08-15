using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;

namespace groveale
{
    public class SPOFileHelper
    {

        public static void UpdateModifiedDateTime(ClientContext clientContext, string serverRelativeUrl, string modifiedDateTime)
        {
            var file = clientContext.Web.GetFileByServerRelativeUrl(serverRelativeUrl);
            file.ListItemAllFields["Modified"] = modifiedDateTime;
            file.ListItemAllFields.SystemUpdate();
            clientContext.ExecuteQuery();
        }

        public static void UpdateMetaData(ClientContext clientContext, string serverRelativeUrl, Dictionary<string, object> allMetadata)
        {

            //var item = clientContext.Web.GetListItem(serverRelativeUrl);
            var file = clientContext.Web.GetFileByServerRelativeUrl(serverRelativeUrl);

            foreach(var metaData in allMetadata)
            {
                file.ListItemAllFields[metaData.Key] = metaData.Value;
            }

            // Using UpdateOverWriteVersion as it appears to enable you to update the metadata without incrementing the version number
            file.ListItemAllFields.UpdateOverwriteVersion();
            clientContext.ExecuteQuery();

        }

        // All metadata should be SPO really
        public static Dictionary<string, object> GetMetaDataSPO(ClientContext clientContext, string serverRelativeUrl, List<string> columnsToGet)
        {
            var metaData = new Dictionary<string, object>();
            var propsToGet = new List<string> { "Author" , "Created", "Modified", "Editor", "Modified_x0020_By", "Created_x0020_By" };

            propsToGet.AddRange(columnsToGet);

            var file = clientContext.Web.GetFileByServerRelativeUrl(serverRelativeUrl);

            clientContext.Load(file);
            clientContext.Load(file.ListItemAllFields);
            clientContext.ExecuteQuery();

            foreach(var field in file.ListItemAllFields.FieldValues)
            {
                if (propsToGet.Contains(field.Key))
                {
                    metaData.Add(field.Key, field.Value);
                    continue;
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

        public static Dictionary<string, object> GetNonReadOnlyMetaDataSPO(ClientContext clientContext, string serverRelativeUrl)
        {
            var metaData = new Dictionary<string, object>();

            var file = clientContext.Web.GetFileByServerRelativeUrl(serverRelativeUrl);

            clientContext.Load(file);
            clientContext.Load(file, l => l.ListId);
            clientContext.Load(file.ListItemAllFields);
            clientContext.ExecuteQuery();

            var list = clientContext.Web.GetListById(file.ListId);
            clientContext.Load(list.Fields);
            clientContext.ExecuteQuery();

            foreach(var field in list.Fields)
            {
                try 
                {
                    if (field.ReadOnlyField == false)
                    {
                        metaData.Add(field.InternalName, file.ListItemAllFields.FieldValues[field.InternalName]);
                    }
                }
                catch
                {
                    // Ignore
                    System.Console.WriteLine($"Error getting metadata for field: {field.InternalName}");
                }
                
            }

            return metaData;
        }
    }
}