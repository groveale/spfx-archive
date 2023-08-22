import { Log } from '@microsoft/sp-core-library';
import {
  BaseListViewCommandSet,
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
  archiveVersions: string;
  archiveVersionCount: string;
  endPointUrl: string
}

const LOG_SOURCE: string = 'ArchiveButtonCommandSet';
const ARCHIVE_METHOD: string = 'SelfService';

export default class ArchiveButtonCommandSet extends BaseListViewCommandSet<IArchiveButtonCommandSetProperties> {

  //private dialogOpen: boolean = false
  private dialog: SpinnerDialog = new SpinnerDialog ();


  public onInit(): Promise<void> {
    Log.info(LOG_SOURCE, 'Initialized ArchiveButtonCommandSet');

    const archiveVersionsProp: string = this.properties.archiveVersions;
    const archiveVersionCountProp: string = this.properties.archiveVersionCount;

    if (archiveVersionsProp === undefined) {
      this.properties.archiveVersions = "true";
      this.properties.archiveVersionCount = "5";
    }

    console.log("Archive Versions: " + archiveVersionsProp)
    console.log("Archive Version Count: " + archiveVersionCountProp)

    // initial state of the command's visibility
    // const compareOneCommand: Command = this.tryGetCommand('COMMAND_1');
    // compareOneCommand.visible = false;

    this.context.listView.listViewStateChangedEvent.add(this, this._onListViewStateChanged);

    return Promise.resolve();
  }

  public onExecute(event: IListViewCommandSetExecuteEventParameters): void {

    for (let i = 0; i < this.context.listView.selectedRows.length; i++) {

      var fileLeafRef: string = this.context.listView.selectedRows[i].getValueByName("FileLeafRef")
      var fileRef: string = this.context.listView.selectedRows[i].getValueByName("FileRef")
      var spItemUrl = this.context.listView.selectedRows[i].getValueByName(".spItemUrl")
      var serverRelativeUrl = this.context.pageContext.web.serverRelativeUrl
  
      const requestHeaders: Headers = new Headers();
      requestHeaders.append('Content-type', 'application/json');
  
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
  
      console.log(body)
  
      const httpClientOptions: IHttpClientOptions = {
        body: body,
        headers: requestHeaders
      };
  
      switch (event.itemId) {
        // Archive
        case 'COMMAND_1':
          // Send http request to flow
          // full path to file

          // Don't want to restore an item that is already restored
          if (fileLeafRef.endsWith("_archive.txt")) {
            // this.dialog.message = `Skipping Archiving file - Already Archived`
            // this.dialog.show();
            break;
          }
  
          this.dialog.message = `Archiving files (${this.context.listView.selectedRows.length})`
          this.dialog.show();
          //this.dialogOpen = true;
  
          //this.sendRequest(`http://localhost:7071/api/ArchiveFile`, httpClientOptions, i + 1, this.context.listView.selectedRows.length)
  
          //this.sendRequest(`https://ag-spfx-archive.azurewebsites.net/api/archivefile`, httpClientOptions, i + 1, this.context.listView.selectedRows.length)
  
          this.sendRequest(`https://bp-archiving-function.azurewebsites.net/api/archivefile`, httpClientOptions, i + 1, this.context.listView.selectedRows.length)
  
          break;
        // Rehradte
        case 'COMMAND_2':
          // Dialog.prompt(`Clicked ${this.properties.sampleTextTwo}. Enter something to alert:`).then((value: string) => {
          //   Dialog.alert(value);
          // });

          // Don't want to restore an item that is already restored
          if (!fileLeafRef.endsWith("_archive.txt")) {
            // this.dialog.message = `Skipping Rehydrating file ${i + 1} / ${this.context.listView.selectedRows.length} - Already Here`
            // this.dialog.show();
            break;
          }

          this.dialog.message = `Rehydrating files (${this.context.listView.selectedRows.length})`
          this.dialog.show();
          //this.dialogOpen = true;
  
          //this.sendRequest(`https://ag-spfx-archive.azurewebsites.net/api/rehydratefile`, httpClientOptions)
          this.sendRequest(`https://bp-archiving-function.azurewebsites.net/api/rehydratefile`, httpClientOptions, i + 1, this.context.listView.selectedRows.length)
          //this.sendRequest(`http://localhost:7071/api/RehydrateFile`, httpClientOptions, i + 1, this.context.listView.selectedRows.length)
  
          break;
        default:
          throw new Error('Unknown command');
      }
    }
  }

  private async sendRequest(uri: string, httpClientOptions: IHttpClientOptions, reqNum: number, totalReqs: number) : Promise<void> {
    return await this.context.httpClient.post(
        uri,
        HttpClient.configurations.v1, 
        httpClientOptions)
      //.then(response => response.json())
      .then(response => {
        console.log(response.status)
        //this.dialogOpen = false;
        //this._onListViewStateChanged(new ListViewStateChangedEventArgs());
        
        if (reqNum === totalReqs) {
          this.dialog.close();
          location.reload();
        }
      });
  }

  private _onListViewStateChanged = (args: ListViewStateChangedEventArgs): void => {
    Log.info(LOG_SOURCE, 'List view state changed');

    // reload the page
    console.log("LISTVIEW: " + args.stateChanges)

    const archiveVersionsProp: string = this.properties.archiveVersions;
    const archiveVersionCountProp: string = this.properties.archiveVersionCount;

    console.log("Archive Versions: " + archiveVersionsProp)
    console.log("Archive Version Count: " + archiveVersionCountProp)
    

    // const compareOneCommand: Command = this.tryGetCommand('COMMAND_1');
    // if (compareOneCommand) {
    //   // This command should be hidden unless exactly one row is selected and that item is not a url.
    //   compareOneCommand.visible = (this.context.listView.selectedRows?.length === 1  && 
    //   !this.context.listView.selectedRows[0].getValueByName("FileLeafRef").endsWith('_archive.txt'));
    // }

    // // Only show if item is a stub
    // const compareTwoCommand: Command = this.tryGetCommand('COMMAND_2');
    // if (compareTwoCommand) {
    //   // This command should be hidden unless exactly one row is selected and that item is a url.
    //   compareTwoCommand.visible = (this.context.listView.selectedRows?.length === 1 && 
    //     this.context.listView.selectedRows[0].getValueByName("FileLeafRef").endsWith('_archive.txt'));
    // }

    // Refresh when link is added
    // if (this.dialogOpen && (args.prevState.rows.length < this.context.listView.rows.length))
    // {
    //   // no need to refresh, just close dialogs
    //   //location.reload()
    //   this.dialogOpen = false;
    //   this.dialog.close();
    // }

    // You should call this.raiseOnChage() to update the command bar
    this.raiseOnChange();
  }
}