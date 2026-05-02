import { extension } from '@tessa/application';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';

@extension()
export class OutgoingPartnerUIExtension extends CardUIExtension {
  public initialized(context: ICardUIExtensionContext): void {
    if (context.card.typeId !== 'c59b76d9-c0db-01cd-a3fb-b339740f0620') {
      // OutgoingTypeID
      return;
    }

    const dciSection = context.card.sections.tryGet('DocumentCommonInfo');
    if (!dciSection) {
      return;
    }

    dciSection.fields.fieldChanged.add((e, s) => {
      if (e.fieldName === 'PartnerID') {
        s.set('ReceiverName', null);
        s.set('ReceiverRowID', null);
      }
    });
  }
}
