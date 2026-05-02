import { useRef, useEffect } from 'react';
import { observable, runInAction } from 'mobx';
import { observer } from 'mobx-react-lite';
import { injectable } from '@tessa/application';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { Label } from 'ui';
import { Button } from 'ui/button/button';
import { Button as ButtonViewModel } from 'ui/button/buttonViewModel';
import { IframeHtmlViewer } from 'ui/htmlViewer/iFrameHtmlViewer';
import { MailModuleToken } from 'ui/richTextBox/modules';
import { RichEditorHelper, RichStorage, RichTextBox, RichTextBoxViewModel } from 'ui/richTextBox';
import { TextFieldViewModel } from 'ui/textField';
import { TextFieldView } from 'ui/textField/textFieldView';
import { GeneratorType } from './contentGenerator/contentGeneratorTypes';
import { RichTextBoxArticleBase } from './richTextBoxArticleBase';

@injectable()
export class RichTextBoxMailArticle extends RichTextBoxArticleBase {
  //#region static

  private static readonly _mailTemplateContent =
    '<p data-slateId="cbbede41-5997-4e91-adbf-5b147b26281e"><span style="font-size:14px;color:#666666ff;">С уважением,</span></p><p data-slateId="d43e77f4-1e95-4937-b504-416eea5291d7"><span style="font-size:14px;color:#7f7f7fff;">Иван Иванович</span></p><p data-slateId="86038a33-5d8b-41da-8440-77f86476a262"><span style="font-weight:bold;font-size:14px;color:#00b0f0ff;">________________________________________________</span></p><p data-slateId="dc6d7e51-ef96-4be4-a622-ea5bfed3e5bf"></p><p data-slateId="40aa31c4-84ee-4791-9f28-490d56b40ff4"><span style="font-size:14px;color:#7f7f7fff;">ООО «СИНТЕЛЛЕКТ», помощник заместителя правой руки руководителя отдела поставки печенек и тефтелей</span></p><p data-slateId="2080c5a2-768d-4dcc-86a0-baa91ae9062b"><span style="font-size:14px;color:#7f7f7fff;">Москва, Бакунинская ул., д. 69, строение 1</span><a data-slateId="19522a24-b550-4493-b9c8-06374350e1c9" class="forum-url" href="mailto:mneboronov@syntellect.ru" data-custom-href="mailto:mneboronov@syntellect.ru" data-custom-cinl></a></p><p data-slateId="d1e89230-3f34-4126-a2fd-595e56685b54"><span style="font-size:14px;color:#7f7f7fff;"> </span></p><p data-slateId="1a8eb3e5-5cd4-449b-85d8-eab3098037c4"><a data-slateId="a488f487-2993-4a34-8796-c9f84eafe787" class="forum-url" href="http://www.tessa.ru/" data-custom-href="http://www.tessa.ru/" data-custom-cinl><span style="font-size:14px;color:#0563c1ff;">www.tessa.ru</span></a></p>';

  //#endregion

  //#region base overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/RichTextBox/Mail',
      description: 'Example of working with email.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Mail',
      props: async () => {
        const inboxHTML = observable.box('');
        const outboxHTML = observable.box('');

        const inboxMailField = new TextFieldViewModel();
        await inboxMailField.initialize();
        this.disposeList.add(
          inboxMailField.onTextChange.add(() => {
            runInAction(() => inboxHTML.set(inboxMailField.value ?? ''));
          })
        );
        inboxMailField.minRows = 10;
        inboxMailField.maxRows = 20;

        const mailTemplateRichTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.None)
        );
        const mailTemplateStorage = new RichStorage();
        mailTemplateStorage.text = RichTextBoxMailArticle._mailTemplateContent;
        mailTemplateRichTextBox.dataSource.setValue(mailTemplateStorage);
        mailTemplateRichTextBox.modulesContainer.withDefaultModules();
        mailTemplateRichTextBox.editMode = 'edit';
        await mailTemplateRichTextBox.initialize();

        const outboxMailRichTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.None)
        );
        outboxMailRichTextBox.modulesContainer.withDefaultModules().add(MailModuleToken);
        outboxMailRichTextBox.editMode = 'edit';
        await outboxMailRichTextBox.initialize();

        const updateButton = ButtonViewModel.create({
          name: 'CreateAnswer',
          caption: 'Create answer',
          type: 'normal',
          theme: 'tab',
          buttonAction: async () => {
            await mailTemplateRichTextBox.commit();
            outboxMailRichTextBox.clear();
            await outboxMailRichTextBox.commit();

            const mailDataSource = new RichStorage();
            mailTemplateRichTextBox.dataSource.getValue().copyTo(mailDataSource);
            outboxMailRichTextBox.dataSource.setValue(mailDataSource);

            setTimeout(() => {
              const mailEditor = outboxMailRichTextBox.editor.extendedEditor;
              if (RichEditorHelper.isEditor(mailEditor, MailModuleToken)) {
                mailEditor.forwardedMailPanel.text = inboxMailField.value ?? null;
              }
            }, 100);
          }
        });

        const sendButton = ButtonViewModel.create({
          name: 'CreateOutboxMessage',
          caption: 'Create outbox message',
          type: 'normal',
          theme: 'tab',
          buttonAction: async () => {
            await outboxMailRichTextBox.commit();
            const mailEditor = outboxMailRichTextBox.editor.extendedEditor;
            if (RichEditorHelper.isEditor(mailEditor, MailModuleToken)) {
              const text = await mailEditor.createMailContent();
              runInAction(() => outboxHTML.set(text));
            }
          }
        });

        return {
          inboxMailField,
          mailTemplateRichTextBox,
          outboxMailRichTextBox,
          updateButton,
          sendButton,
          outboxHTML,
          inboxHTML
        };
      },
      view: observer(
        ({
          inboxMailField,
          mailTemplateRichTextBox,
          outboxMailRichTextBox,
          updateButton,
          sendButton,
          outboxHTML,
          inboxHTML
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

                .rich-article-label {
                  background: transparent;
                  padding: 0;
                  font-size: 20px;
                  font-weight: semi-bold;
                }

                .rich-article-button {
                  align-self: center;
                }

                .rich-article-iframe {
                  border: var(--control-active-border);
                  border-radius: var(--control-corners);
                }
              `}
            >
              <Label text="Inbox message:" className="rich-article-label" background="none" />
              <TextFieldView viewModel={inboxMailField} />
              <div className="rich-article-iframe">
                <IframeHtmlViewer
                  showImages={true}
                  stretchVertically={true}
                  safeHtml={inboxHTML.get()}
                />
              </div>
              <Label text="Mail template:" className="rich-article-label" background="none" />
              <RichTextBox viewModel={mailTemplateRichTextBox} />
              <div className="rich-article-button">
                <Button viewModel={updateButton} />
              </div>
              <Label text="Editable answer:" className="rich-article-label" background="none" />
              <RichTextBox viewModel={outboxMailRichTextBox} />
              <div className="rich-article-button">
                <Button viewModel={sendButton} />
              </div>
              <Label text="Outbox message:" className="rich-article-label" background="none" />
              <div className="rich-article-iframe">
                <IframeHtmlViewer
                  ref={iframeRef}
                  showImages={true}
                  stretchVertically={true}
                  safeHtml={outboxHTML.get()}
                />
              </div>
            </DemoForm>
          );
        }
      )
    });
  }

  //#endregion
}
