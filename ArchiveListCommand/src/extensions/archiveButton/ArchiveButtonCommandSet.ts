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
    switch (event.itemId) {
      // Archive
      case 'COMMAND_1':
        // Send http request to flow
        // full path to file

        this.dialog.message = "Archiving"
        this.dialog.show();
        this.dialogOpen = true;
        

        const requestHeaders: Headers = new Headers();
        requestHeaders.append('Content-type', 'application/json');

        // Fields are the visible fields in the list view (other fields are accessible)
        var fileName: string = this.context.listView.selectedRows[0].getValueByName("FileLeafRef")
        var serverRelativeUrlToFile: string = this.context.listView.selectedRows[0].getValueByName("FileRef")
        var pathToFile: string = serverRelativeUrlToFile.substring(this.context.pageContext.web.serverRelativeUrl.length)
        var pathToFolder: string = pathToFile.substring(0, pathToFile.length - fileName.length - 1)
        var uniqueIdValue: string = this.context.listView.selectedRows[0].getValueByName("UniqueId")
        var uniqueId: string = uniqueIdValue.substring(1, uniqueIdValue.length - 2)


        // need to pass all metadata really

        const body: string = JSON.stringify({
          'siteUrl': this.context.pageContext.web.absoluteUrl,
          'pathToFile': pathToFile,
          'pathToFolder': pathToFolder,
          'fileName': fileName,
          'listTitle': this.context.listView.list.title,
          'serverRelative': this.context.listView.list.serverRelativeUrl,
          'modified': this.context.listView.selectedRows[0].getValueByName("Modified"),
          'modifiedBy': this.context.listView.selectedRows[0].getValueByName("Editor"),
          'uniqueId': uniqueId,
          'id': this.context.listView.selectedRows[0].getValueByName("ID"),
          'identifier': encodeURIComponent(pathToFile.substring(1))
        });

        const httpClientOptions: IHttpClientOptions = {
          body: body,
          headers: requestHeaders
        };

        this.context.httpClient.post(
          `https://prod-120.westeurope.logic.azure.com:443/workflows/b710d898532f43e98e3db6e7dd1fe72a/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=6Mh8Mv90jJstcRBJ5PJE0N5BHabxGd1LfYx5bc5l6Io`,
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
}
