import { observer } from 'mobx-react-lite';
import { IControlViewModel } from 'tessa/ui/cards';
import {
  useTaskDigest,
  useTaskLimitations,
  ControlProps
} from 'tessa/ui/cards/components/controls';

export const TaskInfoDataExample = observer<ControlProps<IControlViewModel>>(
  function TaskInfoDataExample(props) {
    const digest = useTaskDigest(props);
    const limitations = useTaskLimitations(props);

    return (
      <div
        style={{
          padding: '15px',
          border: '2px solid #3498db',
          borderRadius: '8px',
          backgroundColor: '#f8f9fa',
          marginBottom: '20px'
        }}
      >
        <h3
          style={{
            color: '#2c3e50',
            marginTop: 0,
            borderBottom: '1px solid #ddd',
            paddingBottom: '10px'
          }}
        >
          Данные задачи
        </h3>
        <div style={{ marginBottom: '15px' }}>{digest}</div>
        <div>{limitations}</div>
      </div>
    );
  }
);
