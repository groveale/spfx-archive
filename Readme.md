# SPFx Self Service Archival

This solution has been developed to demonstrate a method to add self service archival functionality to SharePoint. This is low cost, low maintenance solution simple solution. The following features are supported

* Dynamic Metadata
* Permission maintained
* Searchability maintained (except content)
* All File Types
* Maintains last modified / created dates and users
* Fast / Background processing
* Roll Back
* Easy Deployment

The diagram below provides a general overview of the solution.

![Overview](./res/overview.png)

The solution contains five components:

* App Registration          (Context)
* SPFx ListView Extension   (Fontend / UI)
* Azure Function            (API)
* Azure Storage Account     (Storage tier)
* Key Vault                 (Certificate storage)

The front end to the solution could easily be swapped out to something else, a script, a bot even a PowerAutoamte trigger. The SPFx List extension simply posts a data payload to the Azure Function to identify the file that should be archived. Anything that can replicate this functionality can be used.

## App Registration

The app registration is the identity of the Azure Function. The SPFx ListView component runs in the context of the user so does not require an app registration / identity.

The following application permissions are required:

    MSGraph 	Files.ReadWrite.All
	SharePoint 	Sites.FullControl.All

These permission provide the Azure Function the ability to read, create and delete files in every Library in every site. A combination of MSGraph and SharePoint permissions are required as the solution utilizes both of the APIs.

A certificate is used by the Azure Function to obtain a context / connection using the app registration. The certificates public key (.cer) must be uploaded to the App registration.

The certificate private key (.pfx) is stored in the Key Vault, the Azure Function obtains the certificate from the Key Vault when it establishes a connection with M365. 

## SPFx ListView Extension

SharePoint Framework (SPFx) is a modern development model and set of tools provided by Microsoft for creating customizations and solutions for SharePoint. It allows you to build web parts, extensions, and other custom components using popular web technologies like TypeScript, JavaScript, HTML, and CSS. SPFx offers improved performance, easy integration with Microsoft services, and flexible deployment options, making it a powerful and modern way to extend and customize SharePoint.

A ListView extension, also known as a ListViewCommandSet, is a type of SharePoint Framework (SPFx) extension that allows you to customize the command bar and context menu of a SharePoint list or library. By creating a ListView extension, you can add custom actions, buttons, and menu items to enhance the user experience and provide additional functionality when working with list items or documents. ListView extensions are client-side components that are rendered within the SharePoint interface, providing a seamless and integrated customization experience.

The solution utilizes SPFx ListView Extensions to add two additional buttons to both the command bar and context menu. These buttons are as follows:

![ListView Extension](./res/overview.png)

These buttons simply make a post request with the relevant selected item information to the Archiving API (Azure Function)

Example payload below:

```typescript
const body: string = JSON.stringify({
    'spItemUrl': spItemUrl,
    'fileLeafRef': fileLeafRef,
    'serverRelativeUrl': serverRelativeUrl,
    'siteUrl': this.context.pageContext.web.absoluteUrl,
    'fileRelativeUrl': fileRef,
    'archiveVersions': this.properties.archiveVersions,
    'archiveVersionCount': this.properties.archiveVersionCount,
    'archiveMethod': ARCHIVE_METHOD,
    'archiveUserEmail': this.context.pageContext.user.email,
    'associatedLabel': 'todo'
});
```

If more than one file is selected then multiple requests are sent to the API - One request per file.

### Client Side Settings

There are two settings that can be applied to the extention. These settings will apply to t

## Archiving API (Azure Function)

Azure Functions is a serverless compute service offered by Microsoft as part of its Azure cloud platform. It allows you to run code in response to events without the need to manage the underlying infrastructure.

Being serverless, we don't need to worry about scalability. Azure Functions will automatically scale based on the number of incoming events. As the event load increases, the platform can scale out to handle the load efficiently. This ensures that the archiving API remains responsive and performant.

SharePoint Online, MSGraph and Azure Storage Accounts all have `dotnet` SDKs, therefore it was decided to write this function in `C#`. 

### App Settings

The function has a number of `appSettings` that are required for the API to work. Sample app settings are below:

#### Auth

As already discussed the Azure function uses an app registration to authenticate to SharePoint and MSGraph. The details are configured in the four app settings below:

```json
"clientId": "731dfd10-2052-4390-b4c2-178bff6a8196",
"tenantId": "75e67881-b174-484b-9d30-c581c7ebc177",
"thumbprint": "BD4D7AC2DBCD010E04194D467AC996F486512A49",
"sharepointDomain" : "groveale.sharepoint.com",
```

The certificate that the app registration uses is stored in an Azure KeyVault. They certificate is uploaded to the KeyVault as a secret and is accessed using it's name. An access policy is configured on the KeyVault to only allow the Azure Function access to certificate.

```json
"keyVaultName": "ag-spfx-archive-kv",
"certNameKV": "function",
```

#### Logging

All actions are logged to a SPO list - This list must exist on the site specified by the `archiveHubUrl` parameter.

```json
"archiveHubUrl": "https://groverale.sharepoint.com/sites/AchiveHub",
"archiveHubListName": "ArchiveLog",
```

#### Stub

The API creates a Stub .txt file in place of the archived item. This stub contains a message stating that the file has been archived with a link to a page that should explain the process of Archiving and Rehydrating. This link is specified in the `linkToKB` parameter

```json
"linkToKB": "https://learn.microsoft.com/en-us/graph/api/driveitem-post-children?view=graph-rest-1.0&tabs=csharp",
```

#### Archive

The Archive is a gen2 storage account that has versioning enabled. The API uses a connection string to write files to blob storage. This connection string should be added to the KeyVault for production - TODO

```json
"StorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=agversioning;AccountKey=**8**;EndpointSuffix=core.windows.net"
```

### Functions

The API contains two HTTP trigger functions. One for Archiving and one for Rehydrating. Both function contain the same code but the sequencing is reversed. The Archive function will get the files from SPO, create the file in the blob storage, create the Stub in SPO, apply the metadata to the Stub and finally delete the original file in SPO. 

The Rehydrate function will get the file from blob (using the file ids of the stub), stream the content of the file in blob to a new file in SPO. Apply the metadata from the stub to the new file in SPO and finally delete both the blob and the stub.

> **Note**
>
> If a stub is deleted from SPO it will be almost impossible to recover a file from the Archive. The Stub is the key to the archive location. 

## Azure Storage Account