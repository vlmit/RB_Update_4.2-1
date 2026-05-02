import { computed } from 'mobx';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { DoubleClickInfo, IWorkplaceViewComponent } from 'tessa/ui/views';
import { ContentPlaceArea, ContentPlaceOrder, ViewButtonViewModel } from 'tessa/ui/views/content';
import { IStorage } from 'tessa/platform/storage';
import {
  createCardEditorModel,
  LoadingOverlay,
  showError,
  showLoadingOverlay,
  showNotEmpty,
  tryGetFromInfo,
  UIContext
} from 'tessa/ui';
import { ViewReferenceMetadataSealed } from 'tessa/views/metadata';
import {
  ITessaViewResult,
  RequestParameterBuilder,
  TessaViewRequest,
  ViewRowHelper
} from 'tessa/views';
import { CardCopyRequest, CardRequest, CardService } from 'tessa/cards/service';
import { ValidationResult, ValidationResultBuilder } from 'tessa/platform/validation';
import { CardCreationInfo } from 'tessa/ui/cards/cardCreationInfo';
import { CardCreationMode, CardModelFlags } from 'tessa/ui/cards';
import { showCard } from 'tessa/ui/uiHost';
import { extension } from '@tessa/application';
import { ViewCriteriaOperators } from '@tessa/platform';

@extension()
export class CreateCardCopyExtension extends WorkplaceViewComponentExtension {
  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Views.CreateCardCopyExtension';
  }

  public initialize(model: IWorkplaceViewComponent): void {
    model.contentFactories.set(
      'CreateCardCopyExtension',
      c =>
        new CreateCardCopyButtonViewModel(
          new CreateCardCopyExtensionSettings(this.settingsStorage),
          c
        )
    );
  }
}

//#region CreateCardCopyExtensionSettings
class CreateCardCopyExtensionSettings {
  constructor(storage: IStorage) {
    this.idParam = storage['IDParam'] || '';
  }

  public readonly idParam: string;
}

//#endregion

//#region CreateCardCopyButtonViewModel

export class CreateCardCopyButtonViewModel extends ViewButtonViewModel {
  //#region ctor

  constructor(
    settings: CreateCardCopyExtensionSettings,
    viewComponent: IWorkplaceViewComponent,
    area: ContentPlaceArea = ContentPlaceArea.ToolBarPanel,
    order: number = ContentPlaceOrder.Middle
  ) {
    super(viewComponent, area, order);
    this._settings = settings;
    this.icon = 'icon-thin-251';
    this._tooltip = '$Views_CreateCardCopyExtension_Selection_ToolTip';
    this._caption = '$Views_CreateCardCopyExtension_Selection_Caption';
    this._showCaption = false;
    this._theme = 'control';
    this._type = 'small';
    this.onClick = () => this.createCard();
    this._name = 'CreateCardCopyButton';
  }

  //#endregion

  //#region fields

  private _settings: CreateCardCopyExtensionSettings;

  //#endregion

  //#region props

  @computed
  public get canCreateCard(): boolean {
    return this.viewComponent.selectedRow != null && !this.viewComponent.isDataLoading;
  }

  @computed
  public get isEnabled(): boolean {
    return this.canCreateCard && !this.isLoading;
  }

  //#endregion

  //#region methods

  public async createCard(): Promise<void> {
    if (!this.canCreateCard) {
      return;
    }

    return this.createCardActionBySelectedRow();
  }

  private async createCardActionBySelectedRow() {
    if (!this.viewComponent.selectedRow || !this.viewComponent.view) {
      return;
    }

    const references: ViewReferenceMetadataSealed[] = [];
    this.viewComponent.view.metadata.references.forEach(x => references.push(x));
    const ref = references.find(x => x.isCard && x.openOnDoubleClick);
    if (!ref) {
      return;
    }

    const row = this.viewComponent.selectedRow;
    const cardId = ViewRowHelper.getCardId(row, ref.colPrefix!);
    if (cardId) {
      let result: ValidationResult = ValidationResult.empty;
      let cardTypeId: string | null = null;

      const uiContextExecutor = this.viewComponent.workplace.uiContextExecutor;
      await uiContextExecutor(async () => {
        const request = new CardRequest();
        request.requestType = '6a7b57e9-5088-40c9-a4ca-75a489974a9c'; // GetDocTypeInfo
        request.cardId = cardId;
        const response = await CardService.instance.request(request);
        result = response.validationResult.build();

        cardTypeId = result.isSuccessful
          ? tryGetFromInfo<string | null>(response.info, 'cardTypeID')
          : null;
      });

      if (result.isSuccessful) {
        await showNotEmpty(result);
      } else {
        await showNotEmpty(
          new ValidationResultBuilder()
            .add(ValidationResult.fromText('$Views_CreateCardExtension_ErrorGettingType'))
            .add(result)
            .build()
        );
        return;
      }

      if (cardTypeId) {
        await this.createCardInternal(cardId, cardTypeId!, undefined);
        return;
      }
    }

    await showError('$Views_CreateCardExtension_ErrorGettingType');
  }

  private async createCardInternal(cardId: string, cardTypeId?: string, cardTypeName?: string) {
    const context = this.viewComponent.workplace.context;
    const contextInstance = UIContext.create(context);
    try {
      const idParam = this._settings.idParam;
      const inSelectionMode = this.viewComponent.inSelectionMode();
      const idParamMeta = this.viewComponent.view!.metadata.parameters.get(idParam)!;
      const hasIdParam = !!idParamMeta;

      const creationInfo = new CardCreationInfo(async () => {
        const request = new CardCopyRequest();
        request.sourceCardId = cardId;
        request.sourceCardTypeId = cardTypeId ?? null;
        request.sourceCardTypeName = cardTypeName ?? null;

        const response = await CardService.instance.copy(request);
        if (!response.validationResult.isSuccessful) {
          await showNotEmpty(response.validationResult.build());
          return null;
        }

        if (response.cancelOpening) {
          return null;
        }

        const editor = createCardEditorModel();
        const model = await editor.createAndInitializeModel({
          card: response.card,
          sectionRows: response.sectionRows,
          info: response.info,
          flags: CardModelFlags.Template
        });
        await editor.setCardModel(model);

        const newEditor = await showCard({ editor: editor });
        editor.workspaceName = '$UI_Common_DefaultDigest_NewCard';
        editor.operationStatusText = '$UI_Common_StatusBar_Status_CardIsCopied';
        return newEditor;
      }, CardCreationMode.Copy);

      LoadingOverlay.instance.show(async () => {
        await creationInfo.createCard();
      });

      if (inSelectionMode) {
        const createAndSelectId = tryGetFromInfo(context.info, 'CreateAndSelectID') as string;
        if (hasIdParam && createAndSelectId !== undefined) {
          const request = new TessaViewRequest(this.viewComponent.view!.metadata);
          const idParameter = new RequestParameterBuilder()
            .withMetadata(idParamMeta)
            .addCriteria(ViewCriteriaOperators.EqualsTo, createAndSelectId, createAndSelectId)
            .asRequestParameter();

          request.parameters.push(idParameter);

          let result!: ITessaViewResult;
          await showLoadingOverlay(async () => {
            result = await this.viewComponent.view!.getData(request);
          });

          if (result.rows.length === 1 && this.viewComponent.workplace.doubleClickAction) {
            const dinfo = new DoubleClickInfo();
            dinfo.view = this.viewComponent.view!.metadata;
            dinfo.context = context;
            dinfo.selectedObject = ViewRowHelper.convertRowToMap(result.columns, result.rows[0]);
            await this.viewComponent.workplace.doubleClickAction(dinfo);
            return;
          }
        }
      }

      await this.viewComponent.refreshView();
    } finally {
      contextInstance.dispose();
    }
  }

  //#endregion
}

//#endregion
