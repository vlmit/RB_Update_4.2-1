import { injectable } from '@tessa/application';
import Checkbox from 'ui/checkbox';
import { CheckboxViewModel } from 'ui/checkbox/checkboxViewModel';
import { MentionsModuleToken } from 'ui/richTextBox/modules/tokens';
import { AttachmentPermissionFlags, RichTextBox, RichTextBoxViewModel } from 'ui/richTextBox';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { ContentGeneratorModuleToken } from './contentGenerator/contentGeneratorModuleToken';
import { GeneratorType, GeneratorTypeKey } from './contentGenerator/contentGeneratorTypes';
import { RichTextBoxArticleBase } from './richTextBoxArticleBase';

@injectable()
export class RichTextBoxPermissionsArticle extends RichTextBoxArticleBase {
  //#region base overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/RichTextBox/Permissions',
      description: 'Rich text box with linked controls.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Permissions',
      description:
        'Example of changing access rights to a control. It is possible to make the control read-only, as well as separately restrict work with attachments.',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.Permissions)
        );
        richTextBox.modulesContainer
          .withDefaultModules()
          .add(MentionsModuleToken)
          .add(ContentGeneratorModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.Permissions
        };

        await richTextBox.initialize();

        const readOnlyCheckbox = new CheckboxViewModel();
        readOnlyCheckbox.caption = 'read-only';
        readOnlyCheckbox.checked = false;
        this.disposeList.add(
          readOnlyCheckbox.onChange.add(() => {
            richTextBox.readOnly = readOnlyCheckbox.checked;
          })
        );
        await readOnlyCheckbox.initialize();

        const readAttachmentsCheckbox = new CheckboxViewModel();
        readAttachmentsCheckbox.caption = 'read';
        readAttachmentsCheckbox.checked = true;
        this.disposeList.add(
          readAttachmentsCheckbox.onChange.add(() => {
            if (!readAttachmentsCheckbox.checked) {
              changeAttachmentsCheckbox.checked = false;
              richTextBox.attachmentPermissions = AttachmentPermissionFlags.None;
            } else {
              richTextBox.attachmentPermissions = AttachmentPermissionFlags.Read;
            }
          })
        );
        await readAttachmentsCheckbox.initialize();

        const changeAttachmentsCheckbox = new CheckboxViewModel();
        changeAttachmentsCheckbox.caption = 'change';
        changeAttachmentsCheckbox.checked = true;
        this.disposeList.add(
          changeAttachmentsCheckbox.onChange.add(() => {
            if (changeAttachmentsCheckbox.checked) {
              readAttachmentsCheckbox.checked = true;
              richTextBox.attachmentPermissions = AttachmentPermissionFlags.Change;
            } else {
              richTextBox.attachmentPermissions = AttachmentPermissionFlags.Read;
            }
          })
        );
        await changeAttachmentsCheckbox.initialize();

        return {
          richTextBox,
          readOnlyCheckbox,
          readAttachmentsCheckbox,
          changeAttachmentsCheckbox
        };
      },
      view: ({
        richTextBox,
        readOnlyCheckbox,
        readAttachmentsCheckbox,
        changeAttachmentsCheckbox
      }) => (
        <DemoForm
          customStyles={css => css`
            gap: 10px;
            flex-direction: column;
          `}
        >
          <div style={{ flexDirection: 'column' }}>
            <div>
              <div>Control permissions:</div>
              <Checkbox viewModel={readOnlyCheckbox} />
            </div>
            <div>
              <ul style={{ listStyle: 'none', padding: '0' }}>
                <div>Attachment permissions:</div>
                <li>
                  <Checkbox viewModel={readAttachmentsCheckbox} />
                </li>
                <li>
                  <Checkbox viewModel={changeAttachmentsCheckbox} />
                </li>
              </ul>
            </div>
          </div>
          <div>
            <RichTextBox viewModel={richTextBox} />
          </div>
        </DemoForm>
      )
    });
  }

  override dispose(): void {
    super.dispose();

    this.disposeList.dispose();
  }

  //#endregion
}
