import {
  Flags,
  StorageHelper,
  TypedField,
  ValidationResult,
  ValidationResultType
} from '@tessa/core';
import { extension, inject } from '@tessa/application';
import {
  BusinessCalendarHelper,
  Card,
  CardGetRequest,
  CardPermissionFlags,
  CardStoreRequest,
  IBusinessCalendarService,
  IBusinessCalendarService$,
  ICardService,
  ICardService$,
  IOperationService,
  IOperationService$,
  OperationProgressTracker,
  OperationTypes,
  ValidationKeys
} from '@tessa/platform';
import { ErrorTypeID, ErrorTypeName } from 'tessa/cards';
import {
  CardUIExtension,
  ICardUIExtensionContext,
  AdvancedCardDialogManager
} from 'tessa/ui/cards';
import {
  ButtonViewModel,
  AutoCompleteEntryViewModel,
  GridViewModel,
  GridRowAction
} from 'tessa/ui/cards/controls';
import { showNotEmpty, UIContext, showMessage, showError, showLoadingOverlay } from 'tessa/ui';
import { CalendarExclusionsUIHelper } from 'tessa/defaultExtensions/platform/ui/calendarExclusionsUIHelper';

@extension({ name: 'CalendarUIExtension' })
export class CalendarUIExtension extends CardUIExtension {
  //#region ctor

  constructor(
    @inject(IBusinessCalendarService$)
    private readonly _businessCalendarService: IBusinessCalendarService,
    @inject(IOperationService$)
    private readonly _operationService: IOperationService,
    @inject(ICardService$)
    private readonly _cardService: ICardService
  ) {
    super();
  }

  //#endregion

  //#region fields

  private _operationTracker: OperationProgressTracker | null = null;

  //#endregion

  //#region CardUIExtension

  async initialized(context: ICardUIExtensionContext): Promise<void> {
    this.attachCommandToButton(context, 'ValidateCalendar', () =>
      this.validateCalendarButtonAction()
    );
    this.attachCommandToButton(context, 'RebuildCalendar', () =>
      this.rebuildCalendarButtonAction()
    );

    CalendarExclusionsUIHelper.setupExclusionsControls(context, this.disposeList);

    const calendarTypeControl = context.model.controls.get(
      'CalendarType'
    ) as AutoCompleteEntryViewModel;

    if (!calendarTypeControl) {
      return;
    }

    calendarTypeControl.changeFieldCommand.func = async () =>
      await this.openCalendarTypeInDialog(context);

    // Для всех новых записей в именованных интервалах, которые добавляются с клиента - ставим, что они добавлены вручную.
    const namedRangesControl = context.model.controls.get('NamedRanges') as GridViewModel;
    if (!namedRangesControl) {
      return;
    }

    this.disposeList.add(
      namedRangesControl.rowInvoked.addWithDispose(args => {
        if (args.action == GridRowAction.Inserted) {
          args.row.set('IsManual', TypedField.trueBoolean);
        }
      })!
    );
  }

  async finalized(): Promise<void> {
    if (this._operationTracker) {
      this._operationTracker.dispose();
      this._operationTracker = null;
    }
    this.disposeList.dispose();
  }

  //#endregion

  //#region private methods

  private attachCommandToButton(
    context: ICardUIExtensionContext,
    buttonAlias: string,
    action: VoidFunction
  ) {
    const button = context.model.controls.get(buttonAlias) as ButtonViewModel;
    if (!button) {
      return;
    }

    if (
      !Flags.hasFlag(
        context.model.card.permissions.resolver.getCardPermissions(),
        CardPermissionFlags.AllowModify
      )
    ) {
      button.isReadOnly = true;
      return;
    }

    button.onClick = action;
  }

  private async validateCalendarButtonAction() {
    const context = UIContext.current;
    const editor = context.cardEditor;

    const calendarId = editor?.cardModel?.card.sections
      .get(BusinessCalendarHelper.CalendarSettingsSection)
      ?.fields.getNumber('CalendarID');
    if (calendarId == null) {
      await showError('$UI_Common_Messages_CantValidateCalendarWithEmptyID');
      return;
    }

    const cardId = editor!.cardModel!.card.id;
    const result = await this._businessCalendarService.validateCalendar(cardId);

    await showNotEmpty(result, '$UI_BusinessCalendar_ValidatedTitle');
  }

  private async rebuildCalendarButtonAction() {
    const context = UIContext.current;
    const editor = context.cardEditor;

    if (editor && !editor.operationInProgress) {
      const calendarId = editor.cardModel?.card.sections
        .get(BusinessCalendarHelper.CalendarSettingsSection)
        ?.fields.getNumber('CalendarID');
      if (calendarId == null) {
        await showError('$UI_Common_Messages_CantCalcCalendarWithEmptyID');
        return;
      }

      const isAlive = await this._operationService.isAlive(OperationTypes.CalendarRebuild);

      if (isAlive) {
        await showError('$UI_Common_Messages_CalendarRebuildOperationAlreadyRunning');
        return;
      }

      editor.setOperationInProgress(async () => {
        const storeRequest = new CardStoreRequest();
        if (editor.cardModel) {
          storeRequest.card = editor.cardModel.card.clone();
          storeRequest.info[BusinessCalendarHelper.RebuildMarkKey] = TypedField.trueBoolean;
        }

        const storeResponse = await this._cardService.store(storeRequest);
        const storeResult = storeResponse.validationResult.build();

        let operationId: string | null;
        if (
          storeResult.isSuccessful &&
          (operationId = StorageHelper.tryGet<string>(
            storeResponse.info,
            BusinessCalendarHelper.RebuildOperationIDKey
          )!)
        ) {
          const cardModel = editor.cardModel!;
          await editor.openCard({
            cardId: cardModel.card.id,
            cardTypeId: cardModel.card.typeId,
            cardTypeName: cardModel.card.typeName,
            context: editor.context
          });

          this._operationTracker = new OperationProgressTracker(this._operationService, null, {
            operationId,
            interval: 500,
            deleteOperationOnDispose: false
          });

          await this._operationTracker.wait();
          this._operationTracker = null;

          const errorCardID = StorageHelper.tryGet<string>(
            storeResponse.info,
            BusinessCalendarHelper.RebuildOperationErrorCardIDKey
          );
          if (errorCardID) {
            const errorCardRequest = new CardGetRequest();
            errorCardRequest.cardId = errorCardID;
            errorCardRequest.cardTypeId = ErrorTypeID;
            errorCardRequest.cardTypeName = ErrorTypeName;

            const errorCardRequestResult = await this._cardService.get(errorCardRequest);

            let errorCard: Card | null;
            let errorSection;
            const errorCardGetResult = errorCardRequestResult.validationResult.build();
            if (
              errorCardGetResult.isSuccessful &&
              (errorCard = errorCardRequestResult.tryGetCard()) &&
              (errorSection = errorCard.sections.tryGet('Errors'))
            ) {
              await showNotEmpty(
                ValidationResult.fromText(
                  errorSection.fields.get('Text'),
                  ValidationResultType.Error
                )
              );
            }

            if (
              errorCardGetResult.items.length != 1 ||
              errorCardGetResult.items[0].key != ValidationKeys.InstanceNotFound
            ) {
              await showNotEmpty(errorCardGetResult);
            }
          }
        }

        await showNotEmpty(storeResult);
        await showMessage('$UI_BusinessCalendar_CalendarIsRebuiltNotification');
      });
    }
  }

  private async openCalendarTypeInDialog(context: ICardUIExtensionContext) {
    let calendarTypeId: string;
    const settingsSection = context.card.sections.tryGet('CalendarSettings');
    if (!settingsSection || !(calendarTypeId = settingsSection.fields.tryGet('CalendarTypeID')!)) {
      return;
    }

    const calendarTypeName = settingsSection.fields.getString('CalendarTypeCaption')!;
    await showLoadingOverlay(async splashResolve => {
      await AdvancedCardDialogManager.instance.openCard({
        cardId: calendarTypeId,
        displayValue: calendarTypeName,
        context: context.uiContext,
        splashResolve,
        cardEditorModifierAction: async () => {
          const uiContext = UIContext.current;

          uiContext.cardEditor?.closed.add(async () => {
            const typeCard = uiContext.cardEditor!.cardModel!.card;
            if (uiContext.cardEditor?.isUpdatedServer) {
              const typeSection = typeCard.sections.get('CalendarTypes');
              settingsSection.fields.set(
                'CalendarTypeCaption',
                typeSection.fields.getField('Caption')!
              );
              settingsSection.fields.set('CalendarTypeID', TypedField.createGuid(calendarTypeId));
            }
          });
        }
      });
    });
  }

  //#endregion
}
