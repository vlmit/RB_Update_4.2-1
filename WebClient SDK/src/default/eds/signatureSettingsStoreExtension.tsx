import { extension } from '@tessa/application';
import { CardRowState, CardStoreExtension, ICardStoreExtensionContext } from '@tessa/platform';
import { EDSHelper } from './edsHelper';

@extension({ name: 'SignatureSettingsStoreExtension' })
export class SignatureSettingsStoreExtension extends CardStoreExtension {
  async beforeRequest(context: ICardStoreExtensionContext): Promise<void> {
    if (context.cardType?.name === EDSHelper.SignatureSettingsTypeName) {
      const rows =
        context?.request.card?.sections?.get(
          EDSHelper.SignatureSettingsCertificateSettingsSectionName
        )?.rows ?? [];

      for (const row of rows) {
        if (
          !row.get(EDSHelper.CertificateSettingsStartDateFieldName) &&
          !row.get(EDSHelper.CertificateSettingsEndDateFieldName) &&
          !row.get(EDSHelper.CertificateSettingsIsValidDateFieldName) &&
          !row.get(EDSHelper.CertificateSettingsCompanyFieldName) &&
          !row.get(EDSHelper.CertificateSettingsSubjectFieldName) &&
          !row.get(EDSHelper.CertificateSettingsIssuerFieldName)
        ) {
          row.state = CardRowState.Deleted;
        }
      }
    }
  }
}
