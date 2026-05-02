import { UIButton } from 'tessa/ui/uiButton';
import { DefaultEventHandlers } from 'ui/hooks/defaultEventHandlers';
import { IRichExtensionContext, richModuleExtension, RichViewModelExtension } from 'ui/richTextBox';
import { group } from 'ui/toolbar/helpers';
import { FakeCardArticleModuleToken } from '../fakeCardArticleModuleToken';

@richModuleExtension({
  name: 'FakeCardArticleToolbarRichTextBoxExtension',
  token: FakeCardArticleModuleToken
})
export class FakeCardArticleToolbarRichTextBoxExtension extends RichViewModelExtension {
  //#region IRichTextBoxExtension implementation

  async viewModelInitialized({ richTextBox }: IRichExtensionContext): Promise<void> {
    richTextBox.toolbar.addButtons(
      UIButton.create({
        name: 'commit',
        key: 'commit',
        icon: 'm-save',
        onMouseDown: async e => {
          DefaultEventHandlers.terminate(e);
          await richTextBox.commit();
        },
        theme: 'transparent',
        type: 'toolbar'
      })
    );

    richTextBox.toolbar.addGroups(group('generateContentExtension', ['commit']));
  }

  //#endregion
}
