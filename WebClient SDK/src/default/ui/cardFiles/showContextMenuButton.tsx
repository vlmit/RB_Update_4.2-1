import { useRef } from 'react';
import { observer } from 'mobx-react-lite';
import { ShowContextMenuButtonViewModel } from './showContextMenuButtonViewModel';
import { Button } from 'ui/button/button';
import { useMenuButtonDropdownProps } from './useMenuButtonDropdownProps';
import { useDropdownMenu } from 'ui/dropdown/useDropdown';

export interface ShowContextMenuButtonProps {
  viewModel: ShowContextMenuButtonViewModel;
}

export const ShowContextMenuButton = observer<ShowContextMenuButtonProps>(
  function ShowContextMenuButton(props) {
    const dropdownRef = useRef<HTMLButtonElement>(null);
    const { actions, config } = useMenuButtonDropdownProps(dropdownRef, props);
    const { element, toggle } = useDropdownMenu(actions, config);

    return (
      <>
        <Button icon="m-etc" ref={dropdownRef} onClick={toggle} type="small" theme="transparent" />
        {element}
      </>
    );
  }
);
