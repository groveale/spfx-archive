using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;

namespace groveale
{
    public class AuthHelper
    {

        public string siteUrl {get;set;}
        public ClientContext clientContext {get;set;}

        public AuthHelper(string siteUrl)
        {
            this.siteUrl = siteUrl;
        }

        public async Task<ClientContext> Init()
        {
            string clientId = "4d3e3609-0313-4bf8-8b07-17d228f98808"; //e.g. 01e54f9a-81bc-4dee-b15d-e661ae13f382

            string certThumprint = "158E6A5066973CA9F6AE580B783967B1EFCC56C8"; // e.g. CE20E000D53A4C968ED8BA3EFC92C40A2692AE98

            //For SharePoint app only auth, the scope will be the SharePoint tenant name followed by /.default
            var scopes = new string[] { "https://groverale.sharepoint.com/.default" };

            //Tenant id can be the tenant domain or it can also be the GUID found in Azure AD properties.
            string tenantId = "groverale.onmicrosoft.com";

            var accessToken = await GetApplicationAuthenticatedClient(clientId, certThumprint, scopes, tenantId);

            this.clientContext = GetClientContextWithAccessToken(this.siteUrl, accessToken);
            return this.clientContext;
        }

        private async Task<string> GetApplicationAuthenticatedClient(string clientId, string certThumprint, string[] scopes, string tenantId)
        {
            X509Certificate2 certificate = GetAppOnlyCertificate(certThumprint);
            IConfidentialClientApplication clientApp = ConfidentialClientApplicationBuilder
                                            .Create(clientId)
                                            .WithCertificate(certificate)
                                            .WithTenantId(tenantId)
                                            .Build();

            AuthenticationResult authResult = await clientApp.AcquireTokenForClient(scopes).ExecuteAsync();
            string accessToken = authResult.AccessToken;
            return accessToken;
        }

        private ClientContext GetClientContextWithAccessToken(string targetUrl, string accessToken)
        {
            ClientContext clientContext = new ClientContext(targetUrl);
            clientContext.ExecutingWebRequest +=
                delegate (object oSender, WebRequestEventArgs webRequestEventArgs)
                {
                    webRequestEventArgs.WebRequestExecutor.RequestHeaders["Authorization"] =
                        "Bearer " + accessToken;
                };
            return clientContext;
        }


        private X509Certificate2 GetAppOnlyCertificate(string thumbPrint)
        {
            X509Certificate2 appOnlyCertificate = null;
            using (X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                certStore.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, thumbPrint, false);
                if (certCollection.Count > 0)
                {
                    appOnlyCertificate = certCollection[0];
                }
                certStore.Close();
                return appOnlyCertificate;
            }
        }
    }
}