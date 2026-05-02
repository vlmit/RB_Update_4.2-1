import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { CardHelper, whenCardTypeIdIs, WorkflowCardTypes } from '@tessa/platform';
import { ComponentsRegistry } from '@tessa/ui';

import { CalendarUIExtension } from './calendarUIExtension';
import { CardDialogPreviewUIExtension } from './cardDialogPreviewUIExtension';
import { InitializeFilesViewUIExtension } from './cardFiles/initializeFilesViewUIExtension';
import { CreateAndSelectToolbarUIExtension } from './createAndSelectToolbarUIExtension';
import { KrCardTasksEditorUIExtension } from './krCardTasksEditorUIExtension';
import { KrDocStateUIExtension } from './krDocStateUIExtension';
import { KrExtendedPermissionsUIExtension } from './krExtendedPermissionsUIExtension';
import { KrGetCycleFileInfoUIExtension } from './krGetCycleFileInfoUIExtension';
import { KrPermissionsUIExtension } from './krPermissionsUIExtension';
import { KrVirtualFilesUIExtension } from './krVirtualFilesUIExtension';
import { OpenCardInViewUIExtension } from './openCardInViewUIExtension';
import { OutgoingPartnerUIExtension } from './outgoingPartnerUIExtension';
import { MakeViewTableControlFormUIExtension } from './tableViewExtension/makeViewTableControlFormUIExtension';
import { MakeViewTableControlUIExtension } from './tableViewExtension/makeViewTableControlUIExtension';
import { TagCardsViewExtension } from './tagCardsViewExtension';
import { MakeViewTaskHistoryUIExtension } from './taskHistory/makeViewTaskHistoryUIExtension';
import { UIErrorPresenterButtonsUIExtension } from './uiErrorPresenterButtonsUIExtension';
import { KrCheckStateTileManagerUIExtension } from './workflowEngine/krCheckStateTileManagerUIExtension';
import KrRoutesInWorkflowEngineUIExtension from './workflowEngine/krRoutesInWorkflowEngineUIExtension';
import { RoleHelper } from 'tessa/roles';
import { DefaultCardTypes } from '../defaultCardTypes';
import { FilesViewControlTableGridViewModel } from './cardFiles/filesViewControlTableGridViewModel';
import { FilesViewControlTableView } from './cardFiles/filesViewControlTableView';
import { InitializeFilesViewFormUIExtension } from './cardFiles/initializeFilesViewFormUIExtension';
import { ShowContextMenuButton } from './cardFiles/showContextMenuButton';
import { ShowContextMenuButtonViewModel } from './cardFiles/showContextMenuButtonViewModel';
import { CardImportUIExtension } from './cardImportUIExtension';
import { CardMobilePreviewUIExtension } from './cardMobilePreviewUIExtension';
import { CardModePreviewButtonsUIExtension } from './cardModePreviewButtonsUIExtension';
import { CardModeUIExtension } from './cardModeUIExtension';
import { FullSizeTextBlockView, FullSizeTextBlockViewModel } from './fullSizeTextBlockViewModel';
import { HelpSectionCardPreviewerUIExtension } from './helpSectionCardPreviewerUIExtension';
import { IncomingOpenAiAssistantUIExtension } from './incomingOpenAiAssistantUIExtension';
import { MySettingsCompactModeExtension } from './mySettingsCompactModeExtension';
import { PdfAnnotationsFileControlExtension } from './pdfAnnotationsFileControlExtension';
import { PdfAnnotationsFileExtension } from './pdfAnnotationsFileExtension';
import { PdfAnnotationsFileVersionExtension } from './pdfAnnotationsFileVersionExtension';
import { PersonalRoleAvatarUIExtension } from './personalRoleAvatarUIExtension';
import { SessionExpireAlertExtension } from './sessionExpireAlertExtension/sessionExpireAlertExtension';
import { SessionMenuButtonExtension } from './sessionExpireAlertExtension/sessionMenuButtonExtension';
import {
  CardTableViewFormContainer,
  CardTableViewFormContainerViewModel
} from './tableViewExtension/cardTableViewFormContainer';
import {
  CardTableViewPanel,
  CardTableViewPanelViewModel
} from './tableViewExtension/cardTableViewPanel';
import { TextFieldAiAssistantUIExtension } from './textFieldAiAssistantUIExtension';
import { TwoFactorAuthUserSettingsUIExtension } from './twoFactorAuthUserSettingsUIExtension';
import { DocLoadFilesBehaviorSettingsUIExtension } from './docLoad/docLoadFilesBehaviorSettingsUIExtension';
import { OpenAiAssistantUIExtension } from './openAiAssistantUIExtension';

export const UIRegistrator: ExtensionRegistrator = {
  async registerTypes() {
    ComponentsRegistry.instance.register(ShowContextMenuButtonViewModel, ShowContextMenuButton);
    ComponentsRegistry.instance.register(
      FilesViewControlTableGridViewModel,
      FilesViewControlTableView
    );
    ComponentsRegistry.instance.register(CardTableViewPanelViewModel, CardTableViewPanel);
    ComponentsRegistry.instance.register(
      CardTableViewFormContainerViewModel,
      CardTableViewFormContainer
    );
    ComponentsRegistry.instance.register(FullSizeTextBlockViewModel, FullSizeTextBlockView);
  },
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: KrGetCycleFileInfoUIExtension,
        stage: ExtensionStage.BeforePlatform
      })
      .registerExtension({
        extension: KrCheckStateTileManagerUIExtension,
        stage: ExtensionStage.Platform
      })
      // TODO временно убираем и рисуем таски всегда в отдельной вкладке
      // .registerExtension({
      //   extension: CardToolbarTaskButtonUIExtension,
      //   stage: ExtensionStage.Platform
      // })
      .registerExtension({
        extension: CalendarUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(CardHelper.CalendarTypeID)
      })
      .registerExtension({
        extension: OutgoingPartnerUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrDocStateUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: CreateAndSelectToolbarUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrVirtualFilesUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs('81250a95-5c1e-488c-a423-106e7f982c6b') // KrVirtualFileTypeID
      })
      .registerExtension({
        extension: KrPermissionsUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(WorkflowCardTypes.KrPermissionsTypeID)
      })
      .registerExtension({
        extension: KrExtendedPermissionsUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrRoutesInWorkflowEngineUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: InitializeFilesViewUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: InitializeFilesViewFormUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: MakeViewTaskHistoryUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: MakeViewTableControlUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: MakeViewTableControlFormUIExtension,
        stage: ExtensionStage.Platform,
        singleton: true
      })
      .registerExtension({
        extension: OpenCardInViewUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: CardDialogPreviewUIExtension,
        stage: ExtensionStage.Platform,
        order: 999
      })
      .registerExtension({
        extension: UIErrorPresenterButtonsUIExtension,
        stage: ExtensionStage.Platform,
        singleton: true
      })
      .registerExtension({
        extension: KrCardTasksEditorUIExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 51
      })
      .registerExtension({
        extension: TagCardsViewExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 52
      })
      .registerExtension({
        extension: PdfAnnotationsFileExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 54
      })
      .registerExtension({
        extension: PdfAnnotationsFileVersionExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 56
      })
      .registerExtension({
        extension: MySettingsCompactModeExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 57
      })
      .registerExtension({
        extension: CardModePreviewButtonsUIExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 58
      })
      .registerExtension({
        extension: CardMobilePreviewUIExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 59
      })
      .registerExtension({
        extension: CardModeUIExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 60
      })
      .registerExtension({
        extension: HelpSectionCardPreviewerUIExtension,
        stage: ExtensionStage.BeforePlatform,
        when: whenCardTypeIdIs(CardHelper.HelpSectionTypeID),
        order: 61
      })
      .registerExtension({
        extension: PdfAnnotationsFileControlExtension,
        stage: ExtensionStage.BeforePlatform,
        order: 62
      })
      .registerExtension({
        extension: SessionExpireAlertExtension,
        stage: ExtensionStage.Platform,
        order: 63
      })
      .registerExtension({
        extension: TwoFactorAuthUserSettingsUIExtension,
        stage: ExtensionStage.Platform,
        when: whenCardTypeIdIs(CardHelper.TwoFactorAuthUserSettingsTypeID),
        order: 64
      })
      .registerExtension({
        extension: PersonalRoleAvatarUIExtension,
        stage: ExtensionStage.Platform,
        when: whenCardTypeIdIs(RoleHelper.personalRoleTypeId),
        order: 65
      })
      .registerExtension({
        extension: SessionMenuButtonExtension,
        stage: ExtensionStage.Finalize,
        order: 66
      })
      .registerExtension({
        extension: IncomingOpenAiAssistantUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(DefaultCardTypes.IncomingTypeID),
        order: 67
      })
      .registerExtension({
        extension: TextFieldAiAssistantUIExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 68
      })
      .registerExtension({
        extension: OpenAiAssistantUIExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 69
      })
      .registerExtension({
        extension: DocLoadFilesBehaviorSettingsUIExtension,
        stage: ExtensionStage.Platform,
        when: whenCardTypeIdIs('33023ffa-2fd3-4b3b-80d9-bba6ab48ea8e'),
        order: 70
      })
      .registerExtension({
        extension: CardImportUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(CardHelper.CardImportTypeID)
      });
  }
};
