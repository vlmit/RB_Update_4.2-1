import { IUIContext, showError, showNotEmpty, UIContext } from 'tessa/ui';
import {
  enableWhenVisibleInCardHandler,
  ITileGlobalExtensionContext,
  ITileLocalExtensionContext,
  Tile,
  TileEvaluationEventArgs,
  TileExtension,
  TileGroups,
  TileHotkey
} from 'tessa/ui/tiles';
import {
  CardGetRestrictionFlags,
  CardRowState,
  CardStoreMode,
  CardTask,
  CardTaskAction
} from 'tessa/cards';
import { extension, localize } from '@tessa/application';
import {
  Card,
  CardGetRequest,
  CardHelper,
  CardStoreRequest,
  ICardMetadata,
  ICardMetadata$,
  ICardService,
  ICardService$
} from '@tessa/platform';
import { IFormDialogManager$ } from 'tessa/ui/formEditor/injects';
import {
  IFormDialogManager,
  IRuntimeBlockViewModel,
  IRuntimeItemViewModel,
  IRuntimeItemWithStateViewModel
} from 'tessa/ui/formEditor/types';
import { isItemWithStates, isRuntimeBlock } from 'tessa/ui/formEditor/typeGuards';
import { IDataProvider } from 'tessa/ui/formEditor/data/definitions';
import { Button } from 'ui/button/buttonViewModel';
import { FormattingHelper, Guid, IStorage, StorageArray, TypedField } from '@tessa/core';
import Decimal from 'decimal.js';
import { ICardEditorModel } from 'tessa/ui/cards';
import { reaction } from 'mobx';
import Moment from 'moment';

/**
 * Плитки для бизнес-процессов Workflow.
 */
@extension()
export class WfTileExtension extends TileExtension {
  constructor(
    @IFormDialogManager$() private readonly _layoutDialogManager: IFormDialogManager,
    @ICardService$() private readonly _cardService: ICardService,
    @ICardMetadata$() private readonly _cardMetadata: ICardMetadata
  ) {
    super();
  }
  public async initializingGlobal(context: ITileGlobalExtensionContext): Promise<void> {
    const panel = context.workspace.leftPanel;
    const contextSource = panel.contextSource;

    panel.tiles.push(
      new Tile({
        name: 'WfCreateResolution',
        caption: '$WfTiles_CreateResolution',
        icon: 'ta icon-thin-091',
        contextSource,
        command: () => this.createWfResolutionAction(),
        group: TileGroups.CardsTop,
        order: 50,
        evaluating: enableWhenVisibleInCardHandler,
        toolTip: '$WfTiles_CreateResolution_ToolTip'
      })
    );
  }

  public initializingLocal(context: ITileLocalExtensionContext): void {
    const panel = context.workspace.leftPanel;
    const panelContext = panel.context;
    if (!panelContext.cardEditor || !panelContext.cardEditor.cardModel) {
      return;
    }

    const hotkeyStorage = panel.contextSource.hotkeyStorage;

    const createResolution = panel.tryGetTile('WfCreateResolution');
    if (createResolution) {
      hotkeyStorage.addTileHotkey(
        new TileHotkey(createResolution, 'Ctrl+Alt+R', 'KeyR', { ctrl: true, alt: true })
      );
    }

    const notificationSubscriptions = panel.tryGetTile('NotificationSubscriptions');
    if (notificationSubscriptions) {
      notificationSubscriptions.evaluating.add(WfTileExtension.enableOnCardIsNotTaskCard);
    }

    const createFileTemplate = panel.tryGetTile('CreateFileFromTemplate');
    if (createFileTemplate) {
      createFileTemplate.evaluating.add(WfTileExtension.enableOnCardIsNotTaskCard);
    }

    // const copyLink = panel.tryGetTile('CopyCardLink');
    // if (copyLink) {
    //   copyLink.evaluating.add(e => {
    //     const editor = e.currentTile.context.cardEditor;
    //     e.setIsEnabledWithCollapsing(e.currentTile,
    //       !!editor
    //       && !!editor.cardModel
    //       && (editor.cardModel.cardType.id !== CardHelper.WfTaskCardTypeID
    //         || tryGetFromInfo(editor.cardModel.card.tryGetInfo()!, 'VirtualMainCardID', true)));
    //   });
    // }
  }

  private async createWfResolutionAction() {
    const context = UIContext.current;
    const editor = context.cardEditor;
    let card = editor?.cardModel?.card;

    if (!editor || !editor.cardModel || !card) {
      return;
    }

    const cardIsNew = card.storeMode === CardStoreMode.Insert;
    if (cardIsNew) {
      const saved = await editor.saveCard(context);
      if (!saved) {
        return;
      }

      card = editor.cardModel.card!;
    }

    await this._layoutDialogManager.showDialog({
      form: 'SendToPerformer',
      initializeAction: ({ rootDataProvider, children }, closeFunc) => {
        const rootBlock = children?.[0] as IRuntimeBlockViewModel;
        if (!rootDataProvider || !rootBlock) {
          return;
        }

        WfTileExtension.subscribeToFieldChanged(rootBlock, rootDataProvider);

        const acceptBtn = rootBlock
          .getItem<IRuntimeItemWithStateViewModel>('Accept')
          ?.getCurrent<Button>();
        const cancelBtn = rootBlock
          .getItem<IRuntimeItemWithStateViewModel>('Cancel')
          ?.getCurrent<Button>();
        if (!acceptBtn) {
          return;
        }

        acceptBtn.buttonAction = async () => {
          return this.acceptAction(editor, context, rootDataProvider, card, closeFunc);
        };
        cancelBtn!.buttonAction = () => {
          closeFunc(false);
        };
      },
      onDialogClosing: async result => {
        return result != null;
      },
      dialogComponentProps: { chromeSettings: { showCloseButton: false } }
    });
  }

  private async acceptAction(
    editor: ICardEditorModel,
    context: IUIContext,
    rootDataProvider: IDataProvider,
    card: Card,
    closeFunc: (result: unknown) => void
  ) {
    const performers = rootDataProvider.getValue('Performers') as { id: string; name: string }[];
    if (!performers.length) {
      return;
    }

    const planned = rootDataProvider.getValue('Planned') as string;
    const durationInDays = rootDataProvider.getValue<Decimal>('DurationInDays');
    if ((!durationInDays || durationInDays.toNumber() <= 0) && !planned) {
      await showError('$WfResolution_Error_ResolutionHasNoPlannedDate');
      return;
    }

    if (planned && Moment(planned).isBefore(Moment.now())) {
      await showError(
        localize(
          '$WfResolution_Error_ResolutionCantBePlannedInThePast',
          FormattingHelper.formatDateTimeWithoutSeconds(planned),
          FormattingHelper.formatDateTimeWithoutSeconds(Moment.now())
        )
      );
      return;
    }

    const freshCard = await this.startProcess(card);
    if (!freshCard) {
      return;
    }

    await this.completeProjectTask(rootDataProvider, performers, freshCard, card.tasks);
    await editor.refreshCard(context);
    closeFunc(true);
  }

  private async startProcess(card: Card) {
    if (
      !(await this.storeCard(card.clone(), {
        '.startProcess': TypedField.createString('WfResolution')
      }))
    ) {
      return null;
    }

    const getRequest = new CardGetRequest();
    getRequest.cardId = card.id;
    getRequest.cardTypeId = card.typeId;
    getRequest.cardTypeName = card.typeName;
    getRequest.restrictionFlags =
      CardGetRestrictionFlags.RestrictFiles | CardGetRestrictionFlags.RestrictTaskCalendar;
    const getResponse = await this._cardService.get(getRequest);
    await showNotEmpty(getResponse.validationResult.build());
    if (!getResponse.validationResult.isSuccessful) {
      return null;
    }

    return getResponse.card;
  }

  private async storeCard(card: Card, info: IStorage = {}) {
    const storeRequest = new CardStoreRequest();
    storeRequest.card = card;
    storeRequest.info = info;
    const response = await this._cardService.store(storeRequest);
    await showNotEmpty(response.validationResult.build());

    return response.validationResult.isSuccessful;
  }

  private async completeProjectTask(
    dataProvider: IDataProvider,
    perfs: { id: string; name: string }[],
    card: Card,
    oldTasks: StorageArray<CardTask>
  ) {
    const newResolution = card.tasks.find(
      t =>
        Guid.equals(t.typeId, 'c989d91f-7ddd-455c-ae16-3bb380132ba8') &&
        !oldTasks.some(tt => tt.card.id === t.card.id)
    );
    if (!newResolution) {
      return false;
    }

    newResolution.state = CardRowState.Deleted;
    newResolution.action = CardTaskAction.Complete;
    newResolution.optionId = 'f4ebe563-14f6-4b20-a61f-0bac4c11c8ac';
    const performers = newResolution.card.sections.get('WfResolutionPerformers')!;
    for (const performer of perfs) {
      const newRow = performers.rows.add();
      newRow.rowId = Guid.newGuid();
      newRow.state = CardRowState.Inserted;
      newRow.set('RoleID', TypedField.createGuid(performer.id));
      newRow.set('RoleName', TypedField.createString(performer.name));
      newRow.set('Order', TypedField.createInt(performers.rows.length - 1));
    }

    const resolutionSection = newResolution.card.sections.get('WfResolutions')!;
    const durationInDays = dataProvider.getValue<Decimal | null>('DurationInDays');
    resolutionSection.fields.set(
      'DurationInDays',
      durationInDays ? TypedField.createDouble(durationInDays.toNumber()) : null
    );

    resolutionSection.fields.set(
      'MassCreation',
      TypedField.createBoolean(dataProvider.getValue('MultiplePerformers') ?? false)
    );
    resolutionSection.fields.set(
      'MajorPerformer',
      TypedField.createBoolean(dataProvider.getValue('MajorPerformer') ?? false)
    );
    resolutionSection.fields.set(
      'WithControl',
      TypedField.createBoolean(dataProvider.getValue('WithControl') ?? false)
    );
    const planned = dataProvider.getValue<string | null>('Planned');
    resolutionSection.fields.set('Planned', planned ? TypedField.createDateTime(planned) : null);

    const comment = dataProvider.getValue<string | null>('Comment');
    if (comment) {
      resolutionSection.fields.set('Comment', TypedField.createString(comment));
    }

    const kindValue = dataProvider.getValue<{ id: string; name: string }[]>('Kind');
    // check for array length because it might be an empty array
    // and mobx logs an "out of bounds" warning. Same for 'From' and 'Controller'
    const kind = kindValue.length ? kindValue[0] : null;
    if (kind?.id) {
      resolutionSection.fields.set('KindID', TypedField.createGuid(kind.id));
      resolutionSection.fields.set('KindCaption', TypedField.createString(kind.name));
    }

    const fromValue = dataProvider.getValue<{ id: string; name: string }[]>('From');
    const from = fromValue.length ? fromValue[0] : null;
    if (from?.id) {
      resolutionSection.fields.set('AuthorID', TypedField.createGuid(from.id));
      resolutionSection.fields.set('AuthorName', TypedField.createString(from.name));
    }

    const controllerValue = dataProvider.getValue<{ id: string; name: string }[]>('Controller');
    const controller = controllerValue.length ? controllerValue[0] : null;
    if (controller?.id) {
      resolutionSection.fields.set('ControllerID', TypedField.createGuid(controller.id));
      resolutionSection.fields.set('ControllerName', TypedField.createString(controller.name));
    }

    const result = await this.storeCard(card);
    if (result) {
      return true;
    }

    // smth went wrong, checking if there is a cancel option
    const type = await this._cardMetadata.getMetadataForType(newResolution.typeId);
    if (!type) {
      return false;
    }

    const cancelOption = type.cardType.completionOptions.find(o => o.typeId === cancelOptionId);
    if (cancelOption) {
      newResolution.optionId = cancelOption.typeId;
    }

    // no cancel, guess nothing to do here
    return this.storeCard(card);
  }

  private static subscribeToFieldChanged(
    block: IRuntimeBlockViewModel,
    rootDataProvider: IDataProvider
  ) {
    WfTileExtension.setControlVisibility(
      block,
      ['Controller'],
      !!rootDataProvider.getValue('WithControl')
    );
    WfTileExtension.setControlVisibility(
      block,
      ['From', 'Kind'],
      !!rootDataProvider.getValue('ShowAdditional')
    );
    const perfs = rootDataProvider.getValue('Performers') as [];
    rootDataProvider.setValue('DurationInDays', new Decimal(3));
    reaction(
      () => perfs.length,
      len => {
        WfTileExtension.setControlVisibility(
          block,
          ['MajorPerformer', 'MultiplePerformers'],
          len > 1
        );

        rootDataProvider.setValue('MultiplePerformers', len > 1);
        rootDataProvider.setValue('MajorPerformer', false);
      },
      { fireImmediately: true }
    );
    let suppressReaction = false;
    rootDataProvider.onChange.add(({ key, newValue }) => {
      if (suppressReaction) {
        suppressReaction = false;
        return;
      }
      switch (key) {
        case 'WithControl':
          WfTileExtension.setControlVisibility(block, ['Controller'], newValue as boolean);
          break;

        case 'ShowAdditional':
          WfTileExtension.setControlVisibility(block, ['From', 'Kind'], newValue as boolean);
          break;

        case 'Planned': {
          suppressReaction = true;
          rootDataProvider.setValue('DurationInDays', null);
          WfTileExtension.setControlVisibility(block, ['DurationInDays'], true);
          break;
        }

        case 'DurationInDays': {
          suppressReaction = true;
          rootDataProvider.setValue('Planned', null);
          break;
        }

        case 'MultiplePerformers': {
          WfTileExtension.setControlVisibility(block, ['MajorPerformer'], newValue as boolean);
          if (!newValue) {
            rootDataProvider.setValue('MajorPerformer', false);
          }
          break;
        }
      }
    });
  }

  private static setControlVisibility(
    block: IRuntimeBlockViewModel,
    names: string[],
    isVisible: boolean
  ) {
    const setVisibility = (control: IRuntimeItemViewModel, isVisible: boolean) => {
      if ('visibility' in control) {
        control.visibility = isVisible;
        return;
      }
      if (isItemWithStates(control)) {
        setVisibility(control.getCurrent(), isVisible);
      }
    };

    if (isRuntimeBlock(block)) {
      for (const control of block.iterate(child => names.includes(child.alias!))) {
        setVisibility(control, isVisible);
      }

      return;
    }
  }

  private static enableOnCardIsNotTaskCard(e: TileEvaluationEventArgs) {
    const editor = e.currentTile.context.cardEditor;
    e.setIsEnabledWithCollapsing(
      e.currentTile,
      !!editor && !!editor.cardModel && editor.cardModel.cardType.id !== CardHelper.WfTaskCardTypeID
    );
  }
}

const cancelOptionId = '2582b66f-375a-0d59-ae86-a149309c5785';
