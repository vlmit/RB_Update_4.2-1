import {
  AsyncLazy,
  FieldType,
  IStorage,
  StorageHelper,
  StorageSerializable,
  TypedJsonConverter
} from '@tessa/core';
import { UIButton } from 'tessa/ui/uiButton';
import { DefaultEventHandlers } from 'ui/hooks/defaultEventHandlers';
import {
  IRichExtensionContext,
  richModuleExtension,
  RichStorage,
  RichTextBoxViewModel,
  RichToolbarHelper,
  RichViewModelExtension
} from 'ui/richTextBox';
import { ContentGeneratorModuleToken } from '../contentGeneratorModuleToken';
import { GeneratorType, GeneratorTypeKey } from '../contentGeneratorTypes';
import { RichTextBoxArticleBase } from '../../richTextBoxArticleBase';

@richModuleExtension({
  name: 'ContentGeneratorRichTextBoxExtension',
  token: ContentGeneratorModuleToken
})
export class ContentGeneratorRichTextBoxExtension extends RichViewModelExtension {
  //#region static fields

  private static _configs = new AsyncLazy<IStorage<string>>(async () => {
    return Object.assign(
      {},
      await import(
        /* webpackChunkName: "playground-rich-modules" */ './contentGeneratorConfigs.json'
      )
    );
  });

  //#endregion

  //#region base overrides

  async viewModelInitialized(context: IRichExtensionContext): Promise<void> {
    const { richTextBox } = context;

    const info = richTextBox.info ?? {};
    const storageType = StorageHelper.tryGetValue(
      info,
      GeneratorTypeKey,
      FieldType.String
    ) as GeneratorType;

    if (!storageType) {
      throw new Error(
        `For '${ContentGeneratorRichTextBoxExtension.name}' to work the '${GeneratorTypeKey}' field must be initialized in the 'RichTextBoxViewModel.info'.`
      );
    }

    if (!RichTextBoxArticleBase.initializedStorages.has(storageType)) {
      RichTextBoxArticleBase.initializedStorages.add(storageType);
      await ContentGeneratorRichTextBoxExtension.initContent(richTextBox, storageType);
    }

    richTextBox.toolbar.addButtons([
      UIButton.create({
        name: 'generateContent',
        key: 'generateContent',
        icon: 'm-copy',
        onMouseDown: async e => {
          DefaultEventHandlers.terminate(e);
          await ContentGeneratorRichTextBoxExtension.initContent(richTextBox, storageType);
        },
        theme: 'transparent',
        type: 'toolbar',
        tooltip: '$UI_Controls_Rich_ToolTip_PlaygroundRestore'
      }),
      UIButton.create({
        name: 'commit',
        key: 'commit',
        icon: 'm-save',
        onMouseDown: async e => {
          DefaultEventHandlers.terminate(e);
          await richTextBox.commit();
        },
        theme: 'transparent',
        type: 'toolbar',
        tooltip: '$UI_Controls_Rich_ToolTip_PlaygroundSave'
      })
    ]);

    richTextBox.toolbar.addGroups(
      RichToolbarHelper.createGroup('generateContentExtension', ['generateContent', 'commit'])
    );
  }

  //#endregion

  //#region private methods

  private static async initContent(
    richTextBox: RichTextBoxViewModel,
    localStorageType: GeneratorType
  ): Promise<void> {
    const text = (await ContentGeneratorRichTextBoxExtension._configs.getValue())[localStorageType];
    const storage = StorageSerializable.deserialize(
      RichStorage,
      TypedJsonConverter.deserialize<IStorage>(text)
    );
    richTextBox.dataSource.setValue(storage);
  }

  //#endregion
}
