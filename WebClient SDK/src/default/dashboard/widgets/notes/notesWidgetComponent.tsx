import { observer } from 'mobx-react-lite';
import { RichTextBox } from 'ui/richTextBox';
import { NotesWidget } from './notesWidget';
import './notesStyle.scss';

export const NotesWidgetComponent = observer<{ viewModel: NotesWidget }>(
  function NotesWidgetComponent({ viewModel }) {
    if (!viewModel.richEditor) {
      return null;
    }

    return (
      <div className="notes-container">
        <RichTextBox viewModel={viewModel.richEditor} />
      </div>
    );
  }
);
