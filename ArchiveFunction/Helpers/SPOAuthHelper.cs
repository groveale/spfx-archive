using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using PnP.Framework;

namespace groveale
{
    public class SPOAuthHelper
    {

        public string siteUrl {get;set;}
        public ClientContext clientContext {get;set;}

        public SPOAuthHelper(string siteUrl)
        {
            this.siteUrl = siteUrl;
        }

        public async Task<ClientContext> Init()
        {
            //string clientId = "4d3e3609-0313-4bf8-8b07-17d228f98808"; //e.g. 01e54f9a-81bc-4dee-b15d-e661ae13f382
            string clientId = Environment.GetEnvironmentVariable("clientId");
            string clientSecret = Environment.GetEnvironmentVariable("clientSecret");
            

            //string certThumprint = "158E6A5066973CA9F6AE580B783967B1EFCC56C8"; // e.g. CE20E000D53A4C968ED8BA3EFC92C40A2692AE98
            string certThumprint = Environment.GetEnvironmentVariable("thumbprint");

            //For SharePoint app only auth, the scope will be the SharePoint tenant name followed by /.default
            string sharepointDomain = Environment.GetEnvironmentVariable("sharepointDomain");
            var scopes = new string[] { $"https://{sharepointDomain}/.default" };


            //Tenant id can be the tenant domain or it can also be the GUID found in Azure AD properties.
            //string tenantId = "groverale.onmicrosoft.com";
            string tenantId = Environment.GetEnvironmentVariable("tenantId");

            // use old ACS method
            //this.clientContext = new AuthenticationManager()
               // .GetACSAppOnlyContext(this.siteUrl, clientId, clientSecret);

            // This works - can connect to SPO
            this.clientContext = new PnP.Framework.AuthenticationManager(clientId, GetAppCertificate(certThumprint), tenantId, null, PnP.Framework.AzureEnvironment.Production, null).GetContext($"{this.siteUrl}");
    
    

            // var accessToken = await GetApplicationAuthenticatedClient(clientId, certThumprint, scopes, tenantId);

            // this.clientContext = GetClientContextWithAccessToken(this.siteUrl, accessToken);
            return this.clientContext;
        }

        private X509Certificate2 GetAppCertificate(string certThumprint)
        {
            X509Certificate2 certificate = null;

            if (Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
            {
                certificate = GetAppOnlyCertificate(certThumprint);
            }
            else
            {
                string keyVaultName = Environment.GetEnvironmentVariable("keyVaultName");
                string certNameKV = Environment.GetEnvironmentVariable("certNameKV");
                certificate = GetCertificateFromKV(certNameKV, keyVaultName);
            }

            return certificate;
        }

        private async Task<string> GetApplicationAuthenticatedClient(string clientId, string certThumprint, string[] scopes, string tenantId)
        {
            X509Certificate2 certificate = null;

            if (Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
            {
                certificate = GetAppOnlyCertificate(certThumprint);
            }
            else
            {
                string keyVaultName = Environment.GetEnvironmentVariable("keyVaultName");
                string certNameKV = Environment.GetEnvironmentVariable("certNameKV");
                certificate = GetCertificateFromKV(certNameKV, keyVaultName);
            }

            IConfidentialClientApplication clientApp = ConfidentialClientApplicationBuilder
                                            .Create(clientId)
                                            //.WithClientSecret(certThumprint)
                                            .WithCertificate(certificate)
                                            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
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

        private X509Certificate2 GetCertificateFromKV(string certName, string keyVaultName)
        {
            string secretName = certName; // Name of the certificate created before

            Uri keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");

            var client = new SecretClient(keyVaultUri, new DefaultAzureCredential());
            KeyVaultSecret secret = client.GetSecret(secretName);

            return new X509Certificate2(Convert.FromBase64String(secret.Value), string.Empty, X509KeyStorageFlags.MachineKeySet);
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