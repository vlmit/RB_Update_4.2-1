import { computed } from 'mobx';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { DoubleClickInfo, IWorkplaceViewComponent } from 'tessa/ui/views';
import { ContentPlaceArea, ContentPlaceOrder, ViewButtonViewModel } from 'tessa/ui/views/content';
import { IStorage } from 'tessa/platform/storage';
import {
  showError,
  showLoadingOverlay,
  showNotEmpty,
  tryGetFromInfo,
  tryGetFromSettings,
  UIContext
} from 'tessa/ui';
import { createCard } from 'tessa/ui/uiHost';
import { ViewReferenceMetadataSealed } from 'tessa/views/metadata';
import {
  ITessaViewResult,
  RequestParameterBuilder,
  TessaViewRequest,
  ViewRowHelper
} from 'tessa/views';
import { CardRequest, CardService } from 'tessa/cards/service';
import { ValidationResult, ValidationResultBuilder } from 'tessa/platform/validation';
import { createTypedField, DotNetType } from 'tessa/platform';
import { KrTypesCache } from 'tessa/workflow';
import { AdvancedCardDialogManager } from 'tessa/ui/cards';
import { extension, localize } from '@tessa/application';
import { ViewCriteriaOperators } from '@tessa/platform';

@extension()
export class CreateCardExtension extends WorkplaceViewComponentExtension {
  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Views.CreateCardExtension';
  }

  public initialize(model: IWorkplaceViewComponent): void {
    // if (!model.inSelectionMode()) {
    model.contentFactories.set(
      'CreateCardExtension',
      c => new CreateCardButtonViewModel(new CreateCardExtensionSettings(this.settingsStorage), c)
    );
    // }
  }
}

//#region CreateCardExtensionSettings

enum CardCreationKind {
  ByTypeFromSelection,
  ByTypeAlias,
  ByDocTypeIdentifier
}

enum CardOpeningKind {
  ApplicationTab,
  ModalDialog
}

class CreateCardExtensionSettings {
  constructor(storage: IStorage) {
    this.cardCreationKind =
      storage['CardCreationKind'] != null
        ? CardCreationKind[storage['CardCreationKind'] as string]
        : CardCreationKind.ByTypeFromSelection;
    this.cardOpeningKind =
      storage['CardOpeningKind'] != null
        ? CardOpeningKind[storage['CardOpeningKind'] as string]
        : CardOpeningKind.ApplicationTab;
    this.typeAlias = storage['TypeAlias'] || '';
    this.docTypeIdentifier = storage['DocTypeIdentifier'] || '';
    this.idParam = storage['IDParam'] || '';
    this.displayValue = storage['DisplayValue'] || '';
    this.openInFullscreen = tryGetFromSettings(storage, 'OpenInFullscreen', false);
    this.openOnlyFirstTab = tryGetFromSettings(storage, 'OpenOnlyFirstTab', false);
  }

  public readonly cardCreationKind: CardCreationKind;
  public readonly cardOpeningKind: CardOpeningKind;
  public readonly typeAlias: string;
  public readonly docTypeIdentifier: string;
  public readonly idParam: string;
  public readonly displayValue: string;

  /** Open in fullscreen. */
  public readonly openInFullscreen: boolean;

  /** Open first tab only. */
  public readonly openOnlyFirstTab: boolean;
}

//#endregion

//#region CreateCardButtonViewModel

export class CreateCardButtonViewModel extends ViewButtonViewModel {
  //#region ctor

  constructor(
    settings: CreateCardExtensionSettings,
    viewComponent: IWorkplaceViewComponent,
    area: ContentPlaceArea = ContentPlaceArea.ToolBarPanel,
    order: number = ContentPlaceOrder.Middle
  ) {
    super(viewComponent, area, order);
    this._settings = settings;

    this._caption = localize('$Views_CreateCardExtension_ButtonCaption');
    this._captionPosition = 'after';
    this._icon = 'm-plus';

    this._tooltip =
      settings.cardCreationKind === CardCreationKind.ByTypeFromSelection
        ? '$Views_CreateCardExtension_Selection_ToolTip'
        : '$Views_CreateCardExtension_SpecifiedType_ToolTip';

    this._onClick = () => this.createCard();

    this._type = 'small';
    this._theme = 'control';
    this._name = 'CreateCardCopyButton';
  }

  //#endregion

  //#region fields
  private _settings: CreateCardExtensionSettings;

  //#endregion

  //#region props

  @computed
  public get canCreateCard(): boolean {
    switch (this._settings.cardCreationKind) {
      case CardCreationKind.ByTypeFromSelection:
        return this.viewComponent.selectedRow != null && !this.viewComponent.isDataLoading;
      case CardCreationKind.ByTypeAlias:
        return !!this._settings.typeAlias && !this.viewComponent.isDataLoading;
      case CardCreationKind.ByDocTypeIdentifier:
        return (
          !!this._settings.docTypeIdentifier &&
          !!KrTypesCache.instance.docTypes.find(x => x.id === this._settings.docTypeIdentifier) &&
          !this.viewComponent.isDataLoading
        );
      default:
        return false;
    }
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

    switch (this._settings.cardCreationKind) {
      case CardCreationKind.ByTypeFromSelection:
        return this.createCardActionBySelectedRow();
      case CardCreationKind.ByTypeAlias:
        return this.createCardActionByTypeAlias();
      case CardCreationKind.ByDocTypeIdentifier:
        return this.createCardActionByDocTypeIdentifier();
      default:
        return;
    }
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
      let docTypeId: string | null = null;
      let docTypeTitle: string | null = null;

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

        if (cardTypeId) {
          docTypeId = tryGetFromInfo<string | null>(response.info, 'docTypeID');
          docTypeTitle = !!docTypeId
            ? tryGetFromInfo<string | null>(response.info, 'docTypeTitle')
            : null;
        }
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
        const info = docTypeId
          ? {
              docTypeID: createTypedField(docTypeId, DotNetType.Guid),
              docTypeTitle: createTypedField(docTypeTitle, DotNetType.String)
            }
          : {};

        await this.createCardInternal(cardTypeId!, undefined, this._settings.displayValue, info);
        return;
      }
    }

    await showError('$Views_CreateCardExtension_ErrorGettingType');
  }

  private async createCardActionByDocTypeIdentifier() {
    const docTypeId = this._settings.docTypeIdentifier;
    if (!docTypeId) {
      return;
    }

    const docType = KrTypesCache.instance.docTypes.find(x => x.id === docTypeId);
    if (!docType) {
      return;
    }

    const info = {
      docTypeID: createTypedField(docTypeId, DotNetType.Guid),
      docTypeTitle: createTypedField(docType.caption, DotNetType.String)
    };

    await this.createCardInternal(docType.cardTypeId, undefined, this._settings.displayValue, info);
  }

  private async createCardActionByTypeAlias() {
    const typeName = this._settings.typeAlias;
    if (!typeName) {
      return;
    }

    await this.createCardInternal(undefined, typeName, this._settings.displayValue);
  }

  private async createCardInternal(
    cardTypeId?: string,
    cardTypeName?: string,
    displayValue?: string,
    info?: IStorage
  ) {
    const context = this.viewComponent.workplace.context;
    const contextInstance = UIContext.create(context);
    try {
      const idParam = this._settings.idParam;
      const inSelectionMode = this.viewComponent.inSelectionMode();
      const idParamMeta = this.viewComponent.view!.metadata.parameters.get(idParam)!;
      const hasIdParam = !!idParamMeta;

      if (inSelectionMode && hasIdParam) {
        context.info['CreateAndSelectID'] = null;
      }
      await showLoadingOverlay(async splashResolve => {
        if (inSelectionMode || this._settings.cardOpeningKind === CardOpeningKind.ApplicationTab) {
          await createCard({
            cardTypeId,
            cardTypeName,
            context,
            info,
            openToTheRightOfSelectedTab: true,
            displayValue: displayValue,
            splashResolve
          });
        } else {
          await AdvancedCardDialogManager.instance.createCard({
            cardTypeId,
            cardTypeName,
            context,
            info,
            displayValue: displayValue,
            splashResolve,
            dialogOptions: {
              openInFullscreen: this._settings.openInFullscreen,
              showOnlyFirstTab: this._settings.openOnlyFirstTab
            }
          });
        }
      });

      if (inSelectionMode || this._settings.cardOpeningKind === CardOpeningKind.ModalDialog) {
        const createAndSelectId = tryGetFromInfo(context.info, 'CreateAndSelectID') as string;
        if (inSelectionMode && hasIdParam && createAndSelectId != undefined) {
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
