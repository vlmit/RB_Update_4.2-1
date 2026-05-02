import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { ApiAccessTokensViewLayoutExtension } from './apiAccessTokensViewLayoutExtension';
import { ApiAccessTokensInfoLayoutExtension } from './apiAccessTokensInfoLayoutExtension';
import { ApiAccessTokensTokenLayoutExtension } from './apiAccessTokensTokenLayoutExtension';
import { ApiAccessTokensRevokeLayoutExtension } from './apiAccessTokensRevokeLayoutExtension';
import { ApiAccessTokensHistoryViewExtension } from './apiAccessTokensHistoryViewExtension';
import { ApiAccessTokensTileExtension } from './apiAccessTokensTileExtension';
import { ApiAccessTokensHelper } from './apiAccessTokensHelper';
import { whenFormAliasIs } from 'tessa/ui/formEditor/extensions/formWhens';

export const ApiAccessTokensRegistrator: ExtensionRegistrator = {
  async registerTypes() {},
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: ApiAccessTokensViewLayoutExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenFormAliasIs(ApiAccessTokensHelper.ApiAccessTokensViewLayout)
      })
      .registerExtension({
        extension: ApiAccessTokensInfoLayoutExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenFormAliasIs(ApiAccessTokensHelper.ApiAccessTokensInfoLayout)
      })
      .registerExtension({
        extension: ApiAccessTokensTokenLayoutExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenFormAliasIs(ApiAccessTokensHelper.ApiAccessTokensTokenLayout)
      })
      .registerExtension({
        extension: ApiAccessTokensRevokeLayoutExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenFormAliasIs(ApiAccessTokensHelper.ApiAccessTokensRevokeLayout)
      })
      .registerExtension({
        extension: ApiAccessTokensTileExtension,
        stage: ExtensionStage.Finalize,
        singleton: true
      })
      .registerExtension({
        extension: ApiAccessTokensHistoryViewExtension,
        stage: ExtensionStage.Finalize,
        singleton: true
      });
  }
};
