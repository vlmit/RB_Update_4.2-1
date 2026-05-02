import { extension, localize } from '@tessa/application';
import { CardControlTypes } from '@tessa/platform';
import { Guid } from '@tessa/core';
import {
  AiContext,
  AiContextType,
  AiRequestType,
  IAiOptions$,
  type IAiOptions,
  type AiAssistantOptions,
  AiAssistantControlDialog,
  AiHelper
} from 'tessa/ui/ai';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { TextBoxViewModel } from 'tessa/ui/cards/controls';
import { UIButton } from 'ui/common/uiButton';
import { TextBoxMode } from 'ui/textField/textBoxMode';
import { Visibility } from 'ui/uiEnums';
import { TextEnhancementAiTool } from '../ai/tools/textEnhancementAiTool';

@extension({ name: 'TextFieldAiAssistantUIExtension' })
export class TextFieldAiAssistantUIExtension extends CardUIExtension {
  //#region ctor

  /**
   * Creates an instance of the {@link TextFieldAiAssistantUIExtension} class.
   * @param _aiOptions Options available for AI settings.
   */
  constructor(@IAiOptions$() private readonly _aiOptions: IAiOptions) {
    super();
  }

  //#endregion

  //#region base overrides

  override initialized(context: ICardUIExtensionContext): void {
    if (!this._aiOptions.isEnabled || !AiHelper.hasAiLicense()) {
      return;
    }

    for (const control of context.model.controlsBag) {
      if (!Guid.equals(control.cardTypeControl.type.id, CardControlTypes.StringControlType.id)) {
        continue;
      }

      if (
        !(control instanceof TextBoxViewModel) ||
        control.textBoxMode !== TextBoxMode.Default ||
        !control.showExpandButton
      ) {
        continue;
      }

      const hoverPanel = control.controlContainer.panels?.find(
        panel => panel.toolbarType === 'hover'
      );

      if (!hoverPanel || !Array.isArray(hoverPanel.content)) {
        continue;
      }

      const indexExpandButton = hoverPanel.content.findIndex(
        button => button.name === 'expand-button'
      );

      if (indexExpandButton < 0) {
        continue;
      }

      const { id, typeId } = context.model.card;
      const [expandButton] = hoverPanel.content.splice(indexExpandButton, 1);
      expandButton.caption = localize('$UI_Cards_ContextMenu_Expand');

      hoverPanel.content.splice(
        indexExpandButton,
        0,
        UIButton.create({
          name: 'menu',
          icon: 'm-kebab',
          type: 'small',
          theme: 'control',
          visibility: () =>
            control.controlContainer.isExpanded ? Visibility.Collapsed : Visibility.Visible,
          child: [
            expandButton,
            UIButton.create({
              name: 'unexpanded-ai-text-enhance',
              icon: 'icon-thin-218',
              caption: localize('$UI_Controls_TextBox_AiAssistant_Button'),
              type: 'small',
              theme: 'control',
              buttonAction: async () => await this.openAiAgentDialog(control, id, typeId)
            })
          ]
        }),
        UIButton.create({
          name: 'expanded-ai-text-enhance',
          icon: 'icon-thin-218',
          type: 'small',
          theme: 'control',
          visibility: () =>
            control.controlContainer.isExpanded ? Visibility.Visible : Visibility.Collapsed,
          buttonAction: async () => await this.openAiAgentDialog(control, id, typeId)
        })
      );
    }
  }

  //#endregion

  //#region private methods

  private async openAiAgentDialog(
    control: TextBoxViewModel,
    cardID: string,
    cardTypeID: string
  ): Promise<void> {
    const options: AiAssistantOptions = {
      context: new AiContext(AiContextType.Card, cardID, cardTypeID, { FromField: true }),
      tool: TextEnhancementAiTool.key,
      startupMessage: localize('$Ai_TextEnhancementAiAgentPlugin_TextField_StartupMessage')
    };

    const dialog = new AiAssistantControlDialog<string | undefined>({ options });

    try {
      await dialog.initialize();

      // Если в контроле есть текст, добавляем его пользовательским сообщением
      if (control.text.length) {
        dialog.control.input.text = control.text;
        await dialog.control.addMessage({
          type: AiRequestType.Message
        });
      }

      const text = await dialog.show();
      if (text) {
        control.text = text;
      }
    } finally {
      dialog.dispose();
    }
  }

  // #endregion;
}
