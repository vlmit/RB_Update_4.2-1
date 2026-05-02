import { observer } from 'mobx-react-lite';
import { ExampleHeaderViewModel } from './31_exampleHeaderViewModel';

type ExampleHeaderProps = {
  viewModel: ExampleHeaderViewModel;
};

export const ExampleHeader = observer<ExampleHeaderProps>(function ExampleHeader({ viewModel }) {
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        margin: '5px 12px',
        padding: '5px',
        border: '1px solid rgb(25, 155, 179)',
        borderRadius: '5px',
        backgroundColor: 'rgba(255, 255, 255, 0.5)'
      }}
    >
      <span>{viewModel.title}</span>
    </div>
  );
});
