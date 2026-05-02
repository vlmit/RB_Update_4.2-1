import {
  FieldType,
  Flags,
  Guid,
  TypedField,
  ValidationResult,
  ValidationResultType
} from '@tessa/core';
import { extension, inject, localize } from '@tessa/application';
import {
  CardHelper,
  CardNewRequest,
  CardRequest,
  CardRow,
  CardRowState,
  CardStoreMode,
  CardTypeFlags,
  ICardMetadataRepository,
  ICardMetadataRepository$,
  ICardService,
  ICardService$,
  IKrTypesCache,
  IKrTypesCache$,
  KrComponents,
  KrComponentsHelper,
  KrPermissionFlagDescriptors,
  KrToken,
  SchemeType,
  ViewCriteriaOperators,
  ViewParameterMetadata,
  ViewRequestParameterBuilder
} from '@tessa/platform';
import { UIContext, showNotEmpty, createCardModel, UIButton, LoadingOverlayHelper } from 'tessa/ui';
import {
  TileExtension,
  ITileGlobalExtensionContext,
  Tile,
  TileGroups,
  TileEvaluationEventArgs,
  ITileLocalExtensionContext,
  disableWithCollapsing,
  openMarkedCard
} from 'tessa/ui/tiles';
import { showView, showFormDialog } from 'tessa/ui/uiHost';
import { ICardModel, ICardEditorModel } from 'tessa/ui/cards';

@extension({ name: 'AcquaintanceTileExtension' })
export class AcquaintanceTileExtension extends TileExtension {
  //#region ctor

  constructor(
    @inject(IKrTypesCache$) private readonly _krTypesCache: IKrTypesCache,
    @inject(ICardService$) private readonly _cardService: ICardService,
    @inject(ICardMetadataRepository$) private readonly _cardMetadata: ICardMetadataRepository
  ) {
    super();
  }

  //#endregion

  //#region TileExtension

  async initializingGlobal(context: ITileGlobalExtensionContext): Promise<void> {
    const panel = context.workspace.leftPanel;
    const contextSource = panel.contextSource;

    const tile = new Tile({
      name: 'AcquaintanceGroup',
      caption: '$KrTiles_AcquaintanceGroup',
      contextSource,
      group: TileGroups.CardsTop,
      order: 28,
      evaluating: AcquaintanceTileExtension.enableOnCardUpdateAndNotTaskCard,
      tiles: [
        new Tile({
          name: 'Acquaintance',
          caption: '$KrTiles_Acquaintance',
          contextSource,
          command: () => this.showAcquaintanceWindow(),
          group: TileGroups.CardsTop,
          order: 1,
          evaluating: AcquaintanceTileExtension.enableOnCardUpdateAndNotTaskCard
        }),

        new Tile({
          name: 'AcquaintanceHistory',
          caption: '$KrTiles_AcquaintanceHistory',
          contextSource,
          command: () => this.openAcquaintanceHistoryView(),
          group: TileGroups.CardsTop,
          order: 2,
          evaluating: AcquaintanceTileExtension.enableOnCardUpdateAndNotTaskCard
        })
      ]
    });

    tile.info['.actionsGrouping'] = true;
    panel.tiles.push(tile);
  }

  async initializingLocal(context: ITileLocalExtensionContext): Promise<void> {
    const panel = context.workspace.leftPanel;
    const editor = panel.context.cardEditor;
    const acquaintanceGroup = panel.tryGetTile('AcquaintanceGroup');
    if (
      !!editor &&
      !!editor.cardModel &&
      !!acquaintanceGroup &&
      (!(await this.typeSupportsWorkflow(editor.cardModel)) ||
        !(await this.canUseResolutions(editor.cardModel)))
    ) {
      disableWithCollapsing(acquaintanceGroup);
    }
  }

  //#endregion

  //#region evaluatings

  private static enableOnCardUpdateAndNotTaskCard(e: TileEvaluationEventArgs) {
    const editor = e.currentTile.context.cardEditor;
    e.setIsEnabledWithCollapsing(
      e.currentTile,
      !!editor &&
        !!editor.cardModel &&
        editor.cardModel.card.storeMode === CardStoreMode.Update &&
        editor.cardModel.cardType.id !== CardHelper.WfTaskCardTypeID
    );
  }

  //#endregion

  //#region methods

  private async showAcquaintanceWindow() {
    const context = UIContext.current;
    const cardEditor = context.cardEditor;

    if (!cardEditor || !cardEditor.cardModel) {
      return;
    }

    const mainCardModel = cardEditor.cardModel;
    const cardIsNew = mainCardModel.card.storeMode === CardStoreMode.Insert;

    if (
      !cardIsNew &&
      ((await mainCardModel.hasChanges()) || this.notEnoughPermissions(mainCardModel))
    ) {
      await openMarkedCard(
        'kr_calculate_resolution_permissions',
        null, // Не требуем подтверждения действия, если не было изменений
        async () => true, // Автоматом подтверждаем сохранение
        async () => {
          if (this.notEnoughPermissions(cardEditor.cardModel!)) {
            await showNotEmpty(
              ValidationResult.fromText(
                `${localize('$KrMessages_NoPermissionsTo')}\n${localize(
                  '$KrPermissions_CreateResolutions'
                )}`,
                ValidationResultType.Error
              )
            );
            return false;
          }

          this.openRolesDialog(cardEditor);
          return true;
        }
      );
    } else {
      this.openRolesDialog(cardEditor);
    }
  }

  private openAcquaintanceHistoryView() {
    const context = UIContext.current;
    const cardEditor = context.cardEditor;

    if (cardEditor && cardEditor.cardModel) {
      const cardId = cardEditor.cardModel.card.id;

      const parameterMetadata = new ViewParameterMetadata();
      parameterMetadata.alias = 'CardIDParam';
      parameterMetadata.caption = '$Views_Acquaintance_CardID';
      parameterMetadata.hidden = true;
      parameterMetadata.schemeType = SchemeType.Guid;
      parameterMetadata.multiple = false;

      const parameters = [
        new ViewRequestParameterBuilder()
          .withMetadata(parameterMetadata)
          .addCriteria(ViewCriteriaOperators.EqualsTo, cardId, cardId)
          .asRequestParameter()
      ];

      showView({
        viewAlias: 'AcquaintanceHistory',
        displayValue: '$Views_AcquaintanceHistory',
        parameters
      });
    }
  }

  private async canUseResolutions(model: ICardModel): Promise<boolean> {
    const usedComponents = await KrComponentsHelper.getKrComponentsByCard(
      model.card,
      this._krTypesCache
    );
    return Flags.hasFlag(usedComponents, KrComponents.Resolutions);
  }

  private async typeSupportsWorkflow(model: ICardModel): Promise<boolean> {
    const cardTypes = await this._krTypesCache.getCardTypes();
    return (
      Flags.hasFlag(model.cardType.flags, CardTypeFlags.AllowTasks) &&
      cardTypes.some(x => Guid.equals(x.id, model.cardType.id))
    );
  }

  private notEnoughPermissions(model: ICardModel): boolean {
    const krToken = KrToken.tryGet(model.card.info);
    return !!krToken && !krToken.hasPermission(KrPermissionFlagDescriptors.CreateResolutions);
  }

  private async openRolesDialog(editor: ICardEditorModel) {
    const model = editor.cardModel;
    if (!model) {
      return;
    }

    // Запрашиваем информируемых по умолчанию
    const defaultRolesRequest = new CardRequest();
    defaultRolesRequest.requestType = '7a2cb692-7a55-4519-b193-0ce2de435523'; // GetDefaultAcquaintanceRoles
    defaultRolesRequest.cardId = model.card.id;

    const defaultRolesResponse = await this._cardService.request(defaultRolesRequest);
    const defaultRolesResult = defaultRolesResponse.validationResult.build();
    await showNotEmpty(defaultRolesResult);
    if (!defaultRolesResult.isSuccessful) {
      return;
    }

    const cardMetadata = await this._cardMetadata.getCardMetadata();
    // Построение диалога
    const dialogsType = cardMetadata.cardTypes.getCardTypeByName('Dialogs');
    if (!dialogsType) {
      return;
    }

    await LoadingOverlayHelper.ensureCardTypeDataLoaded(dialogsType);

    const dialogForm = dialogsType.forms.find(x => x.name === 'Acquaintance');
    if (!dialogForm) {
      return;
    }

    const request = new CardNewRequest();
    request.cardTypeId = dialogsType.id;
    const response = await this._cardService.create(request);
    response.card.id = Guid.newGuid();
    await showNotEmpty(response.validationResult.build());
    if (!response.validationResult.isSuccessful) {
      return;
    }

    const windowCardModel = await createCardModel(response.card, response.sectionRows);

    // Получаем список ролей для ознакомления по умолчанию
    const defaultRoles: { id: string; name: string }[] = [];
    const defaultRolesResponseInfo = defaultRolesResponse.info;
    const defaultRolesDictionary = defaultRolesResponseInfo['.DefaultRoles'];
    if (defaultRolesDictionary) {
      const idsList = defaultRolesDictionary['IDList'] as TypedField<FieldType.Guid>[];
      const namesList = defaultRolesDictionary['NameList'] as TypedField<FieldType.String>[];
      if (idsList && namesList && idsList.length === namesList.length) {
        for (let i = 0; i < idsList.length; i++) {
          defaultRoles.push({
            id: TypedField.get(idsList[i]),
            name: TypedField.get(namesList[i])
          });
        }
      }
    }

    const rolesRows = windowCardModel.card.sections.get('DialogRoles').rows;
    for (const role of defaultRoles) {
      const roleRow = new CardRow();
      roleRow.rowId = Guid.newGuid();
      roleRow.set('RoleID', role.id, FieldType.Guid);
      roleRow.set('RoleName', role.name, FieldType.String);
      roleRow.state = CardRowState.None;
      rolesRows.push(roleRow);
    }

    await showFormDialog(
      dialogForm,
      windowCardModel,
      null,
      [
        UIButton.create({
          caption: '$UI_Common_OK',
          buttonAction: async btn => {
            if (rolesRows.length === 0) {
              await showNotEmpty(
                ValidationResult.fromText(
                  '$KrMessages_Acquaintance_RolesRequired',
                  ValidationResultType.Warning
                )
              );
              return;
            }

            // Получаем комментарий
            const comment =
              windowCardModel.card.sections.get('Dialogs').fields.get<string>('Comment') ?? '';

            const card = model.card;
            const cardId = card.id;

            // Запрос на отправку данных для массового ознакомления
            const sendAcquaintanceRequest = new CardRequest();
            sendAcquaintanceRequest.requestType = '87e36c4a-0cb5-4226-8580-c339b4bbf2b7'; // Acquaintance
            sendAcquaintanceRequest.cardId = cardId;

            sendAcquaintanceRequest.info['.Comment'] = TypedField.createString(comment);
            sendAcquaintanceRequest.info['.Roles'] = rolesRows.map(x => x.getField('RoleID'));
            sendAcquaintanceRequest.info['.ExcludeDeputies'] = TypedField.falseBoolean;
            sendAcquaintanceRequest.info['.AddSuccessMessage'] = TypedField.trueBoolean;

            const sendAcquaintanceResponse =
              await this._cardService.request(sendAcquaintanceRequest);
            await showNotEmpty(sendAcquaintanceResponse.validationResult.build());
            if (!sendAcquaintanceResponse.validationResult.isSuccessful) {
              return;
            }

            btn.close();
          },
          type: 'normal',
          theme: 'primary'
        }),
        UIButton.create({
          caption: '$UI_Common_Cancel',
          buttonAction: btn => {
            btn.close();
          },
          type: 'normal',
          theme: 'secondary'
        })
      ],
      {
        showFullscreenButton: true,
        title: localize(dialogForm.tabCaption ?? '$CardTypes_Tabs_Acquaintance')
      },
      undefined,
      undefined,
      { type: 'controls' }
    );
  }

  //#endregion
}
