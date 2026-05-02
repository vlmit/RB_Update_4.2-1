import { observer } from 'mobx-react-lite';
import { ExamplePreviewerViewModel } from './30_examplePreviewerViewModel';

export type ExamplePreviewerComponentProps = {
  viewModel: ExamplePreviewerViewModel;
};

export const ExamplePreviewerComponent = observer<ExamplePreviewerComponentProps>(
  function ExamplePreviewerComponent({ viewModel }) {
    return (
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'center',
          alignItems: 'center'
        }}
      >
        <h3>Example Previewer</h3>
        <span>{viewModel.text}</span>
      </div>
    );
  }
);
