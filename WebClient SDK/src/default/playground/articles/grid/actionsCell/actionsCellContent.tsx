import { observer } from 'mobx-react-lite';
import { FC, useMemo } from 'react';
import { Button } from 'ui/button/button';
import { IToolbarGroup, Toolbar } from 'ui/toolbar';
import { ActionsGridCellViewModel } from './actionsGridCellViewModel';

type ActionCellContentProps = {
  viewModel: ActionsGridCellViewModel;
};

export const ActionCellContent: FC<ActionCellContentProps> = observer(({ viewModel }) => {
  const versionRef = viewModel.actions.getVersionRef();

  const buttons = useMemo(() => {
    return viewModel.actions.map(a => <Button viewModel={a} key={a.name} />);
  }, [versionRef]);

  const group = useMemo(() => {
    const g: IToolbarGroup = {
      id: 'actions',
      items: viewModel.actions.map(a => a.name),
      spacing: viewModel.gap
    };

    return g;
  }, [versionRef, viewModel.gap]);

  return <Toolbar items={buttons} groups={[group]} overflow="wrap" />;
});
