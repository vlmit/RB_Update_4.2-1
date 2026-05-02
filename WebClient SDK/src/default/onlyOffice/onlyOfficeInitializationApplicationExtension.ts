import { ApplicationExtension, IApplicationExtensionMetadataContext } from 'tessa';
import { OnlyOfficeSettings } from './onlyOfficeSettings';
import { CardSingletonCache } from 'tessa/cards';
import { OnlyOfficeApi } from './onlyOfficeApi';
import { OnlyOfficeApiSingleton } from './onlyOfficeApiSingleton';
import { PageLifecycleSingleton, PageLifecycleState } from 'common';
import { extension } from '@tessa/application';

/**
 * Представляет собой расширение, которое инициализирует работу с OnlyOffice.
 */
@extension()
export class OnlyOfficeInitializationApplicationExtension extends ApplicationExtension {
  public async afterMetadataReceived(context: IApplicationExtensionMetadataContext): Promise<void> {
    if (!context.response) {
      return;
    }

    // инициализировать нужно именно в afterMetadataReceived, так как тут подгрузится карточка настроек
    try {
      const settings = OnlyOfficeInitializationApplicationExtension.getSettings();
      if (!settings) {
        return;
      }

      const api = new OnlyOfficeApi(settings);

      if (settings.previewEnabled) {
        // загружаем скрипт редактора в текущий документ для работы предпросмотра.
        api.ensureApiScriptAdded(document);
      }

      OnlyOfficeApiSingleton.init(api);

      PageLifecycleSingleton.instance.addCallback(() => {
        api.openFiles.forEach(f => f.forceCloseCallback());
      }, PageLifecycleState.terminated);
    } catch (e) {
      console.error(e);
    }
  }

  /**
   * Получает необходимые данные из карточки настроек OnlyOffice или null, если OnlyOffice не настроен.
   */
  private static getSettings(): OnlyOfficeSettings | null {
    const settingsCard = CardSingletonCache.instance.cards.get('OnlyOfficeSettings');
    if (!settingsCard) {
      return null;
    }

    const section = settingsCard.sections.get('OnlyOfficeSettings')!;
    const apiScriptUrl = section.fields.get<string>('ApiScriptUrl');
    if (!apiScriptUrl) {
      return null;
    }

    const previewEnabled = !!section.fields.get('PreviewEnabled');

    const excludedPreviewFormatsJoined = section.fields.get('ExcludedPreviewFormats') as
      | string
      | null;
    const excludedPreviewFormats = excludedPreviewFormatsJoined
      ? excludedPreviewFormatsJoined.split(' ')
      : [];

    return {
      apiScriptUrl,
      previewEnabled,
      excludedPreviewFormats
    };
  }
}
