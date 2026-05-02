import { useMemo, FC, useCallback } from 'react';
import { observer } from 'mobx-react-lite';
import { localize } from '@tessa/application';
import { IconSelectorGrid } from 'tessa/ui/iconSelector/chunk';
import { ClipboardHelper } from 'tessa/ui';
import { Icon } from 'ui/icon/icon';
import type { IconArticleViewModel } from './iconsArticleViewModel';

type IconSelectorArticleProps = { viewModel: IconArticleViewModel };

export const IconSelectorArticle: FC<IconSelectorArticleProps> = observer(({ viewModel }) => {
  const onCopyHandler = useCallback(async () => {
    await ClipboardHelper.copyToClipboard(viewModel.selectedIcon!.icon);
  }, [viewModel.selectedIcon]);

  const selectedIconText = useMemo(
    () =>
      viewModel.selectedIcon && (
        <div className="playground-icon-selector__selected-container">
          <p>
            {localize('$UI_Common_SelectedIcon')} {<span>{viewModel.selectedIcon.icon}</span>}
          </p>
          <button
            onClick={onCopyHandler}
            className="button-small button-theme-transparent playground-icon-selector__selected-button"
          >
            <Icon icon="l-copy" size="m" />
          </button>
        </div>
      ),
    [viewModel.selectedIcon, onCopyHandler]
  );

  return (
    <div className="playground-icon-selector">
      {selectedIconText}
      <IconSelectorGrid viewModel={viewModel} />
    </div>
  );
});
