import { useCallback } from 'react';
import { observer } from 'mobx-react-lite';
import { localize } from '@tessa/application';
import { Dialog, DialogContainer, DialogContent, DialogFooter } from 'ui';
import { IDialogChromeSettings } from 'ui/dialog/definitions';
import { DialogScrollOptions } from 'ui/dialog/dialogContainer/common';
import Platform from 'common/platform';
import { Button } from 'ui/button/button';
import { showViewModelDialog } from 'tessa/ui';
import { CardTypeSelectorViewModel } from './cardTypeSelectorViewModel';
import { CardTypeSelectorDialogViewModel } from './cardTypeSelectorDialogViewModel';
import { CardTypeSelector } from './cardTypeSelector';

type CardTypeSelectorDialogProps = {
  viewModel: CardTypeSelectorDialogViewModel;
};

export const CardTypeSelectorDialog = observer<CardTypeSelectorDialogProps>(
  function CardTypeSelectorDialog({ viewModel }) {
    const handleOk = useCallback(() => viewModel.select(), [viewModel]);
    const handleCancel = useCallback(() => viewModel.cancel(), [viewModel]);

    const chromeSettings: IDialogChromeSettings = {
      title: localize('$Dashboard_CardTypeSelectorDialog_Title')
    };

    return (
      <Dialog
        isOpened={true}
        noPortal={true}
        closeByEsc={true}
        okByEnter={true}
        onCloseRequest={handleCancel}
        className="dashboard-card-type-selector-dialog"
        canReactWindowResize={true}
        openFullscreen={Platform.isMobile()}
        showChrome={true}
        chromeSettings={chromeSettings}
        autoSizeWidth={true}
      >
        <DialogContainer scrollType={DialogScrollOptions.Content}>
          <DialogContent className="dashboard-card-type-selector-dialog-content">
            <CardTypeSelector viewModel={viewModel.cardTypeSelector} />
          </DialogContent>
          <DialogFooter className="dashboard-card-type-selector-dialog-buttons default-footer">
            <Button
              caption="$UI_Common_Add"
              type="normal"
              theme="primary"
              onClick={handleOk}
              isEnabled={viewModel.cardTypeIsSelected}
            />
            <Button
              caption="$UI_Common_Cancel"
              type="normal"
              theme="secondary"
              onClick={handleCancel}
            />
          </DialogFooter>
        </DialogContainer>
      </Dialog>
    );
  }
);

export async function showCardTypeSelectorDialog(
  cardTypeSelector: CardTypeSelectorViewModel
): Promise<boolean> {
  const viewModel = new CardTypeSelectorDialogViewModel(cardTypeSelector);

  await showViewModelDialog<CardTypeSelectorDialogViewModel>(viewModel, CardTypeSelectorDialog);

  return !viewModel.canceled;
}
