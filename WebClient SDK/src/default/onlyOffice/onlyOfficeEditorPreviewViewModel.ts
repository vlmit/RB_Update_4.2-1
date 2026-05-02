import { localize } from '@tessa/application';
import { IPreviewManager, IPreviewPrinter, PreviewerViewModelBase } from 'tessa/ui/preview';
import { IFileVersion } from 'tessa/files';
import { OnlyOfficeApi } from './onlyOfficeApi';
import { showNotEmpty } from 'tessa/ui';
import { ValidationError } from '@tessa/core';

export class OnlyOfficeEditorPreviewViewModel extends PreviewerViewModelBase {
  //#region ctor

  constructor(
    previewManager: IPreviewManager,
    fileVersion: IFileVersion,
    api: OnlyOfficeApi,
    previewPrinter: IPreviewPrinter
  ) {
    super(previewManager, fileVersion, previewPrinter);

    this._api = api;

    this.className.add('OnlyOfficeEditorPreviewViewModel');
  }

  //#endregion

  //#region fields

  private readonly _api: OnlyOfficeApi;

  private _editor: Record<string, unknown> | null = null;

  private _closeEditorCallback: (() => void) | null = null;

  //#endregion

  //#region props

  override get type(): string {
    return 'OnlyOffice';
  }

  //#endregion

  //#region methods

  protected override async loadCore(): Promise<void> {
    // OpenOffice сам загрузит контент
  }

  override reset(): void {
    if (this._editor) {
      if ('destroyEditor' in this._editor) {
        (this._editor['destroyEditor'] as () => void)();
      }
      this._editor = null;
    }

    if (this._closeEditorCallback) {
      this._closeEditorCallback();
      this._closeEditorCallback = null;
    }
  }

  setUpEditorSafety = async (placeholder: string): Promise<void> => {
    const version = this.fileVersion;

    try {
      OnlyOfficeApi.throwIfApiScriptIsNotLoaded(window);
      await this._api.openFile(
        version,
        async ({ id, accessToken }) =>
          await this.openEditorFunc(id, version, placeholder, accessToken),
        () => this.reset(),
        false,
        null,
        false,
        false
      );
    } catch (e) {
      console.error(e);
      this.previewManager.setMessage(localize('$UI_Controls_Preview_NotAvailable'));
    }
  };

  private async openEditorFunc(
    id: string,
    version: IFileVersion,
    placeholder: string,
    accessToken: string
  ): Promise<void> {
    const { config, validationResult } = await this._api.createDefaultDocEditorConfig(
      id,
      version,
      'preview',
      accessToken
    );

    if ((await showNotEmpty(validationResult)) && validationResult.hasErrors) {
      throw new ValidationError(validationResult);
    }

    this._editor = this._api.createDocEditorFrame(placeholder, config!);

    const closePromise = new Promise<void>(resolve => {
      this._closeEditorCallback = resolve;
    });
    await closePromise;
  }

  //#endregion
}
