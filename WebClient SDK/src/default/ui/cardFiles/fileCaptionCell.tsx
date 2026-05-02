import { observer } from 'mobx-react-lite';
import { ITableCellViewModel } from 'tessa/ui/views/content';
import { FileViewModel } from 'tessa/ui/cards/controls';
import { FileListTags } from 'tessa/ui/cards/components/controls';
import { maxWordLength } from 'components/cardElements/grid';
import { forceBreakLongWord } from 'common/utility';

export interface FileCaptionCellProps {
  cell: ITableCellViewModel;
  file: FileViewModel;
}

export const FileCaptionCell = observer<FileCaptionCellProps>(function FileCaptionCell(props) {
  const { cell, file } = props;
  const content =
    typeof cell.convertedValue === 'string'
      ? forceBreakLongWord(cell.convertedValue, maxWordLength)
      : cell.convertedValue;
  return (
    <span className="files-control-item-container">
      {file.isLoading ? (
        <span className="files-control-item-loading">
          <i className="icon fa fa-spinner fa-spin" />
        </span>
      ) : (
        <></>
      )}
      {content}
      <FileListTags
        style={{
          marginLeft: '6px',
          fontSize: '1rem'
        }}
        viewModel={file}
      />
    </span>
  );
});
