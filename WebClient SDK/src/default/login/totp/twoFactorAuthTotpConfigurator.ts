import { inject, injectable } from '@tessa/application';
import {
  FieldStorageMap,
  FieldType,
  IStorage,
  StorageAccessor,
  TypedField,
  ValidationResultType
} from '@tessa/core';
import { Card, CardHelper, CardRequest, ICardService, ICardService$ } from '@tessa/platform';
import { Visibility } from 'tessa/platform/visibility';
import { ValidationResult } from 'tessa/platform/validation';
import { showLoadingOverlay } from 'tessa/ui/loadingOverlay';
import { showNotEmpty } from 'tessa/ui/tessaDialog/showNotEmpty';
import { ICardModel, IControlViewModel, IFormViewModelBase } from 'tessa/ui/cards';
import { ButtonViewModel } from 'tessa/ui/cards/controls/buttonViewModel';
import { ITwoFactorAuthConfigurator } from 'tessa/ui/login/component/twoFactor/twoFactorAuthConfigurator';

/**
 * Предоставляет объект, который выполняет конфигурацию настроек типа
 * двухфакторной аутентификации с использованием одноразового пароля на основе времени.
 */
@injectable()
export class TwoFactorAuthTotpConfigurator implements ITwoFactorAuthConfigurator {
  //#region fields

  private _fields: FieldStorageMap | null | undefined;

  //#endregion

  //#region constructors

  constructor(@inject(ICardService$) private readonly _cardService: ICardService) {}

  //#endregion

  //#region ITwoFactorAuthConfigurator implementation

  public settingsName: string | null = CardHelper.TwoFactorAuthTotpTypeName;

  public async setTypeSettings(card: Card, storage: IStorage | null): Promise<void> {
    this._fields = card.sections.tryGet('TwoFactorAuthTotpSettings')?.fields;
    if (!this._fields) {
      return;
    }

    const sa = new StorageAccessor(storage ?? {});
    let key = sa.tryGetString('Key');
    let uri = sa.tryGetString('Uri');

    if (!key) {
      [key, uri] = await this.generateCode(card.id);
    }

    this._fields.set('Key', key, FieldType.String);
    this._fields.set('Uri', uri, FieldType.String);
  }

  public async modifySettingsModel(
    _form: IFormViewModelBase,
    cardModel: ICardModel
  ): Promise<void> {
    const uriCtrl = cardModel.controls.get('Uri');
    const instructionsCtrl = cardModel.controls.get('InstructionsLabel');
    const generateCtrl = cardModel.controls.get('Generate') as ButtonViewModel;
    if (!uriCtrl || !instructionsCtrl || !generateCtrl) {
      return;
    }

    if (!this._fields?.getString('Uri')) {
      this.setControlsVisibility(Visibility.Collapsed, instructionsCtrl, uriCtrl);
    }

    generateCtrl.onClick = async () => {
      const [key, uri] = await this.generateCode(cardModel.card.id);
      if (key && uri) {
        this._fields?.set('Key', key, FieldType.String);
        this._fields?.set('Uri', uri, FieldType.String);
        this.setControlsVisibility(Visibility.Visible, instructionsCtrl, uriCtrl);
      }
    };
  }

  public async getTypeSettings(): Promise<IStorage | null> {
    const key = this._fields?.tryGetString('Key');
    const uri = this._fields?.tryGetString('Uri');

    return key
      ? {
          ['Key']: TypedField.createString(key),
          ['Uri']: uri ? TypedField.createString(uri) : null
        }
      : null;
  }

  public async validateTypeSettings(): Promise<ValidationResult> {
    return this._fields?.tryGetString('Key')
      ? ValidationResult.empty
      : ValidationResult.fromText(
          '$CardTypes_Validators_TwoFactorAuthTotpKey',
          ValidationResultType.Error
        );
  }

  //#endregion

  //#region IDisposable implementation

  public dispose(): void {
    this._fields = null;
  }

  //#endregion

  //#region private methods

  private async generateCode(cardId: string): Promise<[string | null, string | null]> {
    return await showLoadingOverlay(async () => {
      const request = new CardRequest();
      request.requestType = 'd9c5d25b-d1f8-487a-b317-8de42980dbd8';
      request.cardId = cardId;

      const response = await this._cardService.request(request);
      if (await showNotEmpty(response.validationResult.build())) {
        return [null, null];
      }

      const sa = new StorageAccessor(response.tryGetInfo() ?? {});
      return [sa.tryGetString('Key'), sa.tryGetString('Uri')];
    });
  }

  private setControlsVisibility(visibility: Visibility, ...controls: IControlViewModel[]): void {
    for (const control of controls) {
      control.controlVisibility = visibility;
    }
  }

  //#endregion
}
