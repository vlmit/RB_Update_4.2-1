import {
  IStorage,
  StorageHelper,
  TypedField,
  ValidationKey,
  ValidationResultType
} from '@tessa/core';
import { extension } from '@tessa/application';
import {
  CardGetFileContentExtension,
  CardGetFileContentResponse,
  ICardGetFileContentExtensionContext
} from '@tessa/platform';
import { DeskiManager } from 'tessa/deski';
import Platform from 'common/platform';
import { DeskiIgnoreGetContentExtensionKey } from 'tessa/files';

@extension({ name: 'DeskiFileContentExtension' })
export class DeskiFileContentExtension extends CardGetFileContentExtension {
  //#region CardGetFileContentExtension

  async beforeRequest(context: ICardGetFileContentExtensionContext): Promise<void> {
    if (!DeskiManager.instance.deskiAvailable || Platform.isMobile()) {
      return;
    }

    const request = context.request;

    if (this.isVirtualFile(context) || request.versionRowId == undefined) {
      return;
    }

    const info = request.tryGetInfo();
    if (info && !!StorageHelper.tryGet(info, DeskiIgnoreGetContentExtensionKey)) {
      delete info[DeskiIgnoreGetContentExtensionKey];
      return;
    }

    // если есть поле .deskiContentModified в info, значит запрос контента идет для редактируемого файла
    // .deskiContentModified содержит время редактирования текущего контента или null, если контент еще не редактировался
    const hasContentModified = !!info && '.deskiContentModified' in info;
    if (hasContentModified) {
      await this.requestForEditableFile(context, info!);
    } else {
      await this.requestForReadonlyFile(context);
    }
  }

  private isVirtualFile(context: ICardGetFileContentExtensionContext) {
    return context.request.fileTypeId == undefined;
  }

  private async requestForEditableFile(
    context: ICardGetFileContentExtensionContext,
    info: IStorage
  ) {
    const request = context.request;
    const contentModified = StorageHelper.tryGet<string>(info, '.deskiContentModified');
    const deskiOriginalVersionId = StorageHelper.tryGet<string>(info, '.deskiOriginalVersionId');
    const versionRowId = deskiOriginalVersionId ?? request.versionRowId!;
    delete info['.deskiContentModified'];
    delete info['.deskiOriginalVersionId'];

    const { info: fileInfo, result } = await DeskiManager.instance.getFileInfo(versionRowId, true);

    if (result) {
      context.validationResult.add(result);
      return;
    }

    // ошибку сохранили выше
    if (!fileInfo) {
      return;
    }

    // если контент не поменялся, то отдаем пустой респонс
    if (
      (contentModified &&
        new Date(contentModified).getTime() === new Date(fileInfo.Modified).getTime()) ||
      !fileInfo.IsModified
    ) {
      const fileContentResponse = new CardGetFileContentResponse();
      context.response = fileContentResponse;
      return;
    }

    const fileContentResult = await DeskiManager.instance.getFileData(versionRowId);
    if (!fileContentResult.data) {
      context.validationResult.add(
        ValidationKey.unknown,
        ValidationResultType.Error,
        `Can not find file content in deski. ID: ${versionRowId}`
      );
      return;
    }

    const fileContentResponse = new CardGetFileContentResponse();
    const content = fileContentResult.data;
    const fileName = fileInfo.Name || request.fileName;
    fileContentResponse.setContent(content, fileName);
    fileContentResponse.info['.deskiContentModified'] = TypedField.createDateTime(
      fileInfo.Modified
    );
    context.response = fileContentResponse;
  }

  private async requestForReadonlyFile(context: ICardGetFileContentExtensionContext) {
    const request = context.request;
    const versionRowId = request.versionRowId!;
    try {
      const { info: fileInfo } = await DeskiManager.instance.getFileInfo(versionRowId, false);

      // не нашли файл в deski
      if (!fileInfo) {
        return;
      }

      const fileContentResult = await DeskiManager.instance.getContent(versionRowId);
      if (!fileContentResult.data) {
        return;
      }

      const fileContentResponse = new CardGetFileContentResponse();
      const content = fileContentResult.data;
      const fileName = fileInfo.Name || request.fileName;
      fileContentResponse.setContent(content, fileName);
      context.response = fileContentResponse;
    } catch {}
  }

  //#endregion
}
