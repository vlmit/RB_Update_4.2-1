import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { ComponentsRegistry } from '@tessa/ui';
import Platform from 'common/platform';
import { OnlyOfficeFileExtension } from './onlyOfficeFileExtension';
import { OnlyOfficeInitializationApplicationExtension } from './onlyOfficeInitializationApplicationExtension';
import {
  OnlyOfficeFileControlExtension,
  OnlyOfficeViewFileControlExtension
} from './onlyOfficeFileControlExtension';
import { OnlyOfficeCardUIExtension } from './onlyOfficeCardUIExtension';
import { OnlyOfficePreviewerExtension } from './onlyOfficePreviewerExtension';
import { OnlyOfficeEditorPreviewViewModel } from './onlyOfficeEditorPreviewViewModel';
import { OnlyOfficeEditorPreviewComponent } from './onlyOfficeEditorPreviewComponent';

export const OnlyOfficeRegistrator: ExtensionRegistrator = {
  async registerTypes() {
    if (!Platform.isMobile()) {
      // регистрируем компонент предпросмотра OnlyOffice
      ComponentsRegistry.instance.register(
        OnlyOfficeEditorPreviewViewModel,
        OnlyOfficeEditorPreviewComponent
      );
    }
  },
  async registerExtensions(container) {
    if (!Platform.isMobile()) {
      container
        .registerExtension({
          extension: OnlyOfficeFileExtension,
          stage: ExtensionStage.AfterPlatform,
          singleton: true
        })
        .registerExtension({
          extension: OnlyOfficeInitializationApplicationExtension,
          stage: ExtensionStage.AfterPlatform,
          singleton: true
        })
        .registerExtension({
          extension: OnlyOfficeFileControlExtension,
          stage: ExtensionStage.AfterPlatform,
          singleton: true
        })
        .registerExtension({
          extension: OnlyOfficeViewFileControlExtension,
          stage: ExtensionStage.AfterPlatform,
          singleton: true,
          order: 100
        })
        .registerExtension({
          extension: OnlyOfficeCardUIExtension,
          stage: ExtensionStage.AfterPlatform,
          singleton: true
        })
        .registerExtension({
          extension: OnlyOfficePreviewerExtension,
          stage: ExtensionStage.AfterPlatform,
          singleton: true
        });
    }
  }
};
