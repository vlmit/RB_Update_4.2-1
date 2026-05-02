import React from 'react';
import type { IDropdownConfig } from 'ui/dropdown/definitions';
import type { ShowContextMenuButtonProps } from './showContextMenuButton';
import { MenuAction } from 'tessa/ui/menuAction';

interface IDropDownByShowContextMenuButton {
  actions: readonly MenuAction[];
  config: IDropdownConfig;
}

export const useMenuButtonDropdownProps = (
  ref: React.RefObject<HTMLButtonElement>,
  props: ShowContextMenuButtonProps
): IDropDownByShowContextMenuButton => {
  const config: IDropdownConfig = React.useMemo(() => {
    return {
      rootElement: ref.current,
      openPosition: 'right',
      openDirection: 'right',
      className: 'files-control-dropdown'
    };
  }, [ref.current]);

  const actions = props.viewModel.getMenuActions();

  return React.useMemo(
    () => ({
      config,
      actions
    }),
    [actions, config]
  );
};
