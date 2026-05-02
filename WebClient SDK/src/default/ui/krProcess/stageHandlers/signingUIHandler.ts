import { FieldStorageMapChangedEventArgs } from '@tessa/core';
import { extension } from '@tessa/application';
import {
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors,
  WorkflowCardTypes
} from '@tessa/platform';
import { IKrStageTypeUIHandlerContext, KrStageTypeUIHandler } from 'tessa/ui/workflow/krProcess';
import { IControlViewModel } from 'tessa/ui/cards';
import { TabControlViewModel } from 'tessa/ui/cards/controls';
import { Visibility } from 'tessa/platform';

/**
 * UI обработчик типа этапа {@link StageTypeDescriptors.signingDescriptor}.
 */
@extension({ name: 'SigningUIHandler' })
export class SigningUIHandler extends KrStageTypeUIHandler {
  //#region fields

  private static readonly notReturnEdit = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrSigningStageSettingsVirtual',
    'NotReturnEdit'
  );

  private static readonly signFiles = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrSigningStageSettingsVirtual',
    'SignCardFiles'
  );

  private _returnIfNotSignedFlagControl: IControlViewModel | null;
  private _returnAfterSigningFlagControl: IControlViewModel | null;
  private _noSignFilesDialogSigningFlagControl: IControlViewModel | null;
  private _noCommentDialogSigningFlagControl: IControlViewModel | null;
  private _doNotSignFileCopiesSigningFlagControl: IControlViewModel | null;
  private _fileCategoriesSigningOptionControl: IControlViewModel | null;
  private _hiddenFileCategoriesSigningOptionControl: IControlViewModel | null;

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.signingDescriptor];
  }

  async initialize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    let flagsTabs: IControlViewModel | undefined;

    if (
      (flagsTabs = context.settingsForms
        .find(i => i.name === WorkflowCardTypes.KrSigningStageTypeSettingsTypeName)
        ?.blocks.find(i => i.name === 'SigningStageFlags')
        ?.controls.find(x => x.name === 'FlagsTabs')) &&
      flagsTabs instanceof TabControlViewModel
    ) {
      const commonSettings = flagsTabs.tabs.find(x => x.name === 'CommonSettings');
      const commonStageFlags = commonSettings?.blocks.find(x => x.name === 'StageFlags');
      const signOptionsFlags = commonSettings?.blocks.find(
        x => x.name === 'StageSignCardFilesOptions'
      );
      const additionalStageFlags = flagsTabs.tabs
        .find(x => x.name === 'AdditionalSettings')
        ?.blocks.find(x => x.name === 'StageFlags');

      this._returnIfNotSignedFlagControl =
        commonStageFlags?.controls.find(x => x.name === 'ReturnIfNotSigned') ?? null;

      this._noSignFilesDialogSigningFlagControl =
        signOptionsFlags?.controls.find(x => x.name === 'NoSignFilesDialog') ?? null;

      this._noCommentDialogSigningFlagControl =
        signOptionsFlags?.controls.find(x => x.name === 'NoCommentDialog') ?? null;

      this._doNotSignFileCopiesSigningFlagControl =
        signOptionsFlags?.controls.find(x => x.name === 'DoNotSignFileCopies') ?? null;

      this._fileCategoriesSigningOptionControl =
        signOptionsFlags?.controls.find(x => x.name === 'FileCategories') ?? null;

      this._hiddenFileCategoriesSigningOptionControl =
        signOptionsFlags?.controls.find(x => x.name === 'HiddenFileCategories') ?? null;

      this._returnAfterSigningFlagControl =
        additionalStageFlags?.controls.find(x => x.name === 'ReturnAfterSigning') ?? null;
    }

    context.row.fieldChanged.add(this.onSettingsFieldChanged);

    this.notReturnEditConfigureFields(!!context.row.tryGet(SigningUIHandler.notReturnEdit));
    this.signOptionsConfigureFields(!!context.row.tryGet(SigningUIHandler.signFiles));
  }

  async finalize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    context.row.fieldChanged.remove(this.onSettingsFieldChanged);
  }

  //#endregion

  //#region private methods

  private onSettingsFieldChanged = (e: FieldStorageMapChangedEventArgs) => {
    if (e.fieldName === SigningUIHandler.notReturnEdit) {
      this.notReturnEditConfigureFields(!!e.fieldValue);
    }
    if (e.fieldName === SigningUIHandler.signFiles) {
      this.signOptionsConfigureFields(!!e.fieldValue);
    }
  };

  private signOptionsConfigureFields(signFiles: boolean) {
    const newVisibility = signFiles ? Visibility.Visible : Visibility.Collapsed;
    if (this._noSignFilesDialogSigningFlagControl) {
      this._noSignFilesDialogSigningFlagControl.controlVisibility = newVisibility;
    }
    if (this._noCommentDialogSigningFlagControl) {
      this._noCommentDialogSigningFlagControl.controlVisibility = newVisibility;
    }
    if (this._doNotSignFileCopiesSigningFlagControl) {
      this._doNotSignFileCopiesSigningFlagControl.controlVisibility = newVisibility;
    }
    if (this._fileCategoriesSigningOptionControl) {
      this._fileCategoriesSigningOptionControl.controlVisibility = newVisibility;
    }
    if (this._hiddenFileCategoriesSigningOptionControl) {
      this._hiddenFileCategoriesSigningOptionControl.controlVisibility = newVisibility;
    }
  }

  private notReturnEditConfigureFields(isNotReturnEdit: boolean) {
    if (isNotReturnEdit) {
      if (this._returnIfNotSignedFlagControl) {
        this._returnIfNotSignedFlagControl.controlVisibility = Visibility.Collapsed;
      }

      if (this._returnAfterSigningFlagControl) {
        this._returnAfterSigningFlagControl.controlVisibility = Visibility.Collapsed;
      }
    } else {
      if (this._returnIfNotSignedFlagControl) {
        this._returnIfNotSignedFlagControl.controlVisibility = Visibility.Visible;
      }

      if (this._returnAfterSigningFlagControl) {
        this._returnAfterSigningFlagControl.controlVisibility = Visibility.Visible;
      }
    }
  }

  //#endregion
}
