import { ExtensionStage } from '@tessa/application';
import { RichModule, RichViewModelExtension } from 'ui/richTextBox';
import { FakeCardArticleModuleToken } from './fakeCardArticleModuleToken';

export const FakeCardArticleModule = new RichModule({
  name: FakeCardArticleModuleToken.description!,
  token: FakeCardArticleModuleToken,
  includeType: 'optional',
  async registerExtensions(container) {
    container
      .asLazy(
        () => import(/* webpackChunkName: "playground-rich-modules" */ './chunk'),
        RichViewModelExtension
      )
      .registerExtension({
        extension: m => m.FakeCardArticleRichTextBoxExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: m => m.FakeCardArticleToolbarRichTextBoxExtension,
        stage: ExtensionStage.BeforePlatform
      });
  }
});
