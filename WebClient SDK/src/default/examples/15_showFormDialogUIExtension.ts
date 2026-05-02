import { Guid } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import {
  CardNewRequest,
  ICardMetadata,
  ICardMetadata$,
  ICardService,
  ICardService$
} from '@tessa/platform';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { ButtonViewModel } from 'tessa/ui/cards/controls';
import { showNotEmpty, createCardModel, UIButton, LoadingOverlayHelper } from 'tessa/ui';
import { showFormDialog } from 'tessa/ui/uiHost';

/**
 * При клике на контрол кнопки показываем диалог с выбранной формой.
 *
 * Результат работы расширения:
 * При клике на кнопку "Показать диалог" показываем диалоговое окно "Создать несколько карточек".
 */
@extension()
export class ShowFormDialogUIExtension extends CardUIExtension {
  constructor(
    @inject(ICardService$) private _cardService: ICardService,
    @inject(ICardMetadata$) private _cardMetadata: ICardMetadata
  ) {
    super();
  }

  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // пытаемся найти кнопку "Показать диалог"
    const button = context.model.controls.get('ShowDialogTypeForm') as ButtonViewModel;
    if (!button) {
      return;
    }

    // будем открывать диалог с формой по клику
    button.onClick = () => this.showFormDialog();
  }

  private async showFormDialog() {
    // Ищем тип карточки с диалогами
    const dialogsType = this._cardMetadata.cardTypes.getCardTypeByName('Dialogs');
    if (!dialogsType) {
      return;
    }

    await LoadingOverlayHelper.ensureCardTypeDataLoaded(dialogsType);

    // для примера ищем форму "Создать несколько карточек"
    const dialogForm = dialogsType.forms.find(x => x.name === 'CreateMultipleCards');
    if (!dialogForm) {
      return;
    }

    // получаем новую карточку
    const request = new CardNewRequest();
    request.cardTypeId = dialogsType.id;
    const response = await this._cardService.create(request);
    response.card.id = Guid.newGuid();
    await showNotEmpty(response.validationResult.build());
    if (!response.validationResult.isSuccessful) {
      return;
    }

    // создаем модель карточки
    const windowCardModel = await createCardModel(response.card, response.sectionRows);

    // показываем диалог
    await showFormDialog(
      dialogForm,
      windowCardModel,
      async () => {
        // тут можно управлять и расширить все контролы формы
      },
      [
        UIButton.create({
          caption: '$UI_Common_OK',
          buttonAction: async btn => {
            console.log('OK');
            btn.close();
          },
          type: 'normal',
          theme: 'secondary'
        }),
        UIButton.create({
          caption: '$UI_Common_Cancel',
          buttonAction: async btn => {
            console.log('Cancel');
            btn.close();
          },
          type: 'normal',
          theme: 'secondary'
        })
      ]
    );
  }
}
