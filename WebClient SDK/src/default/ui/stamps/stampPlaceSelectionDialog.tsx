import * as React from 'react';
import { observer } from 'mobx-react-lite';
import { Dialog, DialogContainer, DialogContent, DialogFooter } from 'ui';
import { Autocomplete } from 'ui/autocomplete/autocomplete';
import { Button } from 'ui/button/button';
import ControlContainer from 'ui/controlContainer';
import './stampPlaceDialog.scss';
import './stampPlaceDialogCompact.scss';
import { ControlCaption } from 'tessa/ui/cards/components/controls';
import {
  StampPlace,
  StampPlaceSelectionDialogViewModel
} from './stampPlaceSelectionDialogViewModel';

export interface StampPlaceSelectionDialogProps {
  viewModel: StampPlaceSelectionDialogViewModel;
  onClose: (result: { cancel: boolean; stampPlace: StampPlace | null }) => void;
}

export const StampPlaceSelectionDialog = observer<StampPlaceSelectionDialogProps>(
  function StampPlaceSelectionDialog({ viewModel, onClose }) {
    const renderListCategories = () => {
      return (
        <div className="form-group">
          <ControlCaption>{viewModel.autocompleteFieldCaption}</ControlCaption>
          <ControlContainer isInvalid={viewModel.isInvalid}>
            <Autocomplete viewModel={viewModel.autocomplete} />
          </ControlContainer>
        </div>
      );
    };

    const handleSave = () => {
      if (!viewModel.selectedStampPlace) {
        onClose({
          cancel: false,
          stampPlace: null
        });
        return;
      }

      onClose({
        cancel: false,
        stampPlace: viewModel.selectedStampPlace
      });
    };

    const handleCloseForm = () => {
      onClose({
        cancel: true,
        stampPlace: null
      });
    };

    return (
      <Dialog
        isOpened={true}
        noPortal={true}
        autoSizeWidth={true}
        autoSizeHeight={true}
        onCloseRequest={handleCloseForm}
        style={{
          minHeight: 'auto',
          outline: 'none'
        }}
        showChrome={true}
        chromeSettings={{ title: viewModel.chromeTitle, titlePosition: 'left' }}
        className={'category-dialog'}
        type="controls"
      >
        <DialogContainer>
          <DialogContent>
            <>{renderListCategories()}</>
          </DialogContent>
          <DialogFooter>
            <Button
              key="save_button"
              caption={viewModel.confirmationButtonCaption}
              onClick={handleSave}
              type="normal"
              theme="primary"
              className={viewModel.confirmationButtonIsDisabled ? 'disabled' : ''}
            />
          </DialogFooter>
        </DialogContainer>
      </Dialog>
    );
  }
);
