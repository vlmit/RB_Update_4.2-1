import { observable, runInAction } from 'mobx';
import { observer } from 'mobx-react-lite';
import { injectable } from '@tessa/application';
import { TypedJsonConverter } from '@tessa/core';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { Button } from 'ui/button/button';
import { Button as ButtonViewModel } from 'ui/button/buttonViewModel';
import {
  ChecklistModuleToken,
  ControlsModuleToken,
  MentionsModuleToken
} from 'ui/richTextBox/modules/tokens';
import { RichTextBox, RichTextBoxViewModel } from 'ui/richTextBox';
import { GeneratorType } from './contentGenerator/contentGeneratorTypes';
import { RichTextBoxArticleBase } from './richTextBoxArticleBase';

@injectable()
export class RichTextBoxSerializationArticle extends RichTextBoxArticleBase {
  //#region base overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/RichTextBox/Serialization',
      description: 'Data serialization example.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Serialization',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.None)
        );
        richTextBox.editMode = 'edit';
        richTextBox.modulesContainer
          .withDefaultModules()
          .add(ControlsModuleToken)
          .add(ChecklistModuleToken)
          .add(MentionsModuleToken);

        await richTextBox.initialize();

        const serializationData = observable.box('');
        const serializationButton = ButtonViewModel.create({
          name: 'Serialization',
          caption: 'Serialization',
          type: 'normal',
          theme: 'tab',
          buttonAction: async () => {
            await richTextBox.commit();
            const richStorage = richTextBox.dataSource.getValue().serializeToStorage();
            runInAction(() =>
              serializationData.set(
                TypedJsonConverter.serialize(richStorage, {
                  stringifySpace: 2
                })
              )
            );
          }
        });

        return {
          richTextBox,
          serializationButton,
          serializationData
        };
      },
      view: observer(({ richTextBox, serializationButton, serializationData }) => {
        const serializationContent = serializationData.get();
        return (
          <DemoForm
            customStyles={css => css`
              flex-direction: column;
              gap: 10px;

              .rich-article-serialization-button {
                display: flex;
                align-items: center;
                justify-content: center;
              }
            `}
          >
            <RichTextBox viewModel={richTextBox} />
            <div className="rich-article-serialization-button">
              <Button viewModel={serializationButton} />
            </div>
            <div>
              <h3>Serialization data:</h3>
              <div>
                <pre>{serializationContent}</pre>
              </div>

              <div className="rich-article-serialization-button">
                {serializationContent ? (
                  <Button
                    type={'normal'}
                    theme={'tab'}
                    caption="Copy to clipboard"
                    onClick={() => {
                      navigator.clipboard.writeText(serializationContent);
                    }}
                  />
                ) : null}
              </div>
            </div>
          </DemoForm>
        );
      })
    });
  }

  //#endregion
}
