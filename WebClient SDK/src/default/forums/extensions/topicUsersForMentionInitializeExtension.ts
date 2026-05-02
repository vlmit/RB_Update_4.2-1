import { extension } from '@tessa/application';
import { ComponentsRegistry } from '@tessa/ui';
import { ApplicationExtension } from 'tessa/applicationExtension';
import { TopicUsersForMentionViewSelector } from './topicUsersForMention/topicUsersForMentionViewSelector';
import { TopicUsersForMentionViewSelectorViewModel } from './topicUsersForMention/topicUsersForMentionViewSelectorViewModel';

@extension({ name: 'TopicUsersForMentionInitializeExtension' })
export class TopicUsersForMentionInitializeExtension extends ApplicationExtension {
  async initialize(): Promise<void> {
    ComponentsRegistry.instance.register(
      TopicUsersForMentionViewSelectorViewModel,
      TopicUsersForMentionViewSelector
    );
  }
}
