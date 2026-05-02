import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';

import { GlobalButtonsInitializationExtension } from './initialization/globalButtonsInitializationExtension';
import {
  KrCardStoreExtension,
  KrClientCommandCustomExtension,
  KrClientCommandStoreExtension
} from './requests';
import './commandInterpreter/registrator';
import {
  EditStageTypeFormatter,
  ApprovalStageTypeFormatter,
  ChangeStateStageTypeFormatter,
  CreateCardStageTypeFormatter,
  ResolutionStageTypeFormatter,
  SigningStageTypeFormatter,
  ProcessManagementStageTypeFormatter,
  RegistrationStageTypeFormatter,
  TypedTaskStageTypeFormatter,
  UniversalTaskStageTypeFormatter,
  NotificationStageTypeFormatter,
  AddFileFromTemplateStageTypeFormatter,
  DialogsStageTypeFormatter,
  AcquaintanceStageTypeFormatter,
  ForkManagementStageTypeFormatter,
  ForkStageTypeFormatter,
  HistoryManagementStageTypeFormatter
} from './formatters';

export const KrProcessRegistrator: ExtensionRegistrator = {
  async registerTypes() {},
  async registerExtensions(container) {
    container.registerExtension({
      extension: GlobalButtonsInitializationExtension,
      stage: ExtensionStage.AfterPlatform
    });

    // Requests
    container
      .registerExtension({
        extension: KrCardStoreExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: KrClientCommandCustomExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: KrClientCommandStoreExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      });

    // Formatters
    container
      .registerExtension({
        extension: EditStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ApprovalStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ChangeStateStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: CreateCardStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ResolutionStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: SigningStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ProcessManagementStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: RegistrationStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: TypedTaskStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: UniversalTaskStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: NotificationStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: AddFileFromTemplateStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: DialogsStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: AcquaintanceStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ForkManagementStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ForkStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: HistoryManagementStageTypeFormatter,
        stage: ExtensionStage.AfterPlatform
      });
  }
};
