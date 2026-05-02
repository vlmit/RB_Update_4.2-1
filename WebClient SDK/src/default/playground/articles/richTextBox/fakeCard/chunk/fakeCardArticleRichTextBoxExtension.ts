import { IRichExtensionContext, richModuleExtension, RichViewModelExtension } from 'ui/richTextBox';
import { FakeCardArticleDataSource } from '../fakeCardArticleDataSource';
import { FakeCardArticleModuleToken } from '../fakeCardArticleModuleToken';
import { FakeCardService } from '../fakeCardService';

@richModuleExtension({
  name: 'FakeCardArticleRichTextBoxExtension',
  token: FakeCardArticleModuleToken
})
export class FakeCardArticleRichTextBoxExtension extends RichViewModelExtension {
  //#region IRichTextBoxExtension implementation

  async afterCommit({ richTextBox }: IRichExtensionContext): Promise<void> {
    const dataSource = richTextBox.dataSource as FakeCardArticleDataSource;
    const cardService = richTextBox.info!['FakeService'] as FakeCardService;

    const resolver = dataSource.attachmentsContainer.getAttachmentContentResolver();
    await cardService.storeCardWithContent(dataSource.card, async id => {
      const attachment = richTextBox.getAttachments().find(a => a.id === id);
      return attachment ? resolver(attachment) : null;
    });

    dataSource.card = await cardService.getCard();
  }

  //#endregion
}
