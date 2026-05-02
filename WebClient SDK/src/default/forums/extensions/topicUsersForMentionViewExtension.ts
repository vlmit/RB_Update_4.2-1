import { extension } from '@tessa/application';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { IWorkplaceViewComponent } from 'tessa/ui/views/workplaceViewComponent';
import { TopicUsersForMentionViewSelectorViewModel } from './topicUsersForMention/topicUsersForMentionViewSelectorViewModel';

@extension({ name: 'TopicUsersForMentionViewExtension' })
export class TopicUsersForMentionViewExtension extends WorkplaceViewComponentExtension {
  //#region base overrides

  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Forums.TopicUsersForMentionViewExtension';
  }

  public override initialize(model: IWorkplaceViewComponent): void {
    model.contentFactories.set(
      'TopicUsersForMentionViewSelector',
      c => new TopicUsersForMentionViewSelectorViewModel(c)
    );
  }

  //#endregion
}
