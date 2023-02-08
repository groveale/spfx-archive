using Azure.Identity;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace groveale
{

    class GraphHelper
    {
        
        // Settings object
        private static Settings? _settings;
        // App-ony auth token credential
        private static ClientSecretCredential? _clientSecretCredential;
        // Client configured with app-only authentication
        private static GraphServiceClient? _appClient;


        public static string? _driveId {get;set;}
        private static string? _itemId {get;set;}
        private static string? _parentId {get;set;}
        public static string? _stubId {get;set;}


        // Application permission - Will look to provide an emxaple using delegated
        public static void InitializeGraphForAppOnlyAuth(Settings settings, string spItemURL)
        {
            _settings = settings;

            // Ensure settings isn't null
            _ = settings ??
                throw new System.NullReferenceException("Settings cannot be null");

            _settings = settings;

            if (_clientSecretCredential == null)
            {
                _clientSecretCredential = new ClientSecretCredential(
                    _settings.TenantId, _settings.ClientId, _settings.ClientSecret);
            }

            if (_appClient == null)
            {
                _appClient = new GraphServiceClient(_clientSecretCredential,
                    // Use the default scope, which will request the scopes
                    // configured on the app registration
                    new[] {"https://graph.microsoft.com/.default"});
            }

            // spItem
            ExtractDriveIdFromUrl(spItemURL);
            ExtractFileIdFromUrl(spItemURL);
        }

        
        public static string ExtractDriveIdFromUrl(string spItemURL)
        {
            // 'https://groverale.sharepoint.com:443/_api/v2.0/drives/b!WfqaZ0NAkUeFmlS3n6LyFylhyrNUJcxOhCZ5iI92GLG8_5OG5MO6SKwnP_g6cTD9/items/01WYN5PQOB4CTW3XNK7FFZJVLYDLIFHBQZ?version=Published'

            var split = spItemURL.Split('/');

            _driveId = split[6];

            return _driveId;
        }

        public static string ExtractFileIdFromUrl(string spItemURL)
        {
            // 'https://groverale.sharepoint.com:443/_api/v2.0/drives/b!WfqaZ0NAkUeFmlS3n6LyFylhyrNUJcxOhCZ5iI92GLG8_5OG5MO6SKwnP_g6cTD9/items/01WYN5PQOB4CTW3XNK7FFZJVLYDLIFHBQZ?version=Published'

            // 01WYN5PQOB4CTW3XNK7FFZJVLYDLIFHBQZ?version=Published
            var split = spItemURL.Split('/');
            _itemId = split[8].Split('?')[0];

            return _itemId;
        }
    

        public static async Task<Stream> GetFileStreamContent()
        {
            // Ensure client isn't null
            _ = _appClient ??
                throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

            var stream = await _appClient.Drives[_driveId].Items[_itemId].Content.Request().GetAsync();

            return stream;
        }

        public static async Task<IDictionary<string, object>> GetItemMetadata()
        {
            // Ensure client isn't null
            _ = _appClient ??
                throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

            var listItem = await _appClient.Drives[_driveId].Items[_itemId].ListItem.Request().GetAsync();

            // used for creating the stub / rehydrated file
            _parentId = listItem.ParentReference.Id;

            foreach(var field in listItem.AdditionalData)
            {
                Console.WriteLine($"{field.Key}: {field.Value}");
            }

            return listItem.AdditionalData;
        }

        public static async Task<DriveItem> CreateItem(IDictionary<string, object> metadata, string fileName, bool stub = true)
        {
            // Ensure client isn't null
            _ = _appClient ??
                throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

            // stubs are links
            if (stub)
            {
                fileName += ".url";
            }

            // May need to create item first and then apply metadata 
            var file = new DriveItem
            {
                Name = $"{fileName}",
                File = new Microsoft.Graph.File { },
                ListItem = new ListItem 
                {
                    AdditionalData = metadata
                }
            };

            var newFile = await _appClient.Drives[_driveId].Items[_parentId].Children
                .Request()
                .AddAsync(file);

            if (stub)
            {
                _stubId = newFile.Id;
            }

            return newFile;
        }

        // public static async Task UploadContentFromAzure()
        // {

        // }
        public static async Task DeleteItem()
        {
            // Ensure client isn't null
            _ = _appClient ??
                throw new System.NullReferenceException("Graph has not been initialized for app-only auth");


            await _appClient.Drives[_driveId].Items[_itemId].Request().DeleteAsync();
        
        }
    }
}