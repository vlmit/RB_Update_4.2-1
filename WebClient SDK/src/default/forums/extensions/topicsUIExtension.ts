import {
  CardSavingMode,
  CardSavingRequest,
  CardUIExtension,
  ICardModel,
  ICardUIExtensionContext
} from 'tessa/ui/cards';
import { ForumActionParameters, ForumDialogManager, ForumViewModel } from 'tessa/ui/cards/controls';
import {
  IUIContext,
  showConfirm,
  showConfirmWithCancel,
  showError,
  showMessage,
  tryGetFromInfo,
  UIContext
} from 'tessa/ui';
import { CardStoreMode } from 'tessa/cards';
import { createTypedField, DotNetType, TypedField } from 'tessa/platform';
import { IStorage } from 'tessa/platform/storage';
import { ViewParameterMetadata } from 'tessa/views/metadata';
import { ForumHelper, TopicModel } from 'tessa/forums';
import { RequestParameterBuilder } from 'tessa/views';
import { showView } from 'tessa/ui/uiHost';
import { DefaultFormMainViewModel } from 'tessa/ui/cards/forms';
import { extension } from '@tessa/application';
import { SchemeType } from 'tessa/scheme';
import { ViewCriteriaOperators, KrPermissionFlagDescriptors, KrToken } from '@tessa/platform';

@extension()
export class TopicsUIExtension extends CardUIExtension {
  //#region fields

  private _disposers: ((() => void) | null)[] = [];

  //#endregion

  //#region CardUIExtension

  public async initialized(context: ICardUIExtensionContext): Promise<void> {
    if (!context.validationResult.isSuccessful) {
      return;
    }

    const allForumControls = context.model.controlsBag.filter(
      x => x instanceof ForumViewModel
    ) as ForumViewModel[];
    for (const forumControl of allForumControls) {
      if (!forumControl.isLicenseValid) {
        continue;
      }

      if (forumControl.block.form.isCollapsed) {
        continue;
      }

      if (context.model.inSpecialMode) {
        forumControl.isAddTopicEnabled = false;
        forumControl.isEnabledForumEmptyContextMenu = true;
      }

      forumControl.openParticipantsAction = this.getOpenParticipantsAction(context);

      forumControl.checkAddTopicPermissionAction = this.getCheckAddTopicPermissionsAction(
        context,
        forumControl
      );

      forumControl.checkElevatedPermissionsAction = this.getCheckElevatedPermissionsAction(context);

      const topicId = tryGetFromInfo<string>(context.model.card.info, ForumHelper.TopicIDKey);
      const topicTypeId = tryGetFromInfo<string>(
        context.model.card.info,
        ForumHelper.TopicTypeIDKey
      );
      if (topicId && topicTypeId && forumControl.topicTypeId === topicTypeId) {
        this._disposers.push(
          forumControl.tabSelected.addOnce(async () => await forumControl.showTopic(topicId))
        );
        delete context.model.card.info[ForumHelper.TopicIDKey];
        delete context.model.card.info[ForumHelper.TopicTypeIDKey];
        const mainFrom = context.model.mainForm as DefaultFormMainViewModel;
        mainFrom.selectedTab = forumControl.block.form;
        return;
      }
    }
  }

  public finalized(): void {
    for (const f of this._disposers) {
      f?.();
    }
  }

  //#endregion

  //#region actions

  private getOpenParticipantsAction(context: ICardUIExtensionContext) {
    const openParticipantsAction: (
      params: ForumActionParameters,
      modifyOpenParticipantsAction: (params: ForumActionParameters) => void
    ) => Promise<void> = async (params, modifyOpenParticipants): Promise<void> => {
      modifyOpenParticipants && modifyOpenParticipants(params);

      const paramTopicMeta = new ViewParameterMetadata();
      paramTopicMeta.alias = 'TopicID';
      paramTopicMeta.schemeType = SchemeType.Guid;

      const paramTopicId = new RequestParameterBuilder()
        .withMetadata(paramTopicMeta)
        .addCriteria(ViewCriteriaOperators.EqualsTo, 'topicID', params.topicID!)
        .asRequestParameter();

      const paramCardMeta = new ViewParameterMetadata();
      paramCardMeta.alias = 'CardID';
      paramCardMeta.schemeType = SchemeType.Guid;

      const paramCardId = new RequestParameterBuilder()
        .withMetadata(paramCardMeta)
        .addCriteria(ViewCriteriaOperators.EqualsTo, 'cardID', context.card.id)
        .asRequestParameter();

      await showView({
        viewAlias: 'TopicParticipants',
        displayValue: '$Workplaces_User_TopicParticipants',
        parameters: [paramTopicId, paramCardId],
        treeVisible: false,
        modifyWorkplaceAfterCreate: async workplace => {
          workplace.context.info['.participantTypeId'] = params.participantsTypeID
            ? TypedField.createInt(params.participantsTypeID)
            : undefined;
        }
      });
    };

    return openParticipantsAction;
  }

  private getCheckAddTopicPermissionsAction(
    context: ICardUIExtensionContext,
    control: ForumViewModel
  ) {
    return async (): Promise<TopicModel | null> => {
      return await TopicsUIExtension.openMarkedCard(
        context.uiContext,
        'kr_calculate_addtopic_permissions',
        null, // Не требуем подтверждения действия, если не было изменений
        async cardIsNew =>
          cardIsNew
            ? (await showConfirm('$KrTiles_EditModeConfirmation'))
              ? true
              : null
            : await showConfirmWithCancel('$KrTiles_EditModeConfirmation'),
        async () => await this.addTopicShowDialog(context, control)
      );
    };
  }

  private getCheckElevatedPermissionsAction(context: ICardUIExtensionContext) {
    return async (): Promise<void> => {
      await TopicsUIExtension.openMarkedCard(
        context.uiContext,
        'kr_calculate_elevated_permissions',
        null, // Не требуем подтверждения действия, если не было изменений
        async cardIsNew =>
          cardIsNew
            ? (await showConfirm('$KrTiles_EditModeConfirmation'))
              ? true
              : null
            : await showConfirmWithCancel('$KrTiles_EditModeConfirmation'),
        async () => await this.elevatedPermissionsMessage()
      );
    };
  }

  private addTopicShowDialog = async (
    context: ICardUIExtensionContext,
    control: ForumViewModel
  ): Promise<TopicModel | null> => {
    const card = UIContext.current.cardEditor?.cardModel?.card;
    if (!card) {
      return null;
    }

    const token = KrToken.tryGet(card.info);
    if (token?.hasPermission(KrPermissionFlagDescriptors.AddTopics) == true) {
      const topic = await ForumDialogManager.instance.addTopicShowDialog(context.card.id, model =>
        control.modifyAddingTopicAction(model)
      );

      if (!topic) {
        return null;
      }

      const uiContext = UIContext.current;
      const cardEditor = uiContext.cardEditor!;

      // Добавление нового обсуждения происходит после вызова openMarkedCard, в котором карточка рефрешится,
      // из-за этого параметр control будет указывать на уже неактуальный контрол обсуждений.
      // для того чтобы отобразить добавленный топик добавляем параметры в карточку,
      // чтобы при рефреше расширение в initialized смогло правильно открыть добавленный топик
      await cardEditor.openCard({
        cardId: card.id,
        cardTypeId: card.typeId,
        cardTypeName: card.typeName,
        context: uiContext,
        cardModifierAction: ({ card }) => {
          card.info[ForumHelper.TopicIDKey] = topic.id;
          card.info[ForumHelper.TopicTypeIDKey] = topic.typeId;
        }
      });

      return topic;
    } else {
      await showError('$Forum_Permission_NoPermissionToAddTopic');
      return null;
    }
  };

  private elevatedPermissionsMessage = async () => {
    const card = UIContext.current.cardEditor?.cardModel?.card;
    if (!card) {
      return false;
    }

    const token = KrToken.tryGet(card.info);
    const superModerator =
      token?.hasPermission(KrPermissionFlagDescriptors.SuperModeratorMode) === true;
    const canEditAllMessages =
      token?.hasPermission(KrPermissionFlagDescriptors.EditAllMessages) === true;
    if (superModerator && canEditAllMessages) {
      await showMessage('$Forum_Permission_ElevatedPermissions_All');
      return false;
    }

    if (superModerator) {
      await showMessage('$Forum_Permission_ElevatedPermissions_SuperModerator');
      return false;
    }

    if (canEditAllMessages) {
      await showMessage('$Forum_Permission_ElevatedPermissions_EditingAllMessages');
      return false;
    }

    await showError('$Forum_Permission_NoRequiredPermissions');
    return false;
  };

  private static async openMarkedCard<T>(
    context: IUIContext,
    mark: string | null,
    proceedConfirmation: (() => Promise<boolean>) | null,
    proceedAndSaveCardConfirmation: ((cardIsNew: boolean) => Promise<boolean | null>) | null,
    continuationOnSuccessFunc: (() => Promise<T>) | null = null,
    getInfo: IStorage | null = null
  ): Promise<T | null> {
    const editor = context.cardEditor;
    let model: ICardModel;

    if (!editor || editor.operationInProgress || !(model = editor.cardModel!)) {
      return null;
    }

    const cardIsNew = model.card.storeMode === CardStoreMode.Insert;
    const hasChanges = cardIsNew || (await model.hasChanges());
    let saveCardBeforeOpening: boolean | null;

    if (hasChanges && proceedAndSaveCardConfirmation) {
      saveCardBeforeOpening = await proceedAndSaveCardConfirmation(cardIsNew);
      // Если не указана функция подтверждения с вариантом отмены - сохраняем карточку
      // если есть подтверждение основного действия
    } else if (hasChanges && proceedConfirmation) {
      saveCardBeforeOpening = (await proceedConfirmation()) ? true : null;
      // Если в карточке не было изменений - не вызываем сохранения
    } else if (proceedConfirmation) {
      saveCardBeforeOpening = (await proceedConfirmation()) ? false : null;
      // Если не указана функция подтверждения и нет изменений - вызываем основное действие
      // без подтверждения и сохранения
    } else {
      saveCardBeforeOpening = false;
    }

    if (saveCardBeforeOpening === null) {
      return null;
    }

    if (!getInfo) {
      getInfo = {};
    }

    if (mark) {
      getInfo[mark] = createTypedField(true, DotNetType.Boolean);
    }

    if (saveCardBeforeOpening) {
      const token = KrToken.tryGet(editor.info);
      KrToken.remove(editor.info);

      const saveSuccess = await editor.saveCard(
        context,
        {
          '.SaveWithPermissionsCalc': createTypedField(true, DotNetType.Boolean)
        },
        new CardSavingRequest(CardSavingMode.KeepPreviousCard)
      );

      if (!saveSuccess) {
        return null;
      }

      if (token) {
        token.setInfo(getInfo);
      }
    }

    const cardId = model.card.id;
    const cardType = model.cardType;

    const sendTaskSucceeded = await editor.openCard({
      cardId,
      cardTypeId: cardType.id!,
      cardTypeName: cardType.name!,
      context,
      info: getInfo
    });

    if (sendTaskSucceeded) {
      editor.isUpdatedServer = true;
    } else if (cardIsNew || saveCardBeforeOpening) {
      // если карточка новая или была сохранена, а также не удалось выполнить mark-действие при открытии,
      // то у нас будет "висеть" карточка с некорректной версией;
      // её надо обновить, на этот раз без mark'и

      await editor.openCard({
        cardId,
        cardTypeId: cardType.id!,
        cardTypeName: cardType.name!,
        context
      });
    }

    if (!continuationOnSuccessFunc) {
      return null;
    }

    const contextInstance = UIContext.create(context);
    try {
      return await continuationOnSuccessFunc();
    } finally {
      contextInstance.dispose();
    }
  }

  //#endregion
}
