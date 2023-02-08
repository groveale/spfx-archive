using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;

namespace groveale
{
    public class SPOFileHelper
    {
        public void GetFileMetaData(ClientContext clientContext, string serverRelativeUrl)
        {
            var file = clientContext.Web.GetFileByServerRelativeUrl(serverRelativeUrl);
            var fields = file.ListItemAllFields;



            
        }

        public void GetFileStream(ClientContext clientContext, string serverRelativeUrl, File file)
        {
            
          
            
        }
    }

}