import { observer } from 'mobx-react-lite';
import { IControlViewModel } from 'tessa/ui/cards';
import {
  ControlProps,
  useTaskAssignedRoles,
  useTaskPerformers,
  useTaskFunctionRoles
} from 'tessa/ui/cards/components/controls';

export const TaskInfoRolesExample = observer<ControlProps<IControlViewModel>>(
  function TaskInfoRolesExample(props) {
    const assignedRoles = useTaskAssignedRoles(props);
    const functionRoles = useTaskFunctionRoles(props);
    const performers = useTaskPerformers(props);

    return (
      <div
        style={{
          padding: '15px',
          border: '2px solid #2ecc71',
          borderRadius: '8px',
          backgroundColor: '#f9fff9',
          marginBottom: '20px'
        }}
      >
        <h3
          style={{
            color: '#27ae60',
            marginTop: 0,
            borderBottom: '1px solid #ddd',
            paddingBottom: '10px'
          }}
        >
          Участники задачи
        </h3>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
            gap: '15px',
            marginTop: '10px'
          }}
        >
          <div>{assignedRoles}</div>
          <div>{functionRoles}</div>
          <div>{performers}</div>
        </div>
      </div>
    );
  }
);
