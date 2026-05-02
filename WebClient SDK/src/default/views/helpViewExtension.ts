import { IStorage } from '@tessa/core';
import { extension, localize } from '@tessa/application';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { IWorkplaceViewComponent } from 'tessa/ui/views';
import { ViewButtonViewModel, ContentPlaceArea, ContentPlaceOrder } from 'tessa/ui/views/content';
import { CardHelpMode } from 'ui/uiEnums';
import { IHelpSectionProcessor, IHelpSectionProcessor$ } from 'ui/helpSection';

//#region HelpViewExtension

@extension({ name: 'HelpViewExtension' })
export class HelpViewExtension extends WorkplaceViewComponentExtension {
  constructor(
    @IHelpSectionProcessor$() private readonly _helpSectionProcessor: IHelpSectionProcessor
  ) {
    super();
  }

  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Views.HelpViewExtension';
  }

  public initialize(model: IWorkplaceViewComponent): void {
    model.contentFactories.set(
      'HelpViewExtension',
      c =>
        new HelpViewButtonViewModel(
          this._helpSectionProcessor,
          new HelpViewExtensionSettings(this.settingsStorage),
          c
        )
    );
  }
}

//#endregion

//#region HelpViewExtensionSettings

class HelpViewExtensionSettings {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  constructor(storage: IStorage<any>) {
    this.helpMode =
      storage['HelpMode'] != null ? CardHelpMode[storage['HelpMode'] as string] : CardHelpMode.Url;
    this.value = storage['Value'] || '';
  }

  public readonly helpMode: CardHelpMode;
  public readonly value: string;
}

//#endregion

//#region HelpViewButtonViewModel

class HelpViewButtonViewModel extends ViewButtonViewModel {
  //#region ctor

  constructor(
    private readonly _helpSectionProcessor: IHelpSectionProcessor,
    settings: HelpViewExtensionSettings,
    viewComponent: IWorkplaceViewComponent,
    area: ContentPlaceArea = ContentPlaceArea.ToolBarPanel,
    order: number = ContentPlaceOrder.BeforeAll - 1
  ) {
    super(viewComponent, area, order);
    this.icon = 'icon-thin-228';
    this.type = 'small';
    this.theme = 'control';
    this.tooltip = localize('$UI_Common_Help');
    this.caption = localize('$UI_Common_Help');
    this.showCaption = false;
    this.onClick = () => this._helpSectionProcessor.process(settings.helpMode, settings.value);
    this._name = 'HelpViewButton';
  }

  //#endregion
}

//#endregion
