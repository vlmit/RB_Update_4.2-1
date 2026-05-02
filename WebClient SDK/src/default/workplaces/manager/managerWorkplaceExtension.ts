import { ManagerWorkplaceViewModel } from './managerWorkplace';
import { ManagerWorkplaceSettings } from './managerWorkplaceSettings';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { IWorkplaceViewComponent, StandardViewComponentContentItemFactory } from 'tessa/ui/views';
import { extension } from '@tessa/application';

//#region ManagerWorkplaceExtension

@extension()
export class ManagerWorkplaceExtension extends WorkplaceViewComponentExtension {
  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Workplaces.Manager.ManagerWorkplaceExtension';
  }

  public initialize(model: IWorkplaceViewComponent): void {
    const settings = new ManagerWorkplaceSettings(this.settingsStorage);
    if (!settings.cardId) {
      console.error('ManagerWorkplaceExtension settings is not valid.');
      return;
    }

    model.contentFactories.set(
      StandardViewComponentContentItemFactory.Table,
      c => new ManagerWorkplaceViewModel(settings, c)
    );
  }
}

//#endregion
