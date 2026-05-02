import { Flags } from '@tessa/core';
import {
  IExtensionContainer,
  IExtensionContainer$,
  IExtensionExecutor,
  extension,
  inject,
  ISession$,
  ISession
} from '@tessa/application';
import {
  IKrTypesCache,
  IKrTypesCache$,
  KrComponents,
  KrComponentsHelper,
  KrPermissionFlagDescriptors,
  KrStageState,
  KrStageTypeFormatter,
  KrStageTypeFormatterContext,
  KrToken,
  PerformerUsageMode,
  ViewCriteriaOperators
} from '@tessa/platform';
import {
  Card,
  CardFieldChangedEventArgs,
  CardRow,
  CardRowState,
  CardSection,
  FieldMapStorage,
  userKeyPrefix
} from 'tessa/cards';
import {
  CardUIExtension,
  IBlockViewModel,
  ICardModel,
  ICardUIExtensionContext,
  IControlViewModel,
  IFormWithBlocksViewModel
} from 'tessa/ui/cards';
import {
  ContainerViewModel,
  ControlViewModelBase,
  GridRowAction,
  GridRowAddingEventArgs,
  GridRowEventArgs,
  GridRowValidationEventArgs,
  GridRowViewModel,
  GridViewModel,
  TabControlViewModel
} from 'tessa/ui/cards/controls';
import { DotNetType, Guid, Visibility } from 'tessa/platform';
import { KrStageTypeUIHandler, KrStageTypeUIHandlerContext } from 'tessa/ui/workflow/krProcess';
import {
  MenuAction,
  UIButton,
  showError,
  showNotEmpty,
  showViewModelDialog,
  tryGetFromSettings
} from 'tessa/ui';
import { RequestParameter } from 'tessa/views/metadata';
import { RequestParameterBuilder, TessaViewRequest, ViewService } from 'tessa/views';
import {
  ValidationResult,
  ValidationResultBuilder,
  ValidationResultType
} from 'tessa/platform/validation';
import { designTimeCard, runtimeCard, skipStage } from '../../workflow/krProcess/krUIHelper';
import { BlockContentIndicator } from '../../ui/blockContentIndicator';
import { BlockViewModelBase } from 'tessa/ui/cards/blocks';
import { IStorage } from 'tessa/platform/storage';
import { LocalizationManager } from 'tessa/localization';
import { StageGroup } from '../../ui/krProcess/stageGroup';
import { StageSelectorDialog } from '../../ui/krProcess/stageSelectorDialog';
import { StageSelectorViewModel } from '../../ui/krProcess/stageSelectorViewModel';
import { StageType } from '../../ui/krProcess/stageType';
import { TabContentIndicator } from '../../ui/tabContentIndicator';
import { RuntimeItemWithStateViewModel } from 'tessa/ui/formEditor/controls/runtimeItemWithStateViewModel';

@extension()
export class KrStageUIExtension extends CardUIExtension {
  //#region ctor

  constructor(
    @inject(IExtensionContainer$) private readonly _extensionContainer: IExtensionContainer,
    @inject(IKrTypesCache$) private readonly _krTypesCache: IKrTypesCache,
    @inject(ISession$) private readonly _userSession: ISession
  ) {
    super();
  }

  //#endregion

  //#region fields

  private krStageTypeUIHandlerExecutor: IExtensionExecutor<KrStageTypeUIHandler> | null;

  //#endregion

  //#region CardUIExtension

  async initialized(context: ICardUIExtensionContext): Promise<void> {
    const cardModel = context.model;

    this.setTabIndication(cardModel, 'KrStageTemplates', 'CSharpSourceTable');
    this.setTabIndication(cardModel, 'KrStageGroups', 'CSharpSourceTableDesign');
    this.setTabIndication(cardModel, 'KrStageGroups', 'CSharpSourceTableRuntime');
    this.setBlockIndication(cardModel, 'KrSecondaryProcesses', 'VisibilityScriptsBlock');
    this.setBlockIndication(cardModel, 'KrSecondaryProcesses', 'ExecutionScriptsBlock');

    // Для шаблона этапов и вторички тоже выполняем расширение без проверки компонентов
    if (!designTimeCard(cardModel.cardType.id!)) {
      // KrStageTemplateTypeID
      const usedComponents = await KrComponentsHelper.getKrComponentsByCard(
        cardModel.card,
        this._krTypesCache
      );
      // Выходим если нет согласования
      if (!Flags.hasFlag(usedComponents, KrComponents.Routes)) {
        return;
      }
    }

    const rootBlock = cardModel.wysiwygForm?.getRootBlock();
    const approvalStagesTable = (
      cardModel.mainForm
        ? cardModel.controls.get('ApprovalStagesTable')
        : rootBlock?.getItem<RuntimeItemWithStateViewModel>('ApprovalStagesTable')?.getCurrent()
    ) as GridViewModel;
    if (!approvalStagesTable) {
      return;
    }

    if (!designTimeCard(cardModel.cardType.id!)) {
      approvalStagesTable.leftButtons.push(
        UIButton.create({
          name: 'ActivateStage',
          caption: '$CardTypes_Buttons_ActivateStage',
          buttonAction: () =>
            KrStageUIExtension.activateStagesHandler(approvalStagesTable.selectedRows),
          isEnabled: () =>
            KrStageUIExtension.hasEnableActivateStageButton(
              cardModel.card,
              approvalStagesTable.selectedRows
            ),
          type: 'small',
          theme: 'transparent'
        })
      );

      approvalStagesTable.contextMenuGenerators.push(ctx => {
        ctx.menuActions.push(
          MenuAction.create({
            type: 'normal',
            name: 'ActivateStage',
            caption: '$CardTypes_Buttons_ActivateStage',
            action: () => KrStageUIExtension.activateStagesHandler(ctx.rowsInAction),
            isCollapsed: !KrStageUIExtension.hasEnableActivateStageButton(
              cardModel.card,
              approvalStagesTable.selectedRows
            )
          })
        );
      });

      approvalStagesTable.rows
        .filter(i => !!i.row.tryGet('Hidden') || !!i.row.tryGet('Skip'))
        .forEach(row => {
          row.className.add('hidden-row-background');
        });
    }

    this.disposeList.add(
      approvalStagesTable.rowAdding.addWithDispose(KrStageUIExtension.addStageDialog)!
    );
    this.disposeList.add(
      approvalStagesTable.rowInitializing.addWithDispose(KrStageUIExtension.showSettingsBlock)!
    );
    this.disposeList.add(approvalStagesTable.rowInvoked.addWithDispose(this.bindUIHandlers)!);
    this.disposeList.add(
      approvalStagesTable.rowInvoked.addWithDispose(KrStageUIExtension.bindTimeFieldsRadio)!
    );
    this.disposeList.add(
      approvalStagesTable.rowInvoked.addWithDispose(KrStageUIExtension.rowInvokedSkipStage)!
    );
    this.disposeList.add(
      approvalStagesTable.rowEditorClosing.addWithDispose(this.handleFormatRow)!
    );
    this.disposeList.add(
      approvalStagesTable.rowValidating.addWithDispose(this.validateViaHandlers)!
    );
    this.disposeList.add(
      approvalStagesTable.rowValidating.addWithDispose(KrStageUIExtension.validateTimeLimit)!
    );
    this.disposeList.add(
      approvalStagesTable.rowValidating.addWithDispose(KrStageUIExtension.validatePerformers)!
    );
    this.disposeList.add(
      approvalStagesTable.rowEditorClosed.addWithDispose(this.unbindUIHandlers)!
    );
    this.disposeList.add(
      approvalStagesTable.rowEditorClosed.addWithDispose(KrStageUIExtension.unbindTimeFieldsRadio)!
    );

    const stagesSec = context.card.sections.tryGet('KrStagesVirtual');
    if (stagesSec) {
      for (const row of stagesSec.rows) {
        this.formatRow(row, cardModel, false);
      }
    }
  }

  async finalized(): Promise<void> {
    this.disposeList.dispose();
  }

  //#endregion

  //#region handlers

  private static addStageDialog = async (args: GridRowAddingEventArgs): Promise<void> => {
    const card = args.cardModel.card;
    let group: StageGroup | null = null;

    const krStageTemplates = card.sections.tryGet('KrStageTemplates');
    const krSecondaryProcesses = card.sections.tryGet('KrSecondaryProcesses');
    const hasGroup = !!krStageTemplates || !!krSecondaryProcesses;
    if (hasGroup) {
      if (krStageTemplates) {
        const groupId = krStageTemplates.fields.tryGet<string>('StageGroupID');
        if (groupId != null) {
          group = new StageGroup(groupId, krStageTemplates.fields.tryGet('StageGroupName')!, 0);
        }
      } else if (krSecondaryProcesses) {
        group = new StageGroup(card.id, krSecondaryProcesses.fields.tryGet('Name')!, 0);
      }
    }

    const commonInfo = card.sections.tryGet('DocumentCommonInfo');
    let type = commonInfo ? commonInfo.fields.tryGet<string>('DocTypeID') : null;
    type = type ? type : card.typeId;

    const viewModel = new StageSelectorViewModel(group, card.id, type);
    viewModel.getGroupTypesFunc = async (groupVm: StageGroup | null, _: string, typeId: string) => {
      if (hasGroup && !groupVm) {
        // группа не выбрана в шаблоне этапов
        return [];
      }

      if (groupVm) {
        return [groupVm];
      }

      const stageGroupsView = ViewService.instance.getByName('KrFilteredStageGroups')!;
      const request = new TessaViewRequest(stageGroupsView.metadata);

      const typeIdParam = new RequestParameterBuilder()
        .withMetadata(stageGroupsView.metadata.parameters.get('TypeId')!)
        .addCriteria(ViewCriteriaOperators.EqualsTo, '', typeId)
        .asRequestParameter();
      request.parameters.push(typeIdParam);

      const result = await stageGroupsView.getData(request);
      return result.rows.map(x => new StageGroup(x[0] as string, x[1] as string, x[4] as number));
    };

    viewModel.getStageTypesFunc = async (groupType: string, _: string, typeId: string) => {
      if (hasGroup && !group) {
        // группа не выбрана в шаблоне этапов
        return [];
      }

      const stageTypesView = ViewService.instance.getByName('KrFilteredStageTypes')!;
      const request = new TessaViewRequest(stageTypesView.metadata);

      if (designTimeCard(typeId)) {
        const isTemplateParam = new RequestParameterBuilder()
          .withMetadata(stageTypesView.metadata.parameters.get('IsTemplate')!)
          .addCriteria(ViewCriteriaOperators.EqualsTo, '', true)
          .asRequestParameter();
        request.parameters.push(isTemplateParam);
      }

      const param = new RequestParameterBuilder()
        .withMetadata(stageTypesView.metadata.parameters.get('StageGroupIDParam')!)
        .addCriteria(ViewCriteriaOperators.EqualsTo, '', groupType)
        .asRequestParameter();
      request.parameters.push(param);

      const typeIdMetadata = stageTypesView.metadata.parameters.get('TypeId')!;
      let typeIdParam: RequestParameter | null = null;
      if (designTimeCard(typeId)) {
        const typeRows = card.sections.get('KrStageTypes').rows;
        if (typeRows.length > 0) {
          const typeIdSet = new Set<string>();

          for (const row of typeRows) {
            if (row.state !== CardRowState.Deleted) {
              const templateTypeId = row.get<string>('TypeID');
              if (templateTypeId) {
                typeIdSet.add(templateTypeId);
              }
            }
          }

          if (typeIdSet.size > 0) {
            const typeIdBuilder = new RequestParameterBuilder().withMetadata(typeIdMetadata);

            for (const id of typeIdSet) {
              typeIdBuilder.addCriteria(ViewCriteriaOperators.EqualsTo, '', id);
            }

            typeIdParam = typeIdBuilder.asRequestParameter();
          }
        }
      } else {
        typeIdParam = new RequestParameterBuilder()
          .withMetadata(typeIdMetadata)
          .addCriteria(ViewCriteriaOperators.EqualsTo, '', typeId)
          .asRequestParameter();
      }

      if (typeIdParam) {
        request.parameters.push(typeIdParam);
      }

      const result = await stageTypesView.getData(request);
      return result.rows.map(x => new StageType(x[0] as string, x[1] as string, x[2] as string));
    };

    await viewModel.refresh();
    await viewModel.updateType();

    if (viewModel.groups.length === 0) {
      await showError('$UI_Error_NoAvailableStageGroups');
      args.cancel = true;
      return;
    }

    let result: {
      cancel: boolean;
      group: StageGroup | null;
      type: StageType | null;
    };
    if (viewModel.groups.length === 1 && viewModel.types.length === 1) {
      result = {
        cancel: false,
        group: viewModel.groups[0],
        type: viewModel.types[0]
      };
    } else {
      result = await showViewModelDialog(viewModel, StageSelectorDialog);
    }

    if (!result.group || !result.type) {
      args.cancel = true;
      return;
    }

    args.row.set('StageGroupID', result.group.id, DotNetType.Guid);
    args.row.set('StageGroupName', result.group.name, DotNetType.String);
    args.row.set('StageGroupOrder', result.group.order, DotNetType.Int);
    args.row.set('StageTypeID', result.type.id, DotNetType.Guid);
    args.row.set('StageTypeCaption', result.type.caption, DotNetType.String);
    args.row.set(
      'Name',
      LocalizationManager.instance.localize(result.type.defaultStage),
      DotNetType.String
    );

    if (!designTimeCard(args.cardModel.cardType.id!)) {
      const rows = args.rows;
      const groupId = result.group.id;
      const groupOrder = result.group.order;

      const order = ((): number => {
        if (rows.length === 0) {
          return 0;
        }

        let rowIndex = 0;
        const getId = (): string => rows[rowIndex].tryGet('StageGroupID') ?? Guid.empty;
        const getOrder = (): number =>
          rows[rowIndex].tryGet('StageGroupOrder') ?? Number.MAX_SAFE_INTEGER;
        const nestedStage = (): boolean => !!rows[rowIndex].tryGet(userKeyPrefix + 'NestedStage');

        const cnt = rows.length;

        // Достигаем начало требуемой группы
        while (
          rowIndex < cnt &&
          ((getId() !== groupId && getOrder() < groupOrder) || nestedStage())
        ) {
          rowIndex++;
        }

        // На нестеде тут мы быть не можем, т.к. пропустили возможные нестеды выше
        // Проверим, что мы в конце или на другой группе
        if (rows.length === rowIndex || (getId() !== groupId && getOrder() !== groupOrder)) {
          // Текущая группа последняя
          // В текущей группе нет этапов, просто добавляем на нужное место.
          return rowIndex;
        }

        const firstIndexInGroup = rowIndex;
        rowIndex++;

        // Спускаемся до конца группы
        while (
          rowIndex < cnt &&
          ((getId() === groupId && getOrder() === groupOrder) || nestedStage())
        ) {
          rowIndex++;
        }

        // Поднимаемся вверх до возможного места добавления
        let position = rowIndex;
        const sortedRows = rows
          .map(x => x)
          .sort((a, b) => a.get<number>('Order')! - b.get<number>('Order')!);
        for (let i = rowIndex - 1; i >= firstIndexInGroup; i--) {
          const row = sortedRows[i];
          if (row.tryGet(userKeyPrefix + 'NestedStage')) {
            continue;
          }

          const posId = row.tryGet('BasedOnStageTemplateGroupPositionID');
          const orderChanged = row.tryGet('OrderChanged');
          if (
            posId != undefined &&
            posId === 1 && // AtLast
            orderChanged != undefined &&
            !orderChanged
          ) {
            position = i;
          }
        }

        return position;
      })();

      args.rowIndex = order;
      args.row.rawSet('__OriginalOrder', order, DotNetType.Int);
    }
  };

  private static showSettingsBlock = (args: GridRowEventArgs): void => {
    if (!args.rowModel) {
      return;
    }

    const commonBlock = args.rowModel.blocks.get('StageCommonInfoBlock');
    if (commonBlock) {
      KrStageUIExtension.setOptionalControlVisibility(commonBlock, 'TimeLimitInput', args);
      KrStageUIExtension.setOptionalControlVisibility(commonBlock, 'PlannedInput', args);
      KrStageUIExtension.setOptionalControlVisibility(commonBlock, 'HiddenStageCheckbox', args);
      KrStageUIExtension.setOptionalControlVisibility(commonBlock, 'CanBeSkipCheckbox', args);
    }

    const performersBlock = args.rowModel.blocks.get('PerformersBlock');
    if (performersBlock) {
      const singleVisibility = KrStageUIExtension.setOptionalControlVisibility(
        performersBlock,
        'SinglePerformerEntryAC',
        args
      );
      const multipleVisibility = KrStageUIExtension.setOptionalControlVisibility(
        performersBlock,
        'MultiplePerformersTableAC',
        args
      );

      const sqlPerformersLinkBlock = args.rowModel.blocks.get('KrSqlPerformersLinkBlock');
      if (sqlPerformersLinkBlock && multipleVisibility === Visibility.Visible) {
        sqlPerformersLinkBlock.blockVisibility = Visibility.Visible;
      }

      if (singleVisibility === Visibility.Visible) {
        KrStageUIExtension.setControlCaption(performersBlock, 'SinglePerformerEntryAC', args);
      }
      if (multipleVisibility === Visibility.Visible) {
        KrStageUIExtension.setControlCaption(performersBlock, 'MultiplePerformersTableAC', args);
      }
    }

    const authorBlock = args.rowModel.blocks.get('AuthorBlock');
    if (authorBlock) {
      KrStageUIExtension.setOptionalControlVisibility(authorBlock, 'AuthorEntryAC', args);
    }

    const taskKindBlock = args.rowModel.blocks.get('TaskKindBlock');
    if (taskKindBlock) {
      KrStageUIExtension.setOptionalControlVisibility(taskKindBlock, 'TaskKindEntryAC', args);
    }

    const taskHistoryBlock = args.rowModel.blocks.get('KrTaskHistoryBlockAlias');
    if (taskHistoryBlock) {
      const v = KrStageUIExtension.setOptionalControlVisibility(
        taskHistoryBlock,
        'KrTaskHistoryGroupTypeControlAlias',
        args
      );
      KrStageUIExtension.setOptionalControlVisibility(
        taskHistoryBlock,
        'KrParentTaskHistoryGroupTypeControlAlias',
        args
      );
      KrStageUIExtension.setOptionalControlVisibility(
        taskHistoryBlock,
        'KrTaskHistoryGroupNewIterationControlAlias',
        args
      );
      // Если показывается контрол (а показываются они все по одному правилу), то показывается и заголовок
      taskHistoryBlock.captionVisibility = v !== null ? v : Visibility.Collapsed;
    }

    const settingsBlock = args.rowModel.blocks.get('SettingsBlock');
    if (settingsBlock) {
      const stageTypeId = args.row.tryGet<string>('StageTypeID');

      for (const control of settingsBlock.controls) {
        const controlSettings = control.cardTypeControl.controlSettings;
        const stageHandlerIDs = tryGetFromSettings<string[] | undefined>(
          controlSettings,
          'StageHandlerDescriptorIDSetting'
        );
        if (stageHandlerIDs) {
          if (stageHandlerIDs.some(i => Guid.equals(stageTypeId, i))) {
            control.controlVisibility = Visibility.Visible;
            KrStageUIExtension.setVisibilityViaTags(args.cardModel.cardType.id!, control);
          } else {
            control.controlVisibility = Visibility.Collapsed;
          }
        }
      }
    }
  };

  private static bindTimeFieldsRadio = (args: GridRowEventArgs): void => {
    args.row.fieldChanged.add(KrStageUIExtension.onTimeFieldChanged);
  };

  private static unbindTimeFieldsRadio = (args: GridRowEventArgs): void => {
    args.row.fieldChanged.remove(KrStageUIExtension.onTimeFieldChanged);
  };

  private static onTimeFieldChanged = (
    args: CardFieldChangedEventArgs,
    s: FieldMapStorage
  ): void => {
    if (args.fieldValue == null) {
      return;
    }

    switch (args.fieldName) {
      case 'Planned':
        s.set('TimeLimit', null);
        break;
      case 'TimeLimit':
        s.set('Planned', null);
        break;
    }
  };

  private readonly bindUIHandlers = async (args: GridRowEventArgs): Promise<void> =>
    await this.runHandlers(args, 'initialize');

  private readonly validateViaHandlers = async (args: GridRowValidationEventArgs): Promise<void> =>
    await this.runValidationHandlers(args);

  private readonly unbindUIHandlers = async (args: GridRowEventArgs): Promise<void> =>
    await this.runHandlers(args, 'finalize');

  private async runHandlers(
    args: GridRowEventArgs,
    methodName: 'initialize' | 'finalize'
  ): Promise<void> {
    if (args.action === GridRowAction.Deleting) {
      return;
    }

    const row = args.row;
    const stageTypeId = row.tryGet<string>('StageTypeID');
    if (!stageTypeId) {
      return;
    }

    if (!this.krStageTypeUIHandlerExecutor) {
      this.krStageTypeUIHandlerExecutor =
        this._extensionContainer.resolveExecutor(KrStageTypeUIHandler);
    }

    if (this.krStageTypeUIHandlerExecutor.length === 0) {
      return;
    }

    const context = new KrStageTypeUIHandlerContext(
      stageTypeId,
      args.action,
      args.control,
      row,
      args.rowModel!,
      args.cardModel,
      new ValidationResultBuilder()
    );

    try {
      await this.krStageTypeUIHandlerExecutor.executeAsync(methodName, context);
    } catch (err) {
      context.validationResult.add(ValidationResult.fromError(err));
    } finally {
      await showNotEmpty(context.validationResult.build());
    }
  }

  private async runValidationHandlers(args: GridRowValidationEventArgs): Promise<void> {
    const row = args.row;
    const stageTypeId = row.tryGet<string>('StageTypeID');
    if (!stageTypeId) {
      return;
    }

    if (!this.krStageTypeUIHandlerExecutor) {
      this.krStageTypeUIHandlerExecutor =
        this._extensionContainer.resolveExecutor(KrStageTypeUIHandler);
    }

    if (this.krStageTypeUIHandlerExecutor.length === 0) {
      return;
    }

    const context = new KrStageTypeUIHandlerContext(
      stageTypeId,
      null,
      args.control,
      row,
      args.rowModel!,
      args.cardModel,
      args.validationResult
    );

    try {
      await this.krStageTypeUIHandlerExecutor.executeAsync('validate', context);
    } catch (err) {
      context.validationResult.add(ValidationResult.fromError(err));
    }
  }

  private static validateTimeLimit = (args: GridRowValidationEventArgs): void => {
    const row = args.row;
    const stageTypeId = row.tryGet<string>('StageTypeID');
    const timeLimit = row.tryGet('TimeLimit');
    const planned = row.tryGet('Planned');
    const controls = args.rowModel!.controls;
    let control: IControlViewModel;
    let list: string[];
    const checkField = (inputAlias: string) =>
      (control = controls.get(inputAlias)!) &&
      (list = tryGetFromSettings(
        control.cardTypeControl.controlSettings,
        'VisibleForTypesSetting',
        null
      )!) &&
      list.some(x => Guid.equals(x, stageTypeId));
    const checkTimeLimit = checkField('TimeLimitInput');
    const checkPlanned = checkField('PlannedInput');
    if (checkTimeLimit && checkPlanned) {
      if (timeLimit == null && planned == null) {
        args.validationResult.add(
          ValidationResult.fromText(
            '$UI_Error_TimeLimitOrPlannedNotSpecifiedWarn',
            ValidationResultType.Warning
          )
        );
      }
    } else if (checkTimeLimit && timeLimit == null) {
      args.validationResult.add(
        ValidationResult.fromText(
          '$UI_Error_TimeLimitNotSpecifiedWarn',
          ValidationResultType.Warning
        )
      );
    }
  };

  private static validatePerformers = (args: GridRowValidationEventArgs): void => {
    const controls = args.rowModel!.controls;
    let mode: PerformerUsageMode;
    let control: IControlViewModel;
    if (
      (control = controls.get('SinglePerformerEntryAC')!) &&
      control.controlVisibility === Visibility.Visible
    ) {
      mode = PerformerUsageMode.Single;
    } else if (
      (control = controls.get('MultiplePerformersTableAC')!) &&
      control.controlVisibility === Visibility.Visible
    ) {
      mode = PerformerUsageMode.Multiple;
    } else {
      return;
    }

    const controlSettings = control.cardTypeControl.controlSettings;
    const list = tryGetFromSettings<string[] | null>(
      controlSettings,
      'RequiredForTypesSetting',
      null
    );
    if (!list) {
      return;
    }

    const row = args.row;
    const stageTypeID = row.tryGet<string>('StageTypeID');

    for (const id of list) {
      if (Guid.equals(id, stageTypeID)) {
        let perfSec: CardSection;
        if (
          (mode === PerformerUsageMode.Single &&
            row.get('KrSinglePerformerVirtual__PerformerID') == undefined) ||
          (mode === PerformerUsageMode.Multiple &&
            (perfSec = args.cardModel.card.sections.tryGet('KrPerformersVirtual_Synthetic')!) &&
            !perfSec.rows.some(
              x => Guid.equals(x.get('StageRowID'), row.rowId) && x.state !== CardRowState.Deleted
            ))
        ) {
          args.validationResult.add(
            ValidationResult.fromText(
              '$UI_Error_PerformerNotSpecifiedWarn',
              ValidationResultType.Warning
            )
          );
        }
        break;
      }
    }
  };

  private handleFormatRow = (args: GridRowEventArgs): void => {
    this.formatRow(args.row, args.cardModel, true);
  };

  //#endregion

  //#region private methods

  private setTabIndication(cardModel: ICardModel, sectionName: string, controlName: string): void {
    const section = cardModel.card.sections.tryGet(sectionName);
    if (section) {
      const sectionMeta = cardModel.cardMetadata.sections.getSectionByName(sectionName);
      if (!sectionMeta) {
        return;
      }

      const fieldIds: [string, string][] = sectionMeta.columns.map(x => [x.id || '', x.name || '']);
      const tabControl = cardModel.controls.get(controlName) as TabControlViewModel;
      const indicator = new TabContentIndicator(
        tabControl,
        section.fields.getStorage(),
        fieldIds,
        true
      );
      indicator.update();
      this.disposeList.add(section.fields.fieldChanged.add(indicator.fieldChangedAction));
    }
  }

  private setBlockIndication(
    cardModel: ICardModel,
    sectionName: string,
    controlName: string
  ): void {
    const templateSec = cardModel.card.sections.tryGet(sectionName);

    if (!templateSec) {
      return;
    }

    const control = cardModel.blocks.get(controlName);

    if (!control) {
      return;
    }

    const sectionMeta = cardModel.cardMetadata.sections.find(s => s.name === sectionName);

    if (!sectionMeta) {
      return;
    }

    const fieldIDs = new Map<string, string>();

    for (const column of sectionMeta.columns) {
      if (column.id && column.name) {
        fieldIDs.set(column.id, column.name);
      }
    }

    const indicator = new BlockContentIndicator(control, templateSec.fields, fieldIDs);
    this.disposeList.add(indicator.dispose);
  }

  private static setOptionalControlVisibility(
    block: IBlockViewModel,
    controlAlias: string,
    e: GridRowEventArgs
  ): Visibility | null {
    const inputControl = block.controls.find(x => x.name === controlAlias);
    if (!inputControl) {
      return null;
    }

    const controlSettings = inputControl.cardTypeControl.controlSettings;
    let list = tryGetFromSettings(controlSettings, 'VisibleForTypesSetting');
    if (!list || !Array.isArray(list)) {
      return null;
    }

    const stageTypeId = e.row.tryGet<string>('StageTypeID');

    inputControl.controlVisibility = Visibility.Collapsed;
    for (const id of list) {
      if (Guid.equals(id, stageTypeId)) {
        inputControl.controlVisibility = Visibility.Visible;
        break;
      }
    }

    inputControl.isRequired = false;
    list = tryGetFromSettings(controlSettings, 'RequiredForTypesSetting');
    if (list && Array.isArray(list)) {
      for (const id of list) {
        if (Guid.equals(id, stageTypeId)) {
          inputControl.isRequired = true;
          break;
        }
      }
    }

    KrStageUIExtension.setControlSettings(e.cardModel.cardType.id!, inputControl);
    KrStageUIExtension.setVisibilityViaTags(e.cardModel.cardType.id!, inputControl);

    return inputControl.controlVisibility;
  }

  private static setControlCaption(
    block: IBlockViewModel,
    controlAlias: string,
    e: GridRowEventArgs
  ): void {
    const inputControl = block.controls.find(x => x.name === controlAlias);
    if (!inputControl) {
      return;
    }

    const controlSettings = inputControl.cardTypeControl.controlSettings;
    const captions = tryGetFromSettings<IStorage | null>(
      controlSettings,
      'ControlCaptionsSetting',
      null
    );
    if (!captions) {
      return;
    }

    const stageTypeId = e.row.tryGet<string>('StageTypeID');

    if (stageTypeId) {
      const caption = captions[stageTypeId];
      if (caption) {
        inputControl.caption = caption;
      }
    }
  }

  private static setVisibilityViaTags(typeId: string, rootControl: IControlViewModel): void {
    const queue: Array<IControlViewModel | IBlockViewModel> = [];
    if (rootControl instanceof TabControlViewModel) {
      KrStageUIExtension.enqueueTabs(rootControl, queue);
    } else if (rootControl instanceof ContainerViewModel) {
      KrStageUIExtension.enqueueForm(rootControl.form, queue);
    } else {
      return;
    }

    while (queue.length !== 0) {
      const item = queue.shift();
      if (item instanceof ControlViewModelBase) {
        KrStageUIExtension.setControlSettings(typeId, item);

        if (item instanceof TabControlViewModel) {
          KrStageUIExtension.enqueueTabs(item, queue);
        }
      } else if (item instanceof BlockViewModelBase) {
        KrStageUIExtension.setBlockControlsSettings(typeId, item);
        KrStageUIExtension.enqueueBlock(item, queue);
      }
    }
  }

  private static enqueueTabs(
    tabControl: TabControlViewModel,
    queue: Array<IControlViewModel | IBlockViewModel>
  ): void {
    for (const tab of tabControl.tabs) {
      KrStageUIExtension.enqueueForm(tab, queue);
    }
  }

  private static enqueueForm(
    formViewModel: IFormWithBlocksViewModel,
    queue: Array<IControlViewModel | IBlockViewModel>
  ): void {
    for (const blockViewModel of formViewModel.blocks) {
      queue.push(blockViewModel);
    }
  }

  private static enqueueBlock(
    blockViewModel: IBlockViewModel,
    queue: Array<IControlViewModel | IBlockViewModel>
  ): void {
    for (const controlViewModel of blockViewModel.controls) {
      queue.push(controlViewModel);
    }
  }

  private static setControlSettings(typeId: string, controlViewModel: IControlViewModel): void {
    if (controlViewModel.controlVisibility !== Visibility.Visible) {
      // Если контрол скрыт, нет смысла в нем копаться.
      return;
    }

    const controlSettings = controlViewModel.cardTypeControl.controlSettings;
    const tags = tryGetFromSettings(controlSettings, 'TagsListSetting');
    if (!tags || !Array.isArray(tags)) {
      return;
    }

    let hasRuntime = false;
    let hasDesign = false;
    let hasRuntimeReadonly = false;
    let hasDesignReadonly = false;

    for (const tag of tags) {
      switch (tag) {
        case 'Runtime':
          hasRuntime = true;
          break;
        case 'DesignTime':
          hasDesign = true;
          break;
        case 'RuntimeReadonly':
          hasRuntimeReadonly = true;
          break;
        case 'DesignTimeReadonly':
          hasDesignReadonly = true;
          break;
      }
    }

    if (!hasDesign && !hasRuntime) {
      controlViewModel.controlVisibility = Visibility.Visible;
    } else if (hasDesign && designTimeCard(typeId)) {
      controlViewModel.controlVisibility = Visibility.Visible;
    } else if (hasRuntime && runtimeCard(typeId)) {
      controlViewModel.controlVisibility = Visibility.Visible;
    } else {
      controlViewModel.controlVisibility = Visibility.Collapsed;
    }

    if (controlViewModel.controlVisibility === Visibility.Visible) {
      if (hasDesignReadonly && designTimeCard(typeId)) {
        controlViewModel.isReadOnly = true;
      } else if (hasRuntimeReadonly && runtimeCard(typeId)) {
        controlViewModel.isReadOnly = true;
      }
    }
  }

  private static setBlockControlsSettings(typeId: string, blockViewModel: IBlockViewModel): void {
    if (blockViewModel.blockVisibility !== Visibility.Visible) {
      // Если блок скрыт, нет смысла в нем копаться.
      return;
    }

    const blockSettings = blockViewModel.cardTypeBlock.blockSettings;
    const tags = tryGetFromSettings(blockSettings, 'TagsListSetting');
    if (!tags || !Array.isArray(tags)) {
      return;
    }

    let hasRuntime = false;
    let hasDesign = false;

    for (const tag of tags) {
      switch (tag) {
        case 'Runtime':
          hasRuntime = true;
          break;
        case 'DesignTime':
          hasDesign = true;
          break;
      }
    }

    if (!hasDesign && !hasRuntime) {
      blockViewModel.blockVisibility = Visibility.Visible;
    } else if (hasDesign && designTimeCard(typeId)) {
      blockViewModel.blockVisibility = Visibility.Visible;
    } else if (hasRuntime && runtimeCard(typeId)) {
      blockViewModel.blockVisibility = Visibility.Visible;
    } else {
      blockViewModel.blockVisibility = Visibility.Collapsed;
    }
  }

  private async formatRow(
    row: CardRow,
    cardModel: ICardModel,
    withChanges: boolean
  ): Promise<void> {
    const stageTypeId = row.tryGet<string>('StageTypeID');
    if (!stageTypeId) {
      return;
    }

    const extensionExecutor = this._extensionContainer.resolveExecutor(KrStageTypeFormatter);
    if (extensionExecutor.length === 0) {
      if (withChanges) {
        row.set('DisplaySettings', '', DotNetType.String);
      } else {
        row.rawSet('DisplaySettings', '', DotNetType.String);
      }

      return;
    }

    const info = {};
    const ctx = new KrStageTypeFormatterContext(
      stageTypeId,
      this._userSession,
      info,
      cardModel.card,
      row,
      null
    );
    ctx.displayTimeLimit = row.tryGet('DisplayTimeLimit') || '';
    ctx.displayParticipants = row.tryGet('DisplayParticipants') || '';
    ctx.displaySettings = row.tryGet('DisplaySettings') || '';

    // TODO WEB_REWORK: async
    cardModel.executeInContext(() => extensionExecutor.execute('format', ctx));
    extensionExecutor.dispose();

    if (withChanges) {
      const displayTimeLimit = row.tryGet('DisplayTimeLimit');
      if (displayTimeLimit !== ctx.displayTimeLimit) {
        row.set('DisplayTimeLimit', ctx.displayTimeLimit, DotNetType.String);
      }
      const displayParticipants = row.tryGet('DisplayParticipants');
      if (displayParticipants !== ctx.displayParticipants) {
        row.set('DisplayParticipants', ctx.displayParticipants, DotNetType.String);
      }
      const displaySettings = row.tryGet('DisplaySettings');
      if (displaySettings !== ctx.displaySettings) {
        row.set('DisplaySettings', ctx.displaySettings, DotNetType.String);
      }
    } else {
      row.rawSet('DisplayTimeLimit', ctx.displayTimeLimit, DotNetType.String);
      row.rawSet('DisplayParticipants', ctx.displayParticipants, DotNetType.String);
      row.rawSet('DisplaySettings', ctx.displaySettings, DotNetType.String);
    }
  }

  private static activateStagesHandler = (selectedRowStages: GridRowViewModel[]): void => {
    for (const selectedRowStage of selectedRowStages) {
      selectedRowStage.row.set('Skip', false, DotNetType.Boolean);
      selectedRowStage.className.remove('hidden-row-background');
    }
  };

  private static hasEnableActivateStageButton = (
    card: Card,
    selectedRows: GridRowViewModel[]
  ): boolean => {
    // Нет строк для обработки?
    if (selectedRows.length === 0) {
      return false;
    }

    // Разрешено редактировать маршрут?
    const token: KrToken | null = KrToken.tryGet(card.info);
    const isEditRoute = token && token.hasPermission(KrPermissionFlagDescriptors.CanSkipStages);

    // Разрешено активировать пропущенные неактивные этапы,
    // если они были изменены до сохранения карточки
    // или если есть разрешение на редактирования маршрута.
    return selectedRows.every(
      i =>
        i.row.get('StateID') === KrStageState.Inactive &&
        !!i.row.tryGet('Skip') &&
        (isEditRoute || i.row.isChanged('Skip'))
    );
  };

  private static rowInvokedSkipStage = (args: GridRowEventArgs): void => {
    if (args.action == GridRowAction.Deleting && skipStage(args.row)) {
      const currentRowViewModel = args.control.selectedRows.find(i => i.row === args.row);

      if (currentRowViewModel) {
        currentRowViewModel.isSelected = false;
        currentRowViewModel.className.add('hidden-row-background');
      }

      args.cancel = true;
    }
  };

  //#endregion
}
