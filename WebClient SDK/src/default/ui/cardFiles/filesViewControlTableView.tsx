import * as React from 'react';
import Dropzone from 'react-dropzone';
import { FilesViewControlTableGridViewModel } from './filesViewControlTableGridViewModel';
import { TableView } from 'tessa/ui/views/components';
import './filesViewControl.scss';

export interface FilesViewControlTableViewProps {
  viewModel: FilesViewControlTableGridViewModel;
}

export class FilesViewControlTableView extends React.Component<FilesViewControlTableViewProps> {
  render(): React.ReactElement {
    const { viewModel } = this.props;

    return (
      <React.Fragment>
        <Dropzone onDrop={this.handleDrop} noClick={true} preventDropOnDocument={false}>
          {({ getRootProps, getInputProps }) => (
            <div {...getRootProps({ className: 'files-view-table-wrapper' })}>
              <input
                {...getInputProps({
                  className: 'files-view-table'
                })}
              />
              <TableView viewModel={viewModel} />
            </div>
          )}
        </Dropzone>
      </React.Fragment>
    );
  }

  private handleDrop = (files: File[]) => {
    if (files.length > 0) {
      const { viewModel } = this.props;
      const fileControl = viewModel.fileControl;
      fileControl.handleDropFiles(files);
    }
  };
}
