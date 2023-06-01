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

These permission provide the Azure Function the ability to read, created and delete files in every Library in every site. A combination of MSGraph and SharePoint permissions as the solution ustalises both of the APIs.

A certificate is used by the Azure Function to create a context / connection using the app registration. The certificates public key must be uploaded to the App registration.

The certificate is stored in the Key Vault, the Azure Function (API) requests the function from the Key Vault. Which it uses when creating connections context to SPO and MSGraph

## SPFx ListView Extension

SharePoint Framework (SPFx) is a modern development model and set of tools provided by Microsoft for creating customizations and solutions for SharePoint. It allows you to build web parts, extensions, and other custom components using popular web technologies like TypeScript, JavaScript, HTML, and CSS. SPFx offers improved performance, easy integration with Microsoft services, and flexible deployment options, making it a powerful and modern way to extend and customize SharePoint.

A ListView extension, also known as a ListViewCommandSet, is a type of SharePoint Framework (SPFx) extension that allows you to customize the command bar and context menu of a SharePoint list or library. By creating a ListView extension, you can add custom actions, buttons, and menu items to enhance the user experience and provide additional functionality when working with list items or documents. ListView extensions are client-side components that are rendered within the SharePoint interface, providing a seamless and integrated customization experience.

