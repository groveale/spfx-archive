import { Log } from '@microsoft/sp-core-library';
import {
  BaseListViewCommandSet,
  Command,
  IListViewCommandSetExecuteEventParameters,
  ListViewStateChangedEventArgs
} from '@microsoft/sp-listview-extensibility';
import { HttpClient, IHttpClientOptions } from '@microsoft/sp-http';
import SpinnerDialog from './ProgressSpinner';

/**
 * If your command set uses the ClientSideComponentProperties JSON input,
 * it will be deserialized into the BaseExtension.properties object.
 * You can define an interface to describe it.
 */
export interface IArchiveButtonCommandSetProperties {
  // This is an example; replace with your own properties
  sampleTextOne: string;
  sampleTextTwo: string;
  endPointUrl: string
}

const LOG_SOURCE: string = 'ArchiveButtonCommandSet';

export default class ArchiveButtonCommandSet extends BaseListViewCommandSet<IArchiveButtonCommandSetProperties> {

  private dialogOpen: boolean = false
  private dialog: SpinnerDialog = new SpinnerDialog ();


  public onInit(): Promise<void> {
    Log.info(LOG_SOURCE, 'Initialized ArchiveButtonCommandSet');

    // initial state of the command's visibility
    const compareOneCommand: Command = this.tryGetCommand('COMMAND_1');
    compareOneCommand.visible = false;

    this.context.listView.listViewStateChangedEvent.add(this, this._onListViewStateChanged);

    return Promise.resolve();
  }

  public onExecute(event: IListViewCommandSetExecuteEventParameters): void {

    var fileLeafRef: string = this.context.listView.selectedRows[0].getValueByName("FileLeafRef")
    var spItemUrl = this.context.listView.selectedRows[0].getValueByName(".spItemUrl")
    var serverRelativeUrl = this.context.listView.list.serverRelativeUrl

    const requestHeaders: Headers = new Headers();
    requestHeaders.append('Content-type', 'application/json');

    const body: string = JSON.stringify({
      'spItemUrl': spItemUrl,
      'fileLeafRef': fileLeafRef,
      'serverRelativeUrl': serverRelativeUrl
    });

    const httpClientOptions: IHttpClientOptions = {
      body: body,
      headers: requestHeaders
    };

    switch (event.itemId) {
      // Archive
      case 'COMMAND_1':
        // Send http request to flow
        // full path to file

        this.dialog.message = "Archiving"
        this.dialog.show();
        this.dialogOpen = true;
        
        this.context.httpClient.post(
          `http://localhost:7071/api/ArchiveFile`,
          HttpClient.configurations.v1, 
          httpClientOptions)

        break;
      // Rehradte
      case 'COMMAND_2':
        // Dialog.prompt(`Clicked ${this.properties.sampleTextTwo}. Enter something to alert:`).then((value: string) => {
        //   Dialog.alert(value);
        // });
        this.dialog.message = "Rehydrating"
        this.dialog.show();
        this.dialogOpen = true;

        
        this.context.httpClient.post(
          `http://localhost:7071/api/RehydrateFile`,
          HttpClient.configurations.v1, 
          httpClientOptions)


        break;
      default:
        throw new Error('Unknown command');
    }
  }

  private _onListViewStateChanged = (args: ListViewStateChangedEventArgs): void => {
    Log.info(LOG_SOURCE, 'List view state changed');

    // reload the page
    console.log("LISTVIEW: " + args.stateChanges)
    

    const compareOneCommand: Command = this.tryGetCommand('COMMAND_1');
    if (compareOneCommand) {
      // This command should be hidden unless exactly one row is selected and that item is not a url.
      compareOneCommand.visible = (this.context.listView.selectedRows?.length === 1  && 
      this.context.listView.selectedRows[0].getValueByName("File_x0020_Type") !== 'url');
    }

    // Only show if item is a stub
    const compareTwoCommand: Command = this.tryGetCommand('COMMAND_2');
    if (compareTwoCommand) {
      // This command should be hidden unless exactly one row is selected and that item is a url.
      compareTwoCommand.visible = (this.context.listView.selectedRows?.length === 1 && 
        this.context.listView.selectedRows[0].getValueByName("File_x0020_Type") === 'url');
    }

    // Refresh when link is added
    if (this.dialogOpen && (args.prevState.rows.length < this.context.listView.rows.length))
    {
      // no need to refresh, just close dialogs
      //location.reload()
      this.dialogOpen = false;
      this.dialog.close();
    }

    // You should call this.raiseOnChage() to update the command bar
    this.raiseOnChange();
  }

  // private getFileId() {
  //   // Replace the placeholder values with your own values
  //   const siteUrl = 'https://contoso.sharepoint.com/sites/mysite';
  //   const listId = '{list-guid}';
  //   const itemId = '{item-id}';

  //   fetch(`${siteUrl}/_api/web/lists(guid'${listId}')/items(${itemId})?$select=File/Id`, {
  //     method: 'GET',
  //     headers: {
  //       'Accept': 'application/json;odata=verbose',
  //     },
  //   })
  //     .then(response => response.json())
  //     .then(data => {
  //       const driveItemId = data.d.File.Id;
  //       console.log(driveItemId);
  //     })
  //     .catch(error => console.error(error));

      
  //   }
}
