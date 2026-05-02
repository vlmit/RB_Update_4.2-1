import { extension, localize } from '@tessa/application';
import { CardStoreMode } from '@tessa/platform';
import { getTessaIcon } from 'common/utility/uiHelpers';
import {
  CardToolbarAction,
  CardUIExtension,
  ICardUIExtensionContext,
  ToolbarButtonOrder
} from 'tessa/ui/cards';
import {
  AiContext,
  AiContextType,
  AiHelper,
  IAiOptions,
  IAiOptions$,
  AiAssistantControlDialog
} from 'tessa/ui/ai';

/** Расширение для отображения кнопки "ИИ Ассистент" на панели тулбара карточки. */
@extension({ name: 'OpenAiAssistantUIExtension' })
export class OpenAiAssistantUIExtension extends CardUIExtension {
  //#region constructors

  constructor(@IAiOptions$() protected readonly _aiOptions: IAiOptions) {
    super();
  }

  //#endregion

  //#region base overrides

  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    context.toolbar.removeItemIfExists('AiAssistant');

    if (
      !this._aiOptions.isEnabled ||
      context.card.storeMode === CardStoreMode.Insert ||
      !this._aiOptions.supportedCardTypes.includes(context.card.typeId) ||
      !AiHelper.hasAiLicense()
    ) {
      return;
    }

    context.toolbar.addItem(
      new CardToolbarAction({
        name: 'AiAssistant',
        caption: '$UI_Tiles_AiAssistant',
        order: ToolbarButtonOrder.EndPosition - 1,
        icon: getTessaIcon('Thin218'),
        command: async () => {
          const aiContext = new AiContext(AiContextType.Card, context.card.id, context.card.typeId);
          const dialog = new AiAssistantControlDialog({ options: { context: aiContext } });

          try {
            await dialog.initialize();
            await dialog.show({ chromeSettings: { title: localize('$UI_Tiles_AiAssistant') } });
          } finally {
            dialog.dispose();
          }
        }
      })
    );
  }

  //#endregion
}
