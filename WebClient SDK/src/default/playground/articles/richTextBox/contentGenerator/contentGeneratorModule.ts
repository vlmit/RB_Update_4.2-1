import { ExtensionStage } from '@tessa/application';
import { RichModule, RichViewModelExtension } from 'ui/richTextBox';
import { ContentGeneratorModuleToken } from './contentGeneratorModuleToken';

export const ContentGeneratorModule = new RichModule({
  name: ContentGeneratorModuleToken.description!,
  token: ContentGeneratorModuleToken,
  includeType: 'optional',
  async registerExtensions(container) {
    container
      .asLazy(
        () => import(/* webpackChunkName: "playground-rich-modules" */ './chunk'),
        RichViewModelExtension
      )
      .registerExtension({
        extension: m => m.ContentGeneratorRichTextBoxExtension,
        stage: ExtensionStage.BeforePlatform
      });
  }
});
