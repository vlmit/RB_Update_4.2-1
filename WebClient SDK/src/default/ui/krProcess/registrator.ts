import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';

import { KrUIExtension } from './krUIExtension';
import { KrStageUIExtension } from './krStageUIExtension';
import { KrStageDialogTitleUIExtension } from './krStageDialogTitleUIExtension';
import { KrStageTemplateUIExtension } from './krStageTemplateUIExtension';
import { KrStageSourceUIExtension } from './krStageSourceUIExtension';
import { KrRecalcStagesUIExtension } from './krRecalcStagesUIExtension';
import { KrHideCardTypeSettingsUIExtension } from './krHideCardTypeSettingsUIExtension';
import { KrHideApprovalTabOrDocStateBlockUIExtension } from './krHideApprovalTabOrDocStateBlockUIExtension';
import { KrHideApprovalStagePermissionsDisclaimer } from './krHideApprovalStagePermissionsDisclaimer';
import { KrDocumentWorkspaceInfoUIExtension } from './krDocumentWorkspaceInfoUIExtension';
import { KrCommentRequestUIExtension } from './krCommentRequestUIExtension';
import { KrTilesUIExtension } from './krTilesUIExtension';
import { KrAdditionalApprovalCardUIExtension } from './krAdditionalApprovalCardUIExtension';
import { KrTemplateUIExtension } from './krTemplateUIExtension';
import { KrSecondaryProcessUIExtension } from './krSecondaryProcessUIExtension';
import { KrEditModeToolbarUIExtension } from './krEditModeToolbarUIExtension';
import {
  ResolutionStageUIHandler,
  CreateCardUIHandler,
  ProcessManagementUIHandler,
  UniversalTaskStageTypeUIHandler,
  AddFromTemplateUIHandler,
  DialogUIHandler,
  TabCaptionUIHandler,
  ApprovalUIHandler,
  TypedTaskUIHandler,
  SigningUIHandler,
  ApprovalProcessStageTypeUIHandler
} from './stageHandlers';
import Platform from 'common/platform';
import { DialogExtensionPolicy } from 'tessa/ui/policies';
import { ApprovalProcessManagementStageTypeUIHandler } from './stageHandlers/approvalProcessManagementStageTypeUIHandler';
import { whenCardTypeIdIs } from 'tessa/cards/extensions';
import { ISignFilesProvider$, SignFilesProviderMobile, SignFilesProviderWeb } from './signFiles';

export const UIKrProcessRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    container
      .bind(ISignFilesProvider$)
      .to(Platform.isMobile() ? SignFilesProviderMobile : SignFilesProviderWeb)
      .inSingletonScope();
  },
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: KrUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrStageUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrStageDialogTitleUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrStageTemplateUIExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: KrStageSourceUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrRecalcStagesUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrHideCardTypeSettingsUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrHideApprovalTabOrDocStateBlockUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrHideApprovalStagePermissionsDisclaimer,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrDocumentWorkspaceInfoUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrCommentRequestUIExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: KrTilesUIExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: KrAdditionalApprovalCardUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrTemplateUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrSecondaryProcessUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs('61420fa1-cc1f-47cb-b0bb-4ea8ee77f51a')
      })
      .registerExtension({
        extension: KrEditModeToolbarUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: DialogExtensionPolicy.NoOrDefaultDialogPolicy
      });

    // StageHandlers
    container
      .registerExtension({
        extension: AddFromTemplateUIHandler,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ApprovalUIHandler,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: CreateCardUIHandler,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: DialogUIHandler,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: UniversalTaskStageTypeUIHandler,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ProcessManagementUIHandler,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ResolutionStageUIHandler,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: TabCaptionUIHandler,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: TypedTaskUIHandler,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: SigningUIHandler,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ApprovalProcessStageTypeUIHandler,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ApprovalProcessManagementStageTypeUIHandler,
        stage: ExtensionStage.AfterPlatform
      });
  }
};
