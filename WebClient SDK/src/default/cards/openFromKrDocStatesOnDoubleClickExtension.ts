import {
  DoubleClickInfo,
  IWorkplaceViewComponent,
  openCardIntegerDoubleClickAction
} from 'tessa/ui/views';
import { LoadingOverlay } from 'tessa/ui';
import { DotNetType, TypedField } from 'tessa/platform';
import { AdvancedCardDialogManager } from 'tessa/ui/cards';
import { extension } from '@tessa/application';
import { OpenInDialogOnDoubleClickExtensionBase } from 'tessa/defaultExtensions/platform/cards/openInDialogOnDoubleClickExtensionBase';
import { OnDoubleClickExtensionSettings } from 'tessa/defaultExtensions/platform/cards/onDoubleClickExtensionSettings';

/**
 * Расширение, выполняющее открытие виртуальной карточки по строке в состояниях документа.
 */
@extension()
export class OpenFromKrDocStatesOnDoubleClickExtension extends OpenInDialogOnDoubleClickExtensionBase {
  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Cards.OpenFromKrDocStatesOnDoubleClickExtension';
  }

  public override initialize(model: IWorkplaceViewComponent): void {
    this.settings = new OnDoubleClickExtensionSettings(this.settingsStorage);
    if (model.inSelectionMode()) {
      return;
    }
    model.doubleClickAction = async (info: DoubleClickInfo) => {
      await openCardIntegerDoubleClickAction(info, async (cardId, displayValue, context) => {
        await LoadingOverlay.instance.show(async splashResolve => {
          await AdvancedCardDialogManager.instance.openCard({
            cardTypeId: 'e83a230a-f5fc-445e-9b44-7d0140ee69f6', // KrDocStateTypeID
            displayValue,
            context,
            splashResolve,
            dialogOptions: {
              openInFullscreen: this.settings.openInFullscreen,
              showOnlyFirstTab: this.settings.openOnlyFirstTab,
              withTabControlBackground: true,
              dialogAutoSizeWidth: true
            },
            cardEditorModifierAction: openingContext => {
              // Рефреш представления при закрытии карточки.
              if (this.settings?.refreshViewOnClose === true) {
                const uiContext = openingContext.uiContext;
                uiContext.cardEditor?.closed.add(async () => {
                  if (uiContext.cardEditor?.isUpdatedServer) {
                    await context.viewContext?.refreshView();
                  }
                });
              }
            },
            info: {
              StateID: TypedField.create(cardId, DotNetType.Int)
            }
          });
        });
      });
    };
  }
}
