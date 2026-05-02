import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { CardControlTypeRegistry } from '@tessa/platform';
import { ComponentsRegistry } from '@tessa/ui';
import { CardControlRegistry } from 'tessa/ui/cards/cardControlRegistry';

import { OcrCompareFilesUIExtension } from './extensions/ocrCompareFilesUIExtension';
import { OcrSourceFileTagUIExtension } from './extensions/ocrSourceFileTagUIExtension';
import { OcrSourceFileMenuExtension } from './extensions/ocrSourceFileMenuExtension';
import { OcrMonitoringUIExtension } from './extensions/ocrMonitoringUIExtension';
import { OcrRequestMetadataExtension } from './extensions/ocrRequestMetadataExtension';
import { OcrSettingsUIExtension } from './extensions/ocrSettingsUIExtension';
import { OcrToolbarUIExtension } from './extensions/ocrToolbarUIExtension';
import { OcrVerificationUIExtension } from './extensions/ocrVerificationUIExtension';
import { OcrMetadataExtension } from './extensions/ocrMetadataExtension';
import { OcrGridResultStoreExtension } from './extensions/ocrGridResultStoreExtension';
import { OcrCreateModelUIExtension } from './extensions/ocrCreateModelUIExtension';
import { OcrSliderControlType, OcrSliderType } from './components/slider/ocrSliderType';
import { OcrSlider } from './components/slider/ocrSlider';
import { OcrGridControlType, OcrGridType } from './components/grid/ocrGridTypes';
import { OcrGridView } from './components/grid/ocrGridView';
import { OcrProperty } from './components/grid/properties/ocrProperty';
import { OcrPropertyView } from './components/grid/properties/ocrPropertyView';

export const OcrRegistrator: ExtensionRegistrator = {
  async registerTypes() {
    CardControlTypeRegistry.instance.register(OcrSliderControlType);
    CardControlRegistry.instance.register(OcrSliderControlType.id, OcrSliderType);
    ComponentsRegistry.instance.register(OcrSliderControlType.id, OcrSlider);

    CardControlTypeRegistry.instance.register(OcrGridControlType);
    CardControlRegistry.instance.register(OcrGridControlType.id, OcrGridType);
    ComponentsRegistry.instance.register(OcrGridControlType.id, OcrGridView);

    ComponentsRegistry.instance.register(OcrProperty, OcrPropertyView);
  },
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: OcrMetadataExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: OcrRequestMetadataExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: OcrCreateModelUIExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: OcrSourceFileTagUIExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        when: context => context.control.fileContainer.files.length > 0
      })
      .registerExtension({
        extension: OcrSourceFileMenuExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: OcrToolbarUIExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: OcrVerificationUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: OcrMonitoringUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: OcrCompareFilesUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: OcrSettingsUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: OcrGridResultStoreExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      });
  }
};
