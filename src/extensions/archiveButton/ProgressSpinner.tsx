import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { BaseDialog, IDialogConfiguration } from '@microsoft/sp-dialog';
import { Spinner, SpinnerSize } from '@fluentui/react/lib/Spinner';
import { DialogContent } from '@fluentui/react/lib/Dialog';

export const SpinnerLabeledExample: React.FunctionComponent = (props) => {

  return (
    <DialogContent
          title={props.title}
          showCloseButton={true}
        >
        <Spinner size={SpinnerSize.large} label="Wait, wait..." ariaLive="assertive" labelPosition="right" />
    </DialogContent>
      
  );
};

export default class SpinnerDialog extends BaseDialog {
    public message: string;

  
    public render(): void {
      ReactDOM.render(<SpinnerLabeledExample title={this.message}/>, this.domElement);
    }
  
    public getConfig(): IDialogConfiguration {
      return { isBlocking: false };
    }
  
    protected onAfterClose(): void {
      super.onAfterClose();
  
      // Clean up the element for the next dialog
      ReactDOM.unmountComponentAtNode(this.domElement);
    }

  }