import { DisposeList } from '@tessa/core';
import { extension } from '@tessa/application';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { GridViewModel } from 'tessa/ui/cards/controls';
import { EDSHelper } from './edsHelper';

@extension({ name: 'SignatureSettingsUIExtension' })
export class SignatureSettingsUIExtension extends CardUIExtension {
  async initialized(context: ICardUIExtensionContext): Promise<void> {
    if (context.card.typeName !== EDSHelper.SignatureSettingsTypeName) {
      return;
    }

    const tableControl = context.model.controls.get(
      EDSHelper.SignatureSettingsEncryptionDigestControlName
    ) as GridViewModel;
    if (!tableControl) {
      return;
    }

    const rowDisposeList = new DisposeList();

    this.disposeList.add(
      tableControl.rowInitializing.addWithDispose(e => {
        const control = e.rowModel?.controls.get('DigestAlgorithm');
        if (control) {
          control.isReadOnly =
            e.row.getField(EDSHelper.SignatureSettingsDigestAlgorithmsIDName) == null;
        }

        rowDisposeList.add(
          e.row.fieldChanged.add(event => {
            if (event.fieldName === EDSHelper.SignatureSettingsEncryptionAlgorithmIDName) {
              e.row.set(EDSHelper.SignatureSettingsDigestAlgorithmsIDName, null);
              e.row.set(EDSHelper.SignatureSettingsDigestAlgorithmsNameName, null);
              e.row.set(EDSHelper.SignatureSettingsDigestAlgorithmsOIDName, null);
              if (control) {
                control.isReadOnly = event.fieldValue == null;
              }
            }
          })
        );
      })!,

      tableControl.rowEditorClosed.addWithDispose(() => {
        rowDisposeList.dispose();
      })!
    );
  }
}
