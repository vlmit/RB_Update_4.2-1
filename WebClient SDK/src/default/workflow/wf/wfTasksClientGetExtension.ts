import { CardGetExtension, ICardGetExtensionContext } from 'tessa/cards/extensions';
import { Card, CardTask } from 'tessa/cards';
import { hasNotFlag } from 'tessa/platform';
import { CardTypeFlags } from 'tessa/cards/types';
import { extension } from '@tessa/application';
import { WfHelper } from '@tessa/platform';

/**
 * Загрузка карточки с управлением секциями заданий на клиенте.
 */
@extension()
export class WfTasksClientGetExtension extends CardGetExtension {
  async afterRequest(context: ICardGetExtensionContext): Promise<void> {
    let card: Card;
    if (
      !context.requestIsSuccessful ||
      !context.cardType ||
      hasNotFlag(context.cardType.flags, CardTypeFlags.AllowTasks) ||
      !context.response ||
      !(card = context.response.tryGetCard()!)
    ) {
      return;
    }

    const tasks = card.tryGetTasks();
    if (tasks && tasks.length > 0) {
      for (const task of tasks) {
        if (WfHelper.taskTypeIsResolution(task.typeId) && !task.isLocked) {
          WfTasksClientGetExtension.setResolutionFieldChanged(task);
        }
      }
    }
  }

  private static setResolutionFieldChanged(task: CardTask) {
    const taskCard = task.tryGetCard();
    if (!taskCard) {
      return;
    }

    const taskSections = taskCard.tryGetSections();
    if (!taskSections) {
      return;
    }

    const resolutionSection = taskSections.tryGet('WfResolutions');
    if (!resolutionSection) {
      return;
    }

    let fieldsChangingInClosure = false;
    resolutionSection.fields.fieldChanged.add((e, s) => {
      if (fieldsChangingInClosure) {
        return;
      }

      switch (e.fieldName) {
        case 'Planned':
          fieldsChangingInClosure = true;
          s.set('DurationInDays', null);
          fieldsChangingInClosure = false;
          break;
        case 'DurationInDays':
          fieldsChangingInClosure = true;
          s.set('Planned', null);
          fieldsChangingInClosure = false;
          break;
        case 'ShowAdditional':
          if (!e.fieldValue) {
            s.set('KindID', null);
            s.set('KindCaption', null);
            s.set('AuthorID', null);
            s.set('AuthorName', null);
          }
          break;
        case 'WithControl':
          if (!e.fieldValue) {
            s.set('ControllerID', null);
            s.set('ControllerName', null);
          }
          break;
      }
    });
  }
}
