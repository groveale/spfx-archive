using Azure.Identity;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        public static string? _itemId {get;set;}
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
    

        public static async Task<List<Stream>> GetFileStreamContent(string archiveVersions, string archiveVersionCount)
        {
            // Ensure client isn't null
            _ = _appClient ??
                throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

            if (archiveVersions == "true")
            {
                // Get all versions of the file
                var versions = await _appClient.Drives[_driveId].Items[_itemId].Versions.Request().GetAsync();

                // Get the latest versions N versions
                var latestNVersions = versions.Take(Int32.Parse(archiveVersionCount)).ToList();

                var versionStreams = new List<Stream>();
                foreach (var version in latestNVersions)
                {
                    // Get the content stream
                    var stream = await _appClient.Drives[_driveId].Items[_itemId].Versions[version.Id].Content.Request().GetAsync();
                    versionStreams.Add(stream);
                }
                
                return versionStreams;
            }
            else
            {
                // Just get the file
                var stream = await _appClient.Drives[_driveId].Items[_itemId].Content.Request().GetAsync();

                // Only need one stream
                return new List<Stream>(){ stream };
            }
        }


        public static async Task<List<string>> GetListColumns()
        {
            // Ensure client isn't null
            _ = _appClient ??
                throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

            var columnsToRetrive = new List<String>();

            var coloumns = await _appClient.Drives[_driveId].List.Columns
                                .Request()
                                .GetAsync();

            foreach(var column in coloumns)
            {
                // If column is not read only then we need it
                if (!column.ReadOnly.Value)
                {
                    if (column.Name == "FileLeafRef")
                    {
                        continue;
                    }
                    columnsToRetrive.Add(column.Name);
                }
                else
                {
                    // we also need created data, creator, modified data and modifier
                    // May be a better way to get these
                    // Which are read only coloumns
                    // if (column.Name == "Created" || column.Name == "Modified" ||column.Name == "Editor" || column.Name == "Author")
                    // {
                    //     columnsToRetrive.Add(column.Name);
                    // }
                }
            }

            return columnsToRetrive;
        }

        public static async Task<IDictionary<string, object>> GetItemMetadata(List<string> columnsToRetrive)
        {
            // Ensure client isn't null
            _ = _appClient ??
                throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

            var listItem = await _appClient.Drives[_driveId].Items[_itemId].ListItem.Request().GetAsync();

            // used for creating the stub / rehydrated file
            _parentId = listItem.ParentReference.Id;

            // Will need to make a list of all Non-custom or default fields
            // Might work but would need to be kept uptodate when new features that require fields are released
            Dictionary<string, object> fieldValues = new Dictionary<string, object>();
            foreach(var field in listItem.Fields.AdditionalData)
            {
                if (columnsToRetrive.Contains(field.Key))
                {
                    Console.WriteLine($"{field.Key}: {field.Value}");
                    fieldValues.Add(field.Key, field.Value);
                }
            }

            return fieldValues;
        }

        public static async Task<DriveItem> CreateItem(IDictionary<string, object> metadata, string fileName, bool stub = true)
        {
            // Ensure client isn't null
            _ = _appClient ??
                throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

            // stubs are links
            if (stub)
            {
                fileName += "_archive.txt";
            } 
            else
            {
                // strip the .url
                if (fileName.EndsWith("_archive.txt"))
                {
                    fileName = fileName.Substring(0, fileName.Length - 12);
                }
            }

            // May need to create item first and then apply metadata 
            var file = new DriveItem
            {
                Name = $"{fileName}",
                File = new Microsoft.Graph.File { },
                // set content of file to "hello"
                Content = new MemoryStream(Encoding.UTF8.GetBytes(@$"This file is currently in the archive.{Environment.NewLine}
                                                                {Environment.NewLine}
                                                                Click the following link to learn how to Rehydrate
                                                                {Environment.NewLine}
                                                                {_settings.LinkToKB}")),
                // ListItem = new ListItem 
                // {
                //     AdditionalData = metadata
                // }
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

        public static async Task UpdateMetadata(IDictionary<string, object> metadata, string itemId)
        {
            // Ensure client isn't null
            _ = _appClient ??
                throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

            // Apply Metadata
            var fieldValueSet = new FieldValueSet
            {
                AdditionalData = metadata
            };

            var updatedFile = await _appClient.Drives[_driveId].Items[itemId].ListItem.Fields
                .Request()
                .UpdateAsync(fieldValueSet);
        }

        public static async Task UploadContentFromBlob(Stream blobStream, string newFileId)
        {

            // Create an upload session to add the contents of the file
            var uploadSession = await _appClient.Drives[_driveId].Items[newFileId]
                .CreateUploadSession(new DriveItemUploadableProperties())
                .Request().PostAsync();

            // Upload the contents of the file
            var chunkSize = 320 * 1024;
            var provider = new ChunkedUploadProvider(uploadSession, _appClient, blobStream, chunkSize);
            var item = await provider.UploadAsync();


            // using (var stream = new MemoryStream())
            // {
            //     await blobStream.CopyToAsync(stream);

            //     var contentsAsBytes = stream.ToArray();

            //     // Supports upto 4MB
            //     var request =  _appClient.Drives[_driveId].Items[newFileId].Content.Request();
            //     request.Headers.Add(new HeaderOption("Content-Type", "application/octet-stream"));
    
	        //     var newContentInItem = await request.PutAsync<DriveItem>(stream);

            //     // larger files requires upload session
            //     // https://learn.microsoft.com/en-us/graph/api/driveitem-createuploadsession?view=graph-rest-1.0
                
            //     var newLength = newContentInItem.Content.Length;

            // }


        }
        public static async Task DeleteItem()
        {
            // Ensure client isn't null
            _ = _appClient ??
                throw new System.NullReferenceException("Graph has not been initialized for app-only auth");


            await _appClient.Drives[_driveId].Items[_itemId].Request().DeleteAsync();
        
        }
    }
}