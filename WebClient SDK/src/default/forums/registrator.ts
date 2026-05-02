import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { ForumControlUIExtension } from './extensions/forumControlUIExtension';
import { HideForumTabUIExtension } from './extensions/hideForumTabUIExtension';
import { KrSettingsForumsSettingsUIExtension } from './extensions/krSettingsForumsSettingsUIExtension';
import { OpenForumContextMenuViewExtension } from './extensions/openForumContextMenuViewExtension';
import { OpenTopicOnDoubleClickExtension } from './extensions/openTopicOnDoubleClickExtension';
import { TopicsUIExtension } from './extensions/topicsUIExtension';
import { TopicUsersForMentionInitializeExtension } from './extensions/topicUsersForMentionInitializeExtension';
import { TopicUsersForMentionViewExtension } from './extensions/topicUsersForMentionViewExtension';

export const ForumsRegistrator: ExtensionRegistrator = {
  async registerTypes() {},
  async registerExtensions(container) {
    container.registerExtension({
      extension: TopicUsersForMentionInitializeExtension,
      stage: ExtensionStage.Platform,
      singleton: true
    });

    container
      .registerExtension({
        extension: KrSettingsForumsSettingsUIExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 1
      })
      .registerExtension({
        extension: ForumControlUIExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 2
      })
      .registerExtension({
        extension: HideForumTabUIExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 3
      })
      .registerExtension({
        extension: TopicsUIExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 4
      })
      .registerExtension({
        extension: OpenForumContextMenuViewExtension,
        stage: ExtensionStage.Platform,
        order: 5
      })
      .registerExtension({
        extension: OpenTopicOnDoubleClickExtension,
        stage: ExtensionStage.Platform,
        order: 6
      })
      .registerExtension({
        extension: TopicUsersForMentionViewExtension,
        stage: ExtensionStage.Platform,
        singleton: true,
        order: 8
      });
  }
};
