import { extension, localize } from '@tessa/application';
import { IStorage, StorageSerializable, TypedJsonConverter } from '@tessa/core';
import {
  Card,
  CardImportInfo,
  ICardImportService,
  ICardImportService$,
  ICardService,
  ICardService$
} from '@tessa/platform';
import { UIHost } from '@tessa/ui';
import {
  CardToolbarAction,
  CardUIExtension,
  ICardToolbarViewModel,
  ICardUIExtensionContext
} from 'tessa/ui/cards';
import { LabelViewModel, TextBoxViewModel } from 'tessa/ui/cards/controls';
import { DefaultFormMainViewModel } from 'tessa/ui/cards/forms';
import { CardImportState, ImportCardsHelper } from 'tessa/ui/import';
import { UIContext } from 'tessa/ui/uiContext';
import { Visibility } from 'ui/uiEnums';

@extension({ name: 'CardImportUIExtension' })
export class CardImportUIExtension extends CardUIExtension {
  //#region ctor

  constructor(
    @ICardImportService$() private readonly _importService: ICardImportService,
    @ICardService$() private readonly _cardService: ICardService
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    const cardModel = context.model;
    const card = cardModel.card;

    this.createCancelToolbarItem(context.toolbar, context.card);
    this.createStartImportToolbarItem(context.toolbar, card);

    if (cardModel.mainForm instanceof DefaultFormMainViewModel) {
      cardModel.mainForm.tabsAreCollapsed = true;
    }

    const stateControl = cardModel.controls.get('State') as TextBoxViewModel;
    const progressControl = cardModel.controls.get('Progress') as LabelViewModel;
    const totalCountControl = cardModel.controls.get('TotalCount');
    const importedCountControl = cardModel.controls.get('ImportedCount');
    const skippedCountControl = cardModel.controls.get('SkippedCount');
    const warningCountControl = cardModel.controls.get('WarningCount');
    const errorCountControl = cardModel.controls.get('ErrorCount');

    if (
      !stateControl ||
      !progressControl ||
      !totalCountControl ||
      !importedCountControl ||
      !warningCountControl ||
      !skippedCountControl ||
      !errorCountControl
    ) {
      return;
    }

    const section = card.sections.get('CardImports');
    const stateId = section.fields.getNumber('StateID');

    stateControl.displayFormat = localize(
      `$Enum_CardImportStates_${CardImportState[stateId as CardImportState]}`
    );

    if (!stateId) {
      totalCountControl.controlVisibility = Visibility.Collapsed;
      importedCountControl.controlVisibility = Visibility.Collapsed;
      warningCountControl.controlVisibility = Visibility.Collapsed;
      progressControl.controlVisibility = Visibility.Collapsed;
      skippedCountControl.controlVisibility = Visibility.Collapsed;
      errorCountControl.controlVisibility = Visibility.Collapsed;
      progressControl.controlVisibility = Visibility.Collapsed;
    } else if (stateId === CardImportState.Import) {
      const total = section.fields.getNumber('TotalCount') ?? 0;
      const imported = section.fields.getNumber('ImportedCount') ?? 0;
      const skipped = section.fields.getNumber('SkippedCount') ?? 0;
      const errors = section.fields.getNumber('ErrorCount') ?? 0;
      progressControl.text = localize(
        '$CardTypes_Controls_Progress',
        imported + errors + skipped,
        total
      );
    } else {
      progressControl.controlVisibility = Visibility.Collapsed;
    }
  }

  //#endregion

  //#region private methods

  private createCancelToolbarItem(toolbar: ICardToolbarViewModel, card: Card): void {
    if (ImportCardsHelper.isCancelable(card)) {
      toolbar.addItemIfNotExists(
        new CardToolbarAction({
          name: 'CancelImport',
          caption: '$UI_CardImport_CancelImport',
          icon: 'm-cross',
          command: async () => {
            const uiContext = UIContext.current;
            const editor = uiContext.cardEditor;
            if (!editor) {
              return;
            }

            const result = await UIHost.showLoadingOverlay(
              () => this._importService.cancel(editor.cardModel!.card.id),
              { delay: 300 }
            );
            await UIHost.showNotEmpty(result);

            if (result.isSuccessful) {
              await editor.refreshCard(uiContext);
            }
          }
        })
      );
    } else {
      toolbar.removeItemIfExists('CancelImport');
    }
  }

  private createStartImportToolbarItem(toolbar: ICardToolbarViewModel, card: Card): void {
    if (ImportCardsHelper.isStartable(card)) {
      toolbar.addItemIfNotExists(
        new CardToolbarAction({
          name: 'StartImport',
          caption: '$UI_CardImport_Import',
          icon: 'ta icon-thin-120',
          command: async () => {
            const uiContext = UIContext.current;
            const editor = uiContext.cardEditor;
            if (!editor) {
              return;
            }

            const cardsInfoString = card.sections.get('CardImports').fields.getString('CardsInfo');
            if (!cardsInfoString) {
              return;
            }

            const cardId = card.id;
            const importingCardsInfo = TypedJsonConverter.deserializeList(cardsInfoString).map(
              (s: IStorage) => StorageSerializable.deserialize(CardImportInfo, s)
            );

            if (
              await ImportCardsHelper.showImportDialog(this._cardService, this._importService, {
                importSettings: { cardId, importingCardsInfo }
              })
            ) {
              await editor.refreshCard(uiContext);
            }
          }
        })
      );
    } else {
      toolbar.removeItemIfExists('StartImport');
    }
  }

  //#endregion
}
