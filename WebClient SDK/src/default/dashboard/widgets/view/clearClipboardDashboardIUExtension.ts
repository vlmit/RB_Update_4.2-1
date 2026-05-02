import { extension, inject } from '@tessa/application';
import { DashboardUIExtension, IDashboardUIExtensionContext } from 'tessa/ui/dashboard';
import { IViewWidgetDashboardClipboard$ } from '../widgetInjects';
import { IViewWidgetDashboardClipboard } from './viewWidgetTypes';

@extension({ name: 'ClearClipboardDashboardIUExtension' })
export class ClearClipboardDashboardIUExtension extends DashboardUIExtension {
  //#region ctor

  constructor(
    @inject(IViewWidgetDashboardClipboard$)
    private readonly _clipboard: IViewWidgetDashboardClipboard
  ) {
    super();
  }

  //#endregion

  //#region overrides

  override async initialized(context: IDashboardUIExtensionContext): Promise<void> {
    this.disposeList.add(
      context.dashboard.editModeChanged.add(args => {
        if (!args.editMode) {
          this._clipboard.removeSettings();
        }
      })
    );
  }

  //#endregion
}
