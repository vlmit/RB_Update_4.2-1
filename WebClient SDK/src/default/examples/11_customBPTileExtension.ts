import { Guid, TypedField } from '@tessa/core';
import { extension } from '@tessa/application';
import { CardStoreMode } from '@tessa/platform';
import {
  TileExtension,
  ITileGlobalExtensionContext,
  TileEvaluationEventArgs,
  TileGroups,
  Tile
} from 'tessa/ui/tiles';
import { UIContext } from 'tessa/ui';
import { TestCardTypeID } from './common';

/**
 * Добавление тайла запуска бизнес процесса в левую панель для определенного типа карточек.
 *
 * Результат работы расширения:
 * На левую панель для карточки "Автомобиль" добавляет тайл - “Запустить кастомный БП”. При нажатии на тайл
 * запускается БП (добавляется ключ .startProcess с значением TestProcess).
 */
@extension()
export class CustomBPTileExtension extends TileExtension {
  override async initializingGlobal(context: ITileGlobalExtensionContext): Promise<void> {
    // получаем доступ к левой боковой панели
    const panel = context.workspace.leftPanel;

    // создаем тайл
    const tile = new Tile({
      name: 'StartCustomBPTileExtension',
      caption: 'Запустить кастомный БП',
      icon: 'ta icon-thin-002',
      contextSource: panel.contextSource,
      group: TileGroups.CardsTop,
      order: 100,
      command: CustomBPTileExtension.startCustomBP,
      evaluating: CustomBPTileExtension.enableIfCard
    });

    // добавляем созданный тайл в левую боковую панель
    panel.tiles.push(tile);
  }

  private static async startCustomBP() {
    // пытаемся получить контекст
    const context = UIContext.current;
    const editor = context.cardEditor;

    if (!editor || !editor.cardModel) {
      return;
    }

    // если карточка новая, то сохраняем её
    const cardIsNew = editor.cardModel.card.storeMode === CardStoreMode.Insert;
    if (cardIsNew) {
      const saved = await editor.saveCard(context);
      if (!saved) {
        return;
      }
    }

    // сохраняем карточку и записываем флажок в info
    // флажок нуобходимо добавлять через createTypedField, чтобы на сервере правильно отрезолвить тип
    editor.saveCard(context, {
      '.startProcess': TypedField.createString('TestProcess')
    });
  }

  private static enableIfCard(e: TileEvaluationEventArgs) {
    const editor = UIContext.current.cardEditor;
    e.setIsEnabledWithCollapsing(
      e.currentTile,
      !!editor && !!editor.cardModel && Guid.equals(editor.cardModel.cardType.id, TestCardTypeID) // проверяем, что карточка тестовая
    );
  }
}
