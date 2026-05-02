import { reaction } from 'mobx';
import { DiContainer, extension } from '@tessa/application';
import { CardRow, CardRowsListener, CardRowState } from '@tessa/platform';
import {
  FieldType,
  Guid,
  IStorage,
  StorageHelper,
  TypedJsonConverter,
  ValidationKey,
  ValidationResultType
} from '@tessa/core';
import { ArrayStorage } from 'tessa/platform/storage/arrayStorage';
import { CardUIExtension } from 'tessa/ui/cards/cardUIExtension';
import { ICardUIExtensionContext } from 'tessa/ui/cards/cardUIExtensionContext';
import { ContainerViewModel, GridRowAction } from 'tessa/ui/cards/controls';
import { IFormWithBlocksViewModel } from 'tessa/ui/cards/interfaces';
import { FormCreationOptions } from 'tessa/ui/formCreationOptions';
import { showNotEmpty } from 'tessa/ui/tessaDialog/showNotEmpty';
import { createDialogForm } from 'tessa/ui/uiHelper';
import { ITableRowViewModel } from 'tessa/ui/views/content';
import {
  ITwoFactorAuthConfigurator,
  ITwoFactorAuthConfigurator$
} from 'tessa/ui/login/component/twoFactor/twoFactorAuthConfigurator';
import type { CardTableViewControlViewModel } from './tableViewExtension/cardTableViewControlViewModel';
import type { CardTableViewRowData } from './tableViewExtension/cardTableViewRowData';

/** Расширение для модификации диалога настройки параметров двухфакторной аутентификации для пользователя. */
@extension({ name: 'TwoFactorAuthUserSettingsUIExtension' })
export class TwoFactorAuthUserSettingsUIExtension extends CardUIExtension {
  //#region fields

  // TODO переделать на фабрику this._container.getNamedAsync(ITwoFactorAuthConfigurator$, typeID)
  private readonly _container: DiContainer;
  private readonly _disposers: Array<VoidFunction | null>;
  private _listener: CardRowsListener | null;
  private _selectionContext: {
    configurator: ITwoFactorAuthConfigurator;
    storage: IStorage | null;
    cardRow: CardRow;
    deletion?: boolean;
  } | null;

  //#endregion

  //#region constructors

  constructor() {
    super();
    this._disposers = [];
    this._listener = null;
    this._selectionContext = null;
    this._container = window.tessa.diContainer;
  }

  //#endregion

  //#region base overrides

  public override async initialized(context: ICardUIExtensionContext): Promise<void> {
    if (!context.validationResult.isSuccessful) {
      return;
    }

    const sections = context.card.sections;
    const tableRows = sections.tryGet('TwoFactorAuthUserTypes')?.rows;
    if (!tableRows) {
      return;
    }

    const controls = context.model.controls;
    const tableControl = controls.get('TwoFactorAuthTypes') as CardTableViewControlViewModel;
    const typeSettingsControl = controls.get('TwoFactorAuthTypeSettings') as ContainerViewModel;
    if (!tableControl || !typeSettingsControl) {
      return;
    }

    // Отслеживание изменения поля "По умолчанию" в строках таблицы
    this.onRowFieldChanged(tableRows);
    // Переопределение двойного клика по строке, чтобы форма строки не открывалась
    this.onRowDoubleClick(tableControl);
    // Проверка уникальности выбранного типа 2FA при закрытии формы строки
    this.onRowValidating(tableControl, tableRows);
    // Выделение строки при закрытии формы строки, если форма была закрыта по кнопке "Сохранить"
    this.onRowEditorClosed(tableControl, tableRows);
    // Переопределение действий добавления и удаления строки
    this.onRowAddOrDeleteActions(tableControl);
    // Отслеживание события выделения строки для инициализации настроек нужного типа 2FA
    this.onRowSelected(tableControl, typeSettingsControl, context.card.id);
  }

  public override contextInitialized(context: ICardUIExtensionContext): void {
    // Если мы находимся в диалоге (а это так), то ищем кнопку сохранения диалога
    const button = context.uiContext.cardEditor?.bottomDialogButtons?.find(b => b.name === 'OK');
    if (!button) {
      return;
    }

    // Запоминаем изначальное действие для кнопки
    const buttonAction = button.buttonAction;

    // Переопределяем действие для кнопки сохранения диалога
    button.buttonAction = async args => {
      if (await this.trySetTypeSettings()) {
        buttonAction?.(args);
      }
    };
  }

  public override finalized(_context: ICardUIExtensionContext): void {
    this._selectionContext?.configurator?.dispose();
    this._selectionContext = null;

    this._listener?.dispose();
    this._listener = null;

    for (const disposer of this._disposers) {
      disposer?.();
    }
    this._disposers.length = 0;
  }

  //#endregion

  //#region private methods

  private static selectRow(tableRows: ReadonlyArray<ITableRowViewModel>, rowId: string): void {
    for (const tableRow of tableRows) {
      const tableRowId = (tableRow.data as CardTableViewRowData)?.cardRow.rowId;
      tableRow.selectRow(Guid.equals(tableRowId, rowId));
    }
  }

  private async trySetTypeSettings(): Promise<boolean> {
    if (!this._selectionContext) {
      return true;
    }

    const validationResult = await this._selectionContext.configurator.validateTypeSettings();
    await showNotEmpty(validationResult);
    if (!validationResult.isSuccessful) {
      return false;
    }

    const storage = await this._selectionContext.configurator.getTypeSettings();
    if (!StorageHelper.equals(storage, this._selectionContext.storage)) {
      const json = storage ? TypedJsonConverter.serialize(storage) : null;
      this._selectionContext.cardRow.set('Settings', json, FieldType.String);
    }

    this._selectionContext.configurator.dispose();
    return true;
  }

  private onDefaultChanged(row: CardRow, rows: ArrayStorage<CardRow>): void {
    this._disposers.push(
      row.fieldChanged.add(({ fieldName, fieldValue }) => {
        if (fieldName === 'IsDefault') {
          // Поиск строки, для которой установлен признак "По умолчанию"
          const defaultRow = rows.find(
            r =>
              r.state !== CardRowState.Deleted &&
              !Guid.equals(r.rowId, row.rowId) &&
              r.get('IsDefault')
          );
          if (fieldValue) {
            // Снимаем признак "По умолчанию" для найденной строки
            defaultRow?.set('IsDefault', false, FieldType.Boolean);
          } else if (!defaultRow) {
            // Если кроме текущей строки нету больше строк с признаком "По умолчанию", то не даем сбрасывать этот признак
            row.rawSet('IsDefault', true, FieldType.Boolean);
          }
        }
      })
    );
  }

  private onRowFieldChanged(rows: ArrayStorage<CardRow>): void {
    // Подписка на изменение поля "По умолчанию" для всех существующих строк
    for (const row of rows) {
      this.onDefaultChanged(row, rows);
    }

    // Создание наблюдателя за добавлением / удалением строк
    this._listener = new CardRowsListener();

    // Создание обработчика для добавленных строк
    this._listener.rowInserted.add(({ row, storage }) => {
      // Если в таблице нет больше строк кроме только что добавленной, то устанавливаем признак "По умолчанию"
      const rowId = row.rowId;
      if (!storage.some(r => r.state !== CardRowState.Deleted && !Guid.equals(r.rowId, rowId))) {
        row.set('IsDefault', true, FieldType.Boolean);
      }
      // Подписка на изменение поля "По умолчанию" для добавленной строки
      this.onDefaultChanged(row, storage);
    });

    // Создание обработчика для удаленных строк
    this._listener.rowDeleted.add(({ row, storage }) => {
      // Если для строки был установлен признак "По умолчанию", то находим
      // первую строку и устанавливаем признак "По умолчанию" для нее
      if (row.get('IsDefault')) {
        storage
          .find(row => row.state !== CardRowState.Deleted)
          ?.set('IsDefault', true, FieldType.Boolean);
      }
    });

    // Запускаем наблюдателя для строк
    this._listener.start(rows);
  }

  private onRowDoubleClick(tableControl: CardTableViewControlViewModel): void {
    const tableRowActions = tableControl.table?.modifyRowActions;
    if (tableRowActions) {
      const action = () => {};
      this._disposers.push(tableRowActions.addWithDispose(row => (row.onDoubleClick = action)));
    }
  }

  private onRowValidating(
    tableControl: CardTableViewControlViewModel,
    rows: ArrayStorage<CardRow>
  ): void {
    this._disposers.push(
      tableControl.rowValidating.addWithDispose(({ row, validationResult }) => {
        const rowId = row.rowId;
        const typeId = row.get<string>('TypeID');

        // Поиск строки в таблице с таким же типом 2FA
        if (
          rows.some(
            r =>
              r.state !== CardRowState.Deleted &&
              !Guid.equals(r.rowId, rowId) &&
              Guid.equals(r.get<string>('TypeID'), typeId)
          )
        ) {
          validationResult.add(
            ValidationKey.unknown,
            ValidationResultType.Error,
            '$CardTypes_Validators_TwoFactorAuthTypeNotUnique'
          );
        }
      })
    );
  }

  private onRowEditorClosed(
    tableControl: CardTableViewControlViewModel,
    rows: ArrayStorage<CardRow>
  ): void {
    this._disposers.push(
      tableControl.rowEditorClosed.addWithDispose(args => {
        const rowId = args.row.rowId;
        const tableRows = args.control.table?.rows;

        // Если была отмена или строка не была добавлена, то ничего не делаем
        if (!tableRows || args.cancel || args.action !== GridRowAction.Inserted) {
          return;
        }

        // Если для текущей строки в хранилище отсутствует строка с данными,
        // значит форма строки таблицы была закрыта по кнопке "Отмена"
        if (!rows.some(r => r.state !== CardRowState.Deleted && Guid.equals(r.rowId, rowId))) {
          return;
        }

        // Сбрасываем выделение предыдущей строки и устанавливаем выделение для текущей строки
        TwoFactorAuthUserSettingsUIExtension.selectRow(tableRows, rowId);
      })
    );
  }

  private onRowAddOrDeleteActions(tableControl: CardTableViewControlViewModel): void {
    // Сохранение изначального действия с учетом контекста
    const addRowAction = tableControl.addRowAction.bind(tableControl);
    // Переопределение действия добавления строки
    tableControl.addRowAction = async () => {
      if (this._selectionContext?.configurator) {
        const validationResult = await this._selectionContext.configurator.validateTypeSettings();
        await showNotEmpty(validationResult);
        if (!validationResult.isSuccessful) {
          return;
        }
      }

      if (addRowAction) {
        await addRowAction();
      }
    };

    // Сохранение изначального действия с учетом контекста
    const deleteRowsAction = tableControl.deleteRowsAction.bind(tableControl);
    // Переопределение действия добавления строки
    tableControl.deleteRowsAction = async () => {
      if (this._selectionContext) {
        this._selectionContext.deletion = true;
      }

      if (deleteRowsAction) {
        await deleteRowsAction();
      }
    };
  }

  private onRowSelected(
    tableControl: CardTableViewControlViewModel,
    containerControl: ContainerViewModel,
    cardId: string
  ): void {
    // Сохраняем изначальную форму контейнера, чтобы использовать ее
    const defaultContainerForm = containerControl.form;

    // Создаем действие на реакцию изменения выбранной строки
    const onRowSelectedReaction = async () => {
      // Получаем текущую выделенную строку
      const cardRow = (tableControl.selectedRow as CardTableViewRowData)?.cardRow;

      // Если в контексте задана строка и происходит ее удаление
      // (здесь мы не можем опираться на CardRow.state, так как строка может быть еще не была удалена)
      if (this._selectionContext?.cardRow && !this._selectionContext?.deletion) {
        const rowId = this._selectionContext?.cardRow.rowId;
        // Проверяем, что текущая выделенная строка не равна строке в контексте
        if (!cardRow || Guid.equals(rowId, cardRow.rowId)) {
          return;
        }

        // Сохраняем информацию по ранее выделенной строке перед сбросом контекста
        if (!(await this.trySetTypeSettings())) {
          this._selectionContext.deletion = false;
          // Сбрасываем выделение текущей строки и устанавливаем выделение для строки в контексте
          TwoFactorAuthUserSettingsUIExtension.selectRow(tableControl.table!.rows, rowId);
          return;
        }
      }

      // Сбрасываем контекст строки
      this._selectionContext = null;
      // Устанавливаем форму контейнера по умолчанию
      containerControl.form = defaultContainerForm;

      // Достаем псевдоним типа 2FA, чтобы по нему найти конфигуратор
      const typeID = cardRow?.get<string>('TypeID');
      if (!typeID) {
        return;
      }

      // Получаем необходимый конфигуратор из IoC-контейнера
      if (!this._container.isBoundNamed(ITwoFactorAuthConfigurator$, typeID)) {
        return;
      }
      const configurator = await this._container.getNamedAsync(ITwoFactorAuthConfigurator$, typeID);

      // Проверяем, что в конфигураторе задан псевдоним для диалога
      if (!configurator.settingsName) {
        return;
      }

      // Получаем настройки для типа 2FA из строки и пытаемся их десериализовать
      const json = cardRow.tryGetString('Settings');
      const storage = json ? TypedJsonConverter.deserialize(json) : null;

      // Устанавливаем текущий контекст
      this._selectionContext = { configurator, storage, cardRow };

      // Создаем диалог по псевдониму для настроек типа 2FA
      const createDialogResult = await createDialogForm(
        configurator.settingsName,
        undefined,
        FormCreationOptions.None,
        undefined,
        async response => {
          // Тут мы получили карточку для созданного диалога, можно ее инициализировать
          if (response.validationResult.isSuccessful) {
            response.card.id = cardId;
            await configurator.setTypeSettings(response.card, storage);
          }
        }
      );

      if (createDialogResult) {
        // Выполняем модификацию формы и модели диалогового окна с помощью конфигуратора
        const [form, cardModel] = createDialogResult;
        await configurator.modifySettingsModel(form, cardModel);
        // После инициализации присваиваем форму для контейнера
        containerControl.form = form as IFormWithBlocksViewModel;
      }
    };

    this._disposers.push(reaction(() => tableControl.selectedRow, onRowSelectedReaction));
  }

  //#endregion
}
