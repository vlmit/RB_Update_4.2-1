import { reaction } from 'mobx';
import { Flags, IStorage, StorageHelper, TypedField } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import {
  ICardSingletonCache,
  ICardSingletonCache$,
  IKrTypesCache,
  IKrTypesCache$,
  KrComponents,
  KrComponentsHelper
} from '@tessa/platform';
import { CycleGrouping } from './cycleGrouping';
import { CycleFilesMode } from './cycleFilesMode';
import { switchFilesVisibility, restoreFilesList } from './cycleGroupingUIHelper';
import { FileControlExtension, IFileControlExtensionContext } from 'tessa/ui/files';
import { ICardModel } from 'tessa/ui/cards';
import { UIContext } from 'tessa/ui';

/**
 * Управляет появлением виртуальных файлов "версий" при включении и выключении группировки по циклам согласования.
 */
@extension({ name: 'KrAddCycleGroupingFileControlExtension' })
export class KrAddCycleGroupingFileControlExtension extends FileControlExtension {
  //#region ctor

  constructor(
    @inject(IKrTypesCache$) private readonly _typesCache: IKrTypesCache,
    @inject(ICardSingletonCache$) private readonly _cardSingletonCache: ICardSingletonCache
  ) {
    super();
  }

  //#endregion

  //#region FileControlExtension

  async initialized(context: IFileControlExtensionContext): Promise<void> {
    const model = context.info['.cardModel'] as ICardModel;
    if (!model || model.inSpecialMode) {
      return;
    }

    const card = model.card;
    if (!card) {
      return;
    }

    const krComponents = await KrComponentsHelper.getKrComponentsByCard(card, this._typesCache);
    if (Flags.hasNotFlag(krComponents, KrComponents.Routes)) {
      return;
    }

    let cycleGrouping = context.groupings.find(p => p.name === 'Cycle');

    // Добавляем группировку/фильтр
    if (!cycleGrouping) {
      cycleGrouping = new CycleGrouping('Cycle', '$UI_Controls_FilesControl_GroupingByCycle');
      context.groupings.push(cycleGrouping);
    }

    // Если по умолчанию не выбрана группировка по циклу согласования, то надо убрать виртуальные файлы версий.
    if (!(context.control.selectedGrouping instanceof CycleGrouping)) {
      context.control.removeFiles(file => {
        const cardFile = card.files.find(x => x.rowId == file.id);
        const cycle = StorageHelper.tryGet<number>(file.model.info, 'KrCycleID');
        return cardFile?.isVirtual !== false && cycle != null;
      });
    }

    const sections = card.tryGetSections();
    if (!sections) {
      return;
    }

    const commonInfo = sections.tryGet('KrApprovalCommonInfoVirtual');
    const state = commonInfo?.fields.tryGet<number>('StateID') ?? null;

    let currentCycle: number | null = null;
    const approvalHistory = sections.tryGet('KrApprovalHistoryVirtual');
    if (approvalHistory) {
      const rows = approvalHistory.tryGetRows();
      if (rows && rows.length > 0) {
        currentCycle = Math.max(...rows.map(x => x.tryGet<number>('Cycle') ?? 0));
      }
    }

    this.disposeList.add(
      context.control.containerFileAdded.addWithDispose(e => {
        if (
          state != null &&
          state !== 0 && // KrState.Draft
          !!e.file.origin &&
          currentCycle != null &&
          currentCycle > 0
        ) {
          e.file.info['KrCycleID'] = TypedField.createInt(currentCycle);
        }
      })!
    );

    this.disposeList.add(
      reaction(
        () => context.control.selectedGrouping,
        grouping => {
          if (grouping instanceof CycleGrouping) {
            const currentMode = StorageHelper.tryGet<CycleFilesMode | null>(
              context.control.info,
              'CycleGroupingMode'
            );
            switchFilesVisibility(
              context.control,
              card,
              currentCycle,
              currentMode == null ? CycleFilesMode.ShowAllCycleFiles : currentMode
            );
          } else {
            restoreFilesList(context.control, card);
          }
        }
      )
    );

    let currentCycleMode: CycleFilesMode | null = null;
    let modeFromContext: CycleFilesMode = CycleFilesMode.ShowAllCycleFiles;
    const cardEditor = UIContext.current.cardEditor;
    const groupingModeFromInfo = StorageHelper.tryGet<IStorage | null>(
      UIContext.current.info,
      'CycleGroupingMode'
    );

    if (
      context.control.name &&
      cardEditor &&
      cardEditor.cardModel &&
      card.id === cardEditor.cardModel.card.id &&
      groupingModeFromInfo &&
      context.control.name in groupingModeFromInfo
    ) {
      modeFromContext = groupingModeFromInfo[context.control.name] as CycleFilesMode;
      currentCycleMode = modeFromContext;
    }

    if (currentCycleMode == null) {
      const cardModelInfo = model.info;
      const mode = StorageHelper.tryGet<CycleFilesMode | null>(cardModelInfo, 'CycleGroupingMode');
      if (mode == null) {
        const settings = (await this._cardSingletonCache.getCard('KrSettings'))!;

        // Проверим, что тип карточки/документа включён в настройки
        // Читаем тип карточки/документа ровно один раз
        const dciSection = card.sections.tryGet('DocumentCommonInfo');
        let docCardTypeId = dciSection?.fields.tryGet<string>('DocTypeID');
        if (docCardTypeId == null) {
          docCardTypeId = card.typeId;
        }

        let settingsRowId: string | null = null;
        if (
          state != null &&
          settings.sections.get('KrSettingsCycleGrouping').rows.some(x => {
            const typeId = x.get('TypeID');
            if (typeId === docCardTypeId) {
              settingsRowId = x.get<string>('TypesRowID')!;
              // Проверим состояния
              if (
                settings.sections
                  .get('KrSettingsCycleGroupingStates')!
                  .rows.filter(y => y.get('TypesRowID') === settingsRowId)
                  .some(y => y.get('StateID') === state)
              ) {
                return true;
              }
            }

            return false;
          })
        ) {
          const row = settings.sections
            .get('KrSettingsCycleGroupingTypes')!
            .rows.find(x => x.rowId === settingsRowId);
          currentCycleMode = row ? row.get('DefaultModeID') : null;
          if (currentCycleMode != null && cardModelInfo) {
            cardModelInfo['CycleGroupingMode'] = currentCycleMode;
          }
        }
      }
    }

    if (currentCycleMode != null) {
      context.control.info['CycleGroupingMode'] = currentCycleMode;
      if (!context.control.selectedGrouping) {
        context.control.selectedGrouping = cycleGrouping;
      }
    }
  }

  finalized(): void {
    this.disposeList.dispose();
  }

  //#endregion
}
