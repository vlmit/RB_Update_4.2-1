import {
  TileExtension,
  ITileLocalExtensionContext,
  Tile,
  TileGroups,
  TileEvaluationEventArgs
} from 'tessa/ui/tiles';
import { userSession } from 'common/utility';
import {
  showError,
  showMessage,
  showConfirm,
  showLoadingOverlay,
  showNotEmpty,
  createDialogForm,
  createCardEditorModel
} from 'tessa/ui';
import { AdvancedCardDialogManager, CardToolbarAction } from 'tessa/ui/cards';
import { CardSection, systemKeyPrefix } from 'tessa/cards';
import { localize } from 'tessa/localization/localize';
import { CardRequest, CardService, CardResponse } from 'tessa/cards/service';
import { extension } from '@tessa/application';
import { FieldType, TypedField } from '@tessa/core';

@extension()
export class AbSettingsTileExtension extends TileExtension {
  public initializingLocal(context: ITileLocalExtensionContext): void {
    const panel = context.workspace.leftPanel;
    panel.tiles.push(
      new Tile({
        name: 'AbGenerateTestCards',
        caption: '$AbTest_Generator_GenerateTestCards',
        icon: 'ta icon-thin-001',
        contextSource: panel.contextSource,
        command: AbSettingsTileExtension.generateAction,
        group: TileGroups.CardsTop,
        order: 10,
        evaluating: AbSettingsTileExtension.enableOnSettingsCardAndAdministrator
      })
    );
  }

  private static enableOnSettingsCardAndAdministrator(e: TileEvaluationEventArgs) {
    const editor = e.current.context.cardEditor;

    e.setIsEnabledWithCollapsing(
      e.currentTile,
      !!editor &&
        !!editor.cardModel &&
        editor.cardModel.cardType.id === '35a03878-57b6-4263-ae36-92eb59032132' && // KrSettingsTypeID
        userSession.isAdmin
    );
  }

  private static async generateAction() {
    const dialogResult = await createDialogForm(
      'AbCardGenerator',
      undefined,
      undefined,
      undefined,
      async response => {
        const fields = response.card.sections.get('Table').fields;
        fields.set('UserCount', 0, FieldType.Int);
        fields.set('PartnerCount', 0, FieldType.Int);
      }
    );
    if (!dialogResult) {
      return;
    }

    const [form, model] = dialogResult;
    model.mainForm = form;
    const cardEditor = createCardEditorModel();
    await cardEditor.setCardModel(model);

    await AdvancedCardDialogManager.instance.showCard({
      editor: cardEditor,
      prepareEditorAction: editor => {
        editor.statusBarIsVisible = false;
        editor.toolbar.addItem(
          new CardToolbarAction({
            name: 'CreateCards',
            caption: '$AbTest_Generator_CreateCardsButton',
            icon: 'ta icon-Int426',
            command: async _ => {
              AbSettingsTileExtension.generateButtonAction(
                editor.cardModel!.card.sections.get('Table'),
                async () => await editor.close()
              );
            },
            order: 1
          })
        );

        editor.toolbar.addItem(
          new CardToolbarAction({
            name: 'Cancel',
            caption: '$UI_Common_Cancel',
            icon: 'ta icon-Int626',
            order: 2,
            toolTip: '$UI_Common_Cancel',
            command: async _ => {
              await editor.close();
            }
          })
        );

        editor.context.info[systemKeyPrefix + 'DialogClosingAction'] = () => Promise.resolve(false);
        return true;
      },
      dialogOptions: {
        withTabControlBackground: true
      },
      displayValue: model.card.typeCaption
    });
  }

  private static async generateButtonAction(section: CardSection, closeAction: () => void) {
    const fields = section.fields;
    const userCount = fields.get<number>('UserCount')!;
    if (userCount < 0) {
      await showError('$AbTest_Generator_WarnUserCountNegative');
      return false;
    }

    const partnerCount = fields.get<number>('PartnerCount')!;
    if (partnerCount < 0) {
      await showError('$AbTest_Generator_WarnPartnerCountNegative');
      return false;
    }

    if (userCount === 0 && partnerCount === 0) {
      await showMessage('$AbTest_Generator_WarnCardsCountNotDefined');
      return false;
    }

    const text =
      userCount === 0
        ? `${localize('$AbTest_Generator_CreatePartnersConfirmation')} ${partnerCount}. ${localize(
            '$UI_Common_ContinueConfirmation'
          )}`
        : partnerCount === 0
          ? `${localize('$AbTest_Generator_CreateUsersConfirmation')} ${userCount}. ${localize(
              '$UI_Common_ContinueConfirmation'
            )}`
          : `${localize('$AbTest_Generator_CreatePartnersConfirmation')} ${partnerCount}.\n` +
            `${localize('$AbTest_Generator_CreateUsersConfirmation')} ${userCount}.\n` +
            `${localize('$UI_Common_ContinueConfirmation')}`;

    if (!(await showConfirm(text))) {
      return false;
    }

    if (
      userCount > 1000 &&
      !(await showConfirm(
        `${localize('$AbTest_Generator_WarnTooMuchUsers')} ${userCount}. ${localize(
          '$UI_Common_ContinueConfirmation'
        )}`,
        '$UI_Common_Attention'
      ))
    ) {
      return false;
    }

    if (
      partnerCount > 1000 &&
      !(await showConfirm(
        `${localize('$AbTest_Generator_WarnTooMuchPartners')} ${partnerCount}. ${localize(
          '$UI_Common_ContinueConfirmation'
        )}`,
        '$UI_Common_Attention'
      ))
    ) {
      return false;
    }

    closeAction();

    let response!: CardResponse;
    await showLoadingOverlay(async () => {
      const request = new CardRequest();
      request.requestType = '207e75b5-abb8-403a-a12a-897019afccf6'; // AbTestData
      request.info = {
        UserCount: TypedField.createInt(userCount),
        PartnerCount: TypedField.createInt(partnerCount)
      };
      response = await CardService.instance.request(request);
    });

    const result = response.validationResult.build();
    await showNotEmpty(result);

    return true;
  }
}
