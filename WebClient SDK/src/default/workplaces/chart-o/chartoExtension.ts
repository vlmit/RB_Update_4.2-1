import { extension } from '@tessa/application';
import { ChartoViewViewModel } from './charto';
import { NoLicenseChartoViewViewModel } from './noLicenceCharto';
import { ChartoSettings } from './chartoSettings';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { IWorkplaceViewComponent, StandardViewComponentContentItemFactory } from 'tessa/ui/views';
import { ChartsId, EnterpriseId, LicenseManager } from 'tessa/platform/licensing';

//#region ChartoViewExtension

@extension()
export class ChartoViewExtension extends WorkplaceViewComponentExtension {
  override getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Workplaces.WebChart.WebChartWorkplaceExtension';
  }

  override initialize(model: IWorkplaceViewComponent): void {
    const settings = new ChartoSettings(this.settingsStorage);

    if (LicenseManager.instance.license.hasAnyModules(ChartsId, EnterpriseId)) {
      model.contentFactories.set(StandardViewComponentContentItemFactory.Paging, () => null);
      model.contentFactories.set(StandardViewComponentContentItemFactory.Table, c => {
        c.firstRowSelection = false;
        c.hideRowsCounter = true;
        return new ChartoViewViewModel(settings, c);
      });
    } else {
      model.contentFactories.set(
        StandardViewComponentContentItemFactory.Table,
        c => new NoLicenseChartoViewViewModel(c)
      );
    }
  }
}

//#endregion
