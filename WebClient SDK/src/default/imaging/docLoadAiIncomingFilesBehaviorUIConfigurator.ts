import { injectable, localize } from '@tessa/application';
import { IStorage, StorageHelper } from '@tessa/core';
import { DocLoadFilesBehaviorUIConfigurator } from 'tessa/ui/imaging/docLoadFilesBehaviorUIConfigurator';
import { PropertyGridBuilder } from 'tessa/ui/propertyGrid';

/**
 * Предоставляет объект, который предоставляет интерфейс для редактирования настроек обработчика потокового ввода DocLoadAiIncomingFilesBehavior.
 */
@injectable()
export class DocLoadAiIncomingFilesBehaviorUIConfigurator extends DocLoadFilesBehaviorUIConfigurator {
  //#region constructors

  constructor() {
    super(
      'DocLoadAiIncomingFilesBehavior',
      dataProvider => {
        const separateByWhitePageProperty = PropertyGridBuilder.createBooleanProperty({
          data: dataProvider,
          alias: 'SeparateByWhitePage',
          visibility: true,
          captionVisibility: true,
          caption: '$CardTypes_Controls_DocLoad_SeparateByWhitePage'
        });

        return [separateByWhitePageProperty];
      },
      jsonSettingsString => {
        const json = JSON.parse(jsonSettingsString) as IStorage;
        const separateByWhiteSpace = StorageHelper.tryGet<boolean>(json, 'SeparateByWhitePage');
        return (
          localize('$CardTypes_Controls_DocLoad_SeparateByWhitePage') +
          ': ' +
          (separateByWhiteSpace ? localize('$UI_Common_Yes') : localize('$UI_Common_No'))
        );
      }
    );
  }
  //#endregion
}
