import { observable, runInAction } from 'mobx';
import { CancellationToken } from '@tessa/core';
import {
  IPreviewManager,
  IPreviewPrinter,
  PreviewerTypes,
  PreviewerViewModelBase
} from 'tessa/ui/preview';
import { IFileVersion } from 'tessa/files';
import Platform from 'common/platform';
import { group } from 'ui/toolbar/helpers';
import { UIButton } from 'tessa/ui';
import { getInitialToolbarItems } from './helper';

export class ExamplePreviewerViewModel extends PreviewerViewModelBase {
  //#region ctor

  constructor(
    previewManager: IPreviewManager,
    fileVersion: IFileVersion,
    previewPrinter: IPreviewPrinter
  ) {
    super(previewManager, fileVersion, previewPrinter);
  }

  //#endregion

  //#region fields

  @observable
  private _text = '';

  private _content: File | null = null;

  //#endregion

  //#region props

  override get type(): string {
    return PreviewerTypes.Txt;
  }

  get text(): string {
    return this._text;
  }

  //#endregion

  //#region methods

  protected override async initializeCore(): Promise<void> {
    this.initDefaultButtons();
    this.initToolbarProps();
  }

  private initDefaultButtons(): void {
    this._printButton.buttonAction = this.print;
  }

  private initToolbarProps(): void {
    const printFileGroup =
      !Platform.isMobile() && this.canPrintFile ? group('left-items', ['print-file']) : undefined;
    const groups = [
      { group: printFileGroup, panel: this.panel.controlPanel },
      { group: printFileGroup, panel: this.dialogPanel.controlPanel }
    ];

    // добавляем группы в тулбар конфиг
    this.initToolbarGroups(groups);

    const controlPanelButtons: UIButton[] = [];
    const dialogControlPanelButtons: UIButton[] = [];
    if (this.canPrintFile) {
      controlPanelButtons.push(this._printButton);
      dialogControlPanelButtons.push(this._printButton);
    }

    const controlPanelData = getInitialToolbarItems(controlPanelButtons);
    const dialogControlPanelData = getInitialToolbarItems(dialogControlPanelButtons);
    const toolbarItemData = [
      {
        items: controlPanelData.items,
        menuItems: controlPanelData.menuItems,
        panel: this.panel.controlPanel
      },
      {
        items: dialogControlPanelData.items,
        menuItems: dialogControlPanelData.menuItems,
        panel: this.dialogPanel.controlPanel
      }
    ];

    // добавляем items и menuItems в тулбар конфиг
    this.initToolbarItems(toolbarItemData);
  }

  protected override async loadCore(content: File, token: CancellationToken): Promise<void> {
    this._content = content;
    const text = await this._content.text();

    token.throwIfCancelled();

    runInAction(() => {
      this._text = text;
    });
  }

  print = (): void => {
    if (!this.canPrintFile || !this._content) {
      return;
    }

    this._previewPrinter.printFile(this._content, 'text/plain');
  };

  override reset(): void {
    runInAction(() => {
      this._text = '';
      this._content = null;
    });

    this._previewPrinter.reset();
  }

  //#endregion
}
