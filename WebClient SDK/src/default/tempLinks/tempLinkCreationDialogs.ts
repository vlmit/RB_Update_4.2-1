import { IApiClient, localize } from '@tessa/application';
import {
  FieldType,
  TypedField,
  ValidationKey,
  ValidationResultBuilder,
  ValidationResultType
} from '@tessa/core';
import {
  ContentTokenRequest,
  ICardSingletonCache,
  IContentService,
  FileContentHelper,
  ContentHelper
} from '@tessa/platform';
import moment, { Moment } from 'moment';
import {
  ClipboardHelper,
  UIButton,
  createDialogForm,
  showLoadingOverlay,
  showNotEmpty
} from 'tessa/ui';
import {
  ButtonViewModel,
  CheckBoxViewModel,
  DateTimeViewModel,
  TextBlockViewModel
} from 'tessa/ui/cards/controls';
import { showDialog } from 'tessa/ui/uiHost';
import { TempLinkHelper } from './tempLinkHelper';
import { FormViewModel } from 'tessa/ui/cards/forms/formViewModel';

/**
 * Helper for file/version temp link dialogs.
 * @helper
 */
export namespace TempLinkDialogs {
  //#region Names

  const CardDialogName = 'CreateFileTempLink';
  const SettingsTabName = 'MainForm';
  const SettingsSectionName = 'FileTempLinkCreationSettings';
  const ValidToName = 'ValidTo';
  const IsInfiniteName = 'IsInfinite';

  const LinkTabName = 'FileLink';
  const LinkSectionName = 'FileTempLink';
  const LinkName = 'Link';
  const CopyButtonName = 'CopyButton';

  //#endregion

  export async function createAndShowTempLink(
    cardCache: ICardSingletonCache,
    contentService: IContentService,
    apiClient: IApiClient,
    cardID: string,
    fileID: string,
    fileVersionID: string | null,
    fileTypeName: string,
    isVirtual: boolean,
    isFileVersion: boolean
  ): Promise<void> {
    const settings = await tempLinkCreationSettingsDialog(cardCache);
    if (!settings) {
      return;
    }

    const response = await showLoadingOverlay(
      async () => {
        const request = new ContentTokenRequest();
        request.contentType = TempLinkHelper.getContentType(isFileVersion);
        request.info = {
          [FileContentHelper.cardIdKey]: TypedField.create(cardID, FieldType.Guid),
          [FileContentHelper.fileIdKey]: TypedField.create(fileID, FieldType.Guid),
          [FileContentHelper.fileVersionIdKey]: TypedField.create(fileVersionID, FieldType.Guid),
          [FileContentHelper.fileTypeNameKey]: TypedField.create(fileTypeName, FieldType.String),
          [FileContentHelper.fileVirtualKey]: TypedField.create(isVirtual, FieldType.Boolean)
        };

        if (settings.validTo) {
          request.info[FileContentHelper.validTo] = TypedField.create(
            settings.validTo.utc().format(),
            FieldType.DateTime
          );
        }

        return await contentService.getToken(request);
      },
      { text: localize('$CardTypes_CreateFileTempLink') }
    );

    const validationResult = response.validationResult;
    if (!validationResult.isSuccessful) {
      await showNotEmpty(validationResult.build());
      return;
    }

    if (!response.tokenInfo) {
      validationResult.add(
        ValidationKey.unknown,
        ValidationResultType.Error,
        localize('$Content_Message_CannotCreateToken')
      );

      await showNotEmpty(validationResult.build());
      return;
    }

    const contentID = isFileVersion
      ? ContentHelper.getGuidContentId(fileVersionID!)
      : ContentHelper.getGuidContentId(fileID);

    const link = await TempLinkHelper.getTempLink(
      apiClient,
      contentID,
      response.tokenInfo.token,
      isFileVersion
    );
    await tempLinkCreatedDialog(link);
  }

  export async function tempLinkCreationSettingsDialog(cardCache: ICardSingletonCache): Promise<{
    validTo: Moment | null;
  } | null> {
    const createDialogResult = await createDialogForm(CardDialogName, SettingsTabName);
    if (!createDialogResult) {
      return null;
    }
    const [form, cardModel] = createDialogResult;
    const actualForm = form as FormViewModel;
    const data = cardModel.card.sections.tryGet(SettingsSectionName)?.fields;
    const calendarValidTo = cardModel.controls.get(ValidToName) as DateTimeViewModel;
    const checkInfinite = cardModel.controls.get(IsInfiniteName) as CheckBoxViewModel;
    if (!data || !calendarValidTo || !checkInfinite) {
      return null;
    }
    // set calendar value to next day.
    const maxDuration = await TempLinkHelper.getTempFileMaxLifetime(cardCache);
    const minDate = moment().utc().startOf('d');
    const maxDate = moment().utc().add(maxDuration).add(1, 'd');
    const date = moment().utc().add(1, 'd').format();
    data.set('ValidTo', date, FieldType.DateTime);
    calendarValidTo.minDate = minDate;
    calendarValidTo.maxDate = maxDate;

    const createLinkEventHandlerDisposer = data.fieldChanged.add(e => {
      if (e.fieldName === IsInfiniteName) {
        calendarValidTo.isReadOnly = e.fieldValue as boolean;
      }
    });

    const dialogResult = await showDialog<boolean>(
      form,
      null,
      [
        UIButton.create({
          caption: '$UI_Common_OK',
          theme: 'primary',
          type: 'normal',
          buttonAction: async button => {
            const validationResult = new ValidationResultBuilder();

            const selectedDate = moment.utc(data.get<string>(ValidToName, FieldType.DateTime));
            const isInfinite = data.get<boolean>(IsInfiniteName, FieldType.Boolean);
            if (!isInfinite && !selectedDate.isAfter(moment().utc())) {
              validationResult.add(
                ValidationKey.unknown,
                ValidationResultType.Error,
                '$CardTypes_Validators_CreateTempFileLink_DateInPast'
              );
              calendarValidTo.hasActiveValidation = true;
            }

            if (!(await showNotEmpty(validationResult.build()))) {
              button.close(true);
            }
          }
        }),
        UIButton.create({
          caption: '$UI_Common_Cancel',
          theme: 'secondary',
          type: 'normal',
          buttonAction: button => {
            button.close(false);
          }
        })
      ],
      {
        title: localize(actualForm.tabCaption ?? '$CardTypes_CreateFileTempLink')
        //backgroundHolder: true
      },
      () => {
        createLinkEventHandlerDisposer?.();
        return Promise.resolve(true);
      },
      undefined,
      { autoSizeWidth: true, type: 'controls' }
    );

    if (!dialogResult) {
      return null;
    }

    if (data.get<boolean>(IsInfiniteName) === true) {
      return { validTo: null };
    }

    return {
      validTo: moment(data.get<string>(ValidToName, FieldType.DateTime))
    };
  }

  export async function tempLinkCreatedDialog(contentLink: string): Promise<void> {
    const createDialogResult = await createDialogForm(CardDialogName, LinkTabName);
    if (!createDialogResult) {
      return;
    }
    const [form, cardModel] = createDialogResult;
    const actualForm = form as FormViewModel;
    const data = cardModel.card.sections.tryGet(LinkSectionName)?.fields;
    const tbLink = cardModel.controls.get(LinkName) as TextBlockViewModel;
    const btnCopyToClipboard = cardModel.controls.get(CopyButtonName) as ButtonViewModel;
    if (!data || !tbLink || !btnCopyToClipboard) {
      return;
    }
    // set multiline wrapping
    tbLink.controlStyle.add(
      css => css`
        word-break: break-all;
      `
    );

    // set link
    tbLink.text = contentLink;
    btnCopyToClipboard.text = localize('$UI_Controls_FilesControl_CopyToClipboard');
    btnCopyToClipboard.onClick = () => ClipboardHelper.copyToClipboard(contentLink);

    await showDialog(
      form,
      null,
      undefined,
      {
        title: localize(actualForm.tabCaption ?? '$CardTypes_FileTempLink')
      },
      undefined,
      undefined,
      { autoSizeWidth: true, type: 'controls' }
    );
  }
}
