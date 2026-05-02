import { TestCardTypeID } from './common';
import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { whenCardTypeIdIs } from '@tessa/platform';
import { ComponentsRegistry } from '@tessa/ui';
import { ILoginComponentViewModel$ } from 'tessa/ui/login/component/loginComponentViewModel';
import { HideBlockByRefValueUIExtension } from './1_hideBlockByRefValueUIExtension';
import { ChangeFieldOrRowUIExtension } from './2_changeFieldOrRowUIExtension';
import { TableSectionChangedUIExtension } from './3_tableSectionChangedUIExtension';
import { HideFormUIExtension } from './4_hideFormUIExtension';
import { HideTaskBlockUIExtension } from './5_hideTaskBlockUIExtension';
import { SimpleCardTileExtension } from './6_simpleCardTileExtension';
import { SimpleViewTileExtension } from './7_simpleViewTileExtension';
import { RequestTileExtension } from './8_requestTileExtension';
import { AdditionalTableButtonUIExtension } from './9_additionalTableButtonUIExtension';
import { DialogModeAutocompleteUIExtension } from './10_dialogModeAutocompleteUIExtension';
import { CustomBPTileExtension } from './11_customBPTileExtension';
import { FileControlUIExtension } from './12_fileControlUIExtension';
import { HideTileExtension } from './13_hideTileExtension';
import { TableControlDoubleClickUIExtension } from './14_tableControlDoubleClickUIExtension';
import { ShowFormDialogUIExtension } from './15_showFormDialogUIExtension';
import { ShowCustomDialogUIExtension } from './16_showCustomDialogUIExtension';
import {
  CloseCardOnCompleteTaskStoreExtension,
  CloseCardOnCompleteTaskUIExtension
} from './17_closeCardOnCompleteTaskExtension';
import {
  AdditionalMetaInitializationExtension,
  AdditionalMetaTileExtension
} from './18_additionalMetaExtension';
import {
  CustomThemePropApplicationExtension,
  CustomThemePropUIExtension
} from './19_customThemeProp';
import { CustomTabPanelButtonUIExtension } from './20_tabPanelButtonUIExtension';
import { ForumUIExtension } from './21_forumUIExtension';
import './22_serviceClient';
import { ExampleLoginComponentViewModel, ExampleLoginExtension } from './23_loginExtension';
import { ExampleFormUIExtension } from './24_formUIExtension';
import { GridBasicStylingExtension } from './25_gridBasicStylingExtension';
import { GridLayoutStylingExtension } from './26_gridLayoutStylingExtension';
import './27_horizontalScrollViews';
import { AbTaskEnableAttachFilesExampleUIExtension } from './28_abTaskEnableAttachFilesExampleUIExtension';
import { registerSliderControlTypes } from './29_sliderControl/29_sliderControlRegistrator';
import { SliderMetadataExtension } from './29_sliderControl/29_sliderMetadataExtension';
import { registerExamplePreviewerTypes } from './30_examplePreviewer/30_examplePreviewerRegistrator';
import { ExamplePreviewerExtension } from './30_examplePreviewer/30_examplePreviewerExtension';
import { registerExampleHeaderTypes } from './31_exampleHeader/31_exampleHeaderRegistrator';
import { ExampleHeaderUIExtension } from './31_exampleHeader/31_exampleHeaderUIExtension';
import { OcrPreviewerUIExtension } from './32_ocrPreviewerUIExtension';
import { ControlsCustomButtonUIExtension } from './34_controlsCustomButtonUIExtension';
import { PdfAnnsCarFileExtension } from './35_pdfAnnsExampleFileExtension';
import { NumberProperty } from './36_propertyGrid/properties/36_numberPropertyViewModel';
import { NumberPropertyComponent } from './36_propertyGrid/properties/36_numberPropertyView';
import { PropertyGridMetadataExtension } from './36_propertyGrid/36_propertyGridMetadataExtension';
import { PropertyGridUIExtension } from './36_propertyGrid/36_propertyGridUIExtension';
import { GroupedViewToolbarExtension } from './37_groupedViewToolbarExtension';
import './38_loadingUIOverride';
import { MyTasksWidget } from './39_myTasksWidget/39_myTasksWidget';
import { MyTasksWidgetType } from './39_myTasksWidget/39_myTasksWidgetType';
import { ButtonWidgetComponent } from '../dashboard/widgets/button/buttonWidgetComponent';
import { IDashboardWidgetType$ } from 'tessa/ui/dashboard';
import { AutomobileTourViewExtension } from './40_automobileTour';
import { registerCustomUserInfoTypes } from './41_customUserInfo/41_customUserInfoRegistrator';
import { SetTextBoxMaskUIExtension } from './43_setTextBoxMaskUIExtension';
import { registerSolutionMessenger } from './44_solutionMessenger/solutionMessengerRegistrator';
import { AbSideFormUIExtension } from './45_abSideFormUIExtension/45_abSideFormUIExtension';
import { registerExampleTaskInfo } from './46_taskInfoExample/46_taskInfoExampleRegistrator';
import { SettingsAppMenuActionMobileClientExtension } from './47_settingsAppMenuActionMobileClientExtension';

export const ExamplesRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    container
      .bind(ILoginComponentViewModel$)
      .to(ExampleLoginComponentViewModel)
      .inRequestScope()
      .whenTargetNamed('ExampleLoginComponentViewModel');

    registerSliderControlTypes();
    registerExamplePreviewerTypes();
    registerExampleHeaderTypes();
    registerExampleTaskInfo();
    registerCustomUserInfoTypes(container);
    registerSolutionMessenger();

    ComponentsRegistry.instance.register(NumberProperty, NumberPropertyComponent);

    container.bind(IDashboardWidgetType$).to(MyTasksWidgetType).inSingletonScope();
    ComponentsRegistry.instance.register(MyTasksWidget, ButtonWidgetComponent);
  },
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: HideBlockByRefValueUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: ChangeFieldOrRowUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: TableSectionChangedUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: HideFormUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: HideTaskBlockUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: SimpleCardTileExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: SimpleViewTileExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: RequestTileExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: AdditionalTableButtonUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: DialogModeAutocompleteUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: CustomBPTileExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: FileControlUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: HideTileExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: TableControlDoubleClickUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: ShowFormDialogUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: ShowCustomDialogUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: CloseCardOnCompleteTaskStoreExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: CloseCardOnCompleteTaskUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: AdditionalMetaInitializationExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: AdditionalMetaTileExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: CustomThemePropApplicationExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: CustomThemePropUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: CustomTabPanelButtonUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ForumUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: ExampleLoginExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ExampleFormUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: SliderMetadataExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ExamplePreviewerExtension,
        singleton: true,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: GridBasicStylingExtension,
        singleton: true,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: GridLayoutStylingExtension,
        singleton: true,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: AbTaskEnableAttachFilesExampleUIExtension,
        singleton: true,
        stage: ExtensionStage.BeforePlatform,
        order: 1,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: ExampleHeaderUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: OcrPreviewerUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ControlsCustomButtonUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: PdfAnnsCarFileExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: PropertyGridMetadataExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: PropertyGridUIExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        when: whenCardTypeIdIs(TestCardTypeID)
      })
      .registerExtension({
        extension: GroupedViewToolbarExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: AutomobileTourViewExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: SetTextBoxMaskUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: AbSideFormUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: SettingsAppMenuActionMobileClientExtension,
        stage: ExtensionStage.AfterPlatform
      });
  }
};
