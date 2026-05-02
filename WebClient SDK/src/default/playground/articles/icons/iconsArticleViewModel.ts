import { IconSelectorViewModelBase } from 'tessa/ui/iconSelector/chunk';
import type { IconModel, IconSelectorSettings } from 'tessa/ui/iconSelector';

export class IconArticleViewModel extends IconSelectorViewModelBase {
  //#region props

  get selectedIcon(): IconModel | undefined {
    return this.selectedIcons.find(Boolean);
  }

  //#endregion

  //#region methods

  override async initialize(settings?: IconSelectorSettings): Promise<void> {
    this._useSearchBox = settings?.useSearchBox ?? false;
    this._showIconCaption = settings?.showIconCaption ?? false;

    await super.initialize();
  }

  //#endregion
}
