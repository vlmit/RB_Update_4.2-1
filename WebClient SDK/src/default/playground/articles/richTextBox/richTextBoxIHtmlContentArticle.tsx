import { useRef, useEffect } from 'react';
import { observer } from 'mobx-react-lite';
import { observable, runInAction } from 'mobx';
import { injectable } from '@tessa/application';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { AutocompleteDataSource, AutocompleteViewModel } from 'ui/autocomplete';
import { Autocomplete } from 'ui/autocomplete/autocomplete';
import { AutocompleteDataConverter } from 'ui/autocomplete/core/autocompleteDataConverter';
import { AutocompleteDataPlainContext } from 'ui/autocomplete/core/autocompleteDataPlainContext';
import { IAutocompleteItem } from 'ui/autocomplete/core/autocompleteTypes';
import { Button } from 'ui/button/button';
import { Button as ButtonViewModel } from 'ui/button/buttonViewModel';
import { IframeHtmlViewer } from 'ui/htmlViewer/iFrameHtmlViewer';
import {
  RichGetHtmlWithStylesModes,
  RichStorage,
  RichTextBox,
  RichTextBoxViewModel
} from 'ui/richTextBox';
import {
  ChecklistModuleToken,
  ControlsModuleToken,
  MentionsModuleToken
} from 'ui/richTextBox/modules/tokens';
import { ContentGeneratorModuleToken } from './contentGenerator/contentGeneratorModuleToken';
import { GeneratorType, GeneratorTypeKey } from './contentGenerator/contentGeneratorTypes';
import { RichTextBoxArticleBase } from './richTextBoxArticleBase';

@injectable()
export class RichTextBoxHtmlContentArticle extends RichTextBoxArticleBase {
  //#region base overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/RichTextBox/Styled HTML modes',
      description: `This example shows how rich content is converted to HTML in different modes:
                    \r\n- default - converts content to HTML with minimal content (as with regular serialization). In this case, it contains content without styles.
                    \r\n- short - converts content to HTML with only the tags specified in the rich file, and adds styles and unresolved CSS variables. In this case, the resulting content can be used anywhere within the platform, and the theme will be respected.
                    \r\n- full - converts content to full HTML, which can be used regardless of the platform, since all its styles are inlined and CSS variables are resolved. For example, such content can be sent by email, but it should be kept in mind that it will be rendered according to a cold theme.`
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'HTML content',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.HTMLContent)
        );
        richTextBox.modulesContainer
          .withDefaultModules()
          .add(ControlsModuleToken)
          .add(ChecklistModuleToken)
          .add(MentionsModuleToken)
          .add(ContentGeneratorModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.HTMLContent
        };
        await richTextBox.initialize();

        const serializeButton = ButtonViewModel.create({
          name: 'Serialize',
          caption: 'Serialize',
          type: 'normal',
          theme: 'tab'
        });

        const deserializedRich = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.None)
        );
        deserializedRich.modulesContainer
          .withDefaultModules()
          .add(ControlsModuleToken)
          .add(ChecklistModuleToken)
          .add(MentionsModuleToken);
        await deserializedRich.initialize();

        const safeHtml = observable.box('');

        const items: IAutocompleteItem[] = ['default', 'short', 'full'].map(mode => ({
          id: mode,
          name: mode
        }));

        const modesAutocomplete = new AutocompleteViewModel(
          new AutocompleteDataSource(
            new AutocompleteDataPlainContext({
              unique: true,
              multiple: false,
              items
            }),
            new AutocompleteDataConverter(true),
            [items[0]]
          )
        );

        modesAutocomplete.availability = 'readonly';
        modesAutocomplete.toolbar.buttons.availability = 'enabled';
        modesAutocomplete.menu.deleteAction.isCollapsed = false;
        modesAutocomplete.menu.openAction.isCollapsed = false;

        await modesAutocomplete.initialize();

        serializeButton.buttonAction = async () => {
          if (!modesAutocomplete.record) {
            return;
          }

          await richTextBox.commit();

          const styledHtml = await richTextBox.getHtmlWithStyles(
            modesAutocomplete.record.model.id as RichGetHtmlWithStylesModes
          );
          runInAction(() => safeHtml.set(styledHtml));
        };

        const deserializeButton = ButtonViewModel.create({
          name: 'Deserialize',
          caption: 'Deserialize',
          type: 'normal',
          theme: 'tab',
          buttonAction: () => {
            const styledHtml = new DOMParser().parseFromString(safeHtml.get(), 'text/html');
            const richStorage = new RichStorage();
            richStorage.text = styledHtml.body.innerHTML;
            deserializedRich.dataSource.setValue(richStorage);
          }
        });

        return {
          richTextBox,
          serializeButton,
          safeHtml,
          deserializeButton,
          deserializedRich,
          modesAutocomplete
        };
      },
      view: observer(
        ({
          richTextBox,
          serializeButton,
          safeHtml,
          deserializeButton,
          deserializedRich,
          modesAutocomplete
        }) => {
          const iframeRef = useRef<HTMLIFrameElement>(null);
          useEffect(() => {
            if (!iframeRef.current) {
              return;
            }

            iframeRef.current.onload = async function () {
              const iframeDoc = iframeRef.current!.contentDocument!;
              await iframeDoc.fonts.ready;
              const contentHeight =
                iframeDoc.documentElement.scrollHeight || iframeDoc.body.scrollHeight;
              iframeRef.current!.style.height = `${contentHeight}px`;
            };
          }, []);

          return (
            <DemoForm
              customStyles={css => css`
                flex-direction: column;
                gap: 10px;

                .rich-article-iframe-button {
                  align-self: center;
                }

                .rich-article-iframe {
                  border: var(--control-active-border);
                  border-radius: var(--control-corners);
                }
              `}
            >
              <div className="rich-article-iframe-button">
                <Autocomplete viewModel={modesAutocomplete} />
              </div>
              <RichTextBox viewModel={richTextBox} />
              <div className="rich-article-iframe-button">
                <Button viewModel={serializeButton} />
              </div>
              <div className="rich-article-iframe">
                <IframeHtmlViewer
                  ref={iframeRef}
                  showImages={true}
                  stretchVertically={true}
                  safeHtml={safeHtml.get()}
                />
              </div>
              <div className="rich-article-iframe-button">
                <Button viewModel={deserializeButton} />
              </div>
              <RichTextBox viewModel={deserializedRich} />
            </DemoForm>
          );
        }
      )
    });
  }

  //#region
}
