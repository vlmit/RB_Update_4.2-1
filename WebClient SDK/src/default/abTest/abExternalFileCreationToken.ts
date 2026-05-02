import { AbExternalFile } from './abExternalFile';
import { FileCreationToken, IFile } from 'tessa/files';

export class AbExternalFileCreationToken extends FileCreationToken {
  public description: string | Blob;

  public set(file: IFile): void {
    super.set(file);

    this.description = file instanceof AbExternalFile ? file.description : '';
  }
}
