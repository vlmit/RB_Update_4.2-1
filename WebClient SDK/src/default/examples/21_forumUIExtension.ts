import { extension } from '@tessa/application';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { ForumViewModel, IAttachmentMenuContext } from 'tessa/ui/cards/controls';
import { UIButton, showMessage, MenuAction } from 'tessa/ui';
import { runInAction } from 'mobx';

/**
 * Для выбранной карточки добавляет элементы управления:
 * - В верхней панели текущего обсуждения.
 * - В контекстное меню текущего обсуждения.
 *
 * Результат работы расширения:
 * В верхней панели, а также в контекстном меню текущего обсуждения добавлены
 * соответствующие тестовые кнопка и пункт контекстного меню для тестовой карточки "Автомобиль".
 */
@extension()
export class ForumUIExtension extends CardUIExtension {
  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // пытаемся найти контрол "Обсуждения"
    const forumControl = this.tryGetForumControl(context);
    if (!forumControl) {
      return;
    }

    // добавляем пункт тестовый пункт в контекстном меню текущего обсуждения
    forumControl.attachmentContextMenuGenerators.push(this.testMenuGenerator);

    // добавляем контект в найденный контрол
    this.disposeList.add(
      forumControl.addOnContentChangedAndInvoke(() => {
        const editor = forumControl.topicEditor;
        if (!editor) {
          return;
        }

        // добавляем тестовую кнопку в верхнюю панель текущего обсуждения
        runInAction(() => {
          if (editor.rightButtons.find(c => c.name === 'TestButton')) {
            return;
          }
          editor.rightButtons.push(
            UIButton.create({
              name: 'TestButton',
              icon: 'icon-thin-100',
              type: 'small',
              theme: 'transparent',
              buttonAction: async () => {
                await showMessage('Hello from test button!');
              }
            })
          );
        });
      })!
    );
  }

  override async finalized(): Promise<void> {
    this.disposeList.dispose();
  }

  private tryGetForumControl(context: ICardUIExtensionContext): ForumViewModel | null {
    // пытаемся найти вкладку "Обсуждения"
    const forumTab = context.model.forms.find(x => x.name === 'Forum');
    if (!forumTab) {
      return null;
    }

    // пытаемся найти блок "Обсуждения" в полученной вкладке
    const topicsBlock = forumTab.blocks.find(x => x.name === 'Topics');
    if (!topicsBlock) {
      return null;
    }

    // пытаемся найти соответствующий контрол
    const forumControl = topicsBlock.controls[0] as ForumViewModel;
    if (!forumControl || !(forumControl instanceof ForumViewModel)) {
      return null;
    }

    return forumControl;
  }

  private testMenuGenerator(ctx: IAttachmentMenuContext) {
    ctx.menuActions.push(
      MenuAction.create({
        type: 'normal',
        name: 'TestMenu',
        caption: 'TestMenu',
        icon: 'icon-thin-099',
        action: async () => {
          await showMessage('Hello from test menu!');
        }
      })
    );
  }
}
