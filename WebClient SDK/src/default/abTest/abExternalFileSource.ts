import { saveAs } from 'file-saver';
import { AbExternalFileCreationToken } from './abExternalFileCreationToken';
import { AbExternalFile } from './abExternalFile';
import { FileSource, IFileCreationToken, IFile, IFileVersion, FileVersion } from 'tessa/files';
import { merge } from 'tessa/platform/storage/storageHelper';
import { Guid, Result, ResultWithInfo } from 'tessa/platform';
import { IStorage } from 'tessa/platform/storage';
import {
  ValidationResult,
  ValidationResultBuilder,
  ValidationResultType
} from 'tessa/platform/validation';

export class AbExternalFileSource extends FileSource {
  protected createFileCore(token: IFileCreationToken): IFile {
    // здесь возможно исключение, связанное с параметрами метода
    if (!(token instanceof AbExternalFileCreationToken)) {
      throw new Error('Unexpected token type.');
    }

    // все свойства токена проверяются в конструкторе
    const file = new AbExternalFile(
      token.id || Guid.newGuid(),
      token.name,
      token.category,
      token.type,
      this,
      token.permissions.clone(),
      token.modified,
      token.modifiedById,
      token.modifiedByName,
      token.created,
      token.createdById,
      token.createdByName,
      token.isLocal,
      null,
      token.description
    );

    if (token.options && Object.keys(token.options).length > 0) {
      merge(token.options, file.options);
    }

    if (token.info && Object.keys(token.info).length > 0) {
      merge(token.info, file.info);
    }

    if (token.requestInfo && Object.keys(token.requestInfo).length > 0) {
      merge(token.requestInfo, file.requestInfo);
    }

    return file;
  }

  protected getFileCreationTokenCore(): IFileCreationToken {
    return new AbExternalFileCreationToken();
  }

  protected async getContentCore(
    fileOrFileVersion: IFile | IFileVersion,
    _info?: IStorage
  ): Promise<ResultWithInfo<File>> {
    const file =
      fileOrFileVersion instanceof FileVersion ? fileOrFileVersion.file : fileOrFileVersion;

    const builder = new ValidationResultBuilder();
    if (!(file instanceof AbExternalFile)) {
      builder.add(ValidationResult.fromText('Unexpected file type.', ValidationResultType.Error));
    }
    const description = (file as AbExternalFile).description;

    return {
      data: new File([description], fileOrFileVersion.name),
      info: {},
      validationResult: builder.build()
    };
  }

  protected async saveContentCore(
    fileOrFileVersion: IFile | IFileVersion,
    info?: IStorage
  ): Promise<Result<boolean>> {
    const result = await this.getContentCore(fileOrFileVersion, info);
    if (!result.data) {
      return {
        data: false,
        validationResult: result.validationResult
      };
    }

    await saveAs(result.data, fileOrFileVersion.name);

    return {
      data: true,
      validationResult: result.validationResult
    };
  }
}
