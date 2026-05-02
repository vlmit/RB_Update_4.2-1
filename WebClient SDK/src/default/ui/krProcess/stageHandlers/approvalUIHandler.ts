import { FieldStorageMapChangedEventArgs, FieldType, Guid } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import {
  CardRow,
  IViewRepository,
  IViewRepository$,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors,
  WorkflowCardTypes
} from '@tessa/platform';
import { IBlockViewModel, IControlViewModel } from 'tessa/ui/cards';
import { IKrStageTypeUIHandlerContext, KrStageTypeUIHandler } from 'tessa/ui/workflow/krProcess';
import { TabControlViewModel } from 'tessa/ui/cards/controls';
import { Visibility } from 'tessa/platform';
import { advisoryTaskKindId, getKindCaption } from '../../../workflow/krProcess/krUIHelper';

/**
 * UI обработчик типа этапа {@link StageTypeDescriptors.approvalDescriptor}.
 */
@extension({ name: 'ApprovalUIHandler' })
export class ApprovalUIHandler extends KrStageTypeUIHandler {
  //#region ctor

  constructor(@inject(IViewRepository$) private readonly _viewRepository: IViewRepository) {
    super();
  }

  //#endregion

  //#region fields

  private static readonly _advisoryField = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrApprovalSettingsVirtual',
    'Advisory'
  );

  private static readonly _notReturnEditField = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrApprovalSettingsVirtual',
    'NotReturnEdit'
  );

  private static readonly _kindIdField = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrTaskKindSettingsVirtual',
    'KindID'
  );

  private static readonly _kindCaptionField = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrTaskKindSettingsVirtual',
    'KindCaption'
  );

  private static readonly _returnWhenDisapprovedField =
    KrStageTypeFormattingHelper.formatPlainColumnName(
      'KrApprovalSettingsVirtual',
      'ReturnWhenDisapproved'
    );

  private _settings: CardRow | null;
  private _returnIfNotApprovedFlagControl?: IControlViewModel;
  private _returnAfterApprovalFlagControl?: IControlViewModel;

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.approvalDescriptor];
  }

  async initialize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    let flagsBlock: IBlockViewModel | undefined;
    let flagsTabs: IControlViewModel | undefined;

    if (
      (flagsBlock = context.settingsForms
        .find(x => x.name === WorkflowCardTypes.KrApprovalStageTypeSettingsTypeName)
        ?.blocks.find(x => x.name === 'ApprovalStageFlags')) &&
      (flagsTabs = flagsBlock.controls.find(x => x.name === 'FlagsTabs')) &&
      flagsTabs instanceof TabControlViewModel
    ) {
      this._returnIfNotApprovedFlagControl = flagsTabs.tabs
        .find(x => x.name === 'CommonSettings')
        ?.blocks.find(x => x.name === 'StageFlags')
        ?.controls.find(x => x.name === 'ReturnIfNotApproved');

      this._returnAfterApprovalFlagControl = flagsTabs.tabs
        .find(x => x.name === 'AdditionalSettings')
        ?.blocks.find(x => x.name === 'StageFlags')
        ?.controls.find(x => x.name === 'ReturnAfterApproval');
    }

    this._settings = context.row;
    this._settings.fieldChanged.add(this.onSettingsFieldChanged);

    this.advisoryConfigureFields(!!this._settings.tryGet(ApprovalUIHandler._advisoryField));
    this.notReturnEditConfigureFields(
      !!this._settings.tryGet(ApprovalUIHandler._notReturnEditField)
    );
  }

  async finalize(): Promise<void> {
    if (this._settings) {
      this._settings.fieldChanged.remove(this.onSettingsFieldChanged);
      this._settings = null;
    }
  }

  //#endregion

  //#region private methods

  private readonly onSettingsFieldChanged = async (
    e: FieldStorageMapChangedEventArgs
  ): Promise<void> => {
    if (e.fieldName === ApprovalUIHandler._advisoryField) {
      const advisory = !!e.fieldValue;

      this.advisoryConfigureFields(advisory);

      if (advisory) {
        if (!this._settings!.tryGet(ApprovalUIHandler._kindIdField)) {
          const kindCaption = await getKindCaption(this._viewRepository, advisoryTaskKindId);

          if (kindCaption) {
            this._settings!.set(ApprovalUIHandler._kindIdField, advisoryTaskKindId, FieldType.Guid);
            this._settings!.set(ApprovalUIHandler._kindCaptionField, kindCaption, FieldType.String);
          }
        }
      } else {
        if (
          Guid.equals(this._settings!.tryGet(ApprovalUIHandler._kindIdField), advisoryTaskKindId)
        ) {
          this._settings!.set(ApprovalUIHandler._kindIdField, null);
          this._settings!.set(ApprovalUIHandler._kindCaptionField, null);
        }

        if (this._returnIfNotApprovedFlagControl) {
          this._returnIfNotApprovedFlagControl.isReadOnly = false;
        }
      }

      return;
    }

    if (e.fieldName === ApprovalUIHandler._notReturnEditField) {
      this.notReturnEditConfigureFields(!!e.fieldValue);
    }
  };

  private advisoryConfigureFields(isAdvisory: boolean) {
    if (isAdvisory) {
      if (this._returnIfNotApprovedFlagControl) {
        this._returnIfNotApprovedFlagControl.isReadOnly = true;
        this._settings!.set(
          ApprovalUIHandler._returnWhenDisapprovedField,
          false,
          FieldType.Boolean
        );
      }
    }
  }

  private notReturnEditConfigureFields(isNotReturnEdit: boolean) {
    if (isNotReturnEdit) {
      if (this._returnIfNotApprovedFlagControl) {
        this._returnIfNotApprovedFlagControl.controlVisibility = Visibility.Collapsed;
      }

      if (this._returnAfterApprovalFlagControl) {
        this._returnAfterApprovalFlagControl.controlVisibility = Visibility.Collapsed;
      }
    } else {
      if (this._returnIfNotApprovedFlagControl) {
        this._returnIfNotApprovedFlagControl.controlVisibility = Visibility.Visible;
      }

      if (this._returnAfterApprovalFlagControl) {
        this._returnAfterApprovalFlagControl.controlVisibility = Visibility.Visible;
      }
    }
  }

  //#endregion
}
